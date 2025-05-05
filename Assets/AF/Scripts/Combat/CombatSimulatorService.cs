using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
using AF.Combat.Behaviors;  // 파일럿 전략
using UnityEngine;
using System.Collections.ObjectModel; // For ReadOnlyDictionary/Collection
using AF.AI.PilotBehavior; // <<< Utility AI 전략 네임스페이스 추가
using AF.AI.UtilityAI; // <<< IUtilityAction 때문에 추가

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

        // Public accessors for needed services
        public EventBus.EventBus Bus => _eventBus;
        public TextLoggerService Logger => _textLogger;

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

        // Public accessors for needed state
        public IReadOnlyDictionary<ArmoredFrame, int> TeamAssignments => _teamAssignments;
        public IReadOnlyCollection<ArmoredFrame> DefendedThisTurn => _defendedThisTurn;

        // 파일럿 전문화 → 전략
        private Dictionary<SpecializationType, IPilotBehaviorStrategy> _behaviorStrategies;

        // +++ 추가: 액션 가중치 정의 (UtilityAIPilotBehaviorStrategy에서 이동) +++
        private static readonly Dictionary<SpecializationType, Dictionary<CombatActionEvents.ActionType, float>> _actionWeights =
            new Dictionary<SpecializationType, Dictionary<CombatActionEvents.ActionType, float>>
        {
            { SpecializationType.StandardCombat, new Dictionary<CombatActionEvents.ActionType, float> {
                { CombatActionEvents.ActionType.Attack, 1.0f }, { CombatActionEvents.ActionType.Move, 0.8f }, { CombatActionEvents.ActionType.Defend, 0.5f }, { CombatActionEvents.ActionType.Reload, 0.7f }, { CombatActionEvents.ActionType.RepairSelf, 0.6f }, { CombatActionEvents.ActionType.RepairAlly, 0.5f } } },
            { SpecializationType.MeleeCombat,    new Dictionary<CombatActionEvents.ActionType, float> {
                { CombatActionEvents.ActionType.Attack, 1.2f }, { CombatActionEvents.ActionType.Move, 1.0f }, { CombatActionEvents.ActionType.Defend, 0.4f }, { CombatActionEvents.ActionType.Reload, 0.5f }, { CombatActionEvents.ActionType.RepairSelf, 0.5f }, { CombatActionEvents.ActionType.RepairAlly, 0.3f } } }, // 근접 공격 및 접근 선호
            { SpecializationType.RangedCombat,   new Dictionary<CombatActionEvents.ActionType, float> {
                { CombatActionEvents.ActionType.Attack, 1.1f }, { CombatActionEvents.ActionType.Move, 0.7f }, { CombatActionEvents.ActionType.Defend, 0.6f }, { CombatActionEvents.ActionType.Reload, 0.9f }, { CombatActionEvents.ActionType.RepairSelf, 0.6f }, { CombatActionEvents.ActionType.RepairAlly, 0.4f } } }, // 원거리 공격 및 재장전 선호, 접근 회피
            { SpecializationType.Defense,        new Dictionary<CombatActionEvents.ActionType, float> {
                { CombatActionEvents.ActionType.Attack, 0.8f }, { CombatActionEvents.ActionType.Move, 0.5f }, { CombatActionEvents.ActionType.Defend, 1.2f }, { CombatActionEvents.ActionType.Reload, 0.6f }, { CombatActionEvents.ActionType.RepairSelf, 0.7f }, { CombatActionEvents.ActionType.RepairAlly, 0.6f } } }, // 방어 선호
            { SpecializationType.Support,        new Dictionary<CombatActionEvents.ActionType, float> {
                { CombatActionEvents.ActionType.Attack, 0.6f }, { CombatActionEvents.ActionType.Move, 0.7f }, { CombatActionEvents.ActionType.Defend, 0.7f }, { CombatActionEvents.ActionType.Reload, 0.8f }, { CombatActionEvents.ActionType.RepairSelf, 1.0f }, { CombatActionEvents.ActionType.RepairAlly, 1.2f } } },  // 수리 선호
        };
        // +++ 추가 끝 +++

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

            // <<< 전략 매핑 수정: 모든 전문화를 UtilityAI로 변경하고 가중치 주입 >>>
            _behaviorStrategies = new Dictionary<SpecializationType, IPilotBehaviorStrategy>();
            foreach (SpecializationType specType in Enum.GetValues(typeof(SpecializationType)))
            {
                // Get the weight dictionary for the current specialization type.
                // Use GetValueOrDefault to fall back to StandardCombat weights if a specific entry is missing.
                var weightsForSpec = _actionWeights.GetValueOrDefault(specType, _actionWeights[SpecializationType.StandardCombat]);
                
                // Create a UtilityAIPilotBehaviorStrategy instance with the specific weights.
                _behaviorStrategies.Add(specType, new UtilityAIPilotBehaviorStrategy(weightsForSpec));
            }
            // <<< 전략 매핑 수정 끝 >>>

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
            _currentTurn      = 0; // Start at turn 0 (will increment to 1 on first ProcessNextTurn)
            _currentCycle     = 0; // Start at cycle 0
            _battleName       = battleName;
            _currentBattleId  = $"BATTLE_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000,9999)}";
            _combatStartTime  = Time.time;

            _participants     = participants.ToList();
            AssignTeams(participants);
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
                    _currentTurn, _currentCycle, _currentActiveUnit, _currentBattleId)); // Pass turn and cycle
            }

            var activeParticipants = _participants.Where(p => p.IsOperational).ToList();
            bool isNewTurn = _actedThisCycle.SetEquals(activeParticipants); // Check if all active units have acted this turn

            // --- Start New Turn Logic ---
            if (isNewTurn || _currentTurn == 0) // If it's a new turn or the very first turn
            {
                if (_currentTurn > 0) // Publish RoundEnd only after the first turn
                {
                    _eventBus.Publish(new CombatSessionEvents.RoundEndEvent(_currentTurn, _currentBattleId)); // Use _currentTurn
                }

                _currentTurn++;        // Increment the overall turn number
                _currentCycle = 0;     // Reset the activation cycle counter for the new turn
                _actedThisCycle.Clear(); // Clear the set of units that have acted
                _defendedThisTurn.Clear(); // Clear defense status for the new turn
                _movedThisActivation.Clear();

                List<ArmoredFrame> initiativeSequence = activeParticipants; // Use current active participants for initiative

                _eventBus.Publish(new CombatSessionEvents.RoundStartEvent(
                    _currentTurn, _currentBattleId, initiativeSequence)); // Use _currentTurn
            }
            // --- End New Turn Logic ---

            // --- Activate Next Unit ---
            _currentActiveUnit = GetNextActiveUnit(); // Find the next unit yet to act this turn

            // +++ Clear movement set for the new activation +++
            _movedThisActivation.Clear(); 
            // +++ Clear movement set end +++

            if (_currentActiveUnit == null) // Should only happen if battle ends or an error occurs
            {
                // If GetNextActiveUnit is null but it wasn't the start of a new turn, something is wrong
                if (!isNewTurn)
                {
                     Debug.LogWarning($"ProcessNextTurn: GetNextActiveUnit returned null unexpectedly. Turn: {_currentTurn}, Acted: {_actedThisCycle.Count}, Active: {activeParticipants.Count}");
                }
                CheckBattleEndCondition(); // Check end condition regardless
                return !_isInCombat;       // Return false if combat has ended
            }

            // Increment the cycle counter for this unit's activation
            _currentCycle++;

            // +++ AP 회복 전 값 저장 및 회복 호출 +++
            float apBeforeRecovery = _currentActiveUnit.CurrentAP;
            _currentActiveUnit.RecoverAPOnTurnStart(); // Recover AP 
            // +++ AP 회복 로직 끝 +++

            // Publish start event for the NEWLY active unit's cycle
            _eventBus.Publish(new CombatSessionEvents.UnitActivationStartEvent(
                 _currentTurn, _currentCycle, _currentActiveUnit, _currentBattleId, apBeforeRecovery)); // <<< Pass apBeforeRecovery

            // --- Process Active Unit's Cycle ---

            // Check and complete reload for the activated unit at the start of its cycle
            foreach (var w in _currentActiveUnit.GetAllWeapons())
            {
                if (w.IsReloading)
                {
                    // CheckReloadCompletion now also handles FinishReload if completed
                    w.CheckReloadCompletion(_currentTurn); // Pass the current overall turn number
                }
            }

            _statusProcessor.Tick(GetCurrentContext(), _currentActiveUnit); // Apply status effect ticks

            // Determine and execute actions for the unit
            int actions=0, maxActions=30; // Safety break for action loop
            while (_currentActiveUnit.IsOperational && actions<maxActions)
            {
                IUtilityAction determinedAction = DetermineActionForUnit(_currentActiveUnit);

                if (determinedAction == null)
                {
                    _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}] 추가 행동 프로토콜 없음. 시스템 대기 상태로 전환.", LogLevel.Info);
                    break;
                }

                CombatActionEvents.ActionType actionType = determinedAction.AssociatedActionType;
                ArmoredFrame targetFrame = determinedAction.Target;
                Vector3? targetPosition = determinedAction.TargetPosition;
                Weapon weapon = determinedAction.AssociatedWeapon;

                bool ok = _actionExecutor.Execute(
                    GetCurrentContext(), 
                    _currentActiveUnit, 
                    actionType,
                    targetFrame,
                    targetPosition,
                    weapon,
                    determinedAction
                );

                actions++;
                if (!ok || !_currentActiveUnit.IsOperational) break;
            }

            _actedThisCycle.Add(_currentActiveUnit); // Mark this unit as having acted this turn

            // --- End Cycle Processing ---

            CheckBattleEndCondition(); // Check if the battle ended after this cycle
            return _isInCombat;        // Return true if combat is still ongoing
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
                GetCurrentContext(), actor, actionType, targetFrame, targetPosition, weapon,
                null // <<< executedAction: null 추가
            );
        }

        public bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon)
        {
            if (!_isInCombat || attacker != _currentActiveUnit) return false;
            // ActionType.Attack 래퍼 호출
            return _actionExecutor.Execute(
                GetCurrentContext(), attacker, CombatActionEvents.ActionType.Attack,
                target, null, weapon,
                null // <<< executedAction: null 추가
            );
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
        public CombatContext GetCurrentContext() => new CombatContext(
            _eventBus, // 직접 참조
            _textLogger, // 직접 참조
            _currentBattleId,
            _currentTurn,
            _currentCycle,
            _defendedThisTurn, // HashSet 그대로 전달
            _participants, // List는 IList로 암시적 변환됨
            _teamAssignments, // Dictionary는 IDictionary로 암시적 변환됨
            _movedThisActivation // HashSet 그대로 전달
        );

        private void AssignTeams(IEnumerable<ArmoredFrame> parts)
        {
            _teamAssignments.Clear();
            foreach (var p in parts) _teamAssignments[p] = p.TeamId;
        }

        private ArmoredFrame GetNextActiveUnit()
        {
            var operationalUnits = _participants.Where(p => p.IsOperational).ToList();
            if (!operationalUnits.Any()) return null;

            // Find the index of the current unit to start the search from
            int startIndex = _currentActiveUnit != null ? operationalUnits.IndexOf(_currentActiveUnit) : -1;

            // Iterate through units in sequence, wrapping around
            for (int i = 1; i <= operationalUnits.Count; i++)
            {
                var nextUnit = operationalUnits[(startIndex + i) % operationalUnits.Count];
                // Return the first unit found that hasn't acted yet in this turn
                if (!_actedThisCycle.Contains(nextUnit))
                {
                    return nextUnit;
                }
            }

            // If all operational units have acted, return null
            return null;
        }

        private IUtilityAction DetermineActionForUnit(ArmoredFrame unit)
        {
            var spec = unit.Pilot?.Specialization ?? SpecializationType.StandardCombat;
            if (!_behaviorStrategies.TryGetValue(spec, out var strat))
            {
                 // Fallback to StandardCombat strategy if specific type not found (should not happen with the new Initialize)
                 Debug.LogWarning($"Behavior strategy not found for specialization {spec}. Falling back to StandardCombat.");
                 strat = _behaviorStrategies[SpecializationType.StandardCombat]; 
            }
            
            // strat.DetermineAction은 이제 IUtilityAction을 반환
            return strat.DetermineAction(unit, this); 
        }

        private void CheckBattleEndCondition()
        {
            var res = _resultEvaluator.Evaluate(_participants,_teamAssignments);
            if (res!=CombatSessionEvents.CombatEndEvent.ResultType.Draw) EndCombat(res);
        }
    }
}