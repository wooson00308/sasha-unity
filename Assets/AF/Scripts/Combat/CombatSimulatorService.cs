using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
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
        
        // <<< AP 비용 상수 수정 시작 (이동/공격 제거) >>>
        private const float DEFEND_AP_COST = 1f;
        // <<< AP 비용 상수 수정 끝 >>>
        
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
            
            // <<< 턴 시작 시 방어 기록 초기화 >>>
            _defendedThisTurn.Clear();

            // 현재 턴 종료 이벤트 발행 (첫 턴 제외)
            if (_currentTurn > 0 && _currentActiveUnit != null)
            {
                var turnEndEvent = new CombatSessionEvents.TurnEndEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
                _eventBus.Publish(turnEndEvent);
            }
            
            // 다음 턴으로 진행
            _currentTurn++;
            
            // 다음 활성 유닛 선택
            _currentActiveUnit = GetNextActiveUnit();
            if (_currentActiveUnit == null)
            {
                Debug.LogWarning("[CombatSimulatorService] 활성화 가능한 유닛이 없습니다. 전투를 종료합니다."); // Keep Debug for editor warning?
                EndCombat();
                return false;
            }
            
            // <<< AP 회복 로직 추가 시작 >>>
            float recoveredAP = _currentActiveUnit.CombinedStats.APRecovery; // 회복량 계산
            _currentActiveUnit.RecoverAPOnTurnStart();
            // --- AP 상세 로깅 추가 시작 ---
            _textLogger.TextLogger.Log($"===== {_currentActiveUnit.Name} 턴 시작 AP 상세 =====", LogLevel.Info); // 접두사 제거
            _textLogger.TextLogger.Log($" 프레임 AP: {_currentActiveUnit.CurrentAP:F1} / {_currentActiveUnit.CombinedStats.MaxAP:F1} (+ {recoveredAP:F1} 회복)", LogLevel.Info); // 접두사 없음

            // 파일럿 스탯 로깅 (GetTotalStats 사용)
            var pilotStats = _currentActiveUnit.Pilot.GetTotalStats();
            _textLogger.TextLogger.Log($" 파일럿 스탯: MaxAP {pilotStats.MaxAP}, APRecovery {pilotStats.APRecovery}", LogLevel.Info); // 접두사 없음

            // 파츠별 AP 기여도 로깅 (슬롯 식별자 사용)
            LogPartAPStats(_currentActiveUnit.GetPart("Head"), "Head");
            LogPartAPStats(_currentActiveUnit.GetPart("Body"), "Body");
            LogPartAPStats(_currentActiveUnit.GetPart("Arms"), "Arms"); // Arms는 단일 슬롯으로 가정
            LogPartAPStats(_currentActiveUnit.GetPart("Legs"), "Legs");
            LogPartAPStats(_currentActiveUnit.GetPart("Backpack"), "Backpack");
            LogPartAPStats(_currentActiveUnit.GetPart("Weapon1") as Part, "Weapon1");
            LogPartAPStats(_currentActiveUnit.GetPart("Weapon2") as Part, "Weapon2");
            // TODO: 다른 슬롯이 있다면 추가

            _textLogger.TextLogger.Log("==========================================", LogLevel.Info); // 접두사 없음
            // --- AP 상세 로깅 추가 끝 ---
            _textLogger.TextLogger.Log($"{_currentActiveUnit.Name}: AP +{recoveredAP:F1} 회복 (현재 {_currentActiveUnit.CurrentAP:F1} / {_currentActiveUnit.CombinedStats.MaxAP:F1})", LogLevel.Info); // 접두사 제거
            // <<< AP 회복 로직 추가 끝 >>>

            // 턴 시작 이벤트 발행
            var turnStartEvent = new CombatSessionEvents.TurnStartEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
            _eventBus.Publish(turnStartEvent);
            
            // 상태 효과 처리
            ProcessStatusEffects(_currentActiveUnit);
            
            // <<< 행동 결정 및 수행 로직을 루프로 변경 시작 >>>
            int actionsThisTurn = 0;
            int maxActionsPerTurn = 30; // 턴당 최대 행동 횟수 제한 (임시)

            while (_currentActiveUnit.IsOperational && actionsThisTurn < maxActionsPerTurn)
            {
                // 현재 AP로 가능한 행동 결정
                var actionTuple = DetermineActionForUnit(_currentActiveUnit);

                if (actionTuple != null)
                {
                    // 결정된 행동 수행 (PerformAction에서 AP 소모 처리)
                    bool actionSuccess = PerformAction(_currentActiveUnit, actionTuple.Item1, actionTuple.Item2, actionTuple.Item3);
                    
                    actionsThisTurn++;

                    if (_currentActiveUnit == null)
                    {
                        Debug.LogWarning("[CombatSimulatorService] 활성화 가능한 유닛이 없습니다. 전투를 종료합니다."); // Keep Debug for editor warning?
                        EndCombat();
                        return false;
                    }

                    // 행동 실패 시 (AP 부족 외 다른 이유) 또는 유닛 비활성화 시 루프 중단
                    if (!actionSuccess || !_currentActiveUnit.IsOperational)
                    {
                         _textLogger.TextLogger.Log($"{(_currentActiveUnit != null ? _currentActiveUnit.Name : "Unknown Unit")}: 행동 실패 또는 비활성화로 턴 내 행동 중단.", LogLevel.Warning); // 접두사 제거
                        break;
                    }
                    
                    // TODO: 특정 행동 후에는 턴 강제 종료 같은 로직 추가 가능 (예: 필살기 사용)
                }
                else
                {
                    // 더 이상 수행할 행동이 없음 (AP 부족 또는 가능한 액션 부재)
                     _textLogger.TextLogger.Log($"{(_currentActiveUnit != null ? _currentActiveUnit.Name : "Unknown Unit")}: 더 이상 수행할 행동 없음.", LogLevel.Info); // 접두사 제거
                    break; // 루프 종료
                }
            }
            
            if (actionsThisTurn >= maxActionsPerTurn)
            {
                 _textLogger.TextLogger.Log($"{(_currentActiveUnit != null ? _currentActiveUnit.Name : "Unknown Unit")}: 턴당 최대 행동 횟수({maxActionsPerTurn}) 도달.", LogLevel.Warning); // 접두사 제거
            }
            // <<< 행동 결정 및 수행 로직을 루프로 변경 끝 >>>

            // 턴 종료 전 모든 유닛 상태 로깅
            LogAllUnitDetails();
            
            return true;
        }
        
        /// <summary>
        /// 유닛의 행동을 수행하고 AP를 소모하며, 결과를 반환합니다.
        /// </summary>
        /// <param name="actor">행동 주체</param>
        /// <param name="actionType">수행할 행동 유형</param>
        /// <param name="parameters">행동에 필요한 파라미터 (예: 공격 대상, 무기)</param>
        /// <returns>행동 성공 여부</returns>
        public bool PerformAction(ArmoredFrame actor, CombatActionEvents.ActionType actionType, params object[] parameters)
        {
            if (!_isInCombat || actor == null || !actor.IsOperational)
            {
                Debug.LogWarning($"[CombatSimulatorService] PerformAction 실패: 전투 중이 아니거나 유닛({actor?.Name})이 유효하지 않음.");
                return false;
            }

            // 필요한 파라미터 추출
            ArmoredFrame target = parameters.Length > 0 && parameters[0] is ArmoredFrame ? (ArmoredFrame)parameters[0] : null;
            Weapon weapon = parameters.Length > 1 && parameters[1] is Weapon ? (Weapon)parameters[1] : null;

            float apCost = 0;
            string actionName = actionType.ToString(); // 기본 액션 이름

            // <<< AP 비용 계산 로직 수정 시작 >>>
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    if (target == null || weapon == null)
                    {
                         _textLogger.TextLogger.Log($"{actor.Name}: 공격 파라미터 오류 (대상 또는 무기 누락)", LogLevel.Warning); // 접두사 제거
                        return false;
                    }
                    apCost = CalculateAttackAPCost(actor, weapon); // 수정: 에너지 효율 고려한 AP 비용 계산 메서드 사용
                    actionName = $"공격 ({weapon.Name})"; // 액션 이름 구체화
                    break;
                case CombatActionEvents.ActionType.Move: // CombatActionEvents에 Move가 정의되어 있다면 활성화
                    apCost = CalculateMoveAPCost(actor);
                    actionName = "이동";
                    break;
                case CombatActionEvents.ActionType.Defend:
                    apCost = DEFEND_AP_COST;
                    actionName = "방어";
                    break;
                // case CombatActionEvents.ActionType.Skill: // ActionType에 Skill이 정의되어 있다면 활성화
                //     // 스킬 로직 필요 (AP 비용 계산 등)
                //     _textLogger.TextLogger.Log($"{actor.Name}: 스킬 액션 미구현", LogLevel.Warning); // 접두사 제거
                //     return false;
                // case CombatActionEvents.ActionType.Wait: // ActionType에 Wait가 정의되어 있다면 활성화
                //     apCost = 0; // 대기는 AP 소모 없음
                //      _textLogger.TextLogger.Log($"{actor.Name}: 대기 선택", LogLevel.Info); // 접두사 제거
                //     // 대기 액션은 특별한 처리가 필요 없을 수 있음. 이벤트 발행 후 true 반환
                //     // var waitEvent = new CombatActionEvents.WaitActionEvent(actor, _currentBattleId, _currentTurn); // WaitActionEvent 정의 필요
                //     // _eventBus.Publish(waitEvent);
                //     return true; // AP 소모 없이 즉시 성공 처리
                default:
                     // LogLevel.Warning 대신 LogLevel.Warning 사용
                     _textLogger.TextLogger.Log($"{actor.Name}: 알 수 없는 행동 유형({actionType})", LogLevel.Warning); // 접두사 제거
                    return false;
            }
            // <<< AP 비용 계산 로직 수정 끝 >>>


            // AP 소모 가능 여부 확인
            if (actor.CurrentAP < apCost)
            {
                // --- AP 부족 로그 추가 ---
                _textLogger.TextLogger.Log($"{actor.Name}: [{actionName}] 시도 실패 - AP 부족 (필요: {apCost:F1}, 현재: {actor.CurrentAP:F1})", LogLevel.Warning); // 접두사 제거
                // --- AP 부족 로그 추가 끝 ---
                return false; // AP 부족으로 행동 실패
            }

            // AP 소모
            actor.ConsumeAP(apCost);
            // --- AP 소모 로그 추가 ---
            _textLogger.TextLogger.Log($"{actor.Name}: [{actionName}] 수행 (AP -{apCost:F1}, 남은 AP: {actor.CurrentAP:F1})", LogLevel.Info); // 접두사 제거
             // --- AP 소모 로그 추가 끝 ---

            bool actionResult = false;
            // 각 행동 유형에 따른 실제 로직 수행
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    // actionResult = PerformAttack(actor, target, weapon); // 이전 코드: PerformAttack 결과를 바로 할당
                    PerformAttack(actor, target, weapon); // PerformAttack 호출은 유지
                    actionResult = true; // 공격 시도 자체는 성공으로 처리 (명중 여부와 무관)
                    break;
                case CombatActionEvents.ActionType.Move:
                    if (target != null)
                    {
                        // 이동 로직 구현 (이전에 주석 처리했던 로직 참고)
                        float distanceToTarget = Vector3.Distance(actor.Position, target.Position);
                        // 이동 속도 (CombinedStats.Speed 사용)
                        float moveDistance = actor.CombinedStats.Speed; 
                        
                        // 목표보다 멀리 가지 않도록 실제 이동 거리 제한
                        moveDistance = Mathf.Min(moveDistance, distanceToTarget); 
                        
                        if (moveDistance > 0.01f) // 아주 작은 거리는 무시
                        {
                            Vector3 direction = (target.Position - actor.Position).normalized;
                            Vector3 newPosition = actor.Position + direction * moveDistance;
                            actor.Position = newPosition; // 실제 위치 업데이트

                            _textLogger.TextLogger.Log($"{actor.Name}: {target.Name} 방향으로 {moveDistance:F1} 유닛 이동 -> {newPosition}", LogLevel.Info);
                            actionResult = true;
                        }
                        else
                        {
                             _textLogger.TextLogger.Log($"{actor.Name}: 이동할 거리가 거의 없음.", LogLevel.Warning);
                             actionResult = false; // 이동 안 함
                        }
                    }
                    else
                    {
                        _textLogger.TextLogger.Log($"{actor.Name}: 이동 목표 없음.", LogLevel.Warning);
                        actionResult = false; 
                    }
                    break;
                case CombatActionEvents.ActionType.Defend:
                    // 방어 로직 (예: 상태 효과 부여)
                    // var defendEvent = new CombatActionEvents.DefendActionEvent(actor, _currentBattleId, _currentTurn); // DefendActionEvent 정의 필요
                    // _eventBus.Publish(defendEvent);
                    // TODO: 방어 상태 효과 추가 로직
                    _textLogger.TextLogger.Log($"{actor.Name}: 방어 태세 돌입", LogLevel.Info); // 접두사 제거
                    // <<< 방어 성공 시 기록 추가 >>>
                    _defendedThisTurn.Add(actor);
                    actionResult = true;
                    break;
                    // 다른 행동 유형 처리...
            }

            // 행동 결과에 따른 추가 처리 (예: 이벤트 발행, 로그 기록 등)
            if (actionResult)
            {
                // 공통 행동 성공 이벤트 발행 (필요하다면)
                // var genericActionEvent = new CombatActionEvents.GenericActionEvent(actor, actionType, _currentBattleId, _currentTurn, parameters); // GenericActionEvent 정의 필요
                // _eventBus.Publish(genericActionEvent);

                 // 전투 종료 조건 확인 (행동 후)
                CheckBattleEndCondition();

                // 행동 성공 로그 (이미 위에서 AP 소모 로그와 함께 기록됨)
                //_textLogger.TextLogger.Log($"{actor.Name}: {actionName} 성공", LogLevel.Info); // 접두사 제거
            }
            else
            {
                 _textLogger.TextLogger.Log($"{actor.Name}: {actionName} 실패", LogLevel.Warning); // 접두사 제거
                // 실패 시 AP 롤백? (정책에 따라 결정)
                // actor.RecoverAP(apCost); // 예시: 실패 시 AP 복구
            }


            return actionResult;
        }
        
        /// <summary>
        /// 공격 수행
        /// </summary>
        public bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon)
        {
            // 메서드 시작 지점 로그 (LogLevel 수정)
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
            float targetPenalty = targetEvasion * evasionFactor; 
            
            float finalAccuracy = weaponAccuracy + attackerBonus - targetPenalty;
            
            // 최종 명중률은 0% ~ 100% 사이로 제한
            finalAccuracy = Mathf.Clamp01(finalAccuracy); 

            // 명중 판정 (랜덤 값과 최종 명중률 비교)
            float accuracyRoll = UnityEngine.Random.value; // 0.0 ~ 1.0 사이 랜덤 값
            bool hit = accuracyRoll <= finalAccuracy;

            // 추가 로그: 명중률 계산 과정 및 판정 결과 (LogLevel 수정)
            Debug.Log($"명중률 계산: 무기({weaponAccuracy:P1}) + 공격자보정({attackerBonus:P1}) - 대상회피({targetPenalty:P1}) = 최종({finalAccuracy:P1})");
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
                    $"{target.Name}이(가) 공격을 회피했습니다.");
                _eventBus.Publish(damageAvoidedEvent);
                
                // 추가 로그: 회피 상세 정보 (LogLevel 수정)
                Debug.Log($"회피 상세: {target.Name}이(가) {attacker.Name}의 공격을 회피함 (회피 유형: {avoidanceType})");
            }
            
            // 메서드 종료 로그 (LogLevel 수정)
            Debug.Log($"종료: {attacker.Name}의 공격 {(hit ? "명중" : "빗나감")}");
            return hit;
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
        /// </summary>
        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineActionForUnit(ArmoredFrame activeUnit)
        {
            // --- 초기 설정 및 적 탐색 --- 
            var enemies = GetEnemies(activeUnit);
            if (enemies == null || enemies.Count == 0) { return null; }

            ArmoredFrame closestEnemy = null;
            float minDistance = float.MaxValue;
            Vector3 currentUnitPosition = activeUnit.Position;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsOperational) continue;
                Vector3 enemyPosition = enemy.Position;
                float distance = Vector3.Distance(currentUnitPosition, enemyPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy == null) { return null; }

            // --- 행동 결정 로직 시작 --- 
            float moveAPCost = CalculateMoveAPCost(activeUnit);

            // 2. 파일럿 전문화 타입에 따른 행동 결정
            switch (activeUnit.Pilot.Specialization)
            {
                case SpecializationType.MeleeCombat:
                    _textLogger.TextLogger.Log($"{activeUnit.Name}: MeleeCombat 전문화 행동 결정 시작 (적 거리: {minDistance:F1}).", LogLevel.Info);
                    const float MELEE_ENGAGE_RANGE = 1.5f; // 근접 교전 시작 거리
                    Weapon meleeWeapon = activeUnit.GetAllWeapons().FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
                    float meleeAPCost = (meleeWeapon != null) ? CalculateAttackAPCost(activeUnit, meleeWeapon) : 999f;

                    // 적이 근접 범위 밖에 있고 이동 가능하면 -> 이동
                    if (minDistance > MELEE_ENGAGE_RANGE && activeUnit.HasEnoughAP(moveAPCost))
                    {
                        _textLogger.TextLogger.Log($"  -> MeleeCombat: 적에게 접근 이동 결정 (AP:{moveAPCost:F1}).", LogLevel.Info);
                        return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                            CombatActionEvents.ActionType.Move, closestEnemy, null);
                    }
                    // 적이 근접 범위 안에 있고, Melee 무기가 있고, AP가 충분하면 -> Melee 공격
                    else if (minDistance <= MELEE_ENGAGE_RANGE && meleeWeapon != null && activeUnit.HasEnoughAP(meleeAPCost))
                    {
                        _textLogger.TextLogger.Log($"  -> MeleeCombat: Melee 공격 결정 ({meleeWeapon.Name}, AP:{meleeAPCost:F1}).", LogLevel.Info);
                        return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                            CombatActionEvents.ActionType.Attack, closestEnemy, meleeWeapon);
                    }
                    // 낮은 확률로 Ranged 공격 시도 (임시: 30% 확률)
                    else if (UnityEngine.Random.value < 0.3f)
                    {
                         Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> standardAction = DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance); 
                         if (standardAction != null && standardAction.Item1 == CombatActionEvents.ActionType.Attack) // 공격 액션만 고려
                         { 
                            _textLogger.TextLogger.Log($"  -> MeleeCombat: 낮은 확률로 Ranged 공격 시도.", LogLevel.Info);
                             return standardAction; 
                         }
                    }
                     // 기본 이동 (AP 남으면)
                    else if (activeUnit.HasEnoughAP(moveAPCost)) 
                    { 
                         _textLogger.TextLogger.Log($"  -> MeleeCombat: (기타) 이동 결정.", LogLevel.Info); 
                         return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(CombatActionEvents.ActionType.Move, closestEnemy, null); 
                    }
                    break; // MeleeCombat 끝

                case SpecializationType.RangedCombat:
                    _textLogger.TextLogger.Log($"{activeUnit.Name}: RangedCombat 전문화 행동 결정 시작 (적 거리: {minDistance:F1}).", LogLevel.Info);
                    Weapon rangedWeapon = FindBestRangedWeapon(activeUnit, minDistance); // 사용할 원거리 무기 선택 (아래 헬퍼 메서드 참고)
                    if (rangedWeapon != null)
                    {
                        float attackAPCost = CalculateAttackAPCost(activeUnit, rangedWeapon);
                        float optimalMinRange = rangedWeapon.Range * 0.7f; // 최적 사거리 최소값 (예시)
                        float optimalMaxRange = rangedWeapon.Range;         // 최적 사거리 최대값

                        // 최적 사거리보다 너무 멀면 -> 접근 이동
                        if (minDistance > optimalMaxRange && activeUnit.HasEnoughAP(moveAPCost))
                        {
                             _textLogger.TextLogger.Log($"  -> RangedCombat: 최적 사거리({optimalMinRange:F1}~{optimalMaxRange:F1})보다 멀어서 접근 이동 결정 (AP:{moveAPCost:F1}).", LogLevel.Info);
                return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                                CombatActionEvents.ActionType.Move, closestEnemy, null);
            }
                        // 최적 사거리보다 너무 가까우면 -> 후퇴 이동 (단, AP 충분할 때만)
                        else if (minDistance < optimalMinRange && activeUnit.HasEnoughAP(moveAPCost))
            {
                            _textLogger.TextLogger.Log($"  -> RangedCombat: 최적 사거리({optimalMinRange:F1}~{optimalMaxRange:F1})보다 가까워서 후퇴 이동 결정 (AP:{moveAPCost:F1}).", LogLevel.Info);
                            // 후퇴 로직: closestEnemy 반대 방향으로 이동 (간단 구현)
                            Vector3 retreatDirection = (activeUnit.Position - closestEnemy.Position).normalized;
                            // TODO: 실제 이동 로직을 PerformAction으로 옮기거나, Move 액션에 방향 파라미터 추가 필요. 일단 여기서는 액션 타입만 결정.
                return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                                CombatActionEvents.ActionType.Move, closestEnemy, null); // 임시: 이동 목표는 유지하되, PerformAction에서 방향 처리 필요
            }
                        // 최적 사거리 내에 있고 AP가 충분하면 -> 공격
                        else if (activeUnit.HasEnoughAP(attackAPCost))
            {
                             _textLogger.TextLogger.Log($"  -> RangedCombat: 최적 사거리 내, 공격 결정 ({rangedWeapon.Name}, AP:{attackAPCost:F1}).", LogLevel.Info);
                return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                                CombatActionEvents.ActionType.Attack, closestEnemy, rangedWeapon);
                        }
                    }
                     // 공격/이동 못하면 대기 또는 다른 행동 (현재 로직상 아래 Standard로 빠짐)
                    _textLogger.TextLogger.Log($"  -> RangedCombat: 적합한 행동(공격/이동) 불가. 표준 로직으로.", LogLevel.Info);
                    // fallback to standard combat or other actions if conditions aren't met
                     return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
                    // break; // RangedCombat 끝 - Unreachable code detected 경고 제거

                case SpecializationType.Defense: 
                    _textLogger.TextLogger.Log($"{activeUnit.Name}: Defense 전문화 행동 결정 시작.", LogLevel.Info);
                    // <<< Defense Logic Moved Here >>>
                    bool shouldDefend = false;
                    float defenseThreshold = 1.5f; // 방어 고려 임계값
                    if (closestEnemy != null && closestEnemy.CombinedStats.AttackPower > activeUnit.CombinedStats.Defense * defenseThreshold)
                    {
                        shouldDefend = true;
                        _textLogger.TextLogger.Log($"  -> Defense: 타겟({closestEnemy.Name}) 공격력이 높아 방어 고려.", LogLevel.Info);
                    }

                    if (shouldDefend && activeUnit.HasEnoughAP(DEFEND_AP_COST) && !_defendedThisTurn.Contains(activeUnit))
                    {
                         _textLogger.TextLogger.Log($"  -> Defense: 방어 행동 결정 (AP 소모: {DEFEND_AP_COST}).", LogLevel.Info);
                         return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                             CombatActionEvents.ActionType.Defend, null, null);
                    }
                    else
                    {
                        // 방어하지 않으면 표준 로직으로
                         _textLogger.TextLogger.Log($"  -> Defense: 방어 조건 미충족 또는 불필요. 표준 로직 실행.", LogLevel.Info);
                        return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
                    }
                    // break; // Defense 끝 - Unreachable code detected 경고 제거

                case SpecializationType.StandardCombat:
                case SpecializationType.Support: // 임시: 표준 로직 사용
                case SpecializationType.Engineering: // 임시: 표준 로직 사용
                case SpecializationType.Evasion: // 임시: 표준 로직 사용
                default:
                    _textLogger.TextLogger.Log($"{activeUnit.Name}: {activeUnit.Pilot.Specialization} 전문화 행동 결정 (표준 로직 사용).", LogLevel.Info);
                    // 기존의 거리 기반 Melee/Ranged 선택 로직 사용
                    return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
            }

            // 어떤 행동도 결정되지 못했다면 null 반환
            _textLogger.TextLogger.Log($"{activeUnit.Name}: 최종적으로 수행 가능한 행동 없음.", LogLevel.Warning);
            return null;
        }

        /// <summary>
        /// 표준 전투 행동 로직 (기존 로직 분리)
        /// </summary>
        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineStandardCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            const float MELEE_RANGE_THRESHOLD = 1.5f;
            Weapon selectedWeapon = null;
            float attackAPCost = 999f;

            // 1. 근접 가능하면 Melee 선택 시도
            if (minDistance <= MELEE_RANGE_THRESHOLD)
            {
                Weapon meleeWeapon = activeUnit.GetAllWeapons().FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
                if (meleeWeapon != null)
                {
                    float meleeAPCost = CalculateAttackAPCost(activeUnit, meleeWeapon);
                    if (activeUnit.HasEnoughAP(meleeAPCost))
                    {
                        selectedWeapon = meleeWeapon;
                        attackAPCost = meleeAPCost;
                    }
                }
            }

            // 2. Melee 선택 안됐으면 Ranged 탐색
            if (selectedWeapon == null)
            {
                foreach (var weapon in activeUnit.GetAllWeapons())
                {
                    if (weapon.Type != WeaponType.Melee && weapon.IsOperational)
                    {
                        if (minDistance <= weapon.Range)
                        {
                            float rangedAPCost = CalculateAttackAPCost(activeUnit, weapon);
                            if (activeUnit.HasEnoughAP(rangedAPCost))
                            {
                                selectedWeapon = weapon;
                                attackAPCost = rangedAPCost;
                                break; 
                            }
                        }
                    }
                }
            }

            // 3. 공격 가능하면 공격 반환
            if (selectedWeapon != null)
            {
                 _textLogger.TextLogger.Log($"  -> StandardCombat: 공격 결정 ({selectedWeapon.Name}, AP:{attackAPCost:F1}).", LogLevel.Info);
                 return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                     CombatActionEvents.ActionType.Attack, closestEnemy, selectedWeapon);
            }
            // 4. 공격 불가능하고 이동 가능하면 이동 반환
            else
            {
                float moveAPCost = CalculateMoveAPCost(activeUnit);
                if (activeUnit.HasEnoughAP(moveAPCost))
                {
                     _textLogger.TextLogger.Log($"  -> StandardCombat: 이동 결정 (AP:{moveAPCost:F1}).", LogLevel.Info);
                     return new Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon>(
                         CombatActionEvents.ActionType.Move, closestEnemy, null);
                }
            }
            
            // 5. 아무것도 못하면 null
             _textLogger.TextLogger.Log($"  -> StandardCombat: 수행 가능한 행동 없음.", LogLevel.Warning);
                 return null;
            }
        
        /// <summary>
        /// 사용 가능한 최적의 원거리 무기를 찾습니다.
        /// (임시: 첫번째 사거리 내 + AP 충족 무기 반환)
        /// </summary>
        private Weapon FindBestRangedWeapon(ArmoredFrame activeUnit, float targetDistance)
        {
            foreach (var weapon in activeUnit.GetAllWeapons())
            {
                if (weapon.Type != WeaponType.Melee && weapon.IsOperational)
                {
                    if (targetDistance <= weapon.Range)
                    {
                        float apCost = CalculateAttackAPCost(activeUnit, weapon);
                        if (activeUnit.HasEnoughAP(apCost))
                        {
                            return weapon;
                        }
                    }
                }
            }
            return null;
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
                    Debug.Log($"[AssignTeams] Assigned Team {participant.TeamId} to {participant.Name}"); // 할당 로그 추가
                }
                else
                {
                     Debug.LogWarning("[AssignTeams] Null participant found in the list.");
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
        /// 모든 참가 유닛의 상세 상태를 로깅합니다.
        /// </summary>
        private void LogAllUnitDetails()
        {
            _textLogger.TextLogger.Log("--- Current Units Status ---", LogLevel.Info);
            foreach (var unit in _participants)
            {
                LogUnitDetails(unit);
            }
            _textLogger.TextLogger.Log("----------------------------", LogLevel.Info);
        }

        /// <summary>
        /// 특정 유닛의 상세 상태를 로깅합니다.
        /// </summary>
        private void LogUnitDetails(ArmoredFrame unit)
        {
            if (unit == null) return;

            _textLogger.TextLogger.Log($"  Unit: {unit.Name} {(unit.IsOperational ? "(Operational)" : "(DESTROYED)")} at {unit.Position}", LogLevel.Info);
            var stats = unit.CombinedStats;
            _textLogger.TextLogger.Log($"    Combined Stats: Atk:{stats.AttackPower:F1} / Def:{stats.Defense:F1} / Spd:{stats.Speed:F1} / Acc:{stats.Accuracy:F1} / Eva:{stats.Evasion:F1}", LogLevel.Info);

            _textLogger.TextLogger.Log("    Parts Status:", LogLevel.Info);
            // 파츠 상태 로깅 시 슬롯 식별자 사용
            foreach (var kvp in unit.Parts) // ArmoredFrame의 Parts 딕셔너리 순회
            {
                string slotId = kvp.Key;
                Part part = kvp.Value;

                string status;
                var key = (unit, slotId); // 딕셔너리 키에 슬롯 ID 사용
                float currentDurability = part.CurrentDurability;

                if (part.IsOperational)
                {
                    string changeIndicator = "";
                    if (_previousPartDurability.TryGetValue(key, out float previousDurability))
                    {
                        float durabilityChange = currentDurability - previousDurability;
                        if (Mathf.Abs(durabilityChange) > 0.01f)
                        {
                            changeIndicator = $" [{(durabilityChange > 0 ? "+" : "")}{durabilityChange:F0}]" ; 
                        }
                    }
                    status = $"OK ({currentDurability:F0}/{part.MaxDurability:F0}){changeIndicator}";
                    _previousPartDurability[key] = currentDurability;
                }
                else
                {
                    status = "DESTROYED";
                    _previousPartDurability.Remove(key);
                }
                // 슬롯 식별자와 함께 파츠 상태 로깅
                _textLogger.TextLogger.Log($"      - {slotId} ({part.Name}): {status}", LogLevel.Info);
            }

            _textLogger.TextLogger.Log("    Weapon Status:", LogLevel.Info);
            // Use GetAllWeapons() instead of GetEquippedWeapons()
            var weapons = unit.GetAllWeapons(); 
            if (weapons != null && weapons.Count > 0) // Check for null and count
            {
                foreach (var weapon in weapons)
                {
                    string status = weapon.IsOperational ? "Operational" : "Damaged/Overheated"; 
                    _textLogger.TextLogger.Log($"      - {weapon.Name}: {status}", LogLevel.Info);
                }
            }
            else
            {
                _textLogger.TextLogger.Log("      - No weapons equipped or operational.", LogLevel.Info);
            }
        }
        
        #endregion
        
        #region 내부 유틸리티 메서드 (Private Helper Methods)

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
            
            _textLogger.TextLogger.Log($"[T{_currentTurn}] {unit.Name}: 상태 효과 처리 시작", LogLevel.Info);

            foreach (var effect in activeEffects)
            {
                // 틱 기반 효과 처리 - TickEffectType 대신 EffectName 비교 (임시)
                if (effect.EffectName.Contains("DamageOverTime")) // 이름 기반 임시 처리
                {
                    float damageAmount = effect.TickValue; 
                    _textLogger.TextLogger.Log($"  - {effect.EffectName} ({effect.DurationTurns}턴 남음): {damageAmount} 데미지 적용 시도", LogLevel.Info);
                    
                    // TODO: 실제 데미지 적용 로직 개선 필요
                    
                    _eventBus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                    // DamageAppliedEvent 생성자 수정 (PartType.Body 전달)
                    _eventBus.Publish(new DamageEvents.DamageAppliedEvent(null, unit, damageAmount, PartType.Body, false, 0f, 0f)); 
                }
                else if (effect.EffectName.Contains("RepairOverTime")) // 이름 기반 임시 처리
                {
                    float repairAmount = effect.TickValue; 
                    _textLogger.TextLogger.Log($"  - {effect.EffectName} ({effect.DurationTurns}턴 남음): {repairAmount} 수리 적용 시도", LogLevel.Info);
                    
                    // ArmoredFrame의 Repair 메서드 대신 파츠 순회하며 Repair 호출
                    foreach (var partKvp in unit.Parts)
                    {
                        partKvp.Value?.Repair(repairAmount / unit.Parts.Count); // 임시로 수리량 분배
                    }
                    
                    _eventBus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                    // TODO: RepairEvent 같은 이벤트가 있다면 발행
                }
                // 다른 틱 기반 효과들 처리 추가 가능

                // 지속 시간 감소 및 만료 처리
                if (effect.DurationTurns != -1)
                {
                    effect.DurationTurns--; 

                    if (effect.DurationTurns <= 0)
                    {
                        unit.RemoveStatusEffect(effect.EffectName); 
                        _textLogger.TextLogger.Log($"  - {effect.EffectName}: 효과 만료됨", LogLevel.Info);
                        
                        // StatusEffectExpiredEvent 생성자 수정 (임시 타입 매핑 및 이름 전달)
                        var expiredEventType = effect.EffectName.Contains("DamageOverTime")
                            ? StatusEffectEvents.StatusEffectType.Debuff_Burning // 임시 매핑
                            : StatusEffectEvents.StatusEffectType.Buff_RepairField; // 임시 매핑
                            
                        _eventBus.Publish(new StatusEffectEvents.StatusEffectExpiredEvent(unit, expiredEventType, effect.EffectName));
                    }
                    else
                    {
                        _textLogger.TextLogger.Log($"  - {effect.EffectName}: 남은 턴 {effect.DurationTurns}", LogLevel.Info);
                    }
                }
            }
             _textLogger.TextLogger.Log($"[T{_currentTurn}] {unit.Name}: 상태 효과 처리 완료", LogLevel.Info);
        }

        /// <summary>
        /// 이동에 필요한 AP 코스트를 계산합니다.
        /// </summary>
        private float CalculateMoveAPCost(ArmoredFrame unit)
        {
            if (unit == null) return 999f; // 유닛 없으면 매우 큰 값
            
            // 계산식: 기본 코스트 + (무게 * 무게 계수) - (속도 * 속도 계수)
            float baseCost = 0.5f;
            float weightFactor = 0.01f;
            float speedFactor = 0.05f;
            
            float weightPenalty = unit.TotalWeight * weightFactor;
            float speedBonus = unit.CombinedStats.Speed * speedFactor;
            
            float calculatedCost = baseCost + weightPenalty - speedBonus;
            
            // 최소 코스트 제한
            float finalCost = Mathf.Max(0.5f, calculatedCost);
            
            // 로그 추가 (디버깅용) - LogLevel 수정
             _textLogger.TextLogger.Log($"    {unit.Name} 이동 AP 계산: 무게({unit.TotalWeight:F0}) 페널티 {weightPenalty:F2}, 속도({unit.CombinedStats.Speed:F0}) 보너스 {speedBonus:F2} => 최종 {finalCost:F1}", LogLevel.Info);
            
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
            _textLogger.TextLogger.Log($"    {unit.Name} 공격({weapon.Name}) AP 계산: 무기({weapon.BaseAPCost:F1}) / 효율({energyEfficiency:F1}) => 최종 {finalCost:F1}", LogLevel.Info); // Debug -> Info

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

        #endregion // 내부 유틸리티 메서드 끝
    }
} 