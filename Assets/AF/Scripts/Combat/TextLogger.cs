using AF.Combat;
using AF.EventBus;
using AF.Models;
using AF.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// 전투 과정을 텍스트 로그로 기록하는 로거 시스템
    /// </summary>
    public class TextLogger : ITextLogger
    {
        // 이벤트: 새 로그가 추가될 때 발생
        public event Action<string, LogLevel> OnLogAdded;

        private EventBus.EventBus _eventBus;
        private List<LogEntry> _logs = new List<LogEntry>();
        private int _turnCounter = 0;
        private string _currentBattleId;
        private bool _isInitialized = false;

        // 내부 로그 엔트리 클래스
        private class LogEntry
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }
            public int TurnNumber { get; set; }

            public LogEntry(string message, LogLevel level, int turnNumber)
            {
                Message = message;
                Level = level;
                Timestamp = DateTime.Now;
                TurnNumber = turnNumber;
            }
        }

        #region IService Implementation
        
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // 이벤트 버스 참조 가져오기
            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            
            if (_eventBus == null)
            {
                Debug.LogError("TextLogger 초기화 실패: EventBus 참조를 가져올 수 없습니다.");
                return;
            }

            // 전투 이벤트 구독
            SubscribeToEvents();
            
            _isInitialized = true;
            Log("TextLogger 서비스가 초기화되었습니다.", LogLevel.System);
        }

        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            // 이벤트 구독 해제
            UnsubscribeFromEvents();
            
            _isInitialized = false;
            Log("TextLogger 서비스가 종료되었습니다.", LogLevel.System);
        }
        
        #endregion

        #region ITextLogger Implementation
        
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            _logs.Add(new LogEntry(message, level, _turnCounter));
            // 새 로그가 추가되었음을 알리는 이벤트 발생 로직 삭제
            // OnLogAdded?.Invoke(FormatLogEntry(_logs.Last()), level); 
        }

        public void LogEvent(ICombatEvent combatEvent)
        {
            // 이벤트 타입에 따라 적절한 로그 생성
            if (combatEvent is CombatSessionEvents.CombatStartEvent startEvent)
            {
                LogCombatStart(startEvent);
            }
            else if (combatEvent is CombatSessionEvents.CombatEndEvent endEvent)
            {
                LogCombatEnd(endEvent);
            }
            else if (combatEvent is CombatSessionEvents.TurnStartEvent turnStartEvent)
            {
                LogTurnStart(turnStartEvent);
            }
            else if (combatEvent is CombatSessionEvents.TurnEndEvent turnEndEvent)
            {
                LogTurnEnd(turnEndEvent);
            }
            else if (combatEvent is CombatActionEvents.ActionStartEvent actionStartEvent)
            {
                // ActionStart는 너무 상세할 수 있으므로 간략화 또는 제거 고려
                // LogActionStart(actionStartEvent); 
            }
            else if (combatEvent is CombatActionEvents.ActionCompletedEvent actionCompletedEvent)
            {
                LogActionCompleted(actionCompletedEvent);
            }
            else if (combatEvent is CombatActionEvents.WeaponFiredEvent weaponFiredEvent)
            {
                // WeaponFired는 ActionCompleted에서 통합 처리 가능성 있음 (필요시 유지)
                // LogWeaponFired(weaponFiredEvent);
            }
            else if (combatEvent is DamageEvents.DamageCalculatedEvent damageCalculatedEvent)
            {
                // DamageCalculated는 너무 상세할 수 있으므로 제거 고려
                // LogDamageCalculated(damageCalculatedEvent);
            }
            else if (combatEvent is DamageEvents.DamageAppliedEvent damageAppliedEvent)
            {
                LogDamageApplied(damageAppliedEvent);
            }
            else if (combatEvent is DamageEvents.DamageAvoidedEvent damageAvoidedEvent)
            {
                LogDamageAvoided(damageAvoidedEvent);
            }
            else if (combatEvent is PartEvents.PartDestroyedEvent partDestroyedEvent)
            {
                LogPartDestroyed(partDestroyedEvent);
            }
            else if (combatEvent is PartEvents.PartStatusChangedEvent partStatusEvent)
            {
                // PartStatusChanged는 너무 상세할 수 있으므로 간략화 또는 제거 고려
                // LogPartStatusChanged(partStatusEvent);
            }
            else if (combatEvent is PartEvents.SystemCriticalFailureEvent systemFailureEvent)
            {
                LogSystemCriticalFailure(systemFailureEvent);
            }
            // <<< 상태 효과 로깅 추가 >>>
            else if (combatEvent is StatusEffectEvents.StatusEffectAppliedEvent effectAppliedEvent)
            {
                LogStatusEffectApplied(effectAppliedEvent);
            }
            else if (combatEvent is StatusEffectEvents.StatusEffectExpiredEvent effectExpiredEvent)
            {
                LogStatusEffectExpired(effectExpiredEvent);
            }
            else if (combatEvent is StatusEffectEvents.StatusEffectTickEvent effectTickedEvent)
            {
                LogStatusEffectTicked(effectTickedEvent);
            }
            // <<< 상태 효과 로깅 끝 >>>
            else
            {
                // 처리되지 않은 이벤트 타입에 대한 기본 로깅
                Log($"[T{_turnCounter}] 알 수 없는 이벤트 발생: {combatEvent.GetType().Name}", LogLevel.Warning);
            }
        }

        public List<string> GetLogs()
        {
            return _logs.Select(entry => FormatLogEntry(entry)).ToList();
        }

        public List<string> GetLogs(LogLevel level)
        {
            return _logs
                .Where(entry => entry.Level == level)
                .Select(entry => FormatLogEntry(entry))
                .ToList();
        }

        public List<string> SearchLogs(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return GetLogs();

            return _logs
                .Where(entry => entry.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(entry => FormatLogEntry(entry))
                .ToList();
        }

        public void Clear()
        {
            _logs.Clear();
            _turnCounter = 0;
            _currentBattleId = null;
        }

        public bool SaveToFile(string filename)
        {
            try
            {
                // 로그 디렉토리 생성
                string directory = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 파일명에 타임스탬프 추가
                string fullFilename = Path.Combine(directory, 
                    $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                // 모든 로그를 파일에 기록
                using (StreamWriter writer = new StreamWriter(fullFilename))
                {
                    writer.WriteLine($"=== 전투 로그: {filename} ===");
                    writer.WriteLine($"생성 시간: {DateTime.Now}");
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine();

                    foreach (var entry in _logs)
                    {
                        writer.WriteLine(FormatLogEntryForFile(entry));
                    }

                    writer.WriteLine();
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine($"로그 항목 수: {_logs.Count}");
                    writer.WriteLine();
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine("=== 전투 로그 종료 ===");
                }

                Log($"전투 로그가 '{fullFilename}'(으)로 저장되었습니다.", LogLevel.System);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"로그 파일 저장 실패: {ex.Message}");
                return false;
            }
        }

        public string GenerateBattleSummary()
        {
            if (_logs.Count == 0)
                return "기록된, 로그가 없습니다.";

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("=== 전투 요약 ===");

            // 전투 시작/종료 정보 추출
            var startEvent = _logs.FirstOrDefault(l => 
                l.Message.Contains("전투 시작") && l.Level == LogLevel.System);
            
            var endEvent = _logs.FirstOrDefault(l => 
                l.Message.Contains("전투 종료") && l.Level == LogLevel.System);

            if (startEvent != null)
            {
                summary.AppendLine($"전투 시작: {startEvent.Timestamp}");
            }

            if (endEvent != null)
            {
                summary.AppendLine($"전투 종료: {endEvent.Timestamp}");
                TimeSpan duration = endEvent.Timestamp - (startEvent?.Timestamp ?? endEvent.Timestamp);
                summary.AppendLine($"전투 지속 시간: {duration.Minutes}분 {duration.Seconds}초");
            }

            summary.AppendLine();
            summary.AppendLine("주요 이벤트:");

            // 중요 이벤트 추출 (Critical, Danger 레벨의 로그)
            var criticalEvents = _logs
                .Where(l => l.Level == LogLevel.Critical || l.Level == LogLevel.Danger)
                .OrderBy(l => l.Timestamp)
                .Take(10)
                .ToList();

            foreach (var evt in criticalEvents)
            {
                summary.AppendLine($"- {RemoveRichTextTags(evt.Message)}");
            }

            return summary.ToString();
        }
        
        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            // 전투 세션 이벤트
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(OnCombatEvent);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(OnCombatEvent);
            _eventBus.Subscribe<CombatSessionEvents.TurnStartEvent>(OnCombatEvent);
            _eventBus.Subscribe<CombatSessionEvents.TurnEndEvent>(OnCombatEvent);

            // 전투 행동 이벤트
            _eventBus.Subscribe<CombatActionEvents.ActionStartEvent>(OnCombatEvent);
            _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(OnCombatEvent);
            _eventBus.Subscribe<CombatActionEvents.WeaponFiredEvent>(OnCombatEvent);

            // 데미지 이벤트
            _eventBus.Subscribe<DamageEvents.DamageCalculatedEvent>(OnCombatEvent);
            _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(OnCombatEvent);
            _eventBus.Subscribe<DamageEvents.DamageAvoidedEvent>(OnCombatEvent);

            // 파츠 이벤트
            _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(OnCombatEvent);
            _eventBus.Subscribe<PartEvents.PartStatusChangedEvent>(OnCombatEvent);
            _eventBus.Subscribe<PartEvents.SystemCriticalFailureEvent>(OnCombatEvent);

            // 상태 효과 이벤트
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectAppliedEvent>(OnCombatEvent);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectExpiredEvent>(OnCombatEvent);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectTickEvent>(OnCombatEvent);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            // 전투 세션 이벤트
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<CombatSessionEvents.TurnStartEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(OnCombatEvent);

            // 전투 행동 이벤트
            _eventBus.Unsubscribe<CombatActionEvents.ActionStartEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<CombatActionEvents.WeaponFiredEvent>(OnCombatEvent);

            // 데미지 이벤트
            _eventBus.Unsubscribe<DamageEvents.DamageCalculatedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<DamageEvents.DamageAvoidedEvent>(OnCombatEvent);

            // 파츠 이벤트
            _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<PartEvents.PartStatusChangedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<PartEvents.SystemCriticalFailureEvent>(OnCombatEvent);

            // 상태 효과 이벤트
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectAppliedEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectExpiredEvent>(OnCombatEvent);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectTickEvent>(OnCombatEvent);
        }

        private void OnCombatEvent(ICombatEvent combatEvent)
        {
            LogEvent(combatEvent);
        }

        #endregion

        #region Event Logging Methods

        private void LogCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            Clear(); // 전투 시작 시 이전 로그 삭제
            _currentBattleId = evt.BattleId;
            _turnCounter = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"전투 시작: {ColorizeText(evt.BattleName, "yellow")}");
            sb.AppendLine($"위치: {evt.BattleLocation}");
            sb.AppendLine($"참가자:");

            foreach (var participant in evt.Participants)
            {
                sb.AppendLine($"- {ColorizeText($"[{participant.Name}]", "blue")} ({participant.FrameBase.Type})");
            }

            Log(sb.ToString(), LogLevel.System);
        }

        private void LogCombatEnd(CombatSessionEvents.CombatEndEvent evt)
        {
            StringBuilder sb = new StringBuilder();
            
            string resultColor = "white";
            switch (evt.Result)
            {
                case CombatSessionEvents.CombatEndEvent.ResultType.Victory:
                    resultColor = "green";
                    break;
                case CombatSessionEvents.CombatEndEvent.ResultType.Defeat:
                    resultColor = "red";
                    break;
                case CombatSessionEvents.CombatEndEvent.ResultType.Draw:
                    resultColor = "yellow";
                    break;
            }
            
            sb.AppendLine($"전투 종료: {ColorizeText(evt.Result.ToString(), resultColor)}");
            sb.AppendLine($"지속 시간: {evt.Duration:F2}초");
            sb.AppendLine($"생존자:");

            foreach (var survivor in evt.Survivors)
            {
                sb.AppendLine($"- {ColorizeText($"[{survivor.Name}]", "blue")}");
            }

            Log(sb.ToString(), LogLevel.System);
            
            // 전투 종료 시 상세 유닛 상태 로그 추가
            LogUnitStatusSummary(evt.BattleId); 
        }

        private void LogTurnStart(CombatSessionEvents.TurnStartEvent evt)
        {
            _turnCounter = evt.TurnNumber; // 턴 카운터 업데이트
            // 이전 로그보다 더 명확한 턴 시작 구분
            Log(new string('-', 40), LogLevel.System);
            string apInfo = $"AP: {evt.ActiveUnit.CurrentAP:F1} / {evt.ActiveUnit.CombinedStats.MaxAP:F1}";
            // AP 회복량 정보는 CombatSimulatorService의 Debug.Log에 있으므로 여기선 생략하거나, 필요시 이벤트에 추가
            Log($"===== Turn {evt.TurnNumber}: [{evt.ActiveUnit.Name}] 행동 시작 ({apInfo}) =====", LogLevel.Info);
            
            // 턴 시작 시 상태 효과 처리 로그 (필요하다면)
            // Log($"  * 상태 효과 처리 중...", LogLevel.Debug); 
        }

        private void LogTurnEnd(CombatSessionEvents.TurnEndEvent evt)
        {
            // 턴 종료 구분 로그
            string apInfo = $"AP: {evt.ActiveUnit.CurrentAP:F1} / {evt.ActiveUnit.CombinedStats.MaxAP:F1}";
            Log($"===== Turn {evt.TurnNumber}: [{evt.ActiveUnit.Name}] 행동 종료 ({apInfo}) =====", LogLevel.Info);
            Log(new string('-', 40), LogLevel.System);
            Log("", LogLevel.System); // 턴 사이에 빈 줄 추가
        }

        // ActionStart는 간략화 또는 제거 고려 (현재 주석 처리)
        // private void LogActionStart(CombatActionEvents.ActionStartEvent evt)
        // {
        //     Log($"[T{_turnCounter}] {evt.Actor.Name}: 행동 시작 - {GetActionDescription(evt.Action)}", LogLevel.Debug);
        // }

        private void LogActionCompleted(CombatActionEvents.ActionCompletedEvent evt)
        {
            // Wrap actor name in brackets (but don't include in the final log message for this method)
            // string actorName = $"[{evt.Actor.Name}]"; 
            // Target info is not available in this event, so no wrapping needed here
            string apInfo = $"| AP: {evt.Actor.CurrentAP:F1} / {evt.Actor.CombinedStats.MaxAP:F1}"; 
            string resultDetails = string.IsNullOrEmpty(evt.ResultDescription) ? "" : $"- {evt.ResultDescription}";
            
            // Remove actorName from the log message as it's implied by the current turn's active unit
            Log($"  >> [{GetActionDescription(evt.Action)}] {resultDetails} {apInfo}", LogLevel.Info);
        }
        
        // WeaponFired는 ActionCompleted에서 통합 처리 가능 (현재 주석 처리)
        // private void LogWeaponFired(CombatActionEvents.WeaponFiredEvent evt)
        // {
        //     Log($"  -> [무기 발사] {evt.Attacker.Name}의 {evt.Weapon.Name} (대상: {evt.Target.Name})", LogLevel.Debug);
        // }

        // DamageCalculated는 너무 상세하여 제거 고려 (현재 주석 처리)
        // private void LogDamageCalculated(DamageEvents.DamageCalculatedEvent evt)
        // {
        //     Log($"    * 데미지 계산: {evt.DamageSource} -> {evt.Target.Name} ({evt.TargetPartSlot}), 기본 {evt.BaseDamage:F1}, 최종 {evt.FinalDamage:F1}", LogLevel.Debug);
        // }

        private void LogDamageApplied(DamageEvents.DamageAppliedEvent evt)
        {
            // Wrap source and target names in brackets
            string attackerName = $"[{evt.Source.Name}]"; 
            string targetName = $"[{evt.Target.Name}]";
            string partName = evt.DamagedPart.ToString(); 
            string durabilityInfo = $"({evt.PartCurrentDurability:F0}/{evt.PartMaxDurability:F0})";

            Log($"    <color=red>-></color> {attackerName}의 공격! {targetName}의 {partName}{durabilityInfo}에 {evt.DamageDealt:F1} 데미지!", LogLevel.Info);
        }

        private void LogDamageAvoided(DamageEvents.DamageAvoidedEvent evt)
        {
            // Wrap source and target names in brackets
            string attackerName = $"[{evt.Source.Name}]";
            string targetName = $"[{evt.Target.Name}]";
            Log($"    <color=cyan><<</color> {targetName}이(가) {attackerName}의 공격을 회피! ({evt.Type})", LogLevel.Info);
        }

        private void LogPartDestroyed(PartEvents.PartDestroyedEvent evt)
        {
            // Wrap owner name in brackets
            string ownerName = $"[{evt.Frame.Name}]";
            string partName = evt.DestroyedPartType.ToString();
            string effectsInfo = evt.Effects != null && evt.Effects.Length > 0 ? $" ({string.Join(", ", evt.Effects)})" : "";
            Log($"  <color=orange>!!! {ownerName}의 {partName} 파괴됨!</color>{effectsInfo}", LogLevel.Warning);
        }
        
        // PartStatusChanged는 너무 상세하여 제거 고려 (현재 주석 처리)
        // private void LogPartStatusChanged(PartEvents.PartStatusChangedEvent evt)
        // {
        //     string partName = evt.ChangedPart?.Name ?? evt.SlotIdentifier;
        //     Log($"[T{_turnCounter}] {evt.Owner.Name}의 {partName} 상태 변경: {evt.NewStatus}", LogLevel.Debug);
        // }

        private void LogSystemCriticalFailure(PartEvents.SystemCriticalFailureEvent evt)
        {
            // Wrap owner name in brackets
            string ownerName = $"[{evt.Frame.Name}]";
            // evt.Reason 속성이 없으므로 기본 메시지 사용 또는 다른 속성 확인 필요
            string reason = "치명적 시스템 오류"; // evt.Reason 대신 기본 메시지 사용
            Log($"  <color=purple>*** {ownerName}: {reason} ***</color>", LogLevel.Critical);
        }
        
        // <<< 상태 효과 로깅 추가 >>>
        private void LogStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent evt)
        {
            // Wrap target name in brackets
            string targetName = $"[{evt.Target.Name}]";
            string sourceInfo = evt.Source != null ? $"({evt.Source.Name}에 의해) " : "";
            Log($"  >> {targetName}: 상태 효과 '{evt.EffectType}' 적용됨 {sourceInfo}({evt.Duration}턴)", LogLevel.Info);
        }

        private void LogStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent evt)
        {
            // Wrap target name in brackets
            string targetName = $"[{evt.Target.Name}]";
            string reason = evt.WasDispelled ? "(해제됨)" : "(만료됨)";
            Log($"  << {targetName}: 상태 효과 '{evt.EffectType}' 종료 {reason}", LogLevel.Info);
        }

        private void LogStatusEffectTicked(StatusEffectEvents.StatusEffectTickEvent evt)
        {
            // Wrap target name in brackets
            string targetName = $"[{evt.Target.Name}]";
            string effectValueInfo = $"({evt.Effect.TickEffectType}: {evt.Effect.TickValue:F1})";
            Log($"    * {targetName}: 상태 효과 '{evt.Effect.EffectName}' 틱 발생 {effectValueInfo}", LogLevel.Info);
        }
        // <<< 상태 효과 로깅 끝 >>>
        
        #endregion

        #region Formatting Methods

        private string FormatLogEntry(LogEntry entry)
        {
            // TODO: 로그 레벨에 따른 색상/아이콘 등 추가 가능
            return $"[{entry.Timestamp:HH:mm:ss}] {entry.Message}";
        }

        private string FormatLogEntryForFile(LogEntry entry)
        {
            // 파일 저장 시에는 Rich Text 태그 제거 및 상세 정보 포함
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [T{entry.TurnNumber}] [{entry.Level}] {RemoveRichTextTags(entry.Message)}";
        }

        private string GetActionDescription(CombatActionEvents.ActionType action)
        {
            // 나중에 더 구체적인 설명으로 확장 가능
            switch (action)
            {
                case CombatActionEvents.ActionType.Attack: return "공격";
                case CombatActionEvents.ActionType.Defend: return "방어";
                case CombatActionEvents.ActionType.Move: return "이동";
                // case CombatActionEvents.ActionType.Skill: return "스킬 사용";
                // case CombatActionEvents.ActionType.Wait: return "대기";
                default: return action.ToString();
            }
        }

        private string GetDurabilityColor(float percentage)
        {
            if (percentage > 0.7f) return "green";
            if (percentage > 0.3f) return "yellow";
            return "red";
        }

        private string ColorizeText(string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        private string BoldText(string text)
        {
            return $"<b>{text}</b>";
        }

        private string ItalicText(string text)
        {
            return $"<i>{text}</i>";
        }

        private string RemoveRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // 간단한 태그 제거 (더 복잡한 정규식 필요할 수 있음)
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        }

        private void LogUnitStatusSummary(string battleId)
        {
            // TODO: 전투 종료 시 유닛 상태 요약 로그 구현
        }

        public void LogUnitDetails(ArmoredFrame unit) // private -> public
        {
            // TODO: CombatSimulatorService에서 분리된 로깅 메서드 구현 필요
        }
        #endregion
    }
} 