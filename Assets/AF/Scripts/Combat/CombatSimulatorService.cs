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
        /// </summary>
        public bool PerformAction(ArmoredFrame actor, CombatActionEvents.ActionType actionType, params object[] parameters)
        {
            if (!_isInCombat || actor == null || !actor.IsOperational || actor != _currentActiveUnit)
            {
                // Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 행동 요청입니다.");
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 행동 요청입니다.");
                return false;
            }
            
            // <<-- 이벤트 발행 추가 -->>
            var actionStartEvent = new CombatActionEvents.ActionStartEvent(actor, actionType, _currentTurn, parameters);
            _eventBus.Publish(actionStartEvent);
            // <<-- 이벤트 발행 추가 끝 -->>

            bool success = false;
            string resultDescription = "";
            ArmoredFrame target = null;
            Weapon weapon = null;

            // 파라미터 파싱 (안전하게)
            if (parameters.Length > 0 && parameters[0] is ArmoredFrame) target = (ArmoredFrame)parameters[0];
            if (parameters.Length > 1 && parameters[1] is Weapon) weapon = (Weapon)parameters[1];
            // 필요한 경우 다른 파라미터 타입도 파싱

            // AP 비용 계산 및 차감
            float apCost = 0f;
            switch (actionType)
            {
                // ... (각 행동 타입별 AP 비용 계산) ...
                case CombatActionEvents.ActionType.Attack:
                    if (weapon != null) apCost = CalculateAttackAPCost(actor, weapon);
                    break;
                case CombatActionEvents.ActionType.Move:
                    apCost = CalculateMoveAPCost(actor);
                    break;
                case CombatActionEvents.ActionType.Defend:
                    apCost = DEFEND_AP_COST;
                    break;
                // ... 다른 행동 타입들 ...
            }

            if (!actor.ConsumeAP(apCost)) // AP 부족 확인
            {
                resultDescription = "AP 부족";
                success = false;
                // Debug.Log($"[{actor.Name}] {actionType} 행동 실패 (AP 부족)"); // 기존 로그
                // <<-- 이벤트 발행 추가 -->>
                var actionCompletedEvent_ApFail = new CombatActionEvents.ActionCompletedEvent(actor, actionType, success, resultDescription, _currentTurn);
                _eventBus.Publish(actionCompletedEvent_ApFail);
                // <<-- 이벤트 발행 추가 끝 -->>
                return false; // 행동 실패
            }

            // 행동 유형에 따른 로직 수행
            try
            {
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                        if (target != null && weapon != null)
                        {
                            success = PerformAttack(actor, target, weapon);
                            resultDescription = success ? $"{target.Name}에게 {weapon.Name}(으)로 공격 성공" : $"{target.Name}에게 {weapon.Name}(으)로 공격 실패";
                        }
                        else { resultDescription = "공격 대상 또는 무기 정보 없음"; success = false; }
                        break;
                    case CombatActionEvents.ActionType.Move:
                        // 이동 로직 구현 (예시)
                        // Vector3 newPosition = CalculateNewPosition(actor, target); // target이 목표 지점일 경우
                        // actor.MoveTo(newPosition);
                        success = true; // 이동은 일단 성공으로 가정
                        resultDescription = "지정 위치로 이동";
                    break;
                case CombatActionEvents.ActionType.Defend:
                        // 방어 로직 구현 (예시)
                    _defendedThisTurn.Add(actor);
                        success = true;
                        resultDescription = "방어 태세 돌입";
                    break;
                    // ... 다른 행동 타입 처리 ...
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

            // <<-- 기존 로그 호출 제거 -->>
            // _textLogger?.LogAction(_currentBattleId, actor, actionType, success, resultDescription);

            // <<-- 이벤트 발행 추가 -->>
            var actionCompletedEvent = new CombatActionEvents.ActionCompletedEvent(actor, actionType, success, resultDescription, _currentTurn);
            _eventBus.Publish(actionCompletedEvent);
            // <<-- 이벤트 발행 추가 끝 -->>

            return success;
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
            if (activeUnit == null || !activeUnit.IsOperational)
            {
                return null;
            }

            // 1. 가장 가까운 적 찾기
            List<ArmoredFrame> enemies = GetEnemies(activeUnit);
            if (enemies.Count == 0)
            {
                // Log($"[T{_currentTurn}] {activeUnit.Name}: 공격할 적 없음.", LogLevel.Info);
                return null; // 공격할 적 없음
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
                // 파일럿 없으면 표준 로직 사용
                // Debug.Log($"{activeUnit.Name}: 파일럿 없음. 표준 행동 로직 사용."); // 주석처리: 너무 상세할 수 있음
                return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
            }

            // 전문화 기반 행동 결정 로그 (주석 처리)
            // Debug.Log($"{activeUnit.Name}: {pilot.Specialization} 전문화 행동 결정 시작 (적 거리: {minDistance:F1}).");

            switch (pilot.Specialization)
            {
                case SpecializationType.MeleeCombat:
                    return DetermineMeleeCombatAction(activeUnit, closestEnemy, minDistance);
                case SpecializationType.RangedCombat:
                    return DetermineRangedCombatAction(activeUnit, closestEnemy, minDistance);
                case SpecializationType.Defense:
                    return DetermineDefenseAction(activeUnit, closestEnemy, minDistance);
                case SpecializationType.Support:
                    return DetermineSupportAction(activeUnit, closestEnemy, minDistance); // Support 로직 추가 필요
                // 다른 전문화 타입에 대한 로직 추가...
                case SpecializationType.StandardCombat:
                default:
                    // Debug.Log($"  -> {pilot.Specialization}: 표준 로직 사용."); // 주석처리: 너무 상세할 수 있음
                    return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
            }
        }

        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineMeleeCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // 근접 무기 찾기
            Weapon meleeWeapon = activeUnit.EquippedWeapons.FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
            float attackAPCost = meleeWeapon != null ? CalculateAttackAPCost(activeUnit, meleeWeapon) : float.MaxValue;
            float moveAPCost = CalculateMoveAPCost(activeUnit);

            // 1. 근접 공격 사거리 내에 있고 AP가 충분하면 공격
            if (meleeWeapon != null && minDistance <= meleeWeapon.Range && activeUnit.HasEnoughAP(attackAPCost))
            {
                // Debug.Log($"  -> MeleeCombat: 근접 공격 결정 ({meleeWeapon.Name}, AP:{attackAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Attack, closestEnemy, meleeWeapon);
            }
            // 2. 이동 AP가 충분하면 적으로 접근
                    else if (activeUnit.HasEnoughAP(moveAPCost)) 
                    { 
                 // Debug.Log($"  -> MeleeCombat: 적에게 접근 이동 결정 (AP:{moveAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Move, closestEnemy, (Weapon)null);
            }

            // 3. 근접 공격도, 이동도 불가하면 표준 로직 시도 (선택 사항)
            // Debug.LogWarning($"  -> MeleeCombat: 근접 행동 불가, 표준 로직 시도."); // 주석처리
            // return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);

            // 4. 아무것도 할 수 없음
            // Debug.LogWarning($"  -> MeleeCombat: 수행 가능한 행동 없음."); // 주석처리
            return null;
        }

        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineRangedCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // 최적 원거리 무기 찾기
            Weapon bestWeapon = FindBestRangedWeapon(activeUnit, minDistance);
            float attackAPCost = bestWeapon != null ? CalculateAttackAPCost(activeUnit, bestWeapon) : float.MaxValue;
            float moveAPCost = CalculateMoveAPCost(activeUnit);

            // 1. 최적 무기가 있고 사거리 내이며 AP 충분 시 공격
            if (bestWeapon != null && activeUnit.HasEnoughAP(attackAPCost))
            {
                 // Debug.Log($"  -> RangedCombat: 원거리 공격 결정 ({bestWeapon.Name}, AP:{attackAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Attack, closestEnemy, bestWeapon);
            }
            // 2. 이동 AP가 충분하면 적정 사거리 유지하며 이동 (구현 필요)
            // TODO: 적정 사거리 계산 및 해당 위치로 이동하는 로직 추가
            // 현재는 그냥 표준 이동 로직 사용
            else if (activeUnit.HasEnoughAP(moveAPCost))
            {
                 // Debug.Log($"  -> RangedCombat: 이동 결정 (AP:{moveAPCost:F1}) - 현재는 표준 이동."); // 주석처리
                 return Tuple.Create(CombatActionEvents.ActionType.Move, closestEnemy, (Weapon)null);
            }

            // 3. 원거리 공격/이동 불가 시 표준 로직 시도 (선택 사항)
            // Debug.LogWarning($"  -> RangedCombat: 원거리 행동 불가, 표준 로직 시도."); // 주석처리
            // return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);
             
            // 4. 아무것도 할 수 없음
            // Debug.LogWarning($"  -> RangedCombat: 수행 가능한 행동 없음."); // 주석처리
            return null;
        }

        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineDefenseAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            float defendAPCost = DEFEND_AP_COST;

            // 1. 방어 AP가 충분하면 방어 행동
            if (activeUnit.HasEnoughAP(defendAPCost) && !_defendedThisTurn.Contains(activeUnit))
            {
                // Log($"  -> Defense: 방어 결정 (AP:{defendAPCost:F1}).", LogLevel.Info);
                // Debug.Log($"  -> Defense: 방어 결정 (AP:{defendAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Defend, (ArmoredFrame)null, (Weapon)null);
            }
            // 2. 방어 불가 시 표준 로직 시도 (선택 사항)
            // Debug.LogWarning($"  -> Defense: 방어 행동 불가, 표준 로직 시도."); // 주석처리
            // return DetermineStandardCombatAction(activeUnit, closestEnemy, minDistance);

            // 3. 아무것도 할 수 없음
            // Debug.LogWarning($"  -> Defense: 수행 가능한 행동 없음."); // 주석처리
                 return null;
            }

        // Standard Combat Action Logic (Ensure this method exists)
        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineStandardCombatAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // 1. 최적 무기 찾기 (사거리 내)
            Weapon bestWeapon = FindBestRangedWeapon(activeUnit, minDistance);

            // 2. AP 비용 계산
            float attackAPCost = bestWeapon != null ? CalculateAttackAPCost(activeUnit, bestWeapon) : float.MaxValue;
            float moveAPCost = CalculateMoveAPCost(activeUnit);
            float defendAPCost = DEFEND_AP_COST;

            // 3. 행동 결정 (우선순위: 공격 > 이동 > 방어)
            if (bestWeapon != null && activeUnit.HasEnoughAP(attackAPCost))
            {
                 // Debug.Log($"  -> StandardCombat: 공격 결정 ({bestWeapon.Name}, AP:{attackAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Attack, closestEnemy, bestWeapon);
            }
            else if (activeUnit.HasEnoughAP(moveAPCost))
            {
                 // Debug.Log($"  -> StandardCombat: 이동 결정 (AP:{moveAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Move, closestEnemy, (Weapon)null); // 이동 대상은 적
            }
            else if (activeUnit.HasEnoughAP(defendAPCost))
            {
                // Log($"  -> StandardCombat: 방어 결정 (AP:{defendAPCost:F1}).", LogLevel.Debug);
                // Debug.Log($"  -> StandardCombat: 방어 결정 (AP:{defendAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.Defend, (ArmoredFrame)null, (Weapon)null); // 방어는 대상 없음
            }

            // Debug.LogWarning($"  -> StandardCombat: 수행 가능한 행동 없음."); // 주석처리
            return null; // 수행 가능한 행동 없음
        }

        // <<< 서포트 행동 로직 추가 시작 >>>
        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineSupportAction(ArmoredFrame activeUnit, ArmoredFrame closestEnemy, float minDistance)
        {
            // 1. 가장 체력이 낮은 아군 찾기 (자기 자신 제외)
            ArmoredFrame lowestAlly = FindLowestHPDamagedAlly(activeUnit);

            // 2. 자가 수리 또는 아군 수리 능력/AP 확인 (RepairSelf/RepairAlly 액션 타입과 비용 정의 필요)
            float repairSelfAPCost = 2.0f; // 임시 값
            float repairAllyAPCost = 2.5f; // 임시 값

            // Check Body part durability for self-repair condition
            Part selfBodyPart = activeUnit.GetPart("Body");
            bool canRepairSelf = selfBodyPart != null && selfBodyPart.IsOperational && selfBodyPart.CurrentDurability < selfBodyPart.MaxDurability;
            bool canRepairAlly = lowestAlly != null;

            // 3. 행동 결정 (우선순위: 아군 수리 > 자가 수리 > 표준 행동)
            if (canRepairAlly && activeUnit.HasEnoughAP(repairAllyAPCost))
            {
                // Debug.Log($"  -> Support: 아군 수리 결정 ({lowestAlly.Name}, AP:{repairAllyAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.RepairAlly, lowestAlly, (Weapon)null);
            }
            else if (canRepairSelf && activeUnit.HasEnoughAP(repairSelfAPCost))
            {
                // Debug.Log($"  -> Support: 자가 수리 결정 (AP:{repairSelfAPCost:F1})."); // 주석처리
                return Tuple.Create(CombatActionEvents.ActionType.RepairSelf, activeUnit, (Weapon)null); // 대상은 자기 자신
            }
            else
            {
                // 수리 불가 시 표준 행동 로직 사용
                 // Debug.Log($"  -> Support: 수리 행동 불가, 표준 로직 사용."); // 주석처리
                // Corrected method call
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
        // <<< 서포트 행동 로직 추가 끝 >>>
        
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

        #endregion // 내부 유틸리티 메서드 끝
    }
} 