using AF.Services;
using UnityEngine;
using AF.EventBus;
using AF.Models;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AF.Combat
{
    /// <summary>
    /// TextLogger 서비스를 관리하는 서비스 클래스
    /// 서비스 로케이터 패턴에 의해 등록됩니다.
    /// </summary>
    public class TextLoggerService : IService
    {
        private TextLogger _textLogger;
        private EventBus.EventBus _eventBus;
        
        /// <summary>
        /// TextLogger 인스턴스에 대한 퍼블릭 접근자 (인터페이스 타입)
        /// </summary>
        public ITextLogger TextLogger => _textLogger;

        /// <summary>
        /// TextLogger 구체 인스턴스에 대한 퍼블릭 접근자 (외부 사용 시 주의)
        /// </summary>
        public TextLogger ConcreteLogger => _textLogger;
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            _textLogger = new TextLogger();
            
            // TextLogger 초기화
            _textLogger.Initialize();
            
            // EventBus 서비스 가져오기
            var eventBusService = ServiceLocator.Instance.GetService<EventBusService>();
            if (eventBusService == null)
            {
                Debug.LogError("EventBusService를 찾을 수 없습니다!");
                return;
            }
            _eventBus = eventBusService.Bus;

            // 이벤트 구독
            SubscribeToEvents();

            Debug.Log("TextLoggerService가 초기화되고 이벤트 구독을 완료했습니다.");
        }

        /// <summary>
        /// 서비스 종료
        /// </summary>
        public void Shutdown()
        {
            // 이벤트 구독 해제 먼저 수행
            UnsubscribeFromEvents();

            if (_textLogger != null)
            {
                _textLogger.Shutdown();
                _textLogger = null;
            }
            
            _eventBus = null; // 참조 해제

            Debug.Log("TextLoggerService가 종료되고 이벤트 구독을 해제했습니다.");
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null || _textLogger == null) return;

            // 전투 세션 이벤트
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Subscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStart);
            _eventBus.Subscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEnd);

            // 전투 액션 이벤트
            _eventBus.Subscribe<CombatActionEvents.ActionStartEvent>(HandleActionStart);
            _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Subscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);

            // 데미지 이벤트
            _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Subscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);

            // 파츠 이벤트
            _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);

            // 상태 효과 이벤트
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectExpiredEvent>(HandleStatusEffectExpired);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
            
            // 유닛 패배 이벤트
            // _eventBus.Subscribe<CombatSessionEvents.UnitDefeatedEvent>(HandleUnitDefeated);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null || _textLogger == null) return;

            // 구독했던 순서대로 해제 (혹은 타입별로)
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Unsubscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStart);
            _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEnd);

            _eventBus.Unsubscribe<CombatActionEvents.ActionStartEvent>(HandleActionStart);
            _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Unsubscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);

            _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Unsubscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);

            _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);

            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectExpiredEvent>(HandleStatusEffectExpired);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
            
            // _eventBus.Unsubscribe<CombatSessionEvents.UnitDefeatedEvent>(HandleUnitDefeated);
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            _textLogger.Log($"=== 전투 시작 === ID: {ev.BattleId}, 이름: {ev.BattleName}", LogLevel.Info);
            LogAllUnitDetailsOnInit(ev.Participants);
        }

        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            _textLogger.Log($"=== 전투 종료 === ID: {ev.BattleId}, 결과: {ev.Result}, 지속시간: {ev.Duration:F1}초", LogLevel.Info);
        }

        private void HandleTurnStart(CombatSessionEvents.TurnStartEvent ev)
        {
            _textLogger.Log($"--- Turn {ev.TurnNumber} 시작: [{ev.ActiveUnit.Name}] --- (ID: {ev.BattleId})", LogLevel.Info);
            LogUnitDetailsOnTurnStart(ev.ActiveUnit);
        }

        private void HandleTurnEnd(CombatSessionEvents.TurnEndEvent ev)
        {
            _textLogger.Log($"--- Turn {ev.TurnNumber} 종료: [{ev.ActiveUnit.Name}] --- (ID: {ev.BattleId})", LogLevel.Info);
            LogAllUnitDetailsOnTurnEnd();
        }

        private void HandleActionStart(CombatActionEvents.ActionStartEvent ev)
        {
            //_textLogger.Log($"{ev.Actor.Name} 행동 시작.", LogLevel.Info);
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
            string successText = ev.Success ? "성공" : "실패";
            //_textLogger.Log($"{ev.Actor.Name} 행동 완료: {successText}", LogLevel.Info);
        }

        private void HandleWeaponFired(CombatActionEvents.WeaponFiredEvent ev)
        {
            string hitStatus = ev.Hit ? "명중" : "빗나감";
            string logMsg = $"{ev.Attacker.Name} -> {ev.Target.Name} ({ev.Weapon.Name}): {hitStatus} (Roll: {ev.AccuracyRoll:P1})";
            _textLogger.Log(logMsg, LogLevel.Info);
        }

        private void HandleDamageApplied(DamageEvents.DamageAppliedEvent ev)
        {
            string criticalText = ev.IsCritical ? " (치명타!)" : "";
            string logMsg = $"{ev.Target.Name} < 데미지 받음{criticalText}";
            _textLogger.Log(logMsg, LogLevel.Warning);
        }

        private void HandleDamageAvoided(DamageEvents.DamageAvoidedEvent ev)
        {
            string logMsg = $"{ev.Target.Name} < 공격 회피 ({ev.Type})";
            _textLogger.Log(logMsg, LogLevel.Info);
        }

        private void HandlePartDestroyed(PartEvents.PartDestroyedEvent ev)
        {
            string logMsg = $"파츠 파괴됨!";
            _textLogger.Log(logMsg, LogLevel.Warning);
        }

        private void HandleStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent ev)
        {
            string logMsg = $"{ev.Target.Name} < 상태 효과 적용됨";
            _textLogger.Log(logMsg, LogLevel.Info);
        }

        private void HandleStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent ev)
        {
            string logMsg = $"{ev.Target.Name} < 상태 효과 만료됨";
            _textLogger.Log(logMsg, LogLevel.Info);
        }

        private void HandleStatusEffectTick(StatusEffectEvents.StatusEffectTickEvent ev)
        {
            string logMsg = $"{ev.Target.Name} < 상태 효과 [{ev.Effect.EffectName}] 틱 발동 (값: {ev.Effect.TickValue})";
            _textLogger.Log(logMsg, LogLevel.Info);
        }
        
        private Dictionary<(ArmoredFrame, string), float> _previousPartDurability = new Dictionary<(ArmoredFrame, string), float>();

        private void LogAllUnitDetailsOnInit(ArmoredFrame[] participants)
        {
            _textLogger.Log("--- Initial Units Status ---", LogLevel.Info);
            _previousPartDurability.Clear();
            if (participants != null)
            {
                foreach (var unit in participants)
                {
                    LogUnitDetailsInternal(unit, true);
                }
            }
            _textLogger.Log("-----------------------------", LogLevel.Info);
        }
        
        private void LogAllUnitDetailsOnTurnEnd()
        {
            _textLogger.Log("--- End of Turn Units Status ---", LogLevel.Info);
            //_textLogger.Log("---------------------------------", LogLevel.Info);
        }

        private void LogUnitDetailsOnTurnStart(ArmoredFrame unit)
        {
            _textLogger.Log($"--- Turn Start: {unit?.Name} Status ---", LogLevel.Info);
            LogUnitDetailsInternal(unit, false);
            _textLogger.Log("------------------------------------", LogLevel.Info);
        }

        private void LogUnitDetailsInternal(ArmoredFrame unit, bool isInitialLog)
        {
            if (unit == null) return;

            // StringBuilder를 사용하여 여러 줄 로그를 하나의 문자열로 만들기
            StringBuilder sb = new StringBuilder();

            // 1. 기본 유닛 정보 추가 (첫 줄)
            sb.AppendLine($"  Unit: {unit.Name} {(unit.IsOperational ? "(Operational)" : "(DESTROYED)")} | AP: {unit.CurrentAP:F1}/{unit.CombinedStats.MaxAP:F1}"); // AppendLine 사용

            // 2. 스탯 정보 추가
            var stats = unit.CombinedStats;
            sb.AppendLine($"    Stats: Atk:{stats.AttackPower:F1}/Def:{stats.Defense:F1}/Spd:{stats.Speed:F1}/Acc:{stats.Accuracy:F1}/Eva:{stats.Evasion:F1}");

            // 3. 파츠 정보 헤더 추가
            sb.AppendLine("    Parts:");
            foreach (var kvp in unit.Parts.OrderBy(pair => pair.Key)) // 정렬 유지
            {
                string slotId = kvp.Key;
                Part part = kvp.Value;
                string status;
                float currentDurability = part.CurrentDurability;
                var key = (unit, slotId);

                if (part.IsOperational)
                {
                    string changeIndicator = "";
                    if (!isInitialLog && _previousPartDurability.TryGetValue(key, out float previousDurability))
                    {
                        float durabilityChange = currentDurability - previousDurability;
                        if (Mathf.Abs(durabilityChange) > 0.01f)
                        {
                            changeIndicator = $" [{(durabilityChange > 0 ? "+" : "")}{durabilityChange:F0}]";
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
                // 파츠 정보 각 줄 추가
                sb.AppendLine($"      - {slotId} ({part.Name}): {status}");
            }

            // 4. 무기 정보 헤더 추가
            sb.AppendLine("    Weapons:");
            var weapons = unit.GetAllWeapons();
            if (weapons != null && weapons.Count > 0)
            {
                foreach (var weapon in weapons)
                {
                    string weaponStatus = weapon.IsOperational ? "Op" : "Dmg";
                    // 무기 정보 각 줄 추가
                    sb.AppendLine($"      - {weapon.Name}: {weaponStatus}");
                }
            } else {
                sb.AppendLine("      - None"); // 무기 없을 때
            }

            // 5. 최종적으로 모든 내용을 담은 문자열을 한 번만 로그로 출력
            _textLogger.Log(sb.ToString().TrimEnd('\r', '\n'), LogLevel.Info); // 마지막 줄바꿈 제거 (선택적)
        }
    }
} 