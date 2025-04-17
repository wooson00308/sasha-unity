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
        
        // 전투 상태 관련 필드
        private string _currentBattleId;
        private string _battleName;
        private bool _isInCombat;
        private int _currentTurn;
        private List<ArmoredFrame> _participants;
        private ArmoredFrame _currentActiveUnit;
        private Dictionary<ArmoredFrame, int> _teamAssignments; // 팀 할당 정보 (ArmoredFrame -> 팀 ID)
        private float _combatStartTime;
        
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
            _participants = new List<ArmoredFrame>();
            _teamAssignments = new Dictionary<ArmoredFrame, int>();
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
            
            // Debug.Log($"[CombatSimulatorService] 전투 종료: {_battleName} (ID: {_currentBattleId}, 결과: {result})");
            Debug.Log($"[CombatSimulatorService] 전투 종료: {_battleName} (ID: {_currentBattleId}, 결과: {result})");
            
            _currentBattleId = null;
            _battleName = null;
        }
        
        /// <summary>
        /// 다음 턴으로 진행
        /// </summary>
        public bool ProcessNextTurn()
        {
            if (!_isInCombat)
            {
                // Debug.LogWarning("[CombatSimulatorService] 진행 중인 전투가 없습니다.");
                Debug.LogWarning("[CombatSimulatorService] 진행 중인 전투가 없습니다.");
                return false;
            }
            
            // 현재 턴 종료 이벤트 발행 (첫 턴 제외)
            if (_currentTurn > 0 && _currentActiveUnit != null)
            {
                var turnEndEvent = new CombatSessionEvents.TurnEndEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
                _eventBus.Publish(turnEndEvent);
            }
            
            // 다음 턴으로 진행
            _currentTurn++;
            
            // 다음 활성 유닛 선택 (현재는 단순하게 목록 순서대로 처리)
            _currentActiveUnit = GetNextActiveUnit();
            if (_currentActiveUnit == null)
            {
                Debug.LogWarning("[CombatSimulatorService] 활성화 가능한 유닛이 없습니다. 전투를 종료합니다."); // Keep Debug for editor warning?
                EndCombat();
                return false;
            }
            
            // 턴 시작 이벤트 발행
            var turnStartEvent = new CombatSessionEvents.TurnStartEvent(_currentTurn, _currentActiveUnit, _currentBattleId);
            _eventBus.Publish(turnStartEvent);
            
            // Debug.Log($"[CombatSimulatorService] 턴 {_currentTurn} 시작: {_currentActiveUnit.Name}의 턴"); 
            // This is already logged via TurnStartEvent in TextLogger
            
            // Determine and perform action for the active unit
            var action = DetermineActionForUnit(_currentActiveUnit);
            if (action != null)
            {
                PerformAction(_currentActiveUnit, action.Item1, action.Item2, action.Item3);
            }
            
            return true;
        }
        
        /// <summary>
        /// 특정 행동 수행
        /// </summary>
        public bool PerformAction(ArmoredFrame actor, CombatActionEvents.ActionType actionType, params object[] parameters)
        {
            if (!_isInCombat || actor != _currentActiveUnit)
            {
                // Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 행동 요청입니다.");
                Debug.LogWarning("[CombatSimulatorService] 유효하지 않은 행동 요청입니다.");
                return false;
            }
            
            // 행동 시작 이벤트 발행
            var actionStartEvent = new CombatActionEvents.ActionStartEvent(actor, actionType, _currentTurn, parameters);
            _eventBus.Publish(actionStartEvent);
            
            // 디버그 로그 추가 (LogLevel 수정)
            Debug.Log($"행동 시작: {actor.Name}, 행동 유형: {actionType}, 턴: {_currentTurn}");
            
            // 행동 처리 결과 (기본적으로 성공으로 설정)
            bool success = true;
            string resultDescription = "행동이 성공적으로 수행되었습니다.";
            
            // 행동 유형에 따른 처리
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    if (parameters.Length >= 2 && parameters[0] is ArmoredFrame target && parameters[1] is Weapon weapon)
                    {
                        Debug.Log($"공격 파라미터 확인: 대상={target.Name}, 무기={weapon.Name}");
                        success = PerformAttack(actor, target, weapon);
                        resultDescription = success ? $"{target.Name}에게 공격 성공" : $"{target.Name}에게 공격 실패";
                    }
                    else
                    {
                        success = false;
                        resultDescription = "공격 파라미터가 잘못되었습니다 (대상 또는 무기 누락).";
                        // Debug.LogError($"[{_currentBattleId}-T{_currentTurn}] {actor.Name}: 공격 파라미터 오류");
                        Debug.LogError($"[{_currentBattleId}-T{_currentTurn}] {actor.Name}: 공격 파라미터 오류");
                    }
                    break;
                
                case CombatActionEvents.ActionType.Move:
                    // TODO: 이동 로직 구현
                    resultDescription = "이동 로직 미구현";
                    success = false;
                    break;
                
                case CombatActionEvents.ActionType.Defend:
                    // TODO: 방어 로직 구현
                    resultDescription = "방어 로직 미구현";
                    success = false;
                    break;
                
                default:
                    resultDescription = "알 수 없는 행동 유형입니다.";
                    success = false;
                    break;
            }
            
            // 행동 완료 이벤트 발행 (수정됨)
            var actionCompletedEvent = new CombatActionEvents.ActionCompletedEvent(actor, actionType, success, resultDescription, _currentTurn);
            _eventBus.Publish(actionCompletedEvent);
            
            // 디버그 로그는 유지
            if (success)
            {
                Debug.Log($"행동 성공: {resultDescription}");
            }
            else
            {
                Debug.Log($"행동 실패: {resultDescription}");
            }
            
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
            
            // 명중 판정 (간단한 랜덤 방식 사용, 차후에 더 복잡한 로직으로 대체 가능)
            float accuracyRoll = UnityEngine.Random.value;
            bool hit = accuracyRoll <= 0.7f; // 70% 명중률 (임시)
            
            // 추가 로그: 명중 판정 결과 (LogLevel 수정)
            Debug.Log($"명중 판정: {(hit ? "성공" : "실패")} (Roll: {accuracyRoll})");
            
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

                PartType targetPartType;

                // 파츠 정보 가져오기 (내구도 등)
                Part damagedPart;
                float currentDurability;
                float maxDurability;

                do
                {
                    // 타겟팅할 파츠 결정 (현재는 랜덤)
                    targetPartType = GetRandomTargetPart();
                    damagedPart = target.GetPart(targetPartType);
                    currentDurability = damagedPart != null ? damagedPart.CurrentDurability : 0f;
                } while (currentDurability <= 0);

                // 추가 로그: 타겟 파츠 (LogLevel 수정)
                Debug.Log($"타겟 파츠: {targetPartType}");
                
                // 데미지 계산 이벤트 발행
                var damageCalculatedEvent = new DamageEvents.DamageCalculatedEvent(
                    attacker, target, weapon, rawDamage, calculatedDamage, weapon.DamageType, targetPartType);
                _eventBus.Publish(damageCalculatedEvent);
                
                // 데미지 적용
                // 임시로 약간의 랜덤 요소 추가 (크리티컬 여부)
                bool isCritical = UnityEngine.Random.value <= 0.2f; // 20% 크리티컬 확률
                float finalDamage = isCritical ? calculatedDamage * 1.5f : calculatedDamage;
                
                // 추가 로그: 크리티컬 및 최종 데미지 (LogLevel 수정)
                Debug.Log($"최종 데미지: {finalDamage} {(isCritical ? "(크리티컬!)" : "")}");
                
                // 추가 로그: 데미지 적용 시도 (LogLevel 수정)
                Debug.Log($"{target.Name}의 {targetPartType}에 {finalDamage} 데미지 적용 시도");
                
                // 실제 데미지 적용
                bool partDestroyed = target.ApplyDamage(targetPartType, finalDamage);
                
                // 추가 로그: 파츠 파괴 여부 (LogLevel 수정)
                Debug.Log($"파츠 파괴 여부: {(partDestroyed ? "파괴됨" : "파괴되지 않음")}");

                currentDurability = damagedPart != null ? damagedPart.CurrentDurability : 0f;
                maxDurability = damagedPart != null ? damagedPart.MaxDurability : 0f;

                // 추가 로그: 파츠 남은 내구도 (LogLevel 수정)
                Debug.Log($"파츠 내구도: {currentDurability}/{maxDurability}");
                
                // 데미지 적용 이벤트 발행
                var damageAppliedEvent = new DamageEvents.DamageAppliedEvent(
                    attacker, target, finalDamage, targetPartType, isCritical, 
                    currentDurability, maxDurability);
                _eventBus.Publish(damageAppliedEvent);
                
                // 파츠가 파괴된 경우 추가 이벤트 발행
                if (partDestroyed && damagedPart != null)
                {
                    var partDestroyedEvent = new PartEvents.PartDestroyedEvent(
                        target, targetPartType, attacker, 
                        $"{targetPartType} 파트가 파괴되었습니다.", 
                        $"{target.Name}의 성능이 감소했습니다.");
                    _eventBus.Publish(partDestroyedEvent);
                    
                    // 추가 로그: 파츠 파괴 상세 정보 (LogLevel 수정, Danger 유지 -> Error)
                    Debug.LogError($"파츠 파괴 상세: {target.Name}의 {targetPartType} 파츠가 파괴됨 (성능 감소)");
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
        /// 주어진 유닛의 행동을 결정합니다. (현재는 가장 가까운 적 공격)
        /// </summary>
        /// <param name="activeUnit">행동을 결정할 유닛</param>
        /// <returns>결정된 행동 정보 (타입, 대상, 무기) 또는 null</returns>
        private Tuple<CombatActionEvents.ActionType, ArmoredFrame, Weapon> DetermineActionForUnit(ArmoredFrame activeUnit)
        {
            var enemies = GetEnemies(activeUnit);
            if (enemies == null || enemies.Count == 0)
            {
                // Debug.Log($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 공격할 적이 없습니다. 턴을 넘깁니다.");
                Debug.Log($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 공격할 적이 없습니다. 턴을 넘깁니다.");
                return null; 
            }

            // 가장 가까운 적 찾기
            ArmoredFrame closestEnemy = null;
            float minDistance = float.MaxValue;

            // Use the Position property instead of transform.position
            Vector3 currentUnitPosition = activeUnit.Position; 

            foreach (var enemy in enemies)
            {
                if (!enemy.IsOperational) continue;
                
                // Use the Position property instead of transform.position
                Vector3 enemyPosition = enemy.Position; 
                float distance = Vector3.Distance(currentUnitPosition, enemyPosition);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy == null)
            {
                 // Debug.Log($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 유효한 적을 찾지 못했습니다. 턴을 넘깁니다.");
                 Debug.LogWarning($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 유효한 적을 찾지 못했습니다. 턴을 넘깁니다.");
                 return null;
            }

            // 사용할 무기 선택 (첫 번째 무기)
            Weapon weaponToUse = activeUnit.GetEquippedWeapons().FirstOrDefault();
            if (weaponToUse == null)
            {
                // Debug.LogWarning($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 사용할 수 있는 무기가 없습니다.");
                Debug.LogWarning($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 사용할 수 있는 무기가 없습니다.");
                return null; // TODO: 무기 없을 때 다른 행동 (이동 등) 고려
            }

            // Debug.Log($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 가장 가까운 적 {closestEnemy.Name}(거리: {minDistance:F1})에게 {weaponToUse.Name}(으)로 공격 결정.");
            Debug.Log($"[{_currentBattleId}-T{_currentTurn}] {activeUnit.Name}: 가장 가까운 적 {closestEnemy.Name}(거리: {minDistance:F1})에게 {weaponToUse.Name}(으)로 공격 결정.");
            return Tuple.Create(CombatActionEvents.ActionType.Attack, closestEnemy, weaponToUse);
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
        /// 팀 할당 처리
        /// </summary>
        private void AssignTeams(ArmoredFrame[] participants)
        {
            _teamAssignments.Clear();
            
            // 현재는 간단하게 팀 할당 (홀수/짝수 인덱스로 팀 구분)
            for (int i = 0; i < participants.Length; i++)
            {
                int teamId = i % 2; // 0 또는 1
                _teamAssignments[participants[i]] = teamId;
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
        /// 전투 결과 판정
        /// </summary>
        private CombatSessionEvents.CombatEndEvent.ResultType DetermineCombatResult()
        {
            // 팀별 생존자 수 계산
            var teamSurvivors = new Dictionary<int, int>();
            
            foreach (var unit in _participants)
            {
                if (unit.IsOperational && _teamAssignments.ContainsKey(unit))
                {
                    int teamId = _teamAssignments[unit];
                    if (!teamSurvivors.ContainsKey(teamId))
                    {
                        teamSurvivors[teamId] = 0;
                    }
                    teamSurvivors[teamId]++;
                }
            }
            
            // 생존자가 있는 팀 수
            int teamsWithSurvivors = teamSurvivors.Count;
            
            // 모든 팀이 전멸한 경우
            if (teamsWithSurvivors == 0)
            {
                return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
            }
            
            // 한 팀만 생존한 경우 (해당 팀 승리)
            if (teamsWithSurvivors == 1)
            {
                // 팀 0이 승리한 경우 Victory, 팀 1이 승리한 경우 Defeat로 간주
                int survivingTeam = teamSurvivors.Keys.First();
                return survivingTeam == 0 
                    ? CombatSessionEvents.CombatEndEvent.ResultType.Victory 
                    : CombatSessionEvents.CombatEndEvent.ResultType.Defeat;
            }
            
            // 여러 팀이 생존한 경우 (아직 전투 진행 중)
            return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
        }
        
        /// <summary>
        /// 전투 종료 조건 체크
        /// </summary>
        private void CheckBattleEndCondition()
        {
            var result = DetermineCombatResult();
            
            if (result != CombatSessionEvents.CombatEndEvent.ResultType.Draw || 
                !_participants.Any(p => p.IsOperational))
            {
                // Debug.Log($"[CombatSimulatorService] 전투 종료 조건 충족: {result}");
                Debug.Log($"[CombatSimulatorService] 전투 종료 조건 충족: {result}");
                EndCombat(result);
            }
        }
        
        /// <summary>
        /// 랜덤 타겟 파츠 선택
        /// </summary>
        private PartType GetRandomTargetPart()
        {
            // 모든 파츠 타입 배열
            PartType[] partTypes = (PartType[])Enum.GetValues(typeof(PartType));
            
            // 랜덤 인덱스 선택
            int randomIndex = UnityEngine.Random.Range(0, partTypes.Length);
            
            return partTypes[randomIndex];
        }
        
        #endregion
    }
} 