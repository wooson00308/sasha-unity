using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
// using AF.Combat.Behaviors;  // 기존 파일럿 전략 네임스페이스 주석 처리
using AF.AI.BehaviorTree;      // 새로운 행동 트리 네임스페이스 추가
using AF.AI.BehaviorTree.PilotBTs; // BasicAttackBT 사용을 위해 추가
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// 전투 시뮬레이션 서비스 (인터페이스 구현 + 오케스트레이션 전담)
    /// </summary>
    public sealed class CombatSimulatorService : ICombatSimulatorService
    {
        // ──────────────────────────────────────────────────────────────
        // 외부 서브-서비스 (DI 대신 직접 new)
        // ──────────────────────────────────────────────────────────────
        private ICombatActionExecutor  _actionExecutor;
        private IStatusEffectProcessor _statusProcessor;
        private IBattleResultEvaluator _resultEvaluator;

        // ──────────────────────────────────────────────────────────────
        // Unity/서비스 참조
        // ──────────────────────────────────────────────────────────────
        private EventBus.EventBus _eventBus;
        private TextLoggerService _textLogger;

        // ──────────────────────────────────────────────────────────────
        // 전투 상태
        // ──────────────────────────────────────────────────────────────
        private string _currentBattleId;
        private string _battleName;
        private bool   _isInCombat;
        private int    _currentTurn;  // Renamed from _currentCycle (Represents the overall turn/round)
        private int    _currentCycle; // Added (Represents the activation step within a turn)
        private float  _combatStartTime;

        private List<ArmoredFrame>                    _participants;
        private Dictionary<ArmoredFrame,int>          _teamAssignments;
        private ArmoredFrame                          _currentActiveUnit;
        private HashSet<ArmoredFrame>                 _defendedThisTurn;
        private HashSet<ArmoredFrame>                 _actedThisCycle; // Represents units acted *this turn*
        private HashSet<ArmoredFrame>                 _movedThisActivation;

        // 파일럿 전문화 → 전략
        // private Dictionary<SpecializationType, IPilotBehaviorStrategy> _behaviorStrategies; // 주석 처리

        // ──────────────────────────────────────────────────────────────
        // 인터페이스 프로퍼티
        // ──────────────────────────────────────────────────────────────
        public string       CurrentBattleId   => _currentBattleId;
        public bool         IsInCombat        => _isInCombat;
        public int          CurrentTurn       => _currentTurn; // Renamed from CurrentCycle
        public int          CurrentCycle      => _currentCycle; // <<< 구현 추가
        public ArmoredFrame CurrentActiveUnit => _currentActiveUnit;
        public HashSet<ArmoredFrame> MovedThisActivation => _movedThisActivation;

        // ──────────────────────────────────────────────────────────────
        // IService 기본
        // ──────────────────────────────────────────────────────────────
        public void Initialize()
        {
            // 서비스 참조
            _eventBus   = ServiceLocator.Instance.GetService<EventBusService>().Bus;
            _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

            // 서브-서비스
            _actionExecutor  = new CombatActionExecutor();
            _statusProcessor = new StatusEffectProcessor();
            _resultEvaluator = new BattleResultEvaluator();

            // 전략 매핑 주석 처리
            /*
            _behaviorStrategies = new Dictionary<SpecializationType, IPilotBehaviorStrategy>
            {
                { SpecializationType.MeleeCombat,    new MeleeCombatBehaviorStrategy()    },
                { SpecializationType.RangedCombat,   new RangedCombatBehaviorStrategy()   },
                { SpecializationType.Defense,        new DefenseCombatBehaviorStrategy()  },
                { SpecializationType.Support,        new SupportCombatBehaviorStrategy()  },
                { SpecializationType.StandardCombat, new StandardCombatBehaviorStrategy() }
            };
            */

            // 상태 초기화
            _participants      = new List<ArmoredFrame>();
            _teamAssignments   = new Dictionary<ArmoredFrame,int>();
            _defendedThisTurn  = new HashSet<ArmoredFrame>();
            _actedThisCycle    = new HashSet<ArmoredFrame>();
            _movedThisActivation = new HashSet<ArmoredFrame>();
            _isInCombat        = false;
            _currentTurn       = 0; // Initialize turn
            _currentCycle      = 0; // Initialize cycle
            _currentBattleId   = null;
        }

        public void Shutdown()
        {
            if (_isInCombat) EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Aborted);

            _eventBus         = null;
            _textLogger       = null;
            _participants     = null;
            _teamAssignments  = null;
            _defendedThisTurn = null;
            _actedThisCycle   = null;
            _movedThisActivation = null;
            _isInCombat        = false;
            _currentTurn       = 0; // Reset turn
            _currentCycle      = 0; // Reset cycle
            _currentBattleId   = null;
        }

        // ──────────────────────────────────────────────────────────────
        // 전투 시작/종료
        // ──────────────────────────────────────────────────────────────
        public string StartCombat(ArmoredFrame[] participants, string battleName, bool autoProcess = false)
        {
            if (_isInCombat) EndCombat();

            _isInCombat       = true;
            _currentTurn      = 0; 
            _currentCycle     = 0; 
            _battleName       = battleName;
            _currentBattleId  = $"BATTLE_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000,9999)}";
            _combatStartTime  = Time.time;

            _participants     = participants.ToList();
            AssignTeams(participants);

            // +++ 각 참가자에게 행동 트리 할당 +++
            foreach (var unit in _participants)
            {
                if (unit != null)
                {
                    // 기본적으로 BasicAttackBT를 할당. 추후 파일럿 특성 등에 따라 다른 BT 할당 가능
                    unit.BehaviorTreeRoot = BasicAttackBT.Create();
                    unit.AICtxBlackboard.ClearAllData(); // 새 전투 시작 시 블랙보드 초기화
                }
            }
            // +++ 행동 트리 할당 끝 +++

            _defendedThisTurn.Clear();
            _actedThisCycle.Clear();
            _movedThisActivation.Clear();

            _eventBus.Publish(new CombatSessionEvents.CombatStartEvent(
                participants, _currentBattleId, battleName, Vector3.zero));

            return _currentBattleId;
        }

        public void EndCombat(CombatSessionEvents.CombatEndEvent.ResultType? forceResult = null)
        {
            if (!_isInCombat) return;

            var result = forceResult ?? _resultEvaluator.Evaluate(_participants, _teamAssignments);
            float dur  = Time.time - _combatStartTime;
            var survivors = _participants.Where(p => p.IsOperational).ToArray();

            // --- Create Final Snapshot ---
            var finalSnapshot = new Dictionary<string, ArmoredFrameSnapshot>();
            if (_participants != null) // Check if participant list exists
            {
                foreach (var unit in _participants)
                {
                    if (unit != null && !string.IsNullOrEmpty(unit.Name))
                    {
                        finalSnapshot[unit.Name] = new ArmoredFrameSnapshot(unit);
                    }
                }
            }
            // --- Snapshot Creation End ---

            // --- Publish Event with Snapshot ---
            _eventBus.Publish(new CombatSessionEvents.CombatEndEvent(
                survivors, result, _currentBattleId, dur, finalSnapshot)); // Pass snapshot

            // 상태 초기화
            _isInCombat        = false;
            _currentActiveUnit = null;
            // Clear participant lists AFTER publishing the event that uses them for snapshot
            _participants?.Clear();
            _teamAssignments?.Clear();
            _defendedThisTurn?.Clear();
            _actedThisCycle?.Clear();
            _movedThisActivation?.Clear();

            _currentBattleId = null;
            _battleName      = null;
            _currentTurn     = 0; // Reset turn
            _currentCycle    = 0; // Reset cycle
        }

        // ──────────────────────────────────────────────────────────────
        // 턴 진행 (사이클/활성화 로직으로 변경) -> 이름 유지, 내부 변수 변경
        // ──────────────────────────────────────────────────────────────
        public bool ProcessNextTurn() // Method name kept as is, logic reflects turn/cycle
        {
            if (!_isInCombat) return false;

            // Publish end event for the PREVIOUSLY active unit's cycle
            if (_currentActiveUnit != null)
            {
                _eventBus.Publish(new CombatSessionEvents.UnitActivationEndEvent(
                    _currentTurn, _currentCycle, _currentActiveUnit, _currentBattleId));
            }

            var activeParticipants = _participants.Where(p => p.IsOperational).ToList();
            bool isNewTurn = _actedThisCycle.SetEquals(activeParticipants);

            if (isNewTurn || _currentTurn == 0) 
            {
                if (_currentTurn > 0) 
                {
                    _eventBus.Publish(new CombatSessionEvents.RoundEndEvent(_currentTurn, _currentBattleId));
                }
                _currentTurn++;
                _currentCycle = 0;
                _actedThisCycle.Clear();
                _defendedThisTurn.Clear(); // Clear defense status for the new turn
                // _movedThisActivation.Clear(); // Cleared below before new unit activation

                List<ArmoredFrame> initiativeSequence = activeParticipants; 
                _eventBus.Publish(new CombatSessionEvents.RoundStartEvent(
                    _currentTurn, _currentBattleId, initiativeSequence));
            }

            _currentActiveUnit = GetNextActiveUnit();
            _movedThisActivation.Clear(); 

            if (_currentActiveUnit == null) 
            {
                if (!isNewTurn)
                {
                     Debug.LogWarning($"ProcessNextTurn: GetNextActiveUnit returned null unexpectedly. Turn: {_currentTurn}, Acted: {_actedThisCycle.Count}, Active: {activeParticipants.Count}");
                }
                CheckBattleEndCondition(); 
                return !_isInCombat;       
            }

            _currentCycle++;
            float apBeforeRecovery = _currentActiveUnit.CurrentAP;
            _currentActiveUnit.RecoverAPOnTurnStart();
            _eventBus.Publish(new CombatSessionEvents.UnitActivationStartEvent(
                 _currentTurn, _currentCycle, _currentActiveUnit, _currentBattleId, apBeforeRecovery));

            foreach (var w in _currentActiveUnit.GetAllWeapons())
            {
                if (w.IsReloading) { w.CheckReloadCompletion(_currentTurn); }
            }
            _statusProcessor.Tick(MakeCtx(), _currentActiveUnit);

            // --- 행동 트리 실행 로직 시작 ---
            _currentActiveUnit.AICtxBlackboard.ClearAllData(); // 각 유닛 활성화 시작 시 블랙보드 초기화

            NodeStatus btStatus = NodeStatus.Failure; // BT 실행 결과 기본값
            if (_currentActiveUnit.BehaviorTreeRoot != null)
            {
                // 한 유닛의 활성화 동안 BT는 여러 번 Tick 될 수 있음 (예: AP가 다 떨어질 때까지)
                // 여기서는 일단 한 번의 Tick으로 행동을 결정한다고 가정.
                // 반복 실행 로직은 추후 개선 가능 (예: while 루프와 BT_RUNNING 상태 활용)
                btStatus = _currentActiveUnit.BehaviorTreeRoot.Tick(_currentActiveUnit, _currentActiveUnit.AICtxBlackboard, MakeCtx());
            }
            else
            {
                _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}]에게 할당된 행동 트리가 없습니다. 대기합니다.", LogLevel.Warning);
            }

            var decidedActionType = _currentActiveUnit.AICtxBlackboard.DecidedActionType;

            // ActionType.None도 유효한 값으로 취급될 수 있으므로, null 체크와 함께 decidedActionType.Value == ActionType.None 인 경우도 고려해야 함.
            // 하지만 현재 WaitNode는 DecidedActionType을 null로 남기므로, HasValue로만 체크해도 의도대로 동작할 것.
            if (decidedActionType.HasValue && decidedActionType.Value != CombatActionEvents.ActionType.None)
            {
                var targetFrame = _currentActiveUnit.AICtxBlackboard.CurrentTarget;
                var targetPosition = _currentActiveUnit.AICtxBlackboard.IntendedMovePosition;
                var selectedWeapon = _currentActiveUnit.AICtxBlackboard.SelectedWeapon;

                bool actionSuccess = _actionExecutor.Execute(
                    MakeCtx(), 
                    _currentActiveUnit, 
                    decidedActionType.Value, 
                    targetFrame, 
                    targetPosition, 
                    selectedWeapon);

                if (!actionSuccess)
                {
                    _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}]의 행동 {decidedActionType.Value} 실행 실패.", LogLevel.Warning);
                    // 행동 실패 시 추가적인 BT Tick 또는 다른 로직 고려 가능
                }
                // 성공하든 실패하든, 일단 행동을 시도했으므로 루프를 빠져나감 (이전 로직과 유사하게)
            }
            else // decidedActionType이 null이거나 ActionType.None인 경우
            {
                if (btStatus == NodeStatus.Success) // BT는 성공했으나, 결정된 행동이 없거나 'None'인 경우 (예: WaitNode)
                {
                    _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}]이(가) 명시적으로 대기합니다. (BT Status: {btStatus}, DecidedAction: {decidedActionType?.ToString() ?? "null"})", LogLevel.Info);
                    // 여기에 실제 'Wait' 액션 실행 로직을 넣거나 AP 소모 등을 처리할 수 있음.
                    // 예를 들어, _actionExecutor.Execute(MakeCtx(), _currentActiveUnit, CombatActionEvents.ActionType.None, null, null, null);
                    // 만약 ActionType.None을 Executor가 처리한다면. 현재는 그럴 필요 없어 보임.
                }
                else if (btStatus == NodeStatus.Failure || _currentActiveUnit.BehaviorTreeRoot == null) // BT 자체가 실패한 경우
                {
                    _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}]이(가) 행동을 결정하지 못하고 대기합니다. (BT Status: {btStatus})", LogLevel.Info);
                }
                // btStatus가 Running인 경우는 현재 로직에서 특별히 처리하지 않음.
            }
            // --- 행동 트리 실행 로직 끝 ---
            
            _actedThisCycle.Add(_currentActiveUnit);
            CheckBattleEndCondition();
            return _isInCombat;
        }

        // ──────────────────────────────────────────────────────────────
        // 인터페이스 요구: PerformAction / PerformAttack  (래퍼)
        // ──────────────────────────────────────────────────────────────
        public bool PerformAction(ArmoredFrame actor,
                                  CombatActionEvents.ActionType actionType,
                                  ArmoredFrame targetFrame,
                                  Vector3? targetPosition,
                                  Weapon weapon)
        {
            // 시뮬레이터 외부 호출 시 최소 검증
            if (!_isInCombat || actor != _currentActiveUnit) return false;
            return _actionExecutor.Execute(
                MakeCtx(), actor, actionType, targetFrame, targetPosition, weapon);
        }

        public bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon)
        {
            if (!_isInCombat || attacker != _currentActiveUnit) return false;
            // ActionType.Attack 래퍼 호출
            return _actionExecutor.Execute(
                MakeCtx(), attacker, CombatActionEvents.ActionType.Attack,
                target, null, weapon);
        }

        // ──────────────────────────────────────────────────────────────
        // 참가자 / 팀 / 유틸
        // ──────────────────────────────────────────────────────────────
        public bool IsUnitDefeated(ArmoredFrame unit)
            => unit == null || !unit.IsOperational || !_participants.Contains(unit);

        public List<ArmoredFrame> GetParticipants() => new List<ArmoredFrame>(_participants);

        public List<ArmoredFrame> GetAllies(ArmoredFrame unit)
        {
            if (!_teamAssignments.TryGetValue(unit,out var team)) return new();
            return _participants.Where(p=>p!=unit && _teamAssignments.TryGetValue(p,out var t) && t==team).ToList();
        }

        public List<ArmoredFrame> GetEnemies(ArmoredFrame unit)
        {
            if (!_teamAssignments.TryGetValue(unit,out var team)) return new();
            return _participants.Where(p=>_teamAssignments.TryGetValue(p,out var t)&&t!=team).ToList();
        }

        public bool HasUnitDefendedThisTurn(ArmoredFrame unit)
            => _defendedThisTurn.Contains(unit);

        // ──────────────────────────────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────────────────────────────
        private CombatContext MakeCtx() => new CombatContext(
            _eventBus, 
            _textLogger, 
            _actionExecutor,
            _currentBattleId, 
            _currentTurn, 
            _currentCycle,
            _defendedThisTurn, 
            _participants, 
            _teamAssignments, 
            _movedThisActivation);

        private void AssignTeams(IEnumerable<ArmoredFrame> parts)
        {
            _teamAssignments.Clear();
            foreach (var p in parts) _teamAssignments[p] = p.TeamId;
        }

        private ArmoredFrame GetNextActiveUnit()
        {
            var operationalUnits = _participants.Where(p => p.IsOperational).ToList();
            if (!operationalUnits.Any()) return null;

            int startIndex = _currentActiveUnit != null ? operationalUnits.IndexOf(_currentActiveUnit) : -1;
            for (int i = 1; i <= operationalUnits.Count; i++)
            {
                var nextUnit = operationalUnits[(startIndex + i) % operationalUnits.Count];
                if (!_actedThisCycle.Contains(nextUnit))
                {
                    return nextUnit;
                }
            }
            return null;
        }

        private void CheckBattleEndCondition()
        {
            var res = _resultEvaluator.Evaluate(_participants,_teamAssignments);
            if (res!=CombatSessionEvents.CombatEndEvent.ResultType.Draw) EndCombat(res);
        }
    }
}