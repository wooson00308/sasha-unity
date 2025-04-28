using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
using Unity.VisualScripting;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// 전투 시뮬레이션 서비스 구현
    /// 전투 과정을 관리하고 시뮬레이션하는 서비스
    /// </summary>
    public class CombatSimulatorService : ICombatSimulatorService
    {
        // 이벤트 버스 참조
        private EventBus.EventBus _eventBus;
        // 텍스트 로거 서비스 참조 추가
        private TextLoggerService _textLogger;
        
        // 전투 상태 관련 필드
        private string _currentBattleId;
        private string _battleName;
        private bool _isInCombat;
        private int _currentTurn;
        private List<ArmoredFrame> _participants;
        private ArmoredFrame _currentActiveUnit;
        private Dictionary<ArmoredFrame, int> _teamAssignments; // 팀 할당 정보 (ArmoredFrame -> 팀 ID)
        private float _combatStartTime;
        // 이전 턴 파츠 내구도 기록용 딕셔너리 추가
        private Dictionary<(ArmoredFrame, string), float> _previousPartDurability;
        // <<< 이번 턴 방어 유닛 기록용 HashSet 추가 >>>
        private HashSet<ArmoredFrame> _defendedThisTurn = new HashSet<ArmoredFrame>();
        private bool _logAIDecisions; // AI 결정 로그 토글 필드 추가
        
        // <<< AP 비용 상수 수정 시작 (이동/공격 제거) >>>
        private const float DEFEND_AP_COST = 1f;
        // <<< AP 비용 상수 수정 끝 >>>
        
        // <<< 원거리 AI 행동 결정용 상수 추가 >>>
        private const float MIN_RANGED_SAFE_DISTANCE = 2.0f; // 이 거리보다 가까우면 후퇴 시도 (5.0f에서 수정)
        private const float OPTIMAL_RANGE_FACTOR = 0.8f; // 최대 사거리의 이 비율만큼 떨어진 거리를 이상적으로 간주 (예시)
        // <<< 원거리 AI 행동 결정용 상수 추가 끝 >>>
        
        // 프로퍼티 구현
        public string CurrentBattleId => _currentBattleId;
        public bool IsInCombat => _isInCombat;
        public int CurrentTurn => _currentTurn;
        public ArmoredFrame CurrentActiveUnit => _currentActiveUnit;
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus;
            // TextLoggerService 가져오기 추가
            _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>(); 
            
            _participants = new List<ArmoredFrame>();
            _teamAssignments = new Dictionary<ArmoredFrame, int>();
            // 이전 내구도 딕셔너리 초기화 추가
            _previousPartDurability = new Dictionary<(ArmoredFrame, string), float>();
            // <<< 방어 기록 HashSet 초기화 >>>
            _defendedThisTurn = new HashSet<ArmoredFrame>();
            _isInCombat = false;
            _currentTurn = 0;
            _currentBattleId = null;
            
            // Debug.Log("[CombatSimulatorService] 서비스 초기화 완료");
            Debug.Log("[CombatSimulatorService] 서비스 초기화 완료");
        }

        /// <summary>
        /// 서비스 종료
        /// </summary>
        public void Shutdown()
        {
            if (_isInCombat)
            {
                // Debug.LogWarning("[CombatSimulatorService] 전투 중 강제 종료..."); // Optional: Keep Debug for forced shutdowns?
                Debug.LogWarning("[CombatSimulatorService] 전투 중 강제 종료...");
                EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Aborted);
            }
            
            _eventBus = null;
            _participants = null;
            _teamAssignments = null;
            // 이전 내구도 딕셔너리 초기화 추가 (종료 시)
            _previousPartDurability = null;
            // <<< 방어 기록 HashSet 초기화 >>>
            _defendedThisTurn = null;
            
            // Debug.Log("[CombatSimulatorService] 서비스 종료");
            Debug.Log("[CombatSimulatorService] 서비스 종료");
        }
        
        /// <summary>
        /// 전투 시작
        /// </summary>
        public string StartCombat(ArmoredFrame[] participants, string battleName, bool autoProcess = false)
        {
            if (_isInCombat)
            {
                Debug.LogWarning("[CombatSimulatorService] 이미 전투가 진행 중입니다. 기존 전투를 종료합니다."); // Keep Debug for editor warning?
            }
            
            // 기본 초기화
            _isInCombat = true;
            _currentTurn = 0;
            _battleName = battleName;
            _currentBattleId = GenerateBattleId();
            _combatStartTime = Time.time;
            
            // 참가자 설정
            _participants = new List<ArmoredFrame>(participants);
            AssignTeams(participants);
            // 이전 내구도 딕셔너리 초기화 및 초기값 설정
            _previousPartDurability.Clear();
            InitializePreviousDurability();
            
            // 이벤트 발행
            Vector3 battleLocation = Vector3.zero; // 기본값으로 설정, 필요한 경우 위치 정보 추가
            var startEvent = new CombatSessionEvents.CombatStartEvent(participants, _currentBattleId, battleName, battleLocation);
            _eventBus.Publish(startEvent);
            
            // Debug.Log($"[CombatSimulatorService] 전투 시작: {battleName} (ID: {_currentBattleId})");
            Debug.Log($"[CombatSimulatorService] 전투 시작: {battleName} (ID: {_currentBattleId})");
            
            // 자동 처리 모드인 경우 첫 턴 시작
            if (autoProcess)
            {
                ProcessNextTurn();
            }
            
            return _currentBattleId;
        }
        
        /// <summary>
        /// 전투 종료
        /// </summary>
        public void EndCombat(CombatSessionEvents.CombatEndEvent.ResultType? forceResult = null)
        {
            if (!_isInCombat)
            {
                Debug.LogWarning("[CombatSimulatorService] 진행 중인 전투가 없습니다."); // Keep Debug for editor warning?
                return;
            }
            
            // 전투 결과 판정
            var result = forceResult ?? DetermineCombatResult();
            
            // 생존자 목록 생성
            var survivors = _participants.Where(p => p.IsOperational).ToArray();
            
            // 전투 지속 시간 계산
            float duration = Time.time - _combatStartTime;
            
            // 이벤트 발행
            var endEvent = new CombatSessionEvents.CombatEndEvent(survivors, result, _currentBattleId, duration);
            _eventBus.Publish(endEvent);
            
            // 상태 초기화
            _isInCombat = false;
            _currentTurn = 0;
            _currentActiveUnit = null;
            _participants.Clear();
            _teamAssignments.Clear();
            // 이전 내구도 딕셔너리 비우기 추가
            _previousPartDurability.Clear();
            // <<< 방어 기록 HashSet 비우기 >>>
            _defendedThisTurn.Clear();
            
            // Debug.Log($"[CombatSimulatorService] 전투 종료: {_battleName} (ID: {_currentBattleId}, 결과: {result})");
            Debug.Log($"[CombatSimulatorService] 전투 종료: {_battleName} (ID: {_currentBattleId}, 결과: {result})");
            
            _currentBattleId = null;
            _battleName = null;
        }
        
        /// <summary>
        /// 다음 턴으로 진행 (AP 회복 로직 추가)
        /// </summary>
        public bool ProcessNextTurn()
        {
            if (!_isInCombat)
            {
                // Debug.LogWarning("[CombatSimulatorService] 진행 중인 전투가 없습니다.");
                Debug.LogWarning("[CombatSimulatorService] 진행 중인 전투가 없습니다.");
                return false;
            }
            
            _defendedThisTurn.Clear();
            
            if (_currentTurn > 0 && _currentActiveUnit != null)
            {
                var turnEndEvent = new CombatSessionEvents.TurnEndEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
                _eventBus.Publish(turnEndEvent);
            }
            
            _currentTurn++;
            _currentActiveUnit = GetNextActiveUnit();
            if (_currentActiveUnit == null)
            {
                Debug.LogWarning("[CombatSimulatorService] 활성화 가능한 유닛이 없습니다. 전투를 종료합니다."); // Keep Debug for editor warning?
                EndCombat();
                return false;
            }

            // <<<-- 수정 시작: 턴 시작 이벤트 발행 및 AP 회복/로깅 -->>>
            // 턴 시작 이벤트 먼저 발행
            var turnStartEvent = new CombatSessionEvents.TurnStartEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
            _eventBus.Publish(turnStartEvent);

            // <<< 재장전 완료 체크 로직 수정: 모든 참가자 확인 >>>
            foreach (var unit in _participants) // 모든 참가자 순회
            {
                if (unit != null && unit.IsOperational) // 유닛이 유효하고 작동 중일 때만
                {
                    // 로그 추가: 어떤 유닛의 재장전을 체크하는지 명시
                    // _textLogger.TextLogger.Log($"턴 시작 시 [{unit.Name}] 재장전 상태 체크...", LogLevel.Debug); // 너무 빈번할 수 있으므로 주석 처리

                    foreach (var weapon in unit.GetAllWeapons()) // 해당 유닛의 모든 무기 순회
                    {
                        if (weapon.IsReloading && weapon.CheckReloadCompletion(_currentTurn))
                        {
                             // 로그 추가: 재장전 완료 처리 로그 강화
                            _textLogger.TextLogger.Log($"[{unit.Name}]의 [{weapon.Name}] 재장전 완료 감지 및 처리 시작 (턴: {_currentTurn}).", LogLevel.Info);
                            weapon.FinishReload();
                            // TODO: ReloadCompleteEvent 발행 고려
                        }
                        // CheckReloadCompletion 내부에서 "진행 중" 로그는 이미 찍힘
                    }
                }
            }
            // <<< 재장전 완료 체크 로직 수정 끝 >>>

            // 그 다음 AP 회복 및 관련 로그 처리 (원래 로직 복구)
            float recoveredAP = _currentActiveUnit.CombinedStats.APRecovery; // AP 회복량 계산 먼저
            _currentActiveUnit.RecoverAPOnTurnStart(); // 실제 AP 회복

            // 상태 효과 처리
            ProcessStatusEffects(_currentActiveUnit);
            
            // <<< 행동 결정 및 수행 로직을 루프로 변경 시작 >>>
            int actionsThisTurn = 0;
            int maxActionsPerTurn = 30; // 턴당 최대 행동 횟수 제한 (임시)

            while (_currentActiveUnit.IsOperational && actionsThisTurn < maxActionsPerTurn)
            {
                // 현재 AP로 가능한 행동 결정
                var actionTuple = DetermineActionForUnit(_currentActiveUnit);
                if (actionTuple.Item1 != default)
                {
                    var determinedAction = actionTuple.Item1;
                    // 결정된 행동 수행 (PerformAction에서 AP 소모 처리)
                    bool actionSuccess = PerformAction(_currentActiveUnit, determinedAction, actionTuple.Item2, actionTuple.Item3, actionTuple.Item4);
                    
                    actionsThisTurn++;

                    // <<< 재장전 성공 시 턴 종료 로직 추가 >>>
                    if (determinedAction == CombatActionEvents.ActionType.Reload && actionSuccess)
                    {
                        _textLogger.TextLogger.Log($"재장전 시작 후 턴 행동 종료.", LogLevel.Info);
                        break; // 재장전 시작했으면 이번 턴 행동 종료
                    }
                    // <<< 재장전 성공 시 턴 종료 로직 추가 끝 >>>

                    if (_currentActiveUnit == null)
                    {
                        Debug.LogWarning("[CombatSimulatorService] 활성화 가능한 유닛이 없습니다. 전투를 종료합니다."); // Keep Debug for editor warning?
                        EndCombat();
                        return false;
                    }

                    // 행동 실패 시 (AP 부족 외 다른 이유) 또는 유닛 비활성화 시 루프 중단
                    if (!actionSuccess || !_currentActiveUnit.IsOperational)
                    {
                         _textLogger.TextLogger.Log($"행동 실패 또는 비활성화로 턴 내 행동 중단.", LogLevel.Warning); // 접두사 제거
                        break;
                    }
                    
                    // TODO: 특정 행동 후에는 턴 강제 종료 같은 로직 추가 가능 (예: 필살기 사용)
                }
                else
                {
                    // 더 이상 수행할 행동이 없음 (AP 부족 또는 가능한 액션 부재)
                     _textLogger.TextLogger.Log($"더 이상 수행할 행동 없음.", LogLevel.Info); // 접두사 제거
                    break; // 루프 종료
                }
            }
            
            if (actionsThisTurn >= maxActionsPerTurn)
            {
                 _textLogger.TextLogger.Log($"턴당 최대 행동 횟수({maxActionsPerTurn}) 도달.", LogLevel.Warning); // 접두사 제거
            }
            // <<< 행동 결정 및 수행 로직을 루프로 변경 끝 >>>

            // 턴 종료 전 모든 유닛 상태 로깅
            CheckBattleEndCondition();
            
            return true;
        }
        
        /// <summary>
        /// 유닛의 특정 행동을 수행합니다.
        /// 파라미터 형식 변경: params object[] -> 명시적 파라미터
        /// </summary>
        public bool PerformAction(ArmoredFrame actor, CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)
        {
            Debug.Log($"[!!!] PerformAction 함수 진입! 액션: {actionType}"); // <<< 진짜 첫 줄 로그

            if (!_isInCombat || actor == null || !actor.IsOperational || actor != _currentActiveUnit)
            {
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 행동 요청입니다.");
                return false;
            }
            
            // AP 비용 계산
            float apCost = 0f;
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    if (weapon != null) apCost = CalculateAttackAPCost(actor, weapon);
                    else apCost = float.MaxValue; // 무기 없으면 AP 무한대
                    break;
                case CombatActionEvents.ActionType.Move:
                    apCost = CalculateMoveAPCost(actor);
                    break;
                case CombatActionEvents.ActionType.Defend:
                    apCost = DEFEND_AP_COST;
                    break;
                // <<< Reload 액션 타입 추가 시작 >>>
                // TODO: ActionType enum에 Reload 추가 필요
                case CombatActionEvents.ActionType.Reload:
                    if (weapon != null) apCost = weapon.ReloadAPCost; // 재장전 AP 비용 사용
                    else apCost = float.MaxValue; // 재장전할 무기 없으면 AP 무한대
                    break;
                // <<< Reload 액션 타입 추가 끝 >>>
                case CombatActionEvents.ActionType.RepairAlly:
                    apCost = 2.5f; // 임시 값
                    break;
                case CombatActionEvents.ActionType.RepairSelf:
                    apCost = 2.0f; // 임시 값
                    break;
                default:
                    apCost = float.MaxValue; // 알 수 없는 행동은 AP 무한대
                    break;
            }

            // AP 부족 확인
            if (!actor.HasEnoughAP(apCost))
            {
                string apFailDescription = "AP 부족"; // 변수명 변경
                // ActionCompletedEvent 생성자는 weapon 파라미터 없음
                // <<< Reload 액션 시 무기 정보 전달 추가 -> 제거하고 원래대로 복구 >>>
                var actionCompletedEvent_ApFail = new CombatActionEvents.ActionCompletedEvent(
                    actor, actionType, false, apFailDescription, _currentTurn,
                    null, null, targetFrame // weapon 인자 제거
                );
                _eventBus.Publish(actionCompletedEvent_ApFail);
                return false; // 행동 실패
            }

            // 이벤트 발생: 액션 시작 (새 파라미터 사용)
            var actionStartEvent = new CombatActionEvents.ActionStartEvent(actor, actionType, _currentTurn, targetFrame, targetPosition, weapon);
            _eventBus.Publish(actionStartEvent);

            bool success = false;
            string resultDescription = "";
            Vector3? finalPosition = null;
            float? distanceMoved = null;

            // 행동 유형에 따른 로직 수행
            try
            {
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                        if (targetFrame != null && weapon != null)
                        {
                            success = PerformAttack(actor, targetFrame, weapon);
                            // PerformAttack 내부에서 탄약/재장전 실패 시 success가 false가 됨
                            resultDescription = success ? $"{targetFrame.Name}에게 {weapon.Name}(으)로 공격 성공" : $"{targetFrame.Name}에게 {weapon.Name}(으)로 공격 실패";
                            if (!success && !weapon.HasAmmo()) resultDescription += " (탄약 부족)";
                            else if (!success && weapon.IsReloading) resultDescription += " (재장전 중)";
                        }
                        else
                        {
                            resultDescription = "공격 대상 또는 무기 정보 없음";
                            success = false;
                            Debug.LogError("Attack action requires a target frame and weapon.");
                        }
                        break;
                    case CombatActionEvents.ActionType.Move:
                        if (targetPosition.HasValue)
                        {
                            var moveResult = PerformMoveToPosition(actor, targetPosition.Value);
                            success = moveResult.success;
                            resultDescription = moveResult.description;
                            if (success)
                            {
                                finalPosition = moveResult.newPosition;
                                distanceMoved = moveResult.distanceMoved;
                                // 이동 성공 시 상세 이벤트는 여기서 발행 후 리턴하지 않고, 아래에서 공통으로 발행
                            }
                        }
                        // targetPosition이 없고 targetFrame만 있는 경우 (호환성)
                        else if (targetFrame != null)
                        {
                            Debug.LogWarning($"Move action called without targetPosition, moving towards targetFrame {targetFrame.Name} position instead."); // FrameName -> Name
                            var moveResult = PerformMoveToPosition(actor, targetFrame.Position);
                            success = moveResult.success;
                            resultDescription = moveResult.description;
                            if (success)
                            {
                                finalPosition = moveResult.newPosition;
                                distanceMoved = moveResult.distanceMoved;
                            }
                        }
                        else
                        {
                            resultDescription = "이동 목표 위치 또는 대상 프레임 없음";
                            success = false;
                             Debug.LogError("Move action requires either a target position or a target frame.");
                        }
                    break;
                case CombatActionEvents.ActionType.Defend:
                        // 방어 상태 효과 생성 (1턴 동안 방어력 1.5배)
                        var defenseBoostEffect = new StatusEffect(
                            "Defense Buff", // 효과 이름
                            1, // 지속 턴
                            StatType.Defense, // 대상 스탯
                            ModificationType.Multiplicative, // 수정 방식 (곱셈)
                            1.5f // 수정 값 (1.5배)
                        );
                        // 상태 효과 적용
                        actor.AddStatusEffect(defenseBoostEffect);
                        // 이번 턴에 방어했음을 기록
                    _defendedThisTurn.Add(actor);
                        success = true;
                        resultDescription = "방어 태세 돌입 (방어력 증가)";
                    break;
                    case CombatActionEvents.ActionType.Reload:
                        if (weapon != null)
                        {
                            // StartReload 메서드는 이미 재장전 중이거나 탄약 가득 찼는지 확인
                            success = weapon.StartReload(_currentTurn);
                            if (success)
                            {
                                 // <<< resultDescription에 무기 이름 포함 >>>
                                 resultDescription = $"{weapon.Name} 재장전 시작.";
                                 if (weapon.ReloadTurns == 0)
                                 {
                                     resultDescription = $"{weapon.Name} 즉시 재장전 완료.";
                                 }
                            }
                            else
                            {
                                resultDescription = $"{weapon.Name} 재장전 시작 실패.";
                            }
                        }
                        else
                        {
                            resultDescription = "재장전할 무기 정보 없음";
                            success = false;
                            Debug.LogError("Reload action requires a weapon.");
                        }
                        break;
                    case CombatActionEvents.ActionType.RepairAlly:
                        if (targetFrame != null && targetFrame != actor) // 대상이 있고 자기 자신이 아니어야 함
                        {
                            // TODO: 실제 수리 로직 구현 (예: 대상의 Body 파츠 내구도 회복)
                            success = true; // 임시 성공 처리
                            resultDescription = $"{targetFrame.Name} 수리 시도";
                        }
                        else
                        {
                            resultDescription = "수리 대상 아군 지정 필요";
                            success = false;
                        }
                        break;
                    case CombatActionEvents.ActionType.RepairSelf:
                        // TODO: 실제 자가 수리 로직 구현 (예: 자신의 Body 파츠 내구도 회복)
                        success = true; // 임시 성공 처리
                        resultDescription = "자가 수리 시도";
                        break;
                    default:
                        resultDescription = "알 수 없는 행동 타입";
                        success = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{actor.Name}] {actionType} 행동 중 오류 발생: {ex.Message}");
                resultDescription = $"행동 중 오류 발생: {ex.Message}";
                success = false;
            }

            // 행동 성공 시 AP 소모
            if (success)
            {
                 // <<< AP 소모 로직 통합 (actionType 비교 불필요) >>>
                 // apCost 변수는 switch문에서 이미 올바르게 계산됨
                 actor.ConsumeAP(apCost);
            }

            // 이벤트 발생: 액션 완료 (weapon 인자 제거)
             var actionCompletedEvent = new CombatActionEvents.ActionCompletedEvent(
                 actor,
                 actionType,
                 success,
                 resultDescription, // 여기에 무기 이름 포함됨
                 _currentTurn,
                 finalPosition,
                 distanceMoved,
                 targetFrame // Reload 시에도 targetFrame은 null이 아닐 수 있으므로 원래대로 복구
                 // weapon 인자 제거
             );
            _eventBus.Publish(actionCompletedEvent);

            return success;
        }
        
        /// <summary>
        /// 공격 수행
        /// </summary>
        public bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon)
        {
            Debug.Log("[!!!] PerformAttack 진입 확인!"); // <<< 간단 로그 추가

            // 메서드 시작 지점 로그 (LogLevel 수정)
            // Debug.Log($"시작: {attacker.Name}이(가) {target.Name}을(를) {weapon.Name}(으)로 공격 시도"); // 위 로그로 대체
            Debug.Log($"시작: {attacker.Name}이(가) {target.Name}을(를) {weapon.Name}(으)로 공격 시도");
            
            if (!_isInCombat || attacker != _currentActiveUnit)
            {
                // Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 공격 요청입니다.");
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 공격 요청입니다.");
                return false;
            }
            
            if (target == null || !_participants.Contains(target) || !target.IsOperational)
            {
                // Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 대상입니다.");
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 대상입니다.");
                return false;
            }
            
            if (weapon == null)
            {
                // Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 무기입니다.");
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 무기입니다.");
                return false;
            }
            
            if (_teamAssignments.ContainsKey(attacker) && _teamAssignments.ContainsKey(target) && 
                _teamAssignments[attacker] == _teamAssignments[target])
            {
                // Debug.LogWarning("[CombatSimulatorService] 같은 팀원을 공격할 수 없습니다.");
                Debug.LogWarning("[CombatSimulatorService] 같은 팀원을 공격할 수 없습니다.");
                return false;
            }

            // 공격자와 대상 간 거리 계산
            float distance = Vector3.Distance(attacker.Position, target.Position);

            // 사거리 체크 (선택적 방어 코드)
            if (distance > weapon.Range)
            {
                Debug.LogWarning($"[CombatSimulatorService] 공격 불가: 대상({target.Name})이 무기({weapon.Name}) 사거리({weapon.Range:F1}) 밖에 있습니다 (거리: {distance:F1}).");
                // 필요하다면 여기서 공격 실패 이벤트 발행
                return false; // 공격 실패
            }
            
            // <<< 탄약/재장전 체크 추가 시작 >>>
            if (weapon.IsReloading)
            {
                Debug.LogWarning($"[CombatSimulatorService] 공격 불가: 무기({weapon.Name}) 재장전 중입니다.");
                return false; // 재장전 중에는 공격 불가
            }

            if (!weapon.HasAmmo())
            {
                Debug.LogWarning($"[CombatSimulatorService] 공격 불가: 무기({weapon.Name}) 탄약 부족 (남은 탄약: {weapon.CurrentAmmo}). 재장전 필요.");
                // TODO: 나중에 여기서 'Reload' 액션을 자동으로 제안하거나, 실패 이벤트를 발행할 수 있음
                return false; // 탄약 부족
            }
            // <<< 탄약/재장전 체크 추가 끝 >>>

            // <<< 공격 시도 성공 시 탄약 소모 추가 >>>
            bool attackAttemptValid = true; // 여기까지 왔으면 공격 시도는 유효함
            weapon.ConsumeAmmo(); // 공격 시도 시 탄약 소모 (명중 여부와 관계없이)
            // <<< 공격 시도 성공 시 탄약 소모 추가 끝 >>>

            // <<< 명중 판정 로직 수정 시작 >>>
            float attackerAccuracy = attacker.CombinedStats.Accuracy;
            float targetEvasion = target.CombinedStats.Evasion;
            float weaponAccuracy = weapon.Accuracy;

            // 기본 명중률 계산 (예시: 무기 명중률 * 공격자 명중 스탯 계수 - 대상 회피 스탯 계수)
            // TODO: 이 공식은 밸런싱에 따라 얼마든지 변경 가능
            float accuracyFactor = 1.0f; // 공격자 명중 스탯이 1.0일 때 기준
            float evasionFactor = 1.0f; // 대상 회피 스탯이 1.0일 때 기준
            
            // 공격자 명중 스탯이 높을수록 명중률 증가, 낮을수록 감소 (간단 선형 보정)
            // 예: Acc 1.2면 +20%, 0.8이면 -20%
            float attackerBonus = (attackerAccuracy - 1.0f) * accuracyFactor;
            
            // 대상 회피 스탯이 높을수록 명중률 감소 (간단 선형 감소)
            // 예: Eva 0.3이면 -30%?
            // float targetPenalty = targetEvasion * evasionFactor; 
            float targetPenalty = targetEvasion * 0.5f; // 회피 효과를 절반으로 감소 (밸런스 테스트 필요)
            
            // float finalAccuracy = weaponAccuracy + attackerBonus - targetPenalty; // <<< 제거됨
            float baseHitChance = weaponAccuracy + attackerBonus - targetPenalty; // 스탯 기반 명중률

            // <<< 거리 기반 명중률 보정 추가 시작 >>>
            float rangeModifier = 1.0f; // 기본값: 보정 없음
            float optimalRangeRatio = 0.8f; // 최대 사거리의 80%를 최적 사거리로 가정 (임시)
            float optimalDistance = weapon.Range * optimalRangeRatio;

            if (distance > optimalDistance)
            {
                // 최적 사거리 벗어남 -> 명중률 감소
                float excessDistance = distance - optimalDistance;
                float maxExcessDistance = weapon.Range - optimalDistance; // 최적 사거리부터 최대 사거리까지의 거리

                if (maxExcessDistance > 0) // 0으로 나누기 방지
                {
                    // 거리가 멀어질수록 선형적으로 명중률 감소 (최대 50% 감소 가정 - 임시)
                    float penaltyRatio = excessDistance / maxExcessDistance;
                    float maxPenalty = 0.5f; // 최대 50% 패널티 (임시)
                    rangeModifier = 1.0f - (penaltyRatio * maxPenalty);
                    rangeModifier = Mathf.Clamp(rangeModifier, 0.5f, 1.0f); // 최소 50% 명중률 보장 (임시)
                }
                else
                {
                    // 최적 사거리와 최대 사거리가 같은 경우 (근접 무기 등), 벗어나면 바로 최대 패널티
                    rangeModifier = 0.5f; // 최소 명중률 보장 (임시)
                }
                Debug.Log($"거리 보정: 최적({optimalDistance:F1}) 초과 ({distance:F1}), 보정치: {rangeModifier:P1}");
            }
            else
            {
                Debug.Log($"거리 보정: 최적({optimalDistance:F1}) 이내 ({distance:F1}), 보정 없음");
            }

            float finalAccuracy = baseHitChance * rangeModifier; // 최종 명중률 = 스탯 기반 명중률 * 거리 보정치
            // <<< 거리 기반 명중률 보정 추가 끝 >>>

            // 최종 명중률은 0% ~ 100% 사이로 제한
            // finalAccuracy = Mathf.Clamp01(finalAccuracy); 
            
            // 최소/최대 명중률 설정 (예: 1% ~ 95%)
            const float MIN_HIT_CHANCE = 0.01f; 
            const float MAX_HIT_CHANCE = 0.95f; // 0.99f에서 0.95f로 수정
            
            finalAccuracy = Mathf.Clamp(finalAccuracy, MIN_HIT_CHANCE, MAX_HIT_CHANCE); // 최종 명중률을 1% ~ 95% 사이로 제한

            // 명중 판정 (랜덤 값과 최종 명중률 비교)
            float accuracyRoll = UnityEngine.Random.value; // 0.0 ~ 1.0 사이 랜덤 값
            bool hit = accuracyRoll <= finalAccuracy;

            // 추가 로그: 명중률 계산 과정 및 판정 결과 (LogLevel 수정)
            // Debug.Log($"명중률 계산: 무기({weaponAccuracy:P1}) + 공격자보정({attackerBonus:P1}) - 대상회피({targetPenalty:P1}) = 최종({finalAccuracy:P1})"); // 이전 로그
            // Debug.Log($"명중 판정: {(hit ? "성공" : "실패")} (Roll: {accuracyRoll:F2} vs {finalAccuracy:P1})"); // 이전 로그
            Debug.Log($"명중률 계산: 기본({baseHitChance:P1}) * 거리보정({rangeModifier:P1}) = 최종({finalAccuracy:P1})"); // 수정된 로그
            Debug.Log($"명중 판정: {(hit ? "성공" : "실패")} (Roll: {accuracyRoll:F2} vs {finalAccuracy:P1})");
            // <<< 명중 판정 로직 수정 끝 >>>
            
            // 무기 발사 이벤트 발행
            var weaponFiredEvent = new CombatActionEvents.WeaponFiredEvent(attacker, target, weapon, hit, accuracyRoll);
            _eventBus.Publish(weaponFiredEvent);
            
            // 명중한 경우 데미지 처리
            if (hit)
            {
                // 기본 데미지 계산 (간단한 계산 방식 사용)
                float rawDamage = weapon.Damage;
                
                // 방어력 및 기타 요소를 고려한 최종 데미지 계산
                float calculatedDamage = rawDamage * 0.8f; // 예시로 80%만 적용
                
                // 추가 로그: 데미지 계산 (LogLevel 수정)
                Debug.Log($"데미지 계산: 기본 {rawDamage}, 계산 후 {calculatedDamage}");

                // 타겟팅할 슬롯 결정 (이름 변경 및 로직 수정 필요)
                string targetSlotIdentifier = GetRandomTargetPartSlot(target);
                if (string.IsNullOrEmpty(targetSlotIdentifier))
                {
                    Debug.LogWarning($"[{_currentBattleId}-T{_currentTurn}] {target.Name}: 공격 가능한 파츠 슬롯을 찾을 수 없습니다.");
                    return false; // 공격 실패 처리
                }

                // 슬롯 식별자로 파츠 정보 가져오기 (내구도 등 확인용)
                Part damagedPart = target.GetPart(targetSlotIdentifier);
                if (damagedPart == null) // 혹시 모를 방어 코드
                {
                     Debug.LogError($"[{_currentBattleId}-T{_currentTurn}] 슬롯 \'{targetSlotIdentifier}\'에 해당하는 파츠를 찾을 수 없습니다!");
                     return false;
                }
                float currentDurability = damagedPart.CurrentDurability;
                float maxDurability = damagedPart.MaxDurability;

                // 추가 로그: 타겟 슬롯 (LogLevel 수정)
                Debug.Log($"타겟 슬롯: {targetSlotIdentifier} (현재 내구도: {currentDurability:F0})");
                
                // 데미지 계산 이벤트 발행 (PartType 대신 슬롯 식별자?) -> 일단 PartType 유지
                var damageCalculatedEvent = new DamageEvents.DamageCalculatedEvent(
                    attacker, target, weapon, rawDamage, calculatedDamage, weapon.DamageType, damagedPart.Type); // 파츠 타입 전달
                _eventBus.Publish(damageCalculatedEvent);
                
                // 데미지 적용
                // 임시로 약간의 랜덤 요소 추가 (크리티컬 여부)
                bool isCritical = UnityEngine.Random.value <= 0.2f; // 20% 크리티컬 확률
                float finalDamage = isCritical ? calculatedDamage * 1.5f : calculatedDamage;
                
                // 추가 로그: 크리티컬 및 최종 데미지 (LogLevel 수정)
                Debug.Log($"최종 데미지: {finalDamage} {(isCritical ? "(크리티컬!)" : "")}");
                
                // 추가 로그: 데미지 적용 시도 (LogLevel 수정)
                Debug.Log($"{target.Name}의 {targetSlotIdentifier}에 {finalDamage} 데미지 적용 시도");
                
                // 실제 데미지 적용 (PartType 대신 슬롯 식별자 사용)
                bool partDestroyed = target.ApplyDamage(targetSlotIdentifier, finalDamage, _currentTurn);
                
                // 추가 로그: 파츠 파괴 여부 (LogLevel 수정)
                Debug.Log($"파츠 파괴 여부: {(partDestroyed ? "파괴됨" : "파괴되지 않음")}");

                // 추가 로그: 파츠 남은 내구도 (LogLevel 수정)
                Debug.Log($"파츠 내구도: {currentDurability}/{maxDurability}");
                
                // 데미지 적용 이벤트 발행 (파라미터 수정)
                var damageAppliedEvent = new DamageEvents.DamageAppliedEvent(
                    attacker, target, finalDamage, damagedPart.Type, isCritical, // Use PartType
                    damagedPart.CurrentDurability, damagedPart.MaxDurability);
                _eventBus.Publish(damageAppliedEvent);
                
                if (partDestroyed && damagedPart != null)
                {
                    // PartDestroyedEvent 생성자 파라미터 수정
                    var partDestroyedEvent = new PartEvents.PartDestroyedEvent(
                        target, 
                        damagedPart.Type, // Use PartType
                        attacker, // 공격자 정보 (TODO: null 대신 attacker 전달)
                        $"{targetSlotIdentifier} 파트가 파괴되었습니다.", 
                        $"{target.Name}의 성능이 감소했습니다.");
                    _eventBus.Publish(partDestroyedEvent);
                    
                    Debug.LogError($"파츠 파괴 상세: {target.Name}의 {targetSlotIdentifier} 파츠가 파괴됨 (성능 감소)");
                }
                
                // 타겟이 전투 불능 상태가 된 경우 처리
                if (!target.IsOperational)
                {
                    // Debug.Log($"[CombatSimulatorService] {target.Name}이(가) 전투 불능 상태가 되었습니다.");
                    Debug.LogError($"[CombatSimulatorService] {target.Name}이(가) 전투 불능 상태가 되었습니다.");
                    
                    // 전투 종료 조건 체크
                    CheckBattleEndCondition();
                }
            }
            else
            {
                // 회피 이벤트 발행
                var avoidanceType = DamageEvents.DamageAvoidedEvent.AvoidanceType.Dodge;
                var damageAvoidedEvent = new DamageEvents.DamageAvoidedEvent(
                    attacker, target, weapon.Damage, avoidanceType, 
                    $"{target.Name}이(가) 공격을 회피했습니다."); // description 추가
                _eventBus.Publish(damageAvoidedEvent);
                
                // 추가 로그: 회피 상세 정보 (LogLevel 수정)
                Debug.Log($"회피 상세: {target.Name}이(가) {attacker.Name}의 공격을 회피함 (회피 유형: {avoidanceType})");
            }
            
            // 메서드 종료 로그 (LogLevel 수정)
            Debug.Log($"종료: {attacker.Name}의 공격 {(hit ? "명중" : "빗나감")}");
            // 공격 시도 자체가 유효했다면 성공으로 간주 (명중 여부와 별개)
            return true; // <-- hit 대신 true 반환
        }
        
        /// <summary>
        /// 유닛이 전투 불능 상태인지 확인
        /// </summary>
        public bool IsUnitDefeated(ArmoredFrame unit)
        {
            return unit == null || !unit.IsOperational || !_participants.Contains(unit);
        }
        
        /// <summary>
        /// 전투 참가자 목록 가져오기
        /// </summary>
        public List<ArmoredFrame> GetParticipants()
        {
            return new List<ArmoredFrame>(_participants);
        }
        
        /// <summary>
        /// 아군 목록 가져오기
        /// </summary>
        public List<ArmoredFrame> GetAllies(ArmoredFrame forUnit)
        {
            if (!_isInCombat || !_participants.Contains(forUnit) || !_teamAssignments.ContainsKey(forUnit))
            {
                return new List<ArmoredFrame>();
            }
            
            int teamId = _teamAssignments[forUnit];
            return _participants
                .Where(p => p != forUnit && _teamAssignments.ContainsKey(p) && _teamAssignments[p] == teamId)
                .ToList();
        }
        
        /// <summary>
        /// 적군 목록 가져오기
        /// </summary>
        public List<ArmoredFrame> GetEnemies(ArmoredFrame forUnit)
        {
            if (!_isInCombat || !_participants.Contains(forUnit) || !_teamAssignments.ContainsKey(forUnit))
            {
                return new List<ArmoredFrame>();
            }
            
            int teamId = _teamAssignments[forUnit];
            return _participants
                .Where(p => _teamAssignments.ContainsKey(p) && _teamAssignments[p] != teamId)
                .ToList();
        }
        
        
        /// <summary>
        /// 주어진 유닛의 행동을 결정합니다. (AP 계산 메서드 사용)
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineActionForUnit(ArmoredFrame activeUnit)
        {

            if (activeUnit == null || !activeUnit.IsOperational)
            {
                return (default, null, null, null);
            }

            // 1. 가장 가까운 적 찾기
            List<ArmoredFrame> enemies = GetEnemies(activeUnit);
            if (enemies.Count == 0)
            {
                return (default, null, null, null);
            }

            ArmoredFrame closestEnemy = null;
            float minDistance = float.MaxValue;
            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(activeUnit.Position, enemy.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            // 2. 파일럿 전문화에 따라 행동 결정 로직 분기
            Pilot pilot = activeUnit.Pilot;
            if (pilot == null)
            {
                return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
            }


            (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) resultAction; // 결과 저장 변수
            switch (pilot.Specialization)
            {
                case SpecializationType.MeleeCombat:
                    resultAction = DetermineMeleeCombatAction(activeUnit, closestEnemy, minDistance);
                    break;
                case SpecializationType.RangedCombat:
                    resultAction = DetermineRangedCombatAction(activeUnit, closestEnemy, minDistance);
                    break;
                case SpecializationType.Defense:
                    resultAction = DetermineDefenseAction(activeUnit, closestEnemy, minDistance);
                    break;
                case SpecializationType.Support:
                    resultAction = DetermineSupportAction(activeUnit, closestEnemy, minDistance);
                    break;
                case SpecializationType.StandardCombat:
                default:
                    resultAction = DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
                    break;
            }

            return resultAction; // 최종 결과된 액션 반환
        }

        /// <summary>
        /// 근접 전투 전문화 행동 결정
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineMeleeCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // === 재장전 필요성 체크 (근접도 혹시 모르니) ===
            Weapon weaponToReloadMelee = activeUnit.GetAllWeapons()
                .FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational && w.MaxAmmo > 0 && !w.HasAmmo() && !w.IsReloading);

            if (weaponToReloadMelee != null)
            {
                float reloadAPCost = weaponToReloadMelee.ReloadAPCost;
                if (activeUnit.HasEnoughAP(reloadAPCost))
                {
                    Debug.Log($"    -> Melee AI: {weaponToReloadMelee.Name} 재장전 결정 (AP:{reloadAPCost:F1})");
                    return (CombatActionEvents.ActionType.Reload, null, null, weaponToReloadMelee);
                }
            }


            // 거리 0 또는 매우 가까울 때 처리
            if (minDistance < 0.1f)
            {
                Weapon meleeWeaponClose = activeUnit.EquippedWeapons.FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
                if (meleeWeaponClose != null) // 근접 무기 있음
                {
                    float attackAPCostClose = CalculateAttackAPCost(activeUnit, meleeWeaponClose);
                    // <<< 공격 가능 조건에 재장전/탄약 체크 추가 >>>
                    bool canAttackClose = activeUnit.HasEnoughAP(attackAPCostClose) &&
                                          !meleeWeaponClose.IsReloading &&
                                          meleeWeaponClose.HasAmmo();
                    if (canAttackClose)
                    {
                         Debug.Log($"    -> Melee AI: 근접 공격 결정 (가까움, {meleeWeaponClose.Name})");
                        return (CombatActionEvents.ActionType.Attack, closestEnemy, null, meleeWeaponClose);
                    }
                }
                // 공격 불가 or 무기 없음 -> 방어 시도
                float defendAPCost = DEFEND_AP_COST;
                bool canDefend = activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit);
                if (canDefend)
                {
                    Debug.Log($"    -> Melee AI: 방어 결정 (가까움, 공격 불가)");
                    return (CombatActionEvents.ActionType.Defend, null, null, null);
                }
                else
                {
                    Debug.Log($"    -> Melee AI: 행동 보류 (가까움, 공격/방어 불가)");
                    return (default, null, null, null); // 제자리 대기
                }
            }

            // 일반 로직 (거리가 충분히 있을 때)
            Weapon meleeWeapon = activeUnit.EquippedWeapons.FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
            if (meleeWeapon != null) // 근접 무기 있음
            {
                float attackAPCost = CalculateAttackAPCost(activeUnit, meleeWeapon);
                // <<< 공격 가능 조건에 재장전/탄약 체크 추가 >>>
                bool canAttack = minDistance <= meleeWeapon.Range &&
                                 activeUnit.HasEnoughAP(attackAPCost) &&
                                 !meleeWeapon.IsReloading &&
                                 meleeWeapon.HasAmmo();

                if (canAttack)
                {
                    Debug.Log($"    -> Melee AI: 근접 공격 결정 ({meleeWeapon.Name})");
                    return (CombatActionEvents.ActionType.Attack, closestEnemy, null, meleeWeapon);
                }
            }

            // 2. 이동 AP가 충분하면 적으로 접근 (공격 불가 시)
            float moveAPCost = CalculateMoveAPCost(activeUnit);
            bool canMove = activeUnit.HasEnoughAP(moveAPCost);
            if (canMove)
            {
                Debug.Log($"    -> Melee AI: 적에게 접근 이동 결정");
                return (CombatActionEvents.ActionType.Move, closestEnemy, closestEnemy.Position, null);
            }

            // 3. 방어 시도 (공격/이동 불가 시)
            float defendAPCostFinal = DEFEND_AP_COST;
            bool canDefendFinal = activeUnit.HasEnoughAP(defendAPCostFinal) && !_defendedThisTurn.Contains(activeUnit);
            if (canDefendFinal)
            {
                Debug.Log($"    -> Melee AI: 방어 결정 (공격/이동 불가)");
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }


            Debug.Log($"    -> Melee AI: 수행 가능한 행동 없음");
            return (default, null, null, null);
        }

        /// <summary>
        /// 원거리 전투 전문화 행동 결정
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineRangedCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // === 재장전 필요성 체크 (최우선) ===
            Weapon weaponToReloadRanged = activeUnit.GetAllWeapons()
                .Where(w => w.Type != WeaponType.Melee && w.IsOperational && w.MaxAmmo > 0 && !w.HasAmmo() && !w.IsReloading)
                .OrderByDescending(w => w.Damage) // 데미지 높은 무기 먼저 재장전
                .FirstOrDefault();

            if (weaponToReloadRanged != null)
            {
                float reloadAPCost = weaponToReloadRanged.ReloadAPCost;
                if (activeUnit.HasEnoughAP(reloadAPCost))
                {
                    Debug.Log($"    -> Ranged AI: {weaponToReloadRanged.Name} 재장전 결정 (AP:{reloadAPCost:F1})");
                    return (CombatActionEvents.ActionType.Reload, null, null, weaponToReloadRanged);
                }
                else
                {
                     Debug.Log($"    -> Ranged AI: {weaponToReloadRanged.Name} 재장전 필요하나 AP 부족 ({activeUnit.CurrentAP:F1}/{reloadAPCost:F1}).");
                     // 재장전 못하면 일단 다른 행동 고려
                }
            }


            // 1. 사용 가능한 원거리 무기 찾기 (재장전 중 아니고 탄약 있는 것 중)
            Weapon preferredWeapon = activeUnit.GetAllWeapons()
                .Where(w => w.Type != WeaponType.Melee && w.IsOperational && !w.IsReloading && w.HasAmmo()) // 재장전X, 탄약O 추가
                .OrderByDescending(w => w.Damage)
                .FirstOrDefault();

            if (preferredWeapon == null) {
                Debug.LogWarning($"    -> Ranged AI: 사용 가능한 (즉시 발사 가능한) 원거리 무기 없음. 표준 로직 시도.");
                return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance); // 무기 없으면 표준 행동
            }

            float attackAPCost = CalculateAttackAPCost(activeUnit, preferredWeapon);
            float moveAPCost = CalculateMoveAPCost(activeUnit);
            float optimalRange = preferredWeapon.Range * OPTIMAL_RANGE_FACTOR;
            float defendAPCost = DEFEND_AP_COST;

            // 2. 공격 가능? (사거리, AP)
            bool isInRange = minDistance < preferredWeapon.Range + 0.001f;
            bool hasAttackAP = activeUnit.HasEnoughAP(attackAPCost);
            if (isInRange && hasAttackAP)
            {
                Debug.Log($"    -> Ranged AI: 원거리 공격 결정 ({preferredWeapon.Name})");
                return (CombatActionEvents.ActionType.Attack, closestEnemy, null, preferredWeapon);
            }

            // 3. 공격 불가 & 이동 가능?
            bool hasMoveAP = activeUnit.HasEnoughAP(moveAPCost);
            if (hasMoveAP)
            {
                // 3-1. 너무 가까움?
                bool isTooClose = minDistance < MIN_RANGED_SAFE_DISTANCE;
                if (isTooClose)
                {
                    Vector3 retreatDirection;
                    if (minDistance < 0.01f) {
                        retreatDirection = Vector3.back;
                    } else {
                        retreatDirection = (activeUnit.Position - closestEnemy.Position).normalized;
                    }
                    Vector3 retreatTargetPosition = activeUnit.Position + retreatDirection * activeUnit.CombinedStats.Speed;
                     Debug.Log($"    -> Ranged AI: 너무 가까움, 후퇴 이동 결정.");
                    return (CombatActionEvents.ActionType.Move, closestEnemy, retreatTargetPosition, null);
                }

                // 3-2. 너무 멈?
                bool isTooFar = minDistance > optimalRange;
                if (isTooFar) // else if 에서 if로 변경 (중첩 구조 명확화)
                {
                    float distanceToOptimal = minDistance - optimalRange;
                    // 3-2-1. 최적 거리에 거의 도달?
                    bool nearOptimal = distanceToOptimal < 0.1f;
                    if (nearOptimal)
                    {
                        // 최적 거리에 가까우면 공격/방어 시도 후 안되면 대기
                        Debug.Log($"    -> Ranged AI: 최적 거리에 가까움. 공격/방어 시도.");
                        if (activeUnit.HasEnoughAP(attackAPCost)) {
                            Debug.Log($"        -> 공격 시도 ({preferredWeapon.Name})");
                            return (CombatActionEvents.ActionType.Attack, closestEnemy, null, preferredWeapon);
                        }
                        bool canDefendNear = activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit);
                        if (canDefendNear) {
                             Debug.Log($"        -> 방어 결정.");
                            return (CombatActionEvents.ActionType.Defend, null, null, null);
                        }
                        else {
                             Debug.Log("        -> 공격/방어 불가. 행동 보류.");
                            return (default, null, null, null);
                        }
                    }
                    // 3-2-2. 아직 멀었으면 접근
                    else {
                         Debug.Log($"    -> Ranged AI: 너무 멈, 최적 거리로 접근 이동 결정.");
                        Vector3 directionToEnemy = (closestEnemy.Position - activeUnit.Position).normalized;
                        float distanceToMove = Mathf.Min(activeUnit.CombinedStats.Speed, distanceToOptimal);
                        Vector3 approachTargetPosition = activeUnit.Position + directionToEnemy * distanceToMove;
                        return (CombatActionEvents.ActionType.Move, closestEnemy, approachTargetPosition, null);
                    }
                }
                 // 3-3. 그 외 (적정 거리인데 공격 AP 부족)
                 Debug.Log($"    -> Ranged AI: 적정 거리이나 공격 AP 부족. 행동 보류 시도.");
                 // return (default, null, null, null); // 바로 보류하지 않고 방어 시도
            }

            // 4. 방어 시도 (공격/이동 불가 또는 안함 시)
            bool canDefend = activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit);
            if (canDefend)
            {
                Debug.Log($"    -> Ranged AI: 방어 결정 (공격/이동 불가)");
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            // 5. 수행 가능한 행동 없음
            Debug.LogWarning($"    -> Ranged AI: 수행 가능한 행동 없음.");
            return (default, null, null, null);
        }

        /// <summary>
        /// 방어 전문화 행동 결정
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineDefenseAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            float defendAPCost = DEFEND_AP_COST;

            // 1. 방어 가능?
            bool canDefend = activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit);
            if (canDefend)
            {
                 Debug.Log($"    -> Defense AI: 방어 결정 (AP:{defendAPCost:F1}).");
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            // === 2. 방어 불가 시 재장전 필요성 체크 ===
            Weapon weaponToReloadDefense = activeUnit.GetAllWeapons()
                .Where(w => w.IsOperational && w.MaxAmmo > 0 && !w.HasAmmo() && !w.IsReloading)
                .OrderByDescending(w => w.Damage)
                .FirstOrDefault();

            if (weaponToReloadDefense != null)
            {
                float reloadAPCost = weaponToReloadDefense.ReloadAPCost;
                if (activeUnit.HasEnoughAP(reloadAPCost))
                {
                    Debug.Log($"    -> Defense AI: {weaponToReloadDefense.Name} 재장전 결정 (방어 불가, AP:{reloadAPCost:F1})");
                    return (CombatActionEvents.ActionType.Reload, null, null, weaponToReloadDefense);
                }
            }

            // 3. 방어/재장전 불가 시 표준 로직 시도 (선택 사항)
            // return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance); // 필요 시 주석 해제

            Debug.Log($"    -> Defense AI: 방어/재장전 불가. 행동 보류.");
            return (default, null, null, null);
            }

        // Standard Combat Action Logic (Ensure this method exists)
        /// <summary>
        /// 표준 전투 전문화 행동 결정 (개선된 이동 로직 적용 + 재장전 로직 추가)
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineStandardCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // === 재장전 필요성 체크 (최우선 순위?) ===
            // 사용 가능한 무기 중 탄약 없고 재장전 필요한 무기 찾기 (가장 데미지 높은 무기 우선? 또는 현재 장착? 일단 아무거나)
            Weapon weaponToReload = activeUnit.GetAllWeapons()
                .Where(w => w.IsOperational && w.MaxAmmo > 0 && !w.HasAmmo() && !w.IsReloading)
                .OrderByDescending(w => w.Damage) // 데미지 높은 무기 먼저 재장전 시도 (예시)
                .FirstOrDefault();

            if (weaponToReload != null)
            {
                float reloadAPCost = weaponToReload.ReloadAPCost;
                if (activeUnit.HasEnoughAP(reloadAPCost))
                {
                    Debug.Log($"    -> AI 결정: {weaponToReload.Name} 재장전 (AP:{reloadAPCost:F1})");
                    // TODO: ActionType enum에 Reload 추가 필요
                    return (CombatActionEvents.ActionType.Reload, null, null, weaponToReload);
                }
                else
                {
                    Debug.Log($"    -> AI 결정: {weaponToReload.Name} 재장전 필요하나 AP 부족 ({activeUnit.CurrentAP:F1}/{reloadAPCost:F1}).");
                    // 재장전 못하면 다른 행동 고려 (아래 로직 계속 진행)
                }
            }


            // === 1. 근접 공격 시도 ===
            Weapon meleeWeapon = activeUnit.GetAllWeapons().FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
            if (meleeWeapon != null) // 근접 무기가 있는 경우
            {
                float meleeAttackAPCost = CalculateAttackAPCost(activeUnit, meleeWeapon);
                bool isInMeleeRange = minDistance <= meleeWeapon.Range;
                // <<< 재장전/탄약 체크 추가 >>>
                bool canMeleeAttack = isInMeleeRange &&
                                      activeUnit.HasEnoughAP(meleeAttackAPCost) &&
                                      !meleeWeapon.IsReloading && // 근접 무기도 재장전 개념이 있을 수 있음 (미래 확장성)
                                      meleeWeapon.HasAmmo();      // 근접 무기도 탄약 개념이 있을 수 있음 (미래 확장성)

                if (canMeleeAttack)
                {
                    Debug.Log($"    -> AI 결정: 근접 공격 ({meleeWeapon.Name}, AP:{meleeAttackAPCost:F1})");
                    return (CombatActionEvents.ActionType.Attack, closestEnemy, null, meleeWeapon);
                }
            }


            // === 2. 원거리 공격 시도 ===
            // 사용 가능한 원거리 무기 목록 가져오기 (AP, 사거리 고려 안 함, 재장전X, 탄약O 조건 추가)
            var availableRangedWeapons = activeUnit.GetAllWeapons()
                .Where(w => w.Type != WeaponType.Melee && w.IsOperational && !w.IsReloading && w.HasAmmo()) // 재장전X, 탄약O 조건 추가
                .OrderByDescending(w => w.Damage); // 데미지 높은 순 정렬 (예시)

            Weapon bestRangedWeapon = null;
            foreach (var rangedWeapon in availableRangedWeapons)
            {
                // 사거리 및 AP 체크
                bool isInRange = minDistance < rangedWeapon.Range + 0.001f;
                float attackAPCost = CalculateAttackAPCost(activeUnit, rangedWeapon);
                bool hasAttackAP = activeUnit.HasEnoughAP(attackAPCost);

                if (isInRange && hasAttackAP)
                {
                    bestRangedWeapon = rangedWeapon; // 공격 가능한 최적 무기 발견
                    break;
                }
            }

            if (bestRangedWeapon != null) // 공격 가능한 원거리 무기 찾음
            {
                Debug.Log($"    -> AI 결정: 원거리 공격 ({bestRangedWeapon.Name}, AP:{CalculateAttackAPCost(activeUnit, bestRangedWeapon):F1})");
                return (CombatActionEvents.ActionType.Attack, closestEnemy, null, bestRangedWeapon);
            }


            // === 3. 이동 결정 (공격 불가 시) ===
            float moveAPCost = CalculateMoveAPCost(activeUnit);
            bool canMove = activeUnit.HasEnoughAP(moveAPCost);

            if (canMove)
            {
                // 이동 로직: 원거리 가능 여부에 따라 분기
                Weapon anyRangedWeapon = activeUnit.GetAllWeapons().FirstOrDefault(w => w.Type != WeaponType.Melee && w.IsOperational);

                if (anyRangedWeapon != null) // 원거리 무기가 하나라도 있으면 원거리 유닛처럼 행동
                {
                    float optimalRange = anyRangedWeapon.Range * OPTIMAL_RANGE_FACTOR; // 가장 좋은 원거리 무기 기준? 일단 첫번째 것으로.

                    // 너무 가까움 (후퇴)
                    if (minDistance < MIN_RANGED_SAFE_DISTANCE)
                    {
                        Vector3 retreatDirection = (minDistance < 0.01f) ? Vector3.back : (activeUnit.Position - closestEnemy.Position).normalized;
                        Vector3 retreatTargetPosition = activeUnit.Position + retreatDirection * activeUnit.CombinedStats.Speed;
                        return (CombatActionEvents.ActionType.Move, closestEnemy, retreatTargetPosition, null);
                    }
                    // 너무 멈 (접근)
                    else if (minDistance > optimalRange)
                    {
                        Vector3 directionToEnemy = (closestEnemy.Position - activeUnit.Position).normalized;
                        float distanceToOptimal = minDistance - optimalRange;
                        float distanceToMove = Mathf.Min(activeUnit.CombinedStats.Speed, distanceToOptimal);
                         // 접근 목표 지점 계산 (최적 거리까지만 가도록)
                        Vector3 approachTargetPosition = activeUnit.Position + directionToEnemy * distanceToMove;
                        return (CombatActionEvents.ActionType.Move, closestEnemy, approachTargetPosition, null);
                    }
                     // 적정 거리면 이동하지 않음 (아래 방어 로직으로 넘어감)
                    else
                    {
                    }
                }
                else // 근접 무기만 있음
                {
                    float meleeOptimalRange = meleeWeapon?.Range ?? 1.0f; // 근접 무기 사거리 (없으면 기본값)

                    // 너무 멈 (접근)
                    if (minDistance > meleeOptimalRange)
                    {
                        // 근접 유닛은 그냥 적 위치로 바로 접근
                        return (CombatActionEvents.ActionType.Move, closestEnemy, closestEnemy.Position, null);
                    }
                     // 근접 사거리 내면 이동하지 않음 (이미 공격 실패했으므로 아래 방어로 넘어감)
                    else
                    {
                    }
                }
            } // End of if(canMove)


            // === 4. 방어 시도 (공격 및 이동 불가/안함 시) ===
            float defendAPCost = DEFEND_AP_COST;
            bool canDefend = activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit);
            if (canDefend)
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            // === 5. 수행 가능한 행동 없음 ===
            return (default, null, null, null);
        }

        /// <summary>
        /// 지원 전문화 행동 결정
        /// 반환 튜플 형식 변경: (ActionType, TargetFrame, TargetPosition?, Weapon)
        /// </summary>
        private (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon) DetermineSupportAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {

            // 1. 가장 체력이 낮은 아군 찾기
            ArmoredFrame lowestAlly = FindLowestHPDamagedAlly(activeUnit);

            // 2. 자가 수리 또는 아군 수리 능력/AP 확인
            float repairSelfAPCost = 2.0f; // 임시 값
            float repairAllyAPCost = 2.5f; // 임시 값

            Part selfBodyPart = activeUnit.GetPart("Body");
            bool canRepairSelf = selfBodyPart != null && selfBodyPart.IsOperational && selfBodyPart.CurrentDurability < selfBodyPart.MaxDurability && activeUnit.HasEnoughAP(repairSelfAPCost);
            bool canRepairAlly = lowestAlly != null && activeUnit.HasEnoughAP(repairAllyAPCost);

            // 3. 행동 결정 (우선순위: 아군 수리 > 자가 수리 > 표준 행동)
            if (canRepairAlly)
            {
                 // Debug.Log($"  -> Support: 아군 수리 결정 ..."); // 기존 로그 대체
                return (CombatActionEvents.ActionType.RepairAlly, lowestAlly, null, null);
            }
            else if (canRepairSelf)
            {
                 // Debug.Log($"  -> Support: 자가 수리 결정 ..."); // 기존 로그 대체
                return (CombatActionEvents.ActionType.RepairSelf, activeUnit, null, null);
            }
            else
            {
                 // Debug.Log($"  -> Support: 수리 행동 불가, 표준 로직 사용."); // 기존 로그 대체
                return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
            }
        }

        // 가장 체력이 낮은 손상된 아군 찾는 헬퍼 메서드 (예시) - Body 파츠 기준
        private ArmoredFrame FindLowestHPDamagedAlly(ArmoredFrame unit)
        {
            ArmoredFrame lowestAlly = null;
            float lowestHealthPercentage = float.MaxValue;

            foreach (var ally in GetAllies(unit))
            {
                if (ally == unit) continue; // 자기 자신 제외

                // Check Body part durability for health percentage
                Part allyBodyPart = ally.GetPart("Body");
                if (allyBodyPart != null && allyBodyPart.IsOperational && allyBodyPart.MaxDurability > 0)
                {
                    float currentHealthPercentage = allyBodyPart.CurrentDurability / allyBodyPart.MaxDurability;
                    if (currentHealthPercentage < 1.0f && currentHealthPercentage < lowestHealthPercentage)
                    {
                        lowestHealthPercentage = currentHealthPercentage;
                        lowestAlly = ally;
                    }
                }
            }
            return lowestAlly;
        }
        
        /// <summary>
        /// 사용 가능한 최적의 원거리 무기를 찾습니다.
        /// (수정: 모든 사용 가능한 원거리 무기를 확인하여 사거리/AP 조건을 만족하는 첫 번째 무기 반환)
        /// </summary>
        private Weapon FindBestRangedWeapon(ArmoredFrame activeUnit, float targetDistance)
        {
            // 사용 가능하고, 사거리 내에 있으며, AP가 충분한 첫 번째 원거리 무기를 찾습니다.
            // 필요하다면 나중에 데미지 등으로 정렬하여 '최적' 무기를 찾는 로직으로 개선할 수 있습니다.
            return activeUnit.GetAllWeapons()
                .Where(w => w.Type != WeaponType.Melee && w.IsOperational) // 원거리이고 작동 가능한 무기 필터링
                .FirstOrDefault(w => // 첫 번째로 조건을 만족하는 무기 찾기
                {
                    // 부동 소수점 오차 감안하여 사거리 비교
                    bool inRange = targetDistance < w.Range + 0.001f;
                    if (!inRange) return false; // 사거리 밖이면 다음 무기 확인

                    // AP 비용 계산 및 확인
                    float apCost = CalculateAttackAPCost(activeUnit, w);
                    bool canAfford = activeUnit.HasEnoughAP(apCost);

                    return canAfford; // 사거리 내이고 AP 충분하면 이 무기 선택
                });
        }
        
        #region 유틸리티 메서드
        
        /// <summary>
        /// 고유한 전투 ID 생성
        /// </summary>
        private string GenerateBattleId()
        {
            return $"BATTLE_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        /// <summary>
        /// 팀 할당 처리 - ArmoredFrame의 TeamId를 사용하도록 수정
        /// </summary>
        private void AssignTeams(ArmoredFrame[] participants)
        {
            _teamAssignments.Clear();
            
            // ArmoredFrame 객체의 TeamId 프로퍼티를 읽어서 할당
            foreach (var participant in participants)
            {
                if (participant != null) // 참가자가 null이 아닌지 확인
                {
                    _teamAssignments[participant] = participant.TeamId; // ArmoredFrame의 TeamId 사용
                }
                else
                {
                }
            }
        }
        
        /// <summary>
        /// 다음 활성 유닛 선택
        /// </summary>
        private ArmoredFrame GetNextActiveUnit()
        {
            // 현재는 간단하게 처리 (차례대로)
            // 후에 스피드 등의 스탯이나 우선순위를 고려한 로직으로 대체 가능
            int currentIndex = _currentActiveUnit != null ? _participants.IndexOf(_currentActiveUnit) : -1;
            
            // 다음 인덱스부터 순회
            for (int i = 1; i <= _participants.Count; i++)
            {
                int nextIndex = (currentIndex + i) % _participants.Count;
                ArmoredFrame nextUnit = _participants[nextIndex];
                
                if (nextUnit.IsOperational)
                {
                    return nextUnit;
                }
            }
            
            return null; // 활성화 가능한 유닛 없음
        }
        
        /// <summary>
        /// Determines the result of the combat based on the operational status of participants.
        /// This method is now generalized for NvN scenarios.
        /// </summary>
        private CombatSessionEvents.CombatEndEvent.ResultType DetermineCombatResult()
        {
            if (_participants == null || _participants.Count == 0 || _teamAssignments == null || _teamAssignments.Count == 0)
            {
                return CombatSessionEvents.CombatEndEvent.ResultType.Aborted; // Changed Error to Aborted
            }

            // Group participants by team ID and check if they are operational
            var teamsAlive = _participants
                .Where(p => p != null && p.IsOperational && _teamAssignments.ContainsKey(p))
                .GroupBy(p => _teamAssignments[p]) // Group by team ID
                .Select(g => g.Key) // Select the team IDs of teams with at least one survivor
                .ToList();

            int numberOfTeamsAlive = teamsAlive.Count;

            if (numberOfTeamsAlive == 1)
            {
                int winningTeamId = teamsAlive[0];
                
                // Use Victory/Defeat based on Team ID (assuming Team 0 is player/primary)
                 if (winningTeamId == 0) 
                 {
                     return CombatSessionEvents.CombatEndEvent.ResultType.Victory; // Changed Team0Win to Victory
                 }
                 else if (winningTeamId == 1) // Assuming team 1 is the opponent
                 {
                     return CombatSessionEvents.CombatEndEvent.ResultType.Defeat; // Changed Team1Win to Defeat
                 }
                 else
                 {
                     // Handle unexpected winning team ID if necessary (e.g., more than 2 teams)
                     Debug.LogWarning($"Unexpected winning team ID: {winningTeamId}. Defaulting to Draw.");
                     return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
                 }
            }
            else if (numberOfTeamsAlive == 0)
            {
                // No teams remaining - Draw
                return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
            }
            else // numberOfTeamsAlive > 1
            {
                // More than one team still has operational units - Combat is ongoing or forced Draw
                Debug.LogWarning("DetermineCombatResult called when multiple teams are still alive.");
                return CombatSessionEvents.CombatEndEvent.ResultType.Draw; // Default to Draw if forced end with multiple survivors
            }
        }
        
        /// <summary>
        /// Checks if the battle should end based on the number of teams with operational units.
        /// Calls EndCombat if the condition is met.
        /// </summary>
        private void CheckBattleEndCondition()
        {
            if (!_isInCombat || _participants == null || _teamAssignments == null)
            {
                return; 
            }

            // Count how many distinct teams have at least one operational member
            int numberOfTeamsAlive = _participants
                .Where(p => p != null && p.IsOperational && _teamAssignments.ContainsKey(p))
                .Select(p => _teamAssignments[p]) // Get team ID for each operational participant
                .Distinct() // Get unique team IDs
                .Count(); // Count the number of unique teams with survivors

            // End the combat if only 1 or 0 teams remain
            if (numberOfTeamsAlive <= 1)
            {
                // DetermineCombatResult will figure out the exact outcome (Win/Loss/Draw)
                EndCombat(); 
            }
        }
        
        /// <summary>
        /// 랜덤 타겟 파츠 슬롯 식별자를 선택합니다.
        /// </summary>
        private string GetRandomTargetPartSlot(ArmoredFrame target) // 이름 및 반환 타입 변경
        {
            // 작동 중인 모든 파츠 슬롯 목록 가져오기
            List<string> operationalSlots = target.GetAllOperationalPartSlots();
            
            if (operationalSlots == null || operationalSlots.Count == 0)
            {
                Debug.LogWarning($"[{_currentBattleId}-T{_currentTurn}] {target.Name}: 작동 중인 파츠 슬롯이 없습니다.");
                return null; // 공격 가능한 파츠 없음
            }
            
            // 랜덤 인덱스 선택
            int randomIndex = UnityEngine.Random.Range(0, operationalSlots.Count);
            return operationalSlots[randomIndex]; // 슬롯 식별자 반환
        }

        /// <summary>
        /// 전투 시작 시 모든 참가자의 초기 파츠 내구도를 기록합니다.
        /// </summary>
        private void InitializePreviousDurability()
        {
            _previousPartDurability.Clear(); // 시작 시 클리어 추가
            foreach (var unit in _participants)
            {
                foreach (var kvp in unit.Parts) // Parts 딕셔너리 사용
                {
                    string slotId = kvp.Key;
                    Part part = kvp.Value;
                    if (part != null && part.IsOperational)
                    {
                        var key = (unit, slotId); // 키 생성 시 슬롯 ID 사용
                        _previousPartDurability[key] = part.CurrentDurability;
                    }
                }
            }
        }

        /// <summary>
        /// 지정된 유닛의 활성 상태 효과를 처리합니다 (틱 기반 효과 적용, 지속시간 감소 등).
        /// </summary>
        private void ProcessStatusEffects(ArmoredFrame unit)
        {
            if (unit == null || !unit.IsOperational) return;

            // 상태 효과 목록 복사 (반복 중 수정 대비)
            var activeEffects = new List<StatusEffect>(unit.ActiveStatusEffects); 
            
            if(activeEffects == null || activeEffects.Count <= 0) return;

            foreach (var effect in activeEffects)
            {
                // 틱 기반 효과 처리 - TickEffectType 대신 EffectName 비교 (임시)
                if (effect.EffectName.Contains("DamageOverTime")) // 이름 기반 임시 처리
                {
                    float damageAmount = effect.TickValue; 
                    _eventBus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                    _eventBus.Publish(new DamageEvents.DamageAppliedEvent(null, unit, damageAmount, PartType.Body, false, 0f, 0f)); 
                }
                else if (effect.EffectName.Contains("RepairOverTime")) // 이름 기반 임시 처리
                {
                    float repairAmount = effect.TickValue; 
                    _eventBus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                }

                // 지속 시간 감소 및 만료 처리
                if (effect.DurationTurns != -1)
                {
                    effect.DurationTurns--; 

                    if (effect.DurationTurns <= 0)
                    {
                        unit.RemoveStatusEffect(effect.EffectName); 
                        var expiredEventType = effect.EffectName.Contains("DamageOverTime")
                            ? StatusEffectEvents.StatusEffectType.Debuff_Burning
                            : StatusEffectEvents.StatusEffectType.Buff_RepairField;
                        _eventBus.Publish(new StatusEffectEvents.StatusEffectExpiredEvent(unit, expiredEventType, effect.EffectName));
                    }
                    }
                }
        }

        /// <summary>
        /// 이동에 필요한 AP 코스트를 계산합니다.
        /// </summary>
        private float CalculateMoveAPCost(ArmoredFrame unit)
        {
            if (unit == null) return float.MaxValue;

            // 기본 AP 소모량 (나중에 설정 가능)
            float baseMoveCost = 1.0f; // 기본 이동 비용

            // 무게 패널티 계산
            float weightPenaltyFactor = 0.01f; // 무게 1당 AP 소모 증가량 (예시)
            float weightPenalty = unit.TotalWeight * weightPenaltyFactor;

            // 속도 보너스 계산
            float speedBonusFactor = 0.05f; // 속도 1당 AP 소모 감소량 (예시)
            float speedBonus = unit.CombinedStats.Speed * speedBonusFactor;

            // 최종 이동 AP 비용 계산
            float finalCost = baseMoveCost + weightPenalty - speedBonus;

            // 최소 AP 비용 보장 (예: 0.5 AP)
            finalCost = Mathf.Max(0.5f, finalCost);

            // 이동 AP 계산 로그 (주석 처리)
            // Debug.Log($"    {unit.Name} 이동 AP 계산: 무게({unit.TotalWeight:F0}) 페널티 {weightPenalty:F2}, 속도({unit.CombinedStats.Speed:F1}) 보너스 {speedBonus:F2} => 최종 {finalCost:F1}");
            
            return finalCost;
        }

        /// <summary>
        /// 특정 무기로 공격하는 데 필요한 AP 코스트를 계산합니다.
        /// </summary>
        private float CalculateAttackAPCost(ArmoredFrame unit, Weapon weapon)
        {
            if (unit == null || weapon == null) return 999f; // 유닛이나 무기 없으면 매우 큰 값

            // 계산식: 무기 기본 AP 코스트 / 유닛 에너지 효율
            float energyEfficiency = unit.CombinedStats.EnergyEfficiency;
            // 에너지 효율이 0 또는 음수일 경우 분모 문제 방지
            if (energyEfficiency <= 0) energyEfficiency = 0.1f;

            // weapon.WeaponStats.APCost 대신 weapon.BaseAPCost 사용
            float calculatedCost = weapon.BaseAPCost / energyEfficiency;

            // 최소 코스트 제한
            float finalCost = Mathf.Max(0.5f, calculatedCost);

            // 로그 추가 (디버깅용) - LogLevel 수정 및 BaseAPCost 사용
            //_textLogger.TextLogger.Log($"    {unit.Name} 공격({weapon.Name}) AP 계산: 무기({weapon.BaseAPCost:F1}) / 효율({energyEfficiency:F1}) => 최종 {finalCost:F1}", LogLevel.Info); // Debug -> Info

            return finalCost;
        }

        // <<< 파츠 AP 스탯 로깅 헬퍼 메서드 수정 시작 >>>
        /// <summary>
        /// 단일 파츠의 AP 관련 스탯을 로깅합니다.
        /// </summary>
        /// <param name="part">로그할 파츠</param>
        /// <param name="slotIdentifier">파츠가 장착된 슬롯 식별자</param>
        private void LogPartAPStats(Part part, string slotIdentifier) // 슬롯 식별자 파라미터 추가
        {
            if (part != null)
            {
                // PartName 대신 Name, Slot 대신 slotIdentifier, PartStats 사용
                _textLogger.TextLogger.Log($"  {slotIdentifier}({part.Name}): Max +{part.PartStats.MaxAP}, Recovery +{part.PartStats.APRecovery}", LogLevel.Info); // Debug -> Info
            }
            // 슬롯에 파츠가 없는 경우는 로그하지 않음 (선택적)
            // else
            // {
            //    _textLogger.TextLogger.Log($"  {slotIdentifier}: 비어 있음", LogLevel.Info);
            // }
        }
        // <<< 파츠 AP 스탯 로깅 헬퍼 메서드 수정 끝 >>>

        /// <summary>
        /// 지정된 위치로 이동 수행 (PerformAction에서 분리됨)
        /// </summary>
        /// <param name="actor">행위자</param>
        /// <param name="targetPosition">목표 위치</param>
        /// <returns>(성공 여부, 결과 설명, 새 위치, 이동 거리)</returns>
        private (bool success, string description, Vector3? newPosition, float? distanceMoved) PerformMoveToPosition(ArmoredFrame actor, Vector3 targetPosition)
        {
            Vector3 currentPosition = actor.Position;
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);
            
            // 이동 가능 거리 (Speed 스탯 사용)
            float distanceCanMove = actor.CombinedStats.Speed; 

            // 실제 이동 거리 계산 (목표 지점 또는 최대 이동 가능 거리 중 작은 값)
            float distanceToMove = Mathf.Min(distanceCanMove, distanceToTarget);

            // 이동 거리가 매우 작으면 이동하지 않음 (제자리 걸음 방지)
            if (distanceToMove < 0.1f) 
            {
                return (false, "이동할 필요 없음", currentPosition, 0f);
            }

            // 새 위치 계산 및 적용
            Vector3 newPosition = currentPosition + direction * distanceToMove;
            actor.Position = newPosition; // ArmoredFrame의 Position 속성 업데이트

            string description = $"목표 지점 방향으로 {distanceToMove:F1} 만큼 이동. 새 위치: {newPosition}";
            Debug.Log($"[{actor.Name}] 이동 완료. 새 위치: {newPosition}");

            // 상세 정보 포함하여 반환
            return (true, description, newPosition, distanceToMove);
        }

        #endregion // 내부 유틸리티 메서드 끝
    } // CombatSimulatorService 클래스 닫는 중괄호
} // AF.Combat 네임스페이스 닫는 중괄호