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
        // private ICombatActionExecutor _actionExecutor; // Potentially needed later

        // --- Constants ---
        // 관측 벡터 크기: 2(자신) + 4(적) + 4(아군) + 1(교전가능) + 1(취약성) = 12
        private const int OBSERVATION_SIZE = 12;
        // 관측 최대 거리 (정규화용, 필요에 따라 조정)
        private const float MAX_OBSERVATION_DISTANCE = 100f;

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
            _frame = frame;
            if (_frame != null)
            {
                gameObject.name = _frame.Name + "_PilotAgent"; // 게임오브젝트 이름 설정 예시
            }
        }

        /// <summary>
        /// Unity Start: 다른 서비스나 객체 참조 가져오기 (Awake 이후 실행 보장).
        /// </summary>
        void Start()
        {
            _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
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
            AddUnitObservations(sensor, closestEnemy);
            ArmoredFrame closestDamagedAlly = FindClosestUnit(
                _combatSimulator.GetAllies(_frame),
                unit => unit.IsOperational && GetDurabilityRatio(unit) < 1.0f
            );
            AddUnitObservations(sensor, closestDamagedAlly);
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
        private void AddUnitObservations(VectorSensor sensor, ArmoredFrame unit)
        {
            if (unit != null && unit.IsOperational)
            {
                float distance = Vector3.Distance(_frame.Position, unit.Position);
                sensor.AddObservation(Mathf.Clamp01(distance / MAX_OBSERVATION_DISTANCE));
                Vector3 direction = (unit.Position - _frame.Position).normalized;
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
        private float GetDurabilityRatio(ArmoredFrame frame)
        {
            if (frame == null) return 0f;
            float maxDurability = frame.CombinedStats.Durability;
            if (maxDurability <= 0f) return 0f;
            float currentTotalDurability = frame.Parts.Values
                                                .Where(p => p != null && p.IsOperational)
                                                .Sum(p => p.CurrentDurability);
            return Mathf.Clamp01(currentTotalDurability / maxDurability);
        }

        // --- Action & Heuristics ---

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
                unit => unit.IsOperational && GetDurabilityRatio(unit) < 1.0f
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
    }
} 