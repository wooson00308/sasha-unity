using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq; // Needed for Sum(), Min(), FirstOrDefault()
using System.Collections.Generic; // Needed for IEnumerable
using System; // Needed for Func
using AF.Combat;
using AF.Models;
using AF.Services; // For ServiceLocator if needed
using AF.EventBus; // Added for EventBus
using Cysharp.Threading.Tasks; // Added for UniTask
using System.Threading; // Added for CancellationToken

namespace AF.Combat.Agents
{
    /// <summary>
    /// ML-Agents 에이전트 스크립트. 파일럿의 관측, 행동 결정, 보상 등을 담당.
    /// </summary>
    public class PilotAgent : Agent
    {
        // --- Component References ---
        private ArmoredFrame _frame;
        private ICombatSimulatorService _combatSimulator;
        private EventBus.EventBus _eventBus; // Added EventBus reference
        // private ICombatActionExecutor _actionExecutor; // Potentially needed later

        // --- Constants ---
        // 관측 벡터 크기: 2(자신) + 4(적) + 4(아군) + 1(교전가능) + 1(취약성) = 12
        private const int OBSERVATION_SIZE = 12;
        // 관측 최대 거리 (정규화용, 필요에 따라 조정)
        private const float MAX_OBSERVATION_DISTANCE = 100f;

        // Rewards - Define constants for better management
        private const float REWARD_DAMAGE_DEALT_MULTIPLIER = 0.01f;
        private const float REWARD_PART_DESTROYED = 0.2f;
        private const float REWARD_ENEMY_KILL = 1.0f;
        private const float REWARD_DAMAGE_TAKEN_MULTIPLIER = -0.01f;
        private const float REWARD_REPAIR_MULTIPLIER = 0.005f;
        private const float REWARD_DODGE = 0.1f;
        private const float REWARD_MISS = -0.05f;
        private const float REWARD_INEFFICIENT_ACTION = -0.1f;
        private const float REWARD_WIN = 2.0f;
        private const float REWARD_LOSE = -2.0f;
        private const float REWARD_DRAW = -1.0f;
        // Small negative reward per step/action to encourage efficiency
        private const float REWARD_STEP = -0.001f;

        // Async Decision Making
        private UniTaskCompletionSource<(CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)> _decisionCompletionSource;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        // --- Initialization ---

        /// <summary>
        /// Unity Awake: 컴포넌트 초기화. 여기서는 특별한 작업 없음.
        /// </summary>
        void Awake()
        {
            // GetComponent<ArmoredFrame>() 호출 제거 - 외부에서 SetArmoredFrameReference로 설정
        }

        /// <summary>
        /// 외부에서 ArmoredFrame POCO 참조를 설정합니다.
        /// </summary>
        public void SetArmoredFrameReference(ArmoredFrame frame)
        {
            this._frame = frame;
            gameObject.name = $"AgentHost_{_frame?.Pilot?.Name ?? "Unknown"}_{_frame?.Name ?? "Frame"}";
        }

        /// <summary>
        /// Unity Start: 다른 서비스나 객체 참조 가져오기 (Awake 이후 실행 보장).
        /// </summary>
        void Start()
        {
            _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus; // Get EventBus instance
            if (_combatSimulator == null)
            {
                 Debug.LogError("PilotAgent could not find ICombatSimulatorService.", this);
            }
        }

        /// <summary>
        /// ML-Agents 초기화: 에이전트 관련 설정 (필요 시 사용).
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// 에피소드 시작 시 호출 (예: 전투 시작). 상태 초기화.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();
            Debug.Log($"{_frame?.Name ?? "Agent"} starting new episode.");

            // Subscribe to events
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events when the agent is destroyed
            _cancellationTokenRegistration.Dispose(); // Dispose cancellation registration
            UnsubscribeFromEvents();
        }

        // --- Observation ---

        /// <summary>
        /// 에이전트의 상태 관측. VectorSensor에 데이터를 추가.
        /// </summary>
        /// <param name="sensor">관측 데이터를 추가할 센서.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (_frame == null || _combatSimulator == null || !_frame.IsOperational)
            {
                sensor.AddObservation(new float[OBSERVATION_SIZE]);
                Debug.LogWarning($"PilotAgent {gameObject?.name ?? "Unknown"}: Invalid state. Adding zero observations.");
                return;
            }

            sensor.AddObservation(GetDurabilityRatio(_frame));
            sensor.AddObservation(_frame.CombinedStats.MaxAP > 0 ? _frame.CurrentAP / _frame.CombinedStats.MaxAP : 0f);
            ArmoredFrame closestEnemy = FindClosestUnit(_combatSimulator.GetEnemies(_frame));
            AddUnitObservations(sensor, closestEnemy, _frame.Position);
            ArmoredFrame closestDamagedAlly = FindClosestUnit(
                _combatSimulator.GetAllies(_frame),
                unit => unit != _frame && GetDurabilityRatio(unit) < 1.0f
            );
            AddUnitObservations(sensor, closestDamagedAlly, _frame.Position);
            float engagementRatio = 0f;
            if (closestEnemy != null && closestEnemy.IsOperational)
            {
                float distanceToEnemy = Vector3.Distance(_frame.Position, closestEnemy.Position);
                var usableWeapons = _frame.GetAllWeapons()
                                        .Where(w => w != null && w.HasAmmo() && !w.IsReloading)
                                        .ToList();
                if (usableWeapons.Count > 0)
                {
                    int inRangeCount = usableWeapons.Count(w => w.Range >= distanceToEnemy);
                    engagementRatio = (float)inRangeCount / usableWeapons.Count;
                }
            }
            sensor.AddObservation(engagementRatio);
            float lowestPartDurabilityRatio = 1f;
            var operationalParts = _frame.Parts.Values.Where(p => p != null && p.IsOperational).ToList();
            if (operationalParts.Count > 0)
            {
                lowestPartDurabilityRatio = operationalParts.Min(p => {
                    float maxPartDur = p.PartStats.Durability;
                    return maxPartDur > 0 ? Mathf.Clamp01(p.CurrentDurability / maxPartDur) : 1f;
                });
            }
            sensor.AddObservation(lowestPartDurabilityRatio);
        }

        /// <summary>
        /// 특정 유닛 그룹에서 가장 가까운 유닛을 찾습니다.
        /// </summary>
        /// <param name="units">검색 대상 유닛 리스트</param>
        /// <param name="filter">추가 필터링 조건 (Optional)</param>
        /// <returns>가장 가까운 유닛 또는 null</returns>
        private ArmoredFrame FindClosestUnit(IEnumerable<ArmoredFrame> units, Func<ArmoredFrame, bool> filter = null)
        {
            ArmoredFrame closestUnit = null;
            float minDistanceSq = float.MaxValue;
            var query = units.Where(u => u != null && u.IsOperational);
            if (filter != null) { query = query.Where(filter); }
            foreach (var unit in query)
            {
                float distanceSq = (unit.Position - _frame.Position).sqrMagnitude;
                if (distanceSq < minDistanceSq) { minDistanceSq = distanceSq; closestUnit = unit; }
            }
            return closestUnit;
        }

        /// <summary>
        /// 특정 유닛에 대한 관측 데이터를 센서에 추가합니다. 유닛이 null이면 기본값을 추가합니다.
        /// </summary>
        /// <param name="sensor">관측 데이터를 추가할 센서</param>
        /// <param name="unit">관측 대상 유닛 (null 가능)</param>
        private void AddUnitObservations(VectorSensor sensor, ArmoredFrame unit, Vector3 selfPosition)
        {
            if (unit != null && unit.IsOperational)
            {
                float distance = Vector3.Distance(selfPosition, unit.Position);
                sensor.AddObservation(Mathf.Clamp01(distance / MAX_OBSERVATION_DISTANCE));
                Vector3 direction = (unit.Position - selfPosition).normalized;
                sensor.AddObservation(direction.x);
                sensor.AddObservation(direction.z);
                sensor.AddObservation(GetDurabilityRatio(unit));
            }
            else
            {
                sensor.AddObservation(1f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        /// <summary>
        /// ArmoredFrame의 현재 총 내구도 비율을 계산합니다.
        /// </summary>
        /// <param name="frame">대상 프레임</param>
        /// <returns>0.0 ~ 1.0 사이의 내구도 비율</returns>
        private float GetDurabilityRatio(ArmoredFrame unit)
        {
            if (unit == null) return 0f;
            float maxDurability = unit.CombinedStats.Durability;
            if (maxDurability <= 0f) return 0f;
            float currentDurabilitySum = 0f;
            float maxDurabilitySum = 0f;
            var allParts = unit.Parts.Values;
            if (allParts.Any())
            {
                currentDurabilitySum = allParts.Sum(p => p.CurrentDurability);
                maxDurabilitySum = allParts.Sum(p => p.MaxDurability);
            }
            return maxDurabilitySum > 0 ? currentDurabilitySum / maxDurabilitySum : 0f;
        }

        // --- Action & Heuristics ---

        /// <summary>
        /// 비동기적으로 에이전트의 행동 결정을 요청하고 결과를 기다립니다.
        /// </summary>
        /// <param name="cancellationToken">작업 취소 토큰</param>
        /// <returns>결정된 행동 데이터 (튜플)</returns>
        public async UniTask<(CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)> RequestDecisionAsync(CancellationToken cancellationToken = default)
        {
            // 이전 요청이 아직 처리 중이면 취소 또는 경고
            if (_decisionCompletionSource != null && !_decisionCompletionSource.Task.Status.IsCompleted())
            {
                Debug.LogWarning($"[{_frame?.Name ?? "Agent"}] Previous decision request was still pending. Cancelling it.");
                _decisionCompletionSource.TrySetCanceled();
                _cancellationTokenRegistration.Dispose();
            }

            _decisionCompletionSource = new UniTaskCompletionSource<(CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)>();
            // CancellationToken 등록
            _cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                _decisionCompletionSource?.TrySetCanceled(cancellationToken);
            });

            RequestDecision(); // ML-Agents에게 결정 요청 (동기 호출)
            return await _decisionCompletionSource.Task; // OnActionReceived에서 결과 설정될 때까지 비동기 대기
        }

        /// <summary>
        /// 정책(또는 Heuristic)으로부터 행동 결정을 받아 처리.
        /// </summary>
        /// <param name="actions">결정된 행동 버퍼.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
             if (_frame == null || _combatSimulator == null || !_frame.IsOperational || !_combatSimulator.IsInCombat || _combatSimulator.CurrentActiveUnit != _frame)
            {
                // 행동을 수행할 수 없는 상태면 무시
                return;
            }

            AddReward(REWARD_STEP); // Apply step penalty

            // 이산 행동(Discrete Actions) 처리 예시
            int actionIndex = actions.DiscreteActions[0]; // 첫 번째 이산 행동 분기 사용 가정

            // ActionType Enum의 범위를 벗어나는 인덱스 방지
            if (!System.Enum.IsDefined(typeof(CombatActionEvents.ActionType), actionIndex))
            {
                 Debug.LogWarning($"{_frame.Name} received invalid action index: {actionIndex}. Defaulting to None.");
                 actionIndex = (int)CombatActionEvents.ActionType.None;
            }
            CombatActionEvents.ActionType chosenActionType = (CombatActionEvents.ActionType)actionIndex;

            // --- 행동 실행 로직 ---
            ArmoredFrame targetFrame = null;
            Vector3? targetPosition = null;
            Weapon weapon = null;
            bool actionExecuted = false;

            // 대상 찾기 (공통적으로 필요한 경우 미리 찾아둠)
            ArmoredFrame closestEnemy = FindClosestUnit(_combatSimulator.GetEnemies(_frame));
            ArmoredFrame closestDamagedAlly = FindClosestUnit(
                _combatSimulator.GetAllies(_frame),
                unit => unit != _frame && GetDurabilityRatio(unit) < 1.0f
            );

            switch (chosenActionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    targetFrame = closestEnemy;
                    if (targetFrame != null && targetFrame.IsOperational)
                    {
                        weapon = _frame.GetAllWeapons()
                                       .FirstOrDefault(w => w != null && w.HasAmmo() && !w.IsReloading);
                        if (weapon != null)
                        {
                            actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, targetFrame, null, weapon);
                        }
                    }
                    break;

                case CombatActionEvents.ActionType.Move:
                    if (closestEnemy != null && closestEnemy.IsOperational)
                    {
                        targetPosition = closestEnemy.Position;
                        actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, null, targetPosition, null);
                    }
                    break;

                case CombatActionEvents.ActionType.Defend:
                    actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, null, null, null);
                    break;

                case CombatActionEvents.ActionType.Reload:
                    weapon = _frame.GetAllWeapons()
                                   .FirstOrDefault(w => w != null && !w.HasAmmo() && !w.IsReloading && w.ReloadTurns > 0);
                    if (weapon != null)
                    {
                        actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, null, null, weapon);
                    }
                    break;

                case CombatActionEvents.ActionType.RepairAlly:
                    targetFrame = closestDamagedAlly;
                    if (targetFrame != null && targetFrame.IsOperational)
                    {
                        actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, targetFrame, null, null);
                    }
                    break;

                case CombatActionEvents.ActionType.RepairSelf:
                    actionExecuted = _combatSimulator.PerformAction(_frame, chosenActionType, _frame, null, null); // 대상은 자신
                    break;

                case CombatActionEvents.ActionType.None:
                default:
                    actionExecuted = true; // 행동한 것으로 간주 (턴 넘김 목적)
                    break;
            }

            // 결정된 행동 데이터를 _decisionCompletionSource를 통해 전달
            if (_decisionCompletionSource != null && !_decisionCompletionSource.Task.Status.IsCompleted())
            {
                var actionData = (chosenActionType, targetFrame, targetPosition, weapon);
                _decisionCompletionSource.TrySetResult(actionData);
            }
            else
            {
                // 이미 완료되었거나 없는 경우 (예: 타임아웃 또는 취소 후 OnActionReceived가 늦게 호출됨)
                Debug.LogWarning($"[{_frame?.Name ?? "Agent"}] OnActionReceived called but no pending decision request found or it was already completed/cancelled.");
            }
             // 완료 후 리소스 정리
            _cancellationTokenRegistration.Dispose();
            _decisionCompletionSource = null;

            // --- 보상 설정 (예시) ---
            if (!actionExecuted && chosenActionType != CombatActionEvents.ActionType.None)
            {
                 AddReward(-0.01f);
            }
            else if (chosenActionType != CombatActionEvents.ActionType.None)
            {
                 AddReward(-0.001f);
            }
            // TODO: 실제 전투 결과(데미지, 파괴, 회피, 수리 성공 여부 등)에 따른 보상은
            //       CombatActionEvents, DamageEvents 등의 이벤트를 구독하여 처리해야 함.
        }

        /// <summary>
        /// 휴리스틱 모드: 플레이어 입력을 받아 행동 결정 (테스트용).
        /// 이 게임에서는 사용되지 않음.
        /// </summary>
        /// <param name="actionsOut">설정할 행동 버퍼.</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // 비워둠 - 이 게임에서는 휴리스틱(플레이어 직접 제어)을 사용하지 않음.
        }

        // --- Reward ---
        // AddReward(), SetReward() 등은 OnActionReceived 또는 다른 이벤트 핸들러 내에서 호출될 것임.
        // TODO: 이벤트 버스를 구독하여 보상을 설정하는 로직 추가

        #region Event Handling & Rewards

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Subscribe<CombatActionEvents.RepairAppliedEvent>(HandleRepairApplied);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Subscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);
            _eventBus.Subscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            // Check if the service locator still provides the service before unsubscribing
             if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<EventBusService>())
             {
                 _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
                 _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
                 _eventBus.Unsubscribe<CombatActionEvents.RepairAppliedEvent>(HandleRepairApplied);
                 _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
                 _eventBus.Unsubscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);
                 _eventBus.Unsubscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);
             }
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
            // Penalize inefficient actions (e.g., repairing a full health ally/self)
            if (ev.Actor == _frame && ev.Success)
            {
                if ((ev.Action == CombatActionEvents.ActionType.RepairAlly || ev.Action == CombatActionEvents.ActionType.RepairSelf))
                {
                    // Need access to RepairAppliedEvent data or check target health directly *before* action
                    // This requires more complex state tracking or event data adjustment.
                    // Simple check for now: if *no* RepairApplied event was triggered for this action, penalize.
                    // (This assumes RepairApplied is only sent if actual repair happens)
                    // TODO: Improve this check - maybe check target health in ActionStartEvent?
                }
                else if (ev.Action == CombatActionEvents.ActionType.Reload)
                {
                    // Penalize reloading a full weapon
                    // Need weapon state *before* reload.
                    // TODO: Improve this check
                }
                 else if (ev.Action == CombatActionEvents.ActionType.None)
                 {
                     // Penalize doing nothing? Maybe already handled by step penalty.
                 }
            }
        }

        private void HandleDamageApplied(DamageEvents.DamageAppliedEvent ev)
        {
            if (ev.Target == _frame) // We took damage
            {
                AddReward(ev.DamageDealt * REWARD_DAMAGE_TAKEN_MULTIPLIER);
            }
            else if (ev.Source == _frame) // We dealt damage
            {
                AddReward(ev.DamageDealt * REWARD_DAMAGE_DEALT_MULTIPLIER);

                // Check if the part was destroyed
                if (ev.PartCurrentDurability <= 0)
                {
                    AddReward(REWARD_PART_DESTROYED);
                }

                // Check if the target unit was defeated
                if (_combatSimulator != null && _combatSimulator.IsUnitDefeated(ev.Target))
                {
                    AddReward(REWARD_ENEMY_KILL);
                }
            }
        }

        private void HandleRepairApplied(CombatActionEvents.RepairAppliedEvent ev)
        {
            if (ev.Actor == _frame && ev.AmountRepaired > 0) // We performed a successful repair
            {
                AddReward(ev.AmountRepaired * REWARD_REPAIR_MULTIPLIER);
            }
             // Consider penalty if ev.AmountRepaired == 0 (means target was likely full health)?
             else if (ev.Actor == _frame && ev.AmountRepaired <= 0)
             {
                 // Check if the action was RepairSelf or RepairAlly
                 // This event might fire even if 0 repair happens if the action succeeded.
                 // We need to know if the *intent* was repair. Check ActionCompletedEvent instead?
                 // Or add ActionType to RepairAppliedEvent? --> Already added! Use ev.ActionType
                 if(ev.ActionType == CombatActionEvents.ActionType.RepairAlly || ev.ActionType == CombatActionEvents.ActionType.RepairSelf)
                 {
                     AddReward(REWARD_INEFFICIENT_ACTION); // Penalize useless repair attempt
                 }
             }
        }

         private void HandleWeaponFired(CombatActionEvents.WeaponFiredEvent ev)
         {
             if (ev.Attacker == _frame && !ev.Hit) // We fired and missed
             {
                 AddReward(REWARD_MISS);
             }
         }


        private void HandleDamageAvoided(DamageEvents.DamageAvoidedEvent ev)
        {
            if (ev.Target == _frame && ev.Type == DamageEvents.DamageAvoidedEvent.AvoidanceType.Dodge) // We dodged an attack
            {
                AddReward(REWARD_DODGE);
            }
        }

        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            // Determine if our team won or lost
            bool playerTeamWon = false;
            bool enemyTeamWon = false;

            var playerTeamIds = _combatSimulator.GetAllies(_frame).Select(u => u.TeamId).Distinct().ToList();
             if(!playerTeamIds.Contains(_frame.TeamId)) playerTeamIds.Add(_frame.TeamId); // Include self team

            var enemyTeamIds = _combatSimulator.GetEnemies(_frame).Select(u => u.TeamId).Distinct().ToList();

            bool playerUnitsRemain = ev.Survivors.Any(s => playerTeamIds.Contains(s.TeamId));
            bool enemyUnitsRemain = ev.Survivors.Any(s => enemyTeamIds.Contains(s.TeamId));

            if (playerUnitsRemain && !enemyUnitsRemain)
            {
                playerTeamWon = true;
            }
            else if (!playerUnitsRemain && enemyUnitsRemain)
            {
                enemyTeamWon = true;
            }
            // Draw if both or neither remain (and result isn't Aborted)

            if (ev.Result == CombatSessionEvents.CombatEndEvent.ResultType.Victory && playerTeamWon)
            {
                AddReward(REWARD_WIN);
            }
            else if (ev.Result == CombatSessionEvents.CombatEndEvent.ResultType.Defeat && enemyTeamWon)
            {
                AddReward(REWARD_LOSE);
            }
            else if (ev.Result == CombatSessionEvents.CombatEndEvent.ResultType.Draw)
            {
                AddReward(REWARD_DRAW);
            }
            else if (ev.Result == CombatSessionEvents.CombatEndEvent.ResultType.Aborted)
            {
                 // No specific reward/penalty for aborted combat unless needed
            }
            else // Mismatch between result and survivor check (shouldn't happen ideally)
            {
                 Debug.LogWarning("CombatEndEvent result mismatch with survivor check.");
                 // Assign reward based on survivor check as fallback
                 if(playerTeamWon) AddReward(REWARD_WIN * 0.5f); // Reduced reward for uncertainty
                 else if(enemyTeamWon) AddReward(REWARD_LOSE * 0.5f);
                 else AddReward(REWARD_DRAW);
            }


            // Ensure episode ends after processing combat end reward
            EndEpisode();

            // Unsubscribe after episode ends? Or let OnDestroy handle it?
            // UnsubscribeFromEvents(); // Let OnDestroy handle it for now
        }

        #endregion

        #region Helper Methods

        // Remove duplicate definitions below. Keep the ones defined earlier in the file.
        /*
        private ArmoredFrame FindClosestUnit(List<ArmoredFrame> units, System.Func<ArmoredFrame, bool> filter = null)
        {
            // ... Implementation ...
        }

        private void AddUnitObservations(VectorSensor sensor, ArmoredFrame unit, Vector3 selfPosition)
        {
            // ... Implementation ...
        }

        private float GetDurabilityRatio(ArmoredFrame unit)
        {
            // ... Implementation ...
        }
        */

        #endregion
    }
} 