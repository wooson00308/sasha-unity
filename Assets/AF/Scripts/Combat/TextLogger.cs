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
                LogActionStart(actionStartEvent);
            }
            else if (combatEvent is CombatActionEvents.ActionCompletedEvent actionCompletedEvent)
            {
                LogActionCompleted(actionCompletedEvent);
            }
            else if (combatEvent is CombatActionEvents.WeaponFiredEvent weaponFiredEvent)
            {
                LogWeaponFired(weaponFiredEvent);
            }
            else if (combatEvent is DamageEvents.DamageCalculatedEvent damageCalculatedEvent)
            {
                LogDamageCalculated(damageCalculatedEvent);
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
                LogPartStatusChanged(partStatusEvent);
            }
            else if (combatEvent is PartEvents.SystemCriticalFailureEvent systemFailureEvent)
            {
                LogSystemCriticalFailure(systemFailureEvent);
            }
            else
            {
                // 처리되지 않은 이벤트 타입에 대한 기본 로깅
                Log($"알 수 없는 이벤트 발생: {combatEvent.GetType().Name}", LogLevel.Warning);
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
                }

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
        }

        private void OnCombatEvent(ICombatEvent combatEvent)
        {
            LogEvent(combatEvent);
        }

        #endregion

        #region Event Logging Methods

        private void LogCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            _currentBattleId = evt.BattleId;
            _turnCounter = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"전투 시작: {ColorizeText(evt.BattleName, "yellow")}");
            sb.AppendLine($"위치: {evt.BattleLocation}");
            sb.AppendLine($"참가자:");

            foreach (var participant in evt.Participants)
            {
                sb.AppendLine($"- {ColorizeText(participant.Name, "blue")} ({participant.FrameBase.Type})");
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
                sb.AppendLine($"- {ColorizeText(survivor.Name, "blue")}");
            }

            Log(sb.ToString(), LogLevel.System);
        }

        private void LogTurnStart(CombatSessionEvents.TurnStartEvent evt)
        {
            _turnCounter = evt.TurnNumber;
            Log($"턴 {BoldText(evt.TurnNumber.ToString())} 시작 - 활성 유닛: {ColorizeText(evt.ActiveUnit.Name, "blue")}", 
                LogLevel.Info);
        }

        private void LogTurnEnd(CombatSessionEvents.TurnEndEvent evt)
        {
            Log($"턴 {BoldText(evt.TurnNumber.ToString())} 종료 - 활성 유닛: {ColorizeText(evt.ActiveUnit.Name, "blue")}",
                LogLevel.Info);
        }

        private void LogActionStart(CombatActionEvents.ActionStartEvent evt)
        {
            string actionDescription = GetActionDescription(evt.Action);
            
            Log($"{ColorizeText(evt.Actor.Name, "blue")}(이)가 {actionDescription} 행동을 시작합니다.", 
                LogLevel.Info);
        }

        private void LogActionCompleted(CombatActionEvents.ActionCompletedEvent evt)
        {
            string actionDescription = GetActionDescription(evt.Action);
            LogLevel logLevel = evt.Success ? LogLevel.Success : LogLevel.Warning;
            
            Log($"{ColorizeText(evt.Actor.Name, "blue")}의 {actionDescription} 행동 {(evt.Success ? "성공" : "실패")}: {evt.ResultDescription}", 
                logLevel);
        }

        private void LogWeaponFired(CombatActionEvents.WeaponFiredEvent evt)
        {
            string hitText = evt.Hit ? 
                ColorizeText("명중", "green") : 
                ColorizeText("빗나감", "red");
            
            Log($"{ColorizeText(evt.Attacker.Name, "blue")}(이)가 {ColorizeText(evt.Target.Name, "purple")}에게 " +
                $"{ColorizeText(evt.Weapon.Name, "orange")}(으)로 공격: {hitText} (정확도 판정: {evt.AccuracyRoll:F2})", 
                evt.Hit ? LogLevel.Success : LogLevel.Warning);
        }

        private void LogDamageCalculated(DamageEvents.DamageCalculatedEvent evt)
        {
            Log($"데미지 계산: {ColorizeText(evt.Source.Name, "blue")} → {ColorizeText(evt.Target.Name, "purple")} " +
                $"원본 데미지: {evt.RawDamage:F1}, 최종 데미지: {ColorizeText(evt.CalculatedDamage.ToString("F1"), "red")} " +
                $"({evt.DamageType} 타입, 대상 부위: {evt.TargetPart})", 
                LogLevel.Info);
        }

        private void LogDamageApplied(DamageEvents.DamageAppliedEvent evt)
        {
            string criticalText = evt.IsCritical ? "[치명타!] " : "";
            LogLevel logLevel = evt.IsCritical ? LogLevel.Critical : LogLevel.Danger;

            // 내구도 퍼센트 계산 및 NaN 체크
            string durabilityText;
            if (evt.PartMaxDurability <= 0) // 최대 내구도가 0 이하면 계산 불가
            {
                durabilityText = "N/A";
            }
            else
            {
                float durabilityPercentage = evt.PartCurrentDurability / evt.PartMaxDurability;
                if (float.IsNaN(durabilityPercentage))
                {
                    durabilityText = "N/A"; // 계산 결과가 NaN인 경우 (거의 없겠지만 안전 장치)
                }
                else
                {
                    // 정상적인 경우 퍼센트(P0) 형식으로 표시
                    durabilityText = durabilityPercentage.ToString("P0");
                }
            }

            // 로그 메시지 생성 (이벤트 속성 사용)
            string message = $"{evt.Target.Name}(이)가 {evt.DamageDealt:F1} 데미지를 받음 {criticalText}(부위: {evt.DamagedPart}, 남은 내구도: {durabilityText})";
            Log(message, logLevel);
        }

        private void LogDamageAvoided(DamageEvents.DamageAvoidedEvent evt)
        {
            string avoidanceTypeText = "";
            switch (evt.Type)
            {
                case DamageEvents.DamageAvoidedEvent.AvoidanceType.Dodge:
                    avoidanceTypeText = "회피";
                    break;
                case DamageEvents.DamageAvoidedEvent.AvoidanceType.Deflect:
                    avoidanceTypeText = "방어";
                    break;
                case DamageEvents.DamageAvoidedEvent.AvoidanceType.Intercept:
                    avoidanceTypeText = "가로챔";
                    break;
                case DamageEvents.DamageAvoidedEvent.AvoidanceType.Shield:
                    avoidanceTypeText = "보호막";
                    break;
            }
            
            Log($"{ColorizeText(evt.Target.Name, "purple")}(이)가 {ColorizeText(evt.DamageAvoided.ToString("F1"), "cyan")} " +
                $"데미지를 {avoidanceTypeText}함: {evt.Description}", 
                LogLevel.Success);
        }

        private void LogPartDestroyed(PartEvents.PartDestroyedEvent evt)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{ColorizeText(evt.Frame.Name, "purple")}의 {ColorizeText(evt.DestroyedPartType.ToString(), "red")} 파츠가 파괴됨!");
            
            if (evt.Effects != null && evt.Effects.Length > 0)
            {
                sb.AppendLine("효과:");
                foreach (var effect in evt.Effects)
                {
                    sb.AppendLine($"- {effect}");
                }
            }
            
            Log(sb.ToString(), LogLevel.Critical);
        }

        private void LogPartStatusChanged(PartEvents.PartStatusChangedEvent evt)
        {
            string statusText = "";
            string colorCode = "white";
            
            switch (evt.ChangeType)
            {
                case PartEvents.PartStatusChangedEvent.StatusChangeType.Damaged:
                    statusText = "손상됨";
                    colorCode = "orange";
                    break;
                case PartEvents.PartStatusChangedEvent.StatusChangeType.Overheated:
                    statusText = "과열됨";
                    colorCode = "red";
                    break;
                case PartEvents.PartStatusChangedEvent.StatusChangeType.Malfunctioning:
                    statusText = "오작동";
                    colorCode = "yellow";
                    break;
                case PartEvents.PartStatusChangedEvent.StatusChangeType.Disabled:
                    statusText = "작동 불능";
                    colorCode = "gray";
                    break;
                case PartEvents.PartStatusChangedEvent.StatusChangeType.Repaired:
                    statusText = "수리됨";
                    colorCode = "green";
                    break;
            }
            
            Log($"{ColorizeText(evt.Frame.Name, "purple")}의 {evt.PartType} 파츠가 {ColorizeText(statusText, colorCode)} " +
                $"(심각도: {(evt.Severity * 100):F0}%): {evt.Description}", 
                evt.ChangeType == PartEvents.PartStatusChangedEvent.StatusChangeType.Repaired ? 
                    LogLevel.Success : LogLevel.Warning);
        }

        private void LogSystemCriticalFailure(PartEvents.SystemCriticalFailureEvent evt)
        {
            string durationText = evt.IsPermanent ? "영구적" : $"{evt.TurnDuration}턴 동안";
            
            Log($"{ColorizeText(evt.Frame.Name, "purple")}의 {ColorizeText(evt.SystemName, "red")} 시스템 치명적 오류 발생! " +
                $"{durationText}: {evt.FailureDescription}", 
                LogLevel.Critical);
        }

        #endregion

        #region Utility Methods

        private string FormatLogEntry(LogEntry entry)
        {
            string levelPrefix = GetLogLevelPrefix(entry.Level);
            return $"{levelPrefix}{entry.Message}";
        }

        private string FormatLogEntryForFile(LogEntry entry)
        {
            string levelText = entry.Level.ToString().ToUpper();
            return $"[{levelText}] {RemoveRichTextTags(entry.Message)}";
        }

        private string GetLogLevelPrefix(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    return "ℹ️ ";
                case LogLevel.Success:
                    return "✅ ";
                case LogLevel.Warning:
                    return "⚠️ ";
                case LogLevel.Danger:
                    return "🔥 ";
                case LogLevel.Critical:
                    return "⚡ ";
                case LogLevel.System:
                    return "🔧 ";
                default:
                    return "";
            }
        }

        private string GetActionDescription(CombatActionEvents.ActionType action)
        {
            switch (action)
            {
                case CombatActionEvents.ActionType.Attack:
                    return "공격";
                case CombatActionEvents.ActionType.Move:
                    return "이동";
                case CombatActionEvents.ActionType.UseAbility:
                    return "특수 능력 사용";
                case CombatActionEvents.ActionType.Defend:
                    return "방어";
                case CombatActionEvents.ActionType.Retreat:
                    return "후퇴";
                case CombatActionEvents.ActionType.Overwatch:
                    return "감시";
                case CombatActionEvents.ActionType.Reload:
                    return "재장전";
                case CombatActionEvents.ActionType.RepairSelf:
                    return "자가 수리";
                case CombatActionEvents.ActionType.RepairAlly:
                    return "아군 수리";
                default:
                    return action.ToString();
            }
        }

        private string GetDurabilityColor(float percentage)
        {
            if (percentage > 0.66f)
                return "green";
            else if (percentage > 0.33f)
                return "yellow";
            else
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
            // Unity Rich Text 태그 제거 (색상, 굵게, 기울임꼴 등)
            text = System.Text.RegularExpressions.Regex.Replace(text, "<color=.*?>|</color>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, "<b>|</b>", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, "<i>|</i>", "");
            return text;
        }

        #endregion
    }
} 