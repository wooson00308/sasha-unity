using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
// using AF.Combat.Behaviors;  // 기존 파일럿 전략 네임스페이스 주석 처리
using AF.AI.BehaviorTree;      // 새로운 행동 트리 네임스페이스 추가
using AF.AI.BehaviorTree.PilotBTs; // BasicAttackBT 사용을 위해 추가
using AF.Models.Abilities;
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
        private HashSet<ArmoredFrame>                 _defendedThisTurn; // 턴 당 방어 여부
        private HashSet<ArmoredFrame>                 _actedThisCycle; // Represents units acted *this turn*
        private HashSet<ArmoredFrame>                 _movedThisActivation; // 현재 활성화 주기 이동 여부
        private HashSet<ArmoredFrame>                 _defendedThisActivation; // 현재 활성화 주기 방어 여부 (신규)

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
        public HashSet<ArmoredFrame> DefendedThisActivation => _defendedThisActivation; // 신규 프로퍼티

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
            _defendedThisActivation = new HashSet<ArmoredFrame>(); // 신규 초기화
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
            _defendedThisActivation = null; // 신규 초기화
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
                    // 파일럿 전문화에 따라 BT 할당
                    if (unit.Pilot != null && unit.Pilot.Specialization == SpecializationType.RangedCombat)
                    {
                        unit.BehaviorTreeRoot = RangedCombatBT.Create(unit);
                    }
                    else if (unit.Pilot != null && unit.Pilot.Specialization == SpecializationType.MeleeCombat)
                    {
                        unit.BehaviorTreeRoot = MeleeCombatBT.Create(unit); 
                    }
                    else if (unit.Pilot != null && unit.Pilot.Specialization == SpecializationType.Defense)
                    {
                        unit.BehaviorTreeRoot = DefenderBT.Create(unit); // DefenderBT 할당
                    }
                    else if (unit.Pilot != null && unit.Pilot.Specialization == SpecializationType.Support)
                    {
                        unit.BehaviorTreeRoot = SupportBT.Create(unit); 
                    }
                    else // 그 외 모든 경우 (파일럿이 없거나, 다른 특화 타입이거나, StandardCombat 등)
                    {
                        unit.BehaviorTreeRoot = BasicAttackBT.Create(); 
                    }
                    unit.AICtxBlackboard.ClearAllData(); // 새 전투 시작 시 블랙보드 초기화
                }
            }
            // +++ 행동 트리 할당 끝 +++

            _defendedThisTurn.Clear();
            _actedThisCycle.Clear();
            _movedThisActivation.Clear();
            _defendedThisActivation.Clear(); // 신규 초기화

            _eventBus.Publish(new CombatSessionEvents.CombatStartEvent(
                participants, _currentBattleId, battleName, Vector3.zero));

            return _currentBattleId;
        }

        public void EndCombat(CombatSessionEvents.CombatEndEvent.ResultType forceResult = CombatSessionEvents.CombatEndEvent.ResultType.Aborted)
        {
            if (!_isInCombat) return;

            var result = forceResult;
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
            _defendedThisActivation?.Clear(); // 신규 초기화

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
            // A new turn starts if it's the very first turn (currentTurn == 0),
            // OR if there are any active participants and all of them have already acted in the current cycle.
            bool isNewTurn = (_currentTurn == 0) ||
                             (activeParticipants.Any() && activeParticipants.All(p => _actedThisCycle.Contains(p)));

            if (isNewTurn)
            {
                if (_currentTurn > 0)
                {
                    _eventBus.Publish(new CombatSessionEvents.RoundEndEvent(_currentTurn, _currentBattleId));
                    CheckBattleEndCondition();
                    if (!_isInCombat) return false;
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
            _defendedThisActivation.Clear(); // ★ 유닛 활성화 시 방어 기록 초기화

            // +++ 실패한 노드 추적 초기화 +++
            if (_currentActiveUnit?.AICtxBlackboard?.FailedNodesThisActivation != null)
            {
                _currentActiveUnit.AICtxBlackboard.FailedNodesThisActivation.Clear();
            }
            // +++ 실패한 노드 추적 초기화 끝 +++

            if (_currentActiveUnit == null) 
            {
                // if (!isNewTurn)
                // {
                //      Debug.LogWarning($"ProcessNextTurn: GetNextActiveUnit returned null unexpectedly. Turn: {_currentTurn}, Acted: {_actedThisCycle.Count}, Active: {activeParticipants.Count}");
                // }
                CheckBattleEndCondition(); 
                // SASHA: 수정된 반환 로직
                // 만약 CheckBattleEndCondition에 의해 EndCombat이 호출되어 _isInCombat이 false가 되었다면, false를 반환 (전투 종료)
                // 그렇지 않고 _isInCombat이 true라면 (즉, 전투는 계속되어야 하지만 이번 턴에 행동할 유닛이 없는 경우, 다음 턴으로 넘어가야 함), true를 반환.
                return _isInCombat; 
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
            // 현재 활성화된 유닛에게 CombatContext를 미리 설정하여 StatusEffect 핸들러들이 참조할 수 있도록 함
            CombatContext unitCtx = MakeCtx();
            _currentActiveUnit.CurrentCombatContext = unitCtx;
            _statusProcessor.Tick(unitCtx, _currentActiveUnit);

            // --- 행동 트리 실행 로직 시작 ---
            _currentActiveUnit.AICtxBlackboard.ClearAllData(); 

            bool canContinueActing = true;
            int actionsThisActivation = 0;
            const int MAX_ACTIONS_PER_ACTIVATION = 5; 

            CombatContext currentActionContext = MakeCtx(); // 컨텍스트를 루프 전에 생성

            var blackboard = _currentActiveUnit.AICtxBlackboard;

            // +++ 행동 트리 실행 로그 추가 (어떤 BT가 실행되는지 확인) +++
            _textLogger?.TextLogger?.Log($"[ProcessNextTurn] {_currentActiveUnit.Name}: Running Behavior Tree: {_currentActiveUnit.BehaviorTreeRoot?.GetType().Name ?? "None"}", LogLevel.Debug);

            // +++ 멀티액션 루프 시작 +++
            Dictionary<CombatActionEvents.ActionType, int> actionsUsedThisActivation = new Dictionary<CombatActionEvents.ActionType, int>();
            
            while (canContinueActing && actionsThisActivation < MAX_ACTIONS_PER_ACTIVATION && _currentActiveUnit.IsOperational && _currentActiveUnit.CurrentAP > 0.001f)
            {
                // 각 행동 시도 전에 블랙보드 결정 데이터 클리어 (이전 행동의 잔재 제거)
                blackboard.DecidedActionType = null;
                blackboard.SelectedWeapon = null;
                blackboard.CurrentTarget = null;
                blackboard.IntendedMovePosition = null;
                blackboard.WeaponToReload = null;
                blackboard.SelectedAbility = null;

            NodeStatus btStatus = NodeStatus.Failure;
            if (_currentActiveUnit.BehaviorTreeRoot != null)
            {
                btStatus = _currentActiveUnit.BehaviorTreeRoot.Tick(_currentActiveUnit, blackboard, currentActionContext);
            }
            else
            {
                _textLogger?.TextLogger?.Log($"CRITICAL ERROR: [{_currentActiveUnit.Name}] 코어 AI 모듈 연결 실패. 행동 로직 부재. 현 작전 수행 불가. 활성화 강제 종료.", LogLevel.Error);
                canContinueActing = false; 
                    break;
            }

            // === 디버그 로그 추가: Execute 호출 직전 Blackboard 상태 ===
            if (blackboard.DecidedActionType == CombatActionEvents.ActionType.UseAbility)
            {
                string saId = blackboard.SelectedAbility?.AbilityID ?? "NULL_ID";
                string saSoName = blackboard.SelectedAbility?.SourceSO?.AbilityName ?? "NULL_SO_NAME";
                float? saSoAp = blackboard.SelectedAbility?.SourceSO?.APCost;
                _textLogger?.TextLogger?.Log($"[ProcessNextTurn] {_currentActiveUnit.Name} PRE-EXECUTE_ABILITY: SelectedAbility.AbilityID='{saId}', SourceSO.Name='{saSoName}', SourceSO.APCost='{saSoAp?.ToString() ?? "NULL_AP"}'", LogLevel.Debug);
            }
            // === 디버그 로그 추가 끝 ===

            var decidedActionType = blackboard.DecidedActionType;

            if (decidedActionType.HasValue && decidedActionType.Value != CombatActionEvents.ActionType.None)
            {
                    // +++ ActionType별 동적 제한 체크 +++
                    int usedCount = actionsUsedThisActivation.GetValueOrDefault(decidedActionType.Value, 0);
                    int maxAllowed = GetMaxActionsForType(decidedActionType.Value, _currentActiveUnit);
                    if (usedCount >= maxAllowed)
                    {
                        _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}] {decidedActionType.Value} 작전 한계 도달 ({usedCount}/{maxAllowed}). 추가 실행 제한. 다른 프로토콜 탐색.", LogLevel.Debug);
                        actionsThisActivation++;
                        continue; // 제한에 걸린 ActionType은 건너뛰기
                    }
                    // +++ ActionType별 동적 제한 체크 끝 +++

                // +++ SASHA: AP 비용 계산 시 재장전 액션일 경우 WeaponToReload 사용 +++
                Weapon weaponForApCost = decidedActionType.Value == CombatActionEvents.ActionType.Reload 
                                            ? blackboard.WeaponToReload 
                                            : blackboard.SelectedWeapon;

                // +++ SASHA: GetActionAPCost 호출 시 abilityEffect 전달 추가 +++
                AbilityEffect abilityEffectForApCost = decidedActionType.Value == CombatActionEvents.ActionType.UseAbility 
                                                        ? blackboard.SelectedAbility 
                                                        : null;

                float apCost = _actionExecutor.GetActionAPCost(
                    decidedActionType.Value, 
                    _currentActiveUnit, 
                        blackboard,
                    weaponForApCost,
                        abilityEffectForApCost);
                // +++ SASHA: 수정 끝 +++

                if (!_currentActiveUnit.HasEnoughAP(apCost))
                {
                    // SASHA: LogEventType 변경 및 스냅샷 추가, 파라미터 순서 수정
                    var snapshotOnApFail = _currentActiveUnit != null ? new Dictionary<string, ArmoredFrameSnapshot> { { _currentActiveUnit.Name, new ArmoredFrameSnapshot(_currentActiveUnit) } } : null;
                    _textLogger?.TextLogger?.Log(
                            message: $"[{_currentActiveUnit.Name}] {decidedActionType.Value} ({weaponForApCost?.Name ?? "N/A"}) 작전 수행 불가. 에너지 부족 (요구: {apCost:F1}, 잔여: {_currentActiveUnit.CurrentAP:F1}). 활성화 종료.",
                        level: LogLevel.Info,
                        eventType: LogEventType.SystemMessage,
                        contextUnit: _currentActiveUnit,
                        turnStartStateSnapshot: snapshotOnApFail
                    );
                        canContinueActing = false; // AP 부족으로 활성화 종료
                        break;
                }
                
                if (decidedActionType.Value == CombatActionEvents.ActionType.Move && currentActionContext.MovedThisActivation.Contains(_currentActiveUnit))
                {
                    _textLogger?.TextLogger?.Log($"[{_currentActiveUnit.Name}] 현재 활성화 주기 내 기동 완료. 전술적 위치 재조정 불가. 다른 작전 프로토콜 탐색.", LogLevel.Info);
                        // 이동 제한에 걸렸지만 다른 행동은 시도할 수 있으므로 continue
                    actionsThisActivation++; 
                        continue;
                }

                var targetFrame = blackboard.CurrentTarget;
                var targetPosition = blackboard.IntendedMovePosition;

                // +++ SASHA: 액션 실행 시 사용할 무기 결정 로직 추가 +++
                Weapon weaponForExecution = null;
                if (decidedActionType.Value == CombatActionEvents.ActionType.Reload)
                {
                    weaponForExecution = blackboard.WeaponToReload;
                }
                else if (decidedActionType.Value == CombatActionEvents.ActionType.Attack)
                {
                    weaponForExecution = blackboard.SelectedWeapon;
                }
                // 다른 액션 타입(Move, Defend 등)은 weaponForExecution이 null일 수 있음
                // +++ SASHA: 수정 끝 +++

                AbilityEffect abilityToUse = decidedActionType.Value == CombatActionEvents.ActionType.UseAbility ? blackboard.SelectedAbility : null;
                string explicitPartSlotForRepair = null;
                if (decidedActionType.Value == CombatActionEvents.ActionType.RepairAlly)
                {
                    explicitPartSlotForRepair = blackboard.RepairTargetPartSlotId;
                }

                bool actionSuccess = _actionExecutor.Execute(
                    currentActionContext, 
                    _currentActiveUnit, 
                    decidedActionType.Value, 
                    targetFrame, 
                    targetPosition, 
                    weaponForExecution,
                    false, // isCounter
                    false, // freeCounter
                    abilityToUse,
                    explicitPartSlotForRepair // 새로 추가된 파라미터 전달
                );

                    // +++ ActionType 사용 횟수 기록 +++
                    actionsUsedThisActivation[decidedActionType.Value] = usedCount + 1;
                    // +++ ActionType 사용 횟수 기록 끝 +++

                actionsThisActivation++;

                if (actionSuccess)
                {
                    if (decidedActionType.Value == CombatActionEvents.ActionType.Move)
                    {
                        if (!currentActionContext.MovedThisActivation.Contains(_currentActiveUnit))
                        {
                            currentActionContext.MovedThisActivation.Add(_currentActiveUnit);
                        }
                    }
                    else if (decidedActionType.Value == CombatActionEvents.ActionType.Defend)
                    {
                        if (!currentActionContext.DefendedThisActivation.Contains(_currentActiveUnit)) 
                        {
                            currentActionContext.DefendedThisActivation.Add(_currentActiveUnit);
                            _defendedThisTurn.Add(_currentActiveUnit); 
                        }
                    }

                        // 즉시 재장전 로직
                    if (decidedActionType.Value == CombatActionEvents.ActionType.Attack && 
                        _currentActiveUnit.AICtxBlackboard.ImmediateReloadWeapon != null)
                    {
                        Weapon weaponToReloadImmediately = _currentActiveUnit.AICtxBlackboard.ImmediateReloadWeapon;
                            float reloadApCost = _actionExecutor.GetActionAPCost(
                                CombatActionEvents.ActionType.Reload, 
                                _currentActiveUnit, 
                                blackboard, // ⭐ 추가: Blackboard 전달
                                weaponToReloadImmediately
                            );

                        if (_currentActiveUnit.HasEnoughAP(reloadApCost))
                        {
                            _textLogger?.TextLogger?.Log($"SYSTEM: [{_currentActiveUnit.Name}] {weaponToReloadImmediately.Name} 탄약 소진. 즉시 재장전 프로토콜 실행.", LogLevel.Info);
                            bool reloadSuccess = _actionExecutor.Execute(currentActionContext, _currentActiveUnit, CombatActionEvents.ActionType.Reload, null, null, weaponToReloadImmediately);
                            _currentActiveUnit.AICtxBlackboard.ImmediateReloadWeapon = null; 
                            actionsThisActivation++; 
                        }
                        else
                        {
                            // SASHA: LogEventType 변경 및 스냅샷 추가, 파라미터 순서 수정
                            var snapshotOnReloadApFail = _currentActiveUnit != null ? new Dictionary<string, ArmoredFrameSnapshot> { { _currentActiveUnit.Name, new ArmoredFrameSnapshot(_currentActiveUnit) } } : null;
                            _textLogger?.TextLogger?.Log(
                                message: $"[{_currentActiveUnit.Name}] {weaponToReloadImmediately.Name} 즉시 재장전 시도... 에너지 부족 (요구: {reloadApCost:F1}, 잔여: {_currentActiveUnit.CurrentAP:F1}).",
                                level: LogLevel.Info,
                                eventType: LogEventType.SystemMessage,
                                contextUnit: _currentActiveUnit,
                                turnStartStateSnapshot: snapshotOnReloadApFail
                            );
                        }
                    }
                }
                else 
                {
                    _textLogger?.TextLogger?.Log($"WARNING: [{_currentActiveUnit.Name}]의 행동 {decidedActionType.Value} 실행 프로토콜 인터럽트 발생.", LogLevel.Warning);
                }

                CheckBattleEndCondition();
                if (!_isInCombat) 
                {
                        canContinueActing = false;
                        break;
                }

                    // AP 체크: 일정 수준 이하로 떨어지면 활성화 종료
                    if (_currentActiveUnit.CurrentAP <= 0.5f) // 최소 행동 비용보다 낮으면 종료
                {
                    // SASHA: LogEventType 변경 및 스냅샷 추가, 파라미터 순서 수정
                    var snapshotOnApDepleted = _currentActiveUnit != null ? new Dictionary<string, ArmoredFrameSnapshot> { { _currentActiveUnit.Name, new ArmoredFrameSnapshot(_currentActiveUnit) } } : null;
                    _textLogger?.TextLogger?.Log(
                            message: $"SYSTEM: [{_currentActiveUnit.Name}] 주 동력원 임계점 도달 (AP: {_currentActiveUnit.CurrentAP:F1}). 추가 작전 행동 제한. 활성화 사이클 종료.",
                        level: LogLevel.Info,
                        eventType: LogEventType.SystemMessage,
                        contextUnit: _currentActiveUnit,
                        turnStartStateSnapshot: snapshotOnApDepleted
                    );
                    canContinueActing = false;
                        break;
                }
            }
            else 
            {
                    // BT에서 행동을 결정하지 못함 - 활성화 종료
                // SASHA: LogEventType 변경 및 스냅샷 추가, 파라미터 순서 수정
                var snapshotOnNoAction = _currentActiveUnit != null ? new Dictionary<string, ArmoredFrameSnapshot> { { _currentActiveUnit.Name, new ArmoredFrameSnapshot(_currentActiveUnit) } } : null;
                if (btStatus == NodeStatus.Success) 
                {
                    _textLogger?.TextLogger?.Log(
                            message: $"[{_currentActiveUnit.Name}] 전술 목표 달성. 추가 행동 불요. 활성화 사이클 종료.", 
                        level: LogLevel.Info,
                        eventType: LogEventType.SystemMessage,
                        contextUnit: _currentActiveUnit,
                        turnStartStateSnapshot: snapshotOnNoAction
                    );
                }
                else 
                {
                    _textLogger?.TextLogger?.Log(
                            message: $"[{_currentActiveUnit.Name}] 전술 AI 추가 행동 결정 실패. (BT Status: {btStatus}) 활성화 사이클 종료.", 
                            level: LogLevel.Info,
                            eventType: LogEventType.SystemMessage,
                        contextUnit: _currentActiveUnit,
                        turnStartStateSnapshot: snapshotOnNoAction
                    );
                }
                canContinueActing = false; 
                    break;
            }
            
            if (!_currentActiveUnit.IsOperational) 
            {
                _textLogger?.TextLogger?.Log($"FATAL: [{_currentActiveUnit.Name}] 기체 완파 확인. 생명 유지 장치 가동... 실패. 모든 기능 정지. 활성화 즉시 종료.", LogLevel.Error);
                
                // SASHA: 디버그 로그 추가
                if (_textLogger?.TextLogger != null)
                {
                    var aliveDebug = _participants.Where(p => p != null && p.IsOperational).ToList();
                    _textLogger.TextLogger.Log($"[DEBUG] Before CheckBattleEndCondition (after unit [{_currentActiveUnit.Name}] destroyed): Total Participants: {_participants.Count}, Alive: {aliveDebug.Count}", LogLevel.Debug);
                    foreach(var p_debug in aliveDebug)
                    {
                        _textLogger.TextLogger.Log($"  [DEBUG] Alive: {p_debug.Name} (Team: {(_teamAssignments.TryGetValue(p_debug, out int teamId) ? teamId.ToString() : "N/A")})", LogLevel.Debug);
                    }
                    if (aliveDebug.Count == 0 && _participants.Any(p => p != null && !p.IsOperational))
                    {
                        _textLogger.TextLogger.Log($"[DEBUG] All units are non-operational. Evaluator might return Draw or Aborted.", LogLevel.Debug);
                    }
                }
                // SASHA: 디버그 로그 끝

                canContinueActing = false;
                CheckBattleEndCondition();
                if (!_isInCombat)
                {
                        break;
                    }
                }
            }
            // +++ 멀티액션 루프 끝 +++

            _actedThisCycle.Add(_currentActiveUnit);
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
            _movedThisActivation,
            _defendedThisActivation // ★ 신규 컨텍스트 필드 전달
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
            var result = _resultEvaluator.Evaluate(_participants, _teamAssignments);

            if (result.HasValue)
            {
                EndCombat(result.Value);
            }
        }

        private int GetMaxActionsForType(CombatActionEvents.ActionType actionType, ArmoredFrame unit)
        {
            // 기본 제한: 각 ActionType당 1회
            int baseLimit = 1;
            int bonusActions = 0;

            // +++ 향후 확장 가능한 보너스 액션 계산 +++
            
            // 예시 1: 연속 공격 스탯 (향후 구현)
            // if (actionType == CombatActionEvents.ActionType.Attack && unit.Stats.HasContinuousAttack)
            // {
            //     bonusActions += unit.Stats.ContinuousAttackLevel;
            // }

            // 예시 2: 스러스터 부스트 효과 (향후 구현)  
            // if (actionType == CombatActionEvents.ActionType.Move && unit.HasStatusEffect("ThrusterBoost"))
            // {
            //     bonusActions += 1;
            // }

            // 예시 3: 다중 능력 발동 (향후 구현)
            // if (actionType == CombatActionEvents.ActionType.UseAbility && unit.HasAbility("MultiCast"))
            // {
            //     bonusActions += 1;
            // }

            // 예시 4: 파일럿 전문화 보너스 (향후 구현)
            // if (unit.Pilot != null)
            // {
            //     switch (unit.Pilot.Specialization)
            //     {
            //         case SpecializationType.RangedCombat when actionType == CombatActionEvents.ActionType.Attack:
            //             bonusActions += 1; // 원거리 전문가는 공격 2회 가능
            //             break;
            //         case SpecializationType.Support when actionType == CombatActionEvents.ActionType.UseAbility:
            //             bonusActions += 1; // 서포트 전문가는 어빌리티 2회 가능
            //             break;
            //     }
            // }

            // 예시 5: 특수 부품/장비 효과 (향후 구현)
            // if (actionType == CombatActionEvents.ActionType.Defend && unit.HasPart("AdvancedShield"))
            // {
            //     bonusActions += 1;
            // }

            // 예시 6: 임시 버프/디버프 효과 (향후 구현)
            // foreach (var statusEffect in unit.StatusEffects)
            // {
            //     if (statusEffect.Type == StatusEffectType.ActionBonus && statusEffect.TargetActionType == actionType)
            //     {
            //         bonusActions += statusEffect.BonusAmount;
            //     }
            // }

            // +++ 보너스 액션 계산 끝 +++

            int totalLimit = baseLimit + bonusActions;
            
            // 안전장치: 최대 5회까지만 허용 (무한 루프 방지)
            return Mathf.Min(totalLimit, 5);
        }
    }
}