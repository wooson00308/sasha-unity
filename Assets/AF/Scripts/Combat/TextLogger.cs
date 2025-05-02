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
        private int _cycleCounter = 0;
        private string _currentBattleId;
        private bool _isInitialized = false;

        // 포맷팅 제어 플래그
        public bool ShowLogLevel { get; set; } = true;
        public bool ShowTurnPrefix { get; set; } = true;
        public bool UseIndentation { get; set; } = true; // TextLoggerService 핸들러에서 사용할 플래그

        // 내부 로그 엔트리 클래스 -> public으로 변경
        public class LogEntry
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }
            public int TurnNumber { get; set; }
            public int CycleNumber { get; set; }
            public ArmoredFrame ContextUnit { get; set; }
            public bool ShouldUpdateTargetView { get; set; }

            // === 스냅샷 및 델타 정보 ===
            public LogEventType EventType { get; set; }
            public Dictionary<string, ArmoredFrameSnapshot> TurnStartStateSnapshot { get; set; }

            // --- DamageApplied 델타 정보 필드 --- (예시, 필요에 따라 추가/수정)
            public string Damage_SourceUnitName { get; set; }
            public string Damage_TargetUnitName { get; set; }
            public string Damage_DamagedPartSlot { get; set; }
            public float Damage_AmountDealt { get; set; }
            public float Damage_NewDurability { get; set; }
            public bool Damage_IsCritical { get; set; }
            public bool Damage_PartWasDestroyed { get; set; }
            // --- 델타 정보 필드 끝 ---

            // +++ ActionCompleted 델타 정보 필드 +++
            public string Action_ActorName { get; set; }
            public CombatActionEvents.ActionType Action_Type { get; set; }
            public bool Action_IsSuccess { get; set; }
            public string Action_ResultDescription { get; set; }
            public string Action_TargetName { get; set; } // Optional (e.g., for Move action target)
            public float? Action_DistanceMoved { get; set; } // Optional for Move
            public Vector3? Action_NewPosition { get; set; } // Optional for Move
            public bool Action_IsCounterAttack { get; set; } // Added

            // +++ DamageAvoided 델타 정보 필드 +++
            public string Avoid_SourceName { get; set; }
            public string Avoid_TargetName { get; set; }
            public DamageEvents.DamageAvoidedEvent.AvoidanceType? Avoid_Type { get; set; }
            public bool Avoid_IsCounterAttack { get; set; }

            // +++ PartDestroyed 델타 정보 필드 +++
            public string PartDestroyed_OwnerName { get; set; }
            public PartType? PartDestroyed_PartType { get; set; }
            public string PartDestroyed_DestroyerName { get; set; } // Optional

            // +++ WeaponFired 델타 정보 필드 +++
            public string Weapon_AttackerName { get; set; }
            public string Weapon_TargetName { get; set; }
            public string Weapon_WeaponName { get; set; }
            public bool Weapon_IsHit { get; set; }
            public bool Weapon_IsCounterAttack { get; set; }
            public int Weapon_CurrentAmmo { get; set; } // Optional, maybe not needed if snapshot exists
            public int Weapon_MaxAmmo { get; set; } // Optional

            // +++ StatusEffectApplied 델타 정보 필드 +++
            public string StatusApplied_TargetName { get; set; }
            public string StatusApplied_SourceName { get; set; } // Optional
            public StatusEffectEvents.StatusEffectType? StatusApplied_EffectType { get; set; }
            public int StatusApplied_Duration { get; set; }
            public float StatusApplied_Magnitude { get; set; }

            // +++ StatusEffectExpired 델타 정보 필드 +++
            public string StatusExpired_TargetName { get; set; }
            public StatusEffectEvents.StatusEffectType? StatusExpired_EffectType { get; set; }
            public bool StatusExpired_WasDispelled { get; set; }

            // +++ StatusEffectTick 델타 정보 필드 +++
            public string StatusTick_TargetName { get; set; }
            public string StatusTick_EffectName { get; set; } // Use string name
            public float StatusTick_Value { get; set; }
            public TickEffectType StatusTick_TickType { get; set; }

            // +++ RepairApplied 델타 정보 필드 +++
            public string Repair_ActorName { get; set; }
            public string Repair_TargetName { get; set; }
            public string Repair_PartSlot { get; set; }
            public float Repair_Amount { get; set; }
            public CombatActionEvents.ActionType Repair_ActionType { get; set; } // Distinguish Self/Ally repair

            public LogEntry(string message, LogLevel level, int turnNumber, int cycleNumber,
                            LogEventType eventType,
                            ArmoredFrame contextUnit = null, bool shouldUpdateTargetView = false,
                            Dictionary<string, ArmoredFrameSnapshot> turnStartStateSnapshot = null)
            {
                Message = message;
                Level = level;
                Timestamp = DateTime.Now;
                TurnNumber = turnNumber;
                CycleNumber = cycleNumber;
                EventType = eventType;
                ContextUnit = contextUnit;
                ShouldUpdateTargetView = shouldUpdateTargetView;
                TurnStartStateSnapshot = turnStartStateSnapshot;

                // 델타 필드 기본값 초기화 (null 또는 기본값)
                Damage_SourceUnitName = null;
                Damage_TargetUnitName = null;
                Damage_DamagedPartSlot = null;
                Damage_AmountDealt = 0f;
                Damage_NewDurability = -1f;
                Damage_IsCritical = false;
                Damage_PartWasDestroyed = false;

                // +++ 새로 추가된 델타 필드 초기화 +++
                Action_ActorName = null;
                Action_Type = CombatActionEvents.ActionType.None;
                Action_IsSuccess = false;
                Action_ResultDescription = null;
                Action_TargetName = null;
                Action_DistanceMoved = null;
                Action_NewPosition = null;
                Action_IsCounterAttack = false;

                Avoid_SourceName = null;
                Avoid_TargetName = null;
                Avoid_Type = null;
                Avoid_IsCounterAttack = false;

                PartDestroyed_OwnerName = null;
                PartDestroyed_PartType = null;
                PartDestroyed_DestroyerName = null;

                // +++ Initialize NEW delta fields +++
                Weapon_AttackerName = null;
                Weapon_TargetName = null;
                Weapon_WeaponName = null;
                Weapon_IsHit = false;
                Weapon_IsCounterAttack = false;
                Weapon_CurrentAmmo = -1;
                Weapon_MaxAmmo = -1;

                StatusApplied_TargetName = null;
                StatusApplied_SourceName = null;
                StatusApplied_EffectType = null;
                StatusApplied_Duration = 0;
                StatusApplied_Magnitude = 0f;

                StatusExpired_TargetName = null;
                StatusExpired_EffectType = null;
                StatusExpired_WasDispelled = false;

                StatusTick_TargetName = null;
                StatusTick_EffectName = null;
                StatusTick_Value = 0f;
                StatusTick_TickType = TickEffectType.None;

                Repair_ActorName = null;
                Repair_TargetName = null;
                Repair_PartSlot = null;
                Repair_Amount = 0f;
                Repair_ActionType = CombatActionEvents.ActionType.None;
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

            // 전투 이벤트 구독 제거
            // SubscribeToEvents(); 
            
            _isInitialized = true;
            Log("TextLogger 서비스가 초기화되었습니다.", LogLevel.System);
        }

        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            // 전투 이벤트 구독 제거
            // SubscribeToEvents(); 
            
            _isInitialized = false;
            Log("TextLogger 서비스가 종료되었습니다.", LogLevel.System);
        }
        
        #endregion

        #region ITextLogger Implementation
        
        public void Log(string message, LogLevel level = LogLevel.Info, LogEventType eventType = LogEventType.Unknown, ArmoredFrame contextUnit = null, bool shouldUpdateTargetView = false, Dictionary<string, ArmoredFrameSnapshot> turnStartStateSnapshot = null)
        {
            _logs.Add(new LogEntry(message, level, _turnCounter, _cycleCounter, eventType, contextUnit, shouldUpdateTargetView, turnStartStateSnapshot));
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
            else if (combatEvent is CombatSessionEvents.UnitActivationStartEvent unitActivationStartEvent)
            {
                LogUnitActivationStart(unitActivationStartEvent);
            }
            else if (combatEvent is CombatSessionEvents.UnitActivationEndEvent unitActivationEndEvent)
            {
                LogUnitActivationEnd(unitActivationEndEvent);
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

        /// <summary>
        /// Filters and formats logs based on specified include/exclude levels.
        /// </summary>
        /// <param name="levelsToInclude">Array of LogLevels to include. If null or empty, all levels are potentially included (unless excluded).</param>
        /// <param name="levelsToExclude">Array of LogLevels to exclude. If null, no levels are excluded.</param>
        /// <returns>A list of formatted log strings matching the criteria.</returns>
        public List<string> GetFormattedLogs(LogLevel[] levelsToInclude = null, LogLevel[] levelsToExclude = null)
        {
            IEnumerable<LogEntry> filteredLogs = _logs;

            // Apply include filter
            if (levelsToInclude != null && levelsToInclude.Length > 0)
            {
                filteredLogs = filteredLogs.Where(entry => levelsToInclude.Contains(entry.Level));
            }

            // Apply exclude filter
            if (levelsToExclude != null && levelsToExclude.Length > 0)
            {
                filteredLogs = filteredLogs.Where(entry => !levelsToExclude.Contains(entry.Level));
            }

            return filteredLogs.Select(entry => FormatLogEntry(entry)).ToList();
        }

        public void Clear()
        {
            _logs.Clear();
            _turnCounter = 0;
            _cycleCounter = 0;
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

        #region Event Logging Methods

        private void LogCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            Clear(); // 전투 시작 시 이전 로그 삭제
            _currentBattleId = evt.BattleId;
            _turnCounter = 0;
            _cycleCounter = 0;

            StringBuilder sb = new StringBuilder();
            sb.Append("<sprite index=11> "); // BATTLE START 아이콘
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
            sb.Append("<sprite index=12> "); // BATTLE END 아이콘
            
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

        private void LogUnitActivationStart(CombatSessionEvents.UnitActivationStartEvent evt)
        {
            // _turnCounter = evt.TurnNumber; // 턴 카운터 업데이트 - CombatSimulatorService의 Cycle을 참조해야 할 수도 있음. 일단 LogEvent 호출부에서 관리.
            string apInfo = $"AP: {evt.ActiveUnit.CurrentAP:F1} / {evt.ActiveUnit.CombinedStats.MaxAP:F1}";
            // AP 회복량 정보는 CombatSimulatorService의 Debug.Log에 있으므로 여기선 생략하거나, 필요시 이벤트에 추가
            // Log($"<sprite index=13> ===== Turn {evt.TurnNumber}: [{evt.ActiveUnit.Name}] 행동 시작 ({apInfo}) =====", LogLevel.Info); // TURN START 아이콘
            // TextLoggerService에서 이미 포맷된 로그를 전달하므로, 여기서는 상세 로깅 불필요. 필요하다면 추가.
            // Log($"Unit Activation Start: {evt.ActiveUnit.Name}", LogLevel.Debug); // 예시
        }

        private void LogUnitActivationEnd(CombatSessionEvents.UnitActivationEndEvent evt)
        {
            // 턴 종료 구분 로그
            string apInfo = $"AP: {evt.ActiveUnit.CurrentAP:F1} / {evt.ActiveUnit.CombinedStats.MaxAP:F1}";
            // Log($"<sprite index=14> ===== Turn {evt.TurnNumber}: [{evt.ActiveUnit.Name}] 행동 종료 ({apInfo}) =====", LogLevel.Info); // TURN END 아이콘
            // TextLoggerService에서 이미 포맷된 로그를 전달하므로, 여기서는 상세 로깅 불필요. 필요하다면 추가.
            // Log($"Unit Activation End: {evt.ActiveUnit.Name}", LogLevel.Debug); // 예시
        }

        // ActionStart는 간략화 또는 제거 고려 (현재 주석 처리)
        // private void LogActionStart(CombatActionEvents.ActionStartEvent evt)
        // {
        //     Log($"[T{_turnCounter}] {evt.Actor.Name}: 행동 시작 - {GetActionDescription(evt.Action)}", LogLevel.Debug);
        // }

        private void LogActionCompleted(CombatActionEvents.ActionCompletedEvent evt)
        {
            string apInfo = $"| AP: {evt.Actor.CurrentAP:F1} / {evt.Actor.CombinedStats.MaxAP:F1}"; 
            string resultDetails = string.IsNullOrEmpty(evt.ResultDescription) ? "" : $"- {evt.ResultDescription}";
            
            // <<< ActionType에 따른 인덱스 태그 선택 >>>
            string actionSpriteTag = "";
            switch (evt.Action)
            {
                case CombatActionEvents.ActionType.Attack:
                    actionSpriteTag = "<sprite index=8>"; // ATK 아이콘
                    break;
                case CombatActionEvents.ActionType.Defend:
                    actionSpriteTag = "<sprite index=9>"; // DEF 아이콘
                    break;
                case CombatActionEvents.ActionType.Move:
                    actionSpriteTag = "<sprite index=10>"; // MOVE 아이콘
                    break;
                // 다른 ActionType에 대한 태그 추가 가능
            }

            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "  >> " : ">> "; 
            // <<< 인덱스 태그 추가 >>>
            // Log($"{prefix}<sprite index=0> <color=red>-></color> {actionSpriteTag} [{GetActionDescription(evt.Action)}] {resultDetails} {apInfo}", LogLevel.Info);
            // Actor 이름 색상 추가
            Log($"{prefix}<sprite index=0> <color=red>-></color> {actionSpriteTag} [{ColorizeText(evt.Actor.Name, "yellow")}] [{GetActionDescription(evt.Action)}] {resultDetails} {apInfo}", LogLevel.Info);
        }
        
        // WeaponFired는 ActionCompleted에서 통합 처리 가능 (현재 주석 처리)
        // private void LogWeaponFired(CombatActionEvents.WeaponFiredEvent evt)
        // {
        //     // Attacker(yellow), Target(lightblue) 색상 적용 예시
        //     Log($"  -> [무기 발사] {ColorizeText(evt.Attacker.Name, "yellow")}의 {evt.Weapon.Name} (대상: {ColorizeText(evt.Target.Name, "lightblue")})", LogLevel.Debug);
        // }

        // DamageCalculated는 너무 상세하여 제거 고려 (현재 주석 처리)
        // private void LogDamageCalculated(DamageEvents.DamageCalculatedEvent evt)
        // {
        //     Log($"    * 데미지 계산: {evt.DamageSource} -> {evt.Target.Name} ({evt.TargetPartSlot}), 기본 {evt.BaseDamage:F1}, 최종 {evt.FinalDamage:F1}", LogLevel.Debug);
        // }

        private void LogDamageApplied(DamageEvents.DamageAppliedEvent evt)
        {
            // Wrap source and target names in brackets and apply colors
            string attackerName = ColorizeText($"[{evt.Source.Name}]", "yellow"); 
            string targetName = ColorizeText($"[{evt.Target.Name}]", "lightblue");
            string partName = evt.DamagedPart.ToString(); 
            string durabilityInfo = $"({evt.PartCurrentDurability:F0}/{evt.PartMaxDurability:F0})";

            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "    " : "";
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=0> <color=red>-></color> {attackerName}의 공격! {targetName}의 {partName}{durabilityInfo}에 {evt.DamageDealt:F1} 데미지!", LogLevel.Info); // HIT 아이콘
        }

        private void LogDamageAvoided(DamageEvents.DamageAvoidedEvent evt)
        {
            // Wrap source and target names in brackets and apply colors
            string attackerName = ColorizeText($"[{evt.Source.Name}]", "yellow");
            string targetName = ColorizeText($"[{evt.Target.Name}]", "lightblue");
            
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "    " : "";
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=1> <color=lightblue> << </color> {targetName}이(가) {attackerName}의 공격을 회피! ({evt.Type})", LogLevel.Info); // MISS 아이콘
        }

        private void LogPartDestroyed(PartEvents.PartDestroyedEvent evt)
        {
            // Wrap owner name in brackets and apply color (owner is the affected one)
            string ownerName = ColorizeText($"[{evt.Frame.Name}]", "yellow"); // Owner as yellow (actor context)
            string partName = evt.DestroyedPartType.ToString();
            string effectsInfo = evt.Effects != null && evt.Effects.Length > 0 ? $" ({string.Join(", ", evt.Effects)})" : "";
            
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "  " : "";
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=2> <color=orange>!!! {ownerName}의 {partName} 파괴됨!</color>{effectsInfo}", LogLevel.Warning); // DESTROYED 아이콘
        }
        
        // PartStatusChanged는 너무 상세하여 제거 고려 (현재 주석 처리)
        // private void LogPartStatusChanged(PartEvents.PartStatusChangedEvent evt)
        // {
        //     string partName = evt.ChangedPart?.Name ?? evt.SlotIdentifier;
        //     Log($"[T{_turnCounter}] {ColorizeText(evt.Owner.Name, "yellow")}의 {partName} 상태 변경: {evt.NewStatus}", LogLevel.Debug);
        // }

        private void LogSystemCriticalFailure(PartEvents.SystemCriticalFailureEvent evt)
        {
            // Wrap owner name in brackets and apply color (owner is the affected one)
            string ownerName = ColorizeText($"[{evt.Frame.Name}]", "yellow"); // Owner as yellow (actor context)
            // evt.Reason 속성이 없으므로 기본 메시지 사용 또는 다른 속성 확인 필요
            string reason = "치명적 시스템 오류"; // evt.Reason 대신 기본 메시지 사용
            
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "  " : "";
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=3> <color=purple>*** {ownerName}: {reason} ***</color>", LogLevel.Critical); // SYS FAIL 아이콘
        }
        
        // <<< 상태 효과 로깅 추가 >>>
        private void LogStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent evt)
        {
            // Wrap target name in brackets and apply color
            string targetName = ColorizeText($"[{evt.Target.Name}]", "lightblue");
            // Colorize source name if it exists
            string sourceInfo = evt.Source != null ? $"( {ColorizeText(evt.Source.Name, "yellow")}에 의해) " : "";
            
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "  >> " : ">> "; 
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=4> {targetName}: 상태 효과 '{evt.EffectType}' 적용됨 {sourceInfo}({evt.Duration}턴)", LogLevel.Info); // EFFECT+ 아이콘
        }

        private void LogStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent evt)
        {
            // Wrap target name in brackets and apply color
            string targetName = ColorizeText($"[{evt.Target.Name}]", "lightblue");
            string reason = evt.WasDispelled ? "(해제됨)" : "(만료됨)";
            
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = UseIndentation ? "  << " : "<< ";
            // <<< 인덱스 태그 추가 & 색상 적용된 이름 사용 >>>
            Log($"{prefix}<sprite index=5> {targetName}: 상태 효과 '{evt.EffectType}' 종료 {reason}", LogLevel.Info); // EFFECT- 아이콘
        }

        private void LogStatusEffectTicked(StatusEffectEvents.StatusEffectTickEvent evt)
        {
            // Colorize target name
            string targetNameColored = ColorizeText($"[{evt.Target.Name}]", "lightblue"); 
            string effectName = evt.Effect.EffectName;
            string tickAction = evt.Effect.TickEffectType == TickEffectType.DamageOverTime ? "피해" : "회복";
            // <<< 인덱스 태그 사용 >>>
            string tickIconTag = evt.Effect.TickEffectType == TickEffectType.DamageOverTime ? "<sprite index=6>" : "<sprite index=7>"; // TICK / HEAL TICK 아이콘
            // 들여쓰기 로직 제거 (이제 LogStatusEffectTicked는 들여쓰기 안 함)
            // <<< 유니코드 스프라이트 태그 추가 및 기존 이모지 제거 >>>
            string logMsg = $"<sprite index=0> {targetNameColored} < [{effectName}] 틱! ([{evt.Effect.TickValue:F0}] {tickAction})";

            Log(logMsg, LogLevel.Info);
        }
        // <<< 상태 효과 로깅 끝 >>>
        
        #endregion

        #region Formatting Methods

        private string FormatLogEntry(LogEntry entry)
        {
            StringBuilder formattedMessage = new StringBuilder();

            // 턴 넘버 접두사 (플래그 확인)
            if (ShowTurnPrefix && entry.TurnNumber > 0) // 0턴(초기화)은 제외
            {
                formattedMessage.Append($"[T{entry.TurnNumber}] ");
            }

            // 로그 레벨 접두사 (플래그 확인)
            if (ShowLogLevel)
            {
                switch (entry.Level)
                {
                    case LogLevel.Debug: formattedMessage.Append("[DBG] "); break;
                    case LogLevel.Warning: formattedMessage.Append("[WRN] "); break;
                    case LogLevel.Error: formattedMessage.Append("[ERR] "); break;
                    case LogLevel.System: formattedMessage.Append("[SYS] "); break;
                    // Info는 기본이라 생략하거나 [INF] 추가 가능
                    // case LogLevel.Info: formattedMessage.Append("[INF] "); break; 
                }
            }
            
            // 실제 로그 메시지 추가
            formattedMessage.Append(entry.Message);

            return formattedMessage.ToString();
        }

        private string FormatLogEntryForFile(LogEntry entry)
        {
            // FormatLogEntry와 동일하게 토글 플래그 적용
            StringBuilder formattedMessage = new StringBuilder();
            string messageContent = entry.Message;

            // <<< 스프라이트 태그를 텍스트 마커로 변환 >>>
            messageContent = ConvertSpriteTagToTextMarker(messageContent);

            // 턴 넘버 접두사 (플래그 확인)
            if (ShowTurnPrefix && entry.TurnNumber > 0)
            {
                formattedMessage.Append($"[T{entry.TurnNumber}] ");
            }

            // 로그 레벨 접두사 (플래그 확인)
            if (ShowLogLevel)
            {
                // 파일에는 레벨 약어 대신 전체 이름 사용 (선택적)
                formattedMessage.Append($"[{entry.Level.ToString().ToUpper()}] "); 
            }
            
            // 실제 로그 메시지 추가 (리치 텍스트 제거)
            formattedMessage.Append(RemoveRichTextTags(messageContent)); // 변환된 메시지 사용

            return formattedMessage.ToString();
        }

        // <<< 스프라이트 태그를 텍스트 마커로 변환하는 메서드 (인덱스 기반으로 수정) >>>
        private string ConvertSpriteTagToTextMarker(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            // <<< 인덱스-마커 매핑 테이블 >>>
            var markerMapping = new Dictionary<string, string>
            {
                // 인덱스 태그 형식: "<sprite index=N>"
                { "<sprite index=0>", "[HIT]" },          // 데미지 적용 / 내구도 감소
                { "<sprite index=1>", "[MISS]" },
                { "<sprite index=2>", "[DESTROYED]" },   // 파츠 파괴 / 유닛 파괴
                { "<sprite index=3>", "[SYS FAIL]" },      // 시스템 오류 / 무기 고장
                { "<sprite index=4>", "[EFFECT+]" },     // 상태 효과 적용 / 내구도 증가
                { "<sprite index=5>", "[EFFECT-]" },
                { "<sprite index=6>", "[TICK]" },          // 상태 효과 데미지 틱
                { "<sprite index=7>", "[HEAL TICK]" },     // 상태 효과 회복 틱
                { "<sprite index=8>", "[ATK]" },
                { "<sprite index=9>", "[DEF]" },
                { "<sprite index=10>", "[MOVE]" },
                { "<sprite index=11>", "[BATTLE START]" },
                { "<sprite index=12>", "[BATTLE END]" },
                { "<sprite index=13>", "[TURN START]" },
                { "<sprite index=14>", "[TURN END]" },
                { "<sprite index=15>", "[CRIT!]" },        // 크리티컬 히트
                { "<sprite index=16>", "[UNIT]" },          // 유닛 상세 정보 헤더
                { "<sprite index=17>", "[PART OK]" },       // 파츠/유닛/무기 정상
                { "<sprite index=18>", "[PART DMG]" },      // 파츠 손상
                { "<sprite index=19>", "[PART CRIT]" },     // 파츠 위험
                { "<sprite index=20>", "[PART EMPTY]" },    // 파츠 없음
                { "<sprite index=23>", "[COUNTER]" },   // 새 카운터 아이콘
                // 필요시 추가...
            };

            // 메시지 시작 부분에서 매핑되는 태그를 찾아 마커로 교체
            foreach (var kvp in markerMapping)
            {
                if (message.StartsWith(kvp.Key))
                {
                    // 태그를 마커로 바꾸고 뒤에 공백 추가, 나머지 메시지 부분 연결
                    return kvp.Value + " " + message.Substring(kvp.Key.Length).TrimStart(); 
                }
            }

            // 매핑되는 태그가 없으면 원본 메시지 반환
            return message;
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
            // <sprite=...> 태그도 제거되도록 함
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        }

        // <<< LogUnitStatusSummary 메서드 구현 (전투 종료 시 호출되도록 CombatEnd 로그 메서드 수정 필요) >>>
        private void LogUnitStatusSummary(string battleId)
        {
            // 전투 종료 시 모든 참가자의 최종 상태를 기록하기 위해 수정
            // 현재는 CombatSimulatorService에서 participants 리스트 접근 불가
            // TODO: 전투 종료 이벤트에 최종 참가자 상태 정보를 포함하거나, 서비스에서 이 메서드를 직접 호출하는 방식으로 변경 필요
            Log("전투 종료 후 유닛 상태 요약 로그 (구현 필요)", LogLevel.System);
        }

        // <<< LogUnitDetails 메서드 구현 (인덱스 기반 태그 사용) >>>
        public void LogUnitDetails(ArmoredFrame unit) // private -> public
        {
            if (unit == null) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"<sprite index=16> [{unit.Name}] 상태 | AP: {unit.CurrentAP:F1}/{unit.CombinedStats.MaxAP:F1}"); // UNIT 아이콘

            // <<< FrameBase에서 슬롯 정보 가져오기 >>>
            // ArmoredFrame의 FrameBase가 null이 아니어야 함
            IReadOnlyDictionary<string, PartSlotDefinition> slots = unit.FrameBase?.GetPartSlots(); 

            // FrameBase 또는 슬롯 정보가 없으면 기본 정보만 로그 남기고 종료
            if (slots == null)
            {
                sb.Append("\n  (슬롯 정보를 가져올 수 없음)");
                Log(sb.ToString(), LogLevel.Warning);
                return;
            }

            // <<< GetPartSlots() 에서 반환된 키(슬롯 식별자) 목록 사용 >>>
            foreach (var slotId in slots.Keys)
            {
                // _parts 딕셔너리에서 파츠 가져오기
                if (unit.Parts.TryGetValue(slotId, out Part part))
                {
                    // 파츠가 있는 경우
                    float currentDurability = part.CurrentDurability; // Part 클래스에 CurrentDurability 프로퍼티 가정
                    float maxDurability = part.MaxDurability;       // Part 클래스에 MaxDurability 프로퍼티 가정
                    float durabilityPercent = maxDurability > 0 ? currentDurability / maxDurability : 0;
                    string statusIconTag;

                    if (currentDurability <= 0)
                    {
                        statusIconTag = "<sprite index=2>"; // DESTROYED 아이콘
                    }
                    else if (durabilityPercent <= 0.3f)
                    {
                        statusIconTag = "<sprite index=19>"; // PART CRIT 아이콘
                    }
                    else if (durabilityPercent <= 0.7f)
                    {
                        statusIconTag = "<sprite index=18>"; // PART DMG 아이콘
                    }
                    else
                    {
                        statusIconTag = "<sprite index=17>"; // PART OK 아이콘
                    }
                    
                    // 상태 문자열 (추후 Part 클래스에 Status 프로퍼티가 있다면 그것도 표시)
                    string statusText = currentDurability <= 0 ? "(파괴됨)" : ""; 
                    
                    // 들여쓰기 적용하여 로그 라인 추가
                    string prefix = UseIndentation ? "  - " : "- ";
                    sb.Append($"\n{prefix}{statusIconTag} {slotId}: {currentDurability:F0}/{maxDurability:F0} {statusText}");
                }
                else
                {
                    // 파츠가 없는 경우
                    string prefix = UseIndentation ? "  - " : "- ";
                    string missingIconTag = "<sprite index=20>"; // PART EMPTY 아이콘
                    sb.Append($"\n{prefix}{missingIconTag} {slotId}: 없음"); 
                }
            }

            // 최종 로그 기록 (LogLevel은 Debug나 Info 중 선택)
            Log(sb.ToString(), LogLevel.Debug); 
        }

        #region Formatting Flag Control

        public void SetShowLogLevel(bool show)
        {
            ShowLogLevel = show;
        }

        public void SetShowTurnPrefix(bool show)
        {
            ShowTurnPrefix = show;
        }

        public void SetUseIndentation(bool use)
        {
            UseIndentation = use;
        }

        #endregion

        #endregion

        #region Additional Methods

        /// <summary>
        /// Gets the raw list of LogEntry objects.
        /// </summary>
        /// <returns>A list of LogEntry objects.</returns>
        public List<LogEntry> GetLogEntries()
        {
            // 방어적 복사본 반환 (선택적이지만 권장)
            return new List<LogEntry>(_logs); 
        }

        /// <summary>
        /// Directly adds a pre-constructed LogEntry to the internal log list.
        /// Use this when LogEntry details (like EventType and delta info) are set externally.
        /// </summary>
        /// <param name="entry">The LogEntry object to add.</param>
        public void AddLogEntryDirectly(LogEntry entry)
        {
             if (entry == null) return;
             _logs.Add(entry);
             // Note: We might not need to invoke OnLogAdded here if the consuming UI
             // fetches the full log list after combat ends.
             // If real-time log updates are needed, consider invoking:
             // OnLogAdded?.Invoke(FormatLogEntry(entry), entry.Level);
        }

        // +++ Fallback 메서드 추가 +++
        /// <summary>
        /// Gets the current turn number stored internally for logging purposes.
        /// Primarily used as a fallback if CombatSimulatorService is unavailable.
        /// </summary>
        public int GetCurrentTurnForLogging()
        {
            return _turnCounter;
        }

        /// <summary>
        /// Gets the current cycle number stored internally for logging purposes.
        /// Primarily used as a fallback if CombatSimulatorService is unavailable.
        /// </summary>
        public int GetCurrentCycleForLogging()
        {
             return _cycleCounter;
        }

        #endregion
    }
} 