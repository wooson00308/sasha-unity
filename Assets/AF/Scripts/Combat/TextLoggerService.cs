using AF.Services;
using UnityEngine;
using AF.EventBus;
using AF.Models;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AF.Data; // Required for FlavorTextSO
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random
using System; // 추가: Exception 클래스를 사용하기 위함
using AF.Tests; // +++ SASHA: CombatTestRunner 참조를 위해 추가

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
        private CombatTestRunner _combatTestRunnerCache; // +++ SASHA: CombatTestRunner 서비스 캐시
        
        // +++ Flavor Text Storage +++
        private Dictionary<string, List<string>> _flavorTextTemplates = new Dictionary<string, List<string>>();
        // +++ End Flavor Text Storage +++
        
        /// <summary>
        /// TextLogger 인스턴스에 대한 퍼블릭 접근자 (인터페이스 타입)
        /// </summary>
        public ITextLogger TextLogger => _textLogger;

        /// <summary>
        /// TextLogger 구체 인스턴스에 대한 퍼블릭 접근자 (외부 사용 시 주의)
        /// </summary>
        public TextLogger ConcreteLogger => _textLogger;

        private bool _logActionSummaries = true; // 행동 요약 로그 표시 여부 필드 추가
        private bool _useSpriteIcons = true; // <<< 아이콘 사용 여부 필드 추가

        #region Logger Formatting Control

        /// <summary>
        /// 로그 레벨 접두사 표시 여부를 설정합니다.
        /// </summary>
        public void SetShowLogLevel(bool show)
        {
            _textLogger?.SetShowLogLevel(show);
        }

        /// <summary>
        /// 턴 넘버 접두사 표시 여부를 설정합니다.
        /// </summary>
        public void SetShowTurnPrefix(bool show)
        {
            _textLogger?.SetShowTurnPrefix(show);
        }

        /// <summary>
        /// 로그 들여쓰기 사용 여부를 설정합니다.
        /// </summary>
        public void SetUseIndentation(bool use)
        {
            _textLogger?.SetUseIndentation(use);
        }

        /// <summary>
        /// 행동 요약 로그 표시 여부를 설정합니다.
        /// </summary>
        public void SetLogActionSummaries(bool log)
        {
            _logActionSummaries = log;
        }

        /// <summary>
        /// 로그 메시지에 스프라이트 아이콘 사용 여부를 설정합니다.
        /// </summary>
        public void SetUseSpriteIcons(bool use)
        {
            _useSpriteIcons = use;
        }

        #endregion
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            _textLogger = new TextLogger();
            
            // TextLogger 초기화
            _textLogger.Initialize();
            
            // +++ SASHA: CombatTestRunner 서비스 가져오기 +++
            try
            {
                _combatTestRunnerCache = ServiceLocator.Instance.GetService<CombatTestRunner>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TextLoggerService: CombatTestRunner 서비스를 찾을 수 없습니다. 팀 색상 기능이 제한될 수 있습니다. 오류: {ex.Message}");
                _combatTestRunnerCache = null;
            }
            // +++ SASHA: 추가 끝 +++
            
            // +++ Load Flavor Text Templates +++
            LoadFlavorTextTemplates();
            // +++ End Load Flavor Text Templates +++
            
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
            _flavorTextTemplates.Clear(); // Clear templates on shutdown

            Debug.Log("TextLoggerService가 종료되고 이벤트 구독을 해제했습니다.");
        }

        // +++ Method to Load Flavor Text Templates +++
        private void LoadFlavorTextTemplates()
        {
            _flavorTextTemplates.Clear();
            var loadedTemplates = Resources.LoadAll<FlavorTextSO>("FlavorTexts");

            if (loadedTemplates == null || loadedTemplates.Length == 0)
            {
                Debug.LogWarning("FlavorTextSO 에셋을 찾을 수 없습니다. Resources/FlavorTexts 폴더를 확인하세요.");
                return;
            }

            foreach (var templateSO in loadedTemplates)
            {
                if (string.IsNullOrEmpty(templateSO.templateKey) || string.IsNullOrEmpty(templateSO.templateText))
                {
                    Debug.LogWarning($"FlavorTextSO 에셋 ({templateSO.name})에 유효하지 않은 templateKey 또는 templateText가 있습니다.");
                    continue;
                }

                if (!_flavorTextTemplates.ContainsKey(templateSO.templateKey))
                {
                    _flavorTextTemplates[templateSO.templateKey] = new List<string>();
                }
                _flavorTextTemplates[templateSO.templateKey].Add(templateSO.templateText);
            }

            Debug.Log($"총 {_flavorTextTemplates.Count}개의 키에 대해 {loadedTemplates.Length}개의 FlavorText 템플릿을 로드했습니다.");
        }
        // +++ End Method to Load Flavor Text Templates +++

        // +++ Flavor Text Helper Methods +++
        public string GetRandomFlavorText(string templateKey)
        {
            if (_flavorTextTemplates.TryGetValue(templateKey, out var templateList) && templateList.Count > 0)
            {
                // UnityEngine.Random을 명시적으로 사용
                int randomIndex = Random.Range(0, templateList.Count);
                return templateList[randomIndex];
            }
            // 키를 찾지 못하거나 리스트가 비어있으면 null 또는 기본 메시지 반환
            // Debug.LogWarning($"Flavor text template key not found or empty: {templateKey}");
            return null; 
        }

        public string FormatFlavorText(string template, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(template) || parameters == null)
            {
                return template; // 원본 반환
            }

            string result = template;
            foreach (var kvp in parameters)
            {
                // Parameters are expected to be pre-formatted (bracketed and colored)
                string placeholder = "{" + kvp.Key + "}";
                result = result.Replace(placeholder, kvp.Value);
            }
            return result;
        }
        // +++ End Flavor Text Helper Methods +++

        // +++ SASHA: 새로운 팀 색상 적용 헬퍼 메서드 +++
        private string GetTeamColoredName(string name, int teamId, bool addBrackets = true) // Default is true
        {
            string coloredName = name;
            if (_combatTestRunnerCache != null && _combatTestRunnerCache.TryGetTeamColor(teamId, out Color teamColor))
            {
                coloredName = $"<color=#{ColorUtility.ToHtmlStringRGB(teamColor)}>{name}</color>";
            }
            return addBrackets ? $"[{coloredName}]" : coloredName;
        }
        // +++ SASHA: 추가 끝 +++

        // +++ Helper method to colorize text (copied from TextLogger.cs) +++
        // --- SASHA: 이 메서드는 GetTeamColoredName으로 대체되었으므로 삭제 ---
        /*
        private string ColorizeText(string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }
        */
        // +++ End Helper Method +++

        private void SubscribeToEvents()
        {
            if (_eventBus == null || _textLogger == null) return;

            // 전투 세션 이벤트
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Subscribe<CombatSessionEvents.UnitActivationStartEvent>(HandleUnitActivationStart);
            _eventBus.Subscribe<CombatSessionEvents.UnitActivationEndEvent>(HandleUnitActivationEnd);
            _eventBus.Subscribe<CombatSessionEvents.RoundStartEvent>(HandleRoundStart);
            _eventBus.Subscribe<CombatSessionEvents.RoundEndEvent>(HandleRoundEnd);

            // 전투 액션 이벤트
            _eventBus.Subscribe<CombatActionEvents.ActionStartEvent>(HandleActionStart);
            _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Subscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);

            // 데미지 이벤트
            _eventBus.Subscribe<CombatActionEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Subscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);

            // 파츠 이벤트
            _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);

            // 상태 효과 이벤트
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectExpiredEvent>(HandleStatusEffectExpired);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
            
            // +++ 수리 이벤트 구독 추가 +++
            _eventBus.Subscribe<CombatActionEvents.RepairAppliedEvent>(HandleRepairApplied);
            // +++ 수리 이벤트 구독 추가 끝 +++
            
            // 유닛 패배 이벤트
            // _eventBus.Subscribe<CombatSessionEvents.UnitDefeatedEvent>(HandleUnitDefeated);
            _eventBus.Subscribe<CombatActionEvents.CounterAttackAnnouncedEvent>(HandleCounterAttackAnnounced); // +++ SASHA: 카운터 공격 알림 구독
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null || _textLogger == null) return;

            // 구독했던 순서대로 해제 (혹은 타입별로)
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Unsubscribe<CombatSessionEvents.UnitActivationStartEvent>(HandleUnitActivationStart);
            _eventBus.Unsubscribe<CombatSessionEvents.UnitActivationEndEvent>(HandleUnitActivationEnd);
            _eventBus.Unsubscribe<CombatSessionEvents.RoundStartEvent>(HandleRoundStart);
            _eventBus.Unsubscribe<CombatSessionEvents.RoundEndEvent>(HandleRoundEnd);

            _eventBus.Unsubscribe<CombatActionEvents.ActionStartEvent>(HandleActionStart);
            _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Unsubscribe<CombatActionEvents.WeaponFiredEvent>(HandleWeaponFired);

            _eventBus.Unsubscribe<CombatActionEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Unsubscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);

            _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);

            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectExpiredEvent>(HandleStatusEffectExpired);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
            
            // +++ 수리 이벤트 구독 해제 추가 끝 +++
            
            // _eventBus.Unsubscribe<CombatSessionEvents.UnitDefeatedEvent>(HandleUnitDefeated);
            _eventBus.Unsubscribe<CombatActionEvents.CounterAttackAnnouncedEvent>(HandleCounterAttackAnnounced); // +++ SASHA: 카운터 공격 알림 구독 해제
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            // <<< 기존 Log 호출 주석 처리 >>>
            // string iconTag = _useSpriteIcons ? "<sprite index=11> " : "";
            // _textLogger?.Log($"{iconTag}=== 전투 시작 === ID: {ev.BattleId}, 이름: {ev.BattleName}", LogLevel.System, contextUnit: null, shouldUpdateTargetView: false);

            // +++ LogEntry 직접 생성 방식으로 변경 +++
            if (_textLogger != null)
            {
                string iconTag = _useSpriteIcons ? "<sprite index=11> " : "";
                string message = $"{iconTag}=== 전투 시작 === ID: {ev.BattleId}, 이름: {ev.BattleName}";

                var logEntry = new TextLogger.LogEntry(
                    message,
                    LogLevel.System,
                    0, // Combat start is turn 0
                    0, // Combat start is cycle 0
                    LogEventType.CombatStart, // <<< 타입 지정
                    contextUnit: null,
                    shouldUpdateTargetView: false,
                    turnStartStateSnapshot: null // CombatStart에는 스냅샷 없음
                );
                 _textLogger.AddLogEntryDirectly(logEntry);
            }
        }

        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            // <<< 기존 Log 호출 주석 처리 >>>
            // string iconTag = _useSpriteIcons ? "<sprite index=12> " : "";
            // _textLogger?.Log(
            //     $"{iconTag}=== 전투 종료 ===, 결과: {ev.Result}",
            //     LogLevel.Info,
            //     contextUnit: null,
            //     shouldUpdateTargetView: false,
            //     turnStartStateSnapshot: ev.FinalParticipantSnapshots
            // );
            // _textLogger?.Log($"//==[ Combat Session Terminated ]==//", LogLevel.System);

            // +++ LogEntry 직접 생성 방식으로 변경 +++
            if (_textLogger != null)
            {
                string iconTag = _useSpriteIcons ? "<sprite index=12> " : "";
                string message = $"{iconTag}=== 전투 종료 ===, 결과: {ev.Result}";
                // CombatSimulatorService에서 현재 턴/사이클 가져오기 (종료 시점)
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); } catch { /* ignored */ }
                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    message,
                    LogLevel.Info, // Info 레벨 사용
                    currentTurn,  // 종료 시점 턴
                    currentCycle, // 종료 시점 사이클
                    LogEventType.CombatEnd, // <<< 타입 지정
                    contextUnit: null,
                    shouldUpdateTargetView: false,
                    turnStartStateSnapshot: ev.FinalParticipantSnapshots // 최종 스냅샷 전달
                );
                _textLogger.AddLogEntryDirectly(logEntry);

                // 시스템 종료 메시지 추가
                var systemEndEntry = new TextLogger.LogEntry(
                    "//==[ Combat Session Terminated ]==//",
                    LogLevel.System,
                    currentTurn,
                    currentCycle,
                    LogEventType.SystemMessage, // SystemMessage 타입 사용
                    contextUnit: null,
                    shouldUpdateTargetView: false,
                    turnStartStateSnapshot: null
                );
                _textLogger.AddLogEntryDirectly(systemEndEntry);
            }
        }

        private void HandleUnitActivationStart(CombatSessionEvents.UnitActivationStartEvent ev)
        {
            // <<< 기존 Log 호출 주석 처리 및 LogEntry 직접 생성 방식으로 변경 >>>
            if (_textLogger != null)
            {
                // string activeUnitNameColored = ColorizeText($"[{ev.ActiveUnit.Name}]", "yellow"); // 이전 방식
                string activeUnitNameColored = GetTeamColoredName(ev.ActiveUnit.Name, ev.ActiveUnit.TeamId); // +++ SASHA: 팀 색상 사용 +++
                string apInfo = $" (AP: {ev.APBeforeRecovery:F1} -> {ev.ActiveUnit.CurrentAP:F1}/{ev.ActiveUnit.CombinedStats.MaxAP:F1})";
                string iconTag = _useSpriteIcons ? "> " : "";
                string message = $"{iconTag}Unit Activation: {activeUnitNameColored}{apInfo}";
            
                var snapshotDict = new Dictionary<string, ArmoredFrameSnapshot>();
                var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                if (simulator != null)
                {
                    var participants = simulator.GetParticipants();
                    if (participants != null)
                    {
                        foreach (var unit in participants)
                        {
                            if (unit != null && !string.IsNullOrEmpty(unit.Name))
                            {
                                snapshotDict[unit.Name] = new ArmoredFrameSnapshot(unit);
                            }
                        }
                    }
                }
                else { Debug.LogError("Could not get CombatSimulatorService to create unit activation start snapshot."); }

                var logEntry = new TextLogger.LogEntry(
                    message,
                    LogLevel.Info,
                    ev.CurrentTurn, // 이벤트의 턴 번호 사용
                    ev.CurrentCycle, // 이벤트의 사이클 번호 사용
                    LogEventType.UnitActivationStart, // <<< 타입 지정
                    contextUnit: ev.ActiveUnit,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: snapshotDict // 생성된 스냅샷 전달
                );
                 _textLogger.AddLogEntryDirectly(logEntry);
            }
        }

        private void HandleUnitActivationEnd(CombatSessionEvents.UnitActivationEndEvent ev)
        {
            // <<< 기존 Log 호출 주석 처리 >>>
            // string activeUnitNameColored = $"[{ev.ActiveUnit.Name}]";
            // string remainingApInfo = $" (Remaining AP: {ev.ActiveUnit.CurrentAP:F1})";
            // string iconTag = _useSpriteIcons ? "< " : "";
            // _textLogger?.Log(
            //     $"{iconTag}Unit Standby: {activeUnitNameColored}{remainingApInfo}",
            //     LogLevel.Info,
            //     contextUnit: ev.ActiveUnit,
            //     shouldUpdateTargetView: true,
            //     turnStartStateSnapshot: null
            // );
            // _textLogger?.Log($"_\n_", LogLevel.Info);

             // +++ LogEntry 직접 생성 방식으로 변경 +++
            if (_textLogger != null)
            {
                //string activeUnitNameColored = ColorizeText($"[{ev.ActiveUnit.Name}]", "yellow"); // 색상 적용 // 이전 방식
                //string activeUnitName = $"[{ev.ActiveUnit.Name}]"; // 색상 적용 // 이전 방식
                string remainingApInfo = $" (Remaining AP: {ev.ActiveUnit.CurrentAP:F1})";
                string iconTag = _useSpriteIcons ? "< " : "";
                //string message = $"{iconTag}Unit Standby: {activeUnitNameColored}{remainingApInfo}";
                string message = $"{iconTag}Unit Standby: {ev.ActiveUnit.Name}{remainingApInfo}";

                 var logEntry = new TextLogger.LogEntry(
                     message,
                     LogLevel.Info,
                     ev.CurrentTurn, // 이벤트의 턴 번호 사용
                     ev.CurrentCycle, // 이벤트의 사이클 번호 사용
                     LogEventType.UnitActivationEnd, // <<< 타입 지정
                     contextUnit: ev.ActiveUnit,
                     shouldUpdateTargetView: true,
                     turnStartStateSnapshot: null // 종료 시점에는 스냅샷 없음
                 );
                _textLogger.AddLogEntryDirectly(logEntry);

                _textLogger.Log(" ", LogLevel.Info);
                _textLogger.Log(" ", LogLevel.Info);
            }
        }

        private void HandleActionStart(CombatActionEvents.ActionStartEvent ev)
        {
            // 원래 주석 처리 되어 있었으므로 유지
            //_textLogger?.Log($"{ev.Actor.Name} 행동 시작.", LogLevel.Info, ev.Actor, false);
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
            if (ev.IsCounterAttack) return; // 반격 로그는 건너뛰기

            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = ""; // Initialize logMsg
            LogLevel logLevel = ev.Success ? LogLevel.Info : LogLevel.Warning;
            
            // 상세 이동 로그 처리
            if (ev.Action == CombatActionEvents.ActionType.Move && ev.Success)
            {
                string prefix = _textLogger.UseIndentation ? "  " : "";
                // string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow"); // 이전 방식
                string actorNameColored = GetTeamColoredName(ev.Actor.Name, ev.Actor.TeamId); // +++ SASHA: 팀 색상 사용 +++
                // string targetName = ev.MoveTarget != null ? ColorizeText($"[{ev.MoveTarget.Name}]", "lightblue") : "지정되지 않은 목표"; // 이전 방식
                string targetName = ev.MoveTarget != null ? GetTeamColoredName(ev.MoveTarget.Name, ev.MoveTarget.TeamId) : "지정되지 않은 목표"; // +++ SASHA: 팀 색상 사용 +++
                string distanceText = ev.DistanceMoved.HasValue ? $"{ev.DistanceMoved.Value:F1} 만큼" : "일정 거리만큼";
                string targetDistanceText = "";
                if (ev.MoveTarget != null && ev.NewPosition.HasValue)
                {
                    float finalDistance = Vector3.Distance(ev.NewPosition.Value, ev.MoveTarget.Position);
                    targetDistanceText = $". (거리: {finalDistance:F1}m)";
                }
                else if (ev.MoveTarget != null)
                {
                    float finalDistance = Vector3.Distance(ev.Actor.Position, ev.MoveTarget.Position);
                     targetDistanceText = $". (거리: {finalDistance:F1}m)";
                }
                string iconTag = _useSpriteIcons ? "<sprite index=10> " : "";
                logMsg = $"{prefix}{iconTag}{actorNameColored}(이)가 {targetName} 방향으로 {distanceText} 이동{targetDistanceText}";
            }
            // 방어 로그 처리
            else if (ev.Action == CombatActionEvents.ActionType.Defend && ev.Success)
            {
                string prefix = _textLogger.UseIndentation ? "  " : "";
                // string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow"); // 이전 방식
                string actorNameColored = GetTeamColoredName(ev.Actor.Name, ev.Actor.TeamId); // +++ SASHA: 팀 색상 사용 +++
                string actionIconTag = _useSpriteIcons ? "<sprite index=9> " : "";
                string apInfo = $"잔여 동력: {ev.Actor.CurrentAP:F1}/{ev.Actor.CombinedStats.MaxAP:F1}.";
                logMsg = $"{prefix}{actionIconTag}{actorNameColored} 방어 태세 돌입. {apInfo}";
            }
            // 나머지 일반 액션 (Attack, Reload, 실패 등)
            else if (_logActionSummaries)
            {
                // string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow"); // 이전 방식
                string actorNameColored = GetTeamColoredName(ev.Actor.Name, ev.Actor.TeamId); // +++ SASHA: 팀 색상 사용 +++
                string actionName = ev.Action.ToString();
                string prefix = _textLogger.UseIndentation ? "  " : "";
                string actionIconTag = "";
                if (_useSpriteIcons)
                {
                    switch (ev.Action)
                    {
                        case CombatActionEvents.ActionType.Attack: actionIconTag = "<sprite index=8> "; break;
                        case CombatActionEvents.ActionType.Reload: actionIconTag = "<sprite index=13> "; break;
                    }
                }
                if (ev.Success)
                {
                    string apInfo = $"잔여 동력: {ev.Actor.CurrentAP:F1}/{ev.Actor.CombinedStats.MaxAP:F1}.";
                    logMsg = $"{prefix}{actionIconTag}[{actorNameColored}] {actionName} 프로토콜 실행 완료. {apInfo}";
                }
                else
                {
                    string reason = string.IsNullOrEmpty(ev.ResultDescription) ? "알 수 없는 오류" : ev.ResultDescription;
                    logMsg = $"{prefix}{actionIconTag}[{actorNameColored}] {actionName} 프로토콜 실행 실패. (사유: {reason})";
                }
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg)) // logMsg가 생성되었을 때만 기록
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.ActionCompleted, // <<< 이벤트 타입 지정
                    contextUnit: ev.Actor,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.Action_ActorName = ev.Actor.Name;
                logEntry.Action_Type = ev.Action;
                logEntry.Action_IsSuccess = ev.Success;
                logEntry.Action_ResultDescription = ev.ResultDescription;
                logEntry.Action_TargetName = ev.MoveTarget?.Name; // MoveTarget이 null일 수 있음
                logEntry.Action_DistanceMoved = ev.DistanceMoved;
                logEntry.Action_NewPosition = ev.NewPosition;
                logEntry.Action_IsCounterAttack = ev.IsCounterAttack;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }

        private void HandleWeaponFired(CombatActionEvents.WeaponFiredEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            TextLoggerService loggerService = null;
            try { loggerService = ServiceLocator.Instance.GetService<TextLoggerService>(); } catch (Exception ex) { Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}"); }

            float distance = Vector3.Distance(ev.Attacker.Position, ev.Target.Position);
            string logMsg = "";
            string hitOrMissText = ev.Hit ? "명중!" : "빗나갔다!";
            string iconTag = "";
            if (_useSpriteIcons) { iconTag = ev.Hit ? "<sprite index=8> " : "<sprite index=1> "; }

            bool useFlavorText = false;
            if (loggerService != null && !string.IsNullOrEmpty(ev.Weapon.AttackFlavorKey))
            {
                string templateKey = ev.Weapon.AttackFlavorKey;
                string flavorText = loggerService.GetRandomFlavorText(templateKey);
                if (!string.IsNullOrEmpty(flavorText))
                {
                    var parameters = new Dictionary<string, string>
                    {
                        { "attacker", GetTeamColoredName(ev.Attacker.Name, ev.Attacker.TeamId) }, // Ensure brackets
                        { "target", GetTeamColoredName(ev.Target.Name, ev.Target.TeamId) },       // Ensure brackets
                        { "weaponName", ev.Weapon.Name },
                        { "distance", distance.ToString("F1") },
                        { "hitOrMiss", hitOrMissText },
                        { "isCounterAttack", ev.IsCounterAttack.ToString().ToLower() },
                        { "weapon", ev.Weapon?.Name ?? "알 수 없는 무기" }
                    };
                    logMsg = loggerService.FormatFlavorText(flavorText, parameters);
                    logMsg = $"{iconTag}{logMsg}"; 
                    useFlavorText = true;
                    if (ev.Weapon.MaxAmmo > 0) { logMsg += $" (탄약: {ev.Weapon.CurrentAmmo}/{ev.Weapon.MaxAmmo})"; }
                    else { logMsg += " (탄약: ∞)"; }
                }
            }
            if (!useFlavorText)
            {
                // string attackerColor = ev.IsCounterAttack ? "lightblue" : "yellow"; // 이전 방식
                // string targetColor = ev.IsCounterAttack ? "yellow" : "lightblue"; // 이전 방식
                // string attackerNameColored = ColorizeText($"[{ev.Attacker.Name}]", attackerColor); // 이전 방식
                // string targetNameColored = ColorizeText($"[{ev.Target.Name}]", targetColor); // 이전 방식
                string attackerNameColored = GetTeamColoredName(ev.Attacker.Name, ev.Attacker.TeamId); // +++ SASHA: 팀 색상 사용 +++
                string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId);     // +++ SASHA: 팀 색상 사용 +++
                string ammoStatus = ev.Weapon.MaxAmmo <= 0 ? "(탄약: ∞)" : $"(탄약: {ev.Weapon.CurrentAmmo}/{ev.Weapon.MaxAmmo})";
                logMsg = $"{iconTag}{attackerNameColored}의 {ev.Weapon.Name}(이)가 {distance:F1}m 거리에서 {targetNameColored}에게 {hitOrMissText} {ammoStatus}";
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();
            ArmoredFrame contextUnit = ev.IsCounterAttack ? ev.Target : ev.Attacker;
             
                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    LogLevel.Info,
                    currentTurn,
                    currentCycle,
                    LogEventType.WeaponFired, // <<< 이벤트 타입 지정
                    contextUnit: contextUnit,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.Weapon_AttackerName = ev.Attacker.Name;
                logEntry.Weapon_TargetName = ev.Target.Name;
                logEntry.Weapon_WeaponName = ev.Weapon?.Name;
                logEntry.Weapon_IsHit = ev.Hit;
                logEntry.Weapon_IsCounterAttack = ev.IsCounterAttack;
                logEntry.Weapon_CurrentAmmo = ev.Weapon?.CurrentAmmo ?? -1;
                logEntry.Weapon_MaxAmmo = ev.Weapon?.MaxAmmo ?? -1;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }

        private void HandleDamageApplied(CombatActionEvents.DamageAppliedEvent ev)
        {
            // Flavor Text 생성 로직은 그대로 유지 (LogEntry의 Message 필드를 채우기 위함)
            string logMsg;
            string iconTagFlavor = _useSpriteIcons ? "<sprite index=0> " : "";

            if (ev.IsCounterAttack)
            {
                string counterAttackerNameColored = GetTeamColoredName(ev.Source?.Name ?? "Unknown Source", ev.Source?.TeamId ?? -1);
                string counterTargetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId);
                string criticalTag = ev.IsCritical ? " <sprite index=15>!!" : "";
                string partName = ev.DamagedPart?.Name ?? "알 수 없는 파츠";
                string prefix = _textLogger.UseIndentation ? "    " : "";
                logMsg = $"{prefix}{iconTagFlavor}{counterAttackerNameColored}의 반격! {counterTargetNameColored}의 {partName}에 {ev.DamageDealt:F0} 피해!{criticalTag}";
            }
            else
            {
                string templateKey;
                float damagePercentage = ev.PartMaxDurability > 0 ? ev.DamageDealt / ev.PartMaxDurability : 0f;
                if (ev.IsCritical) { templateKey = "DamageApplied_Crit"; }
                else if (damagePercentage > 0.3f) { templateKey = "DamageApplied_High"; }
                else { templateKey = "DamageApplied_Low"; }

                var parameters = new Dictionary<string, string>
                {
                    { "attacker", GetTeamColoredName(ev.Source?.Name ?? "알 수 없는 공격자", ev.Source?.TeamId ?? -1) },
                    { "target", GetTeamColoredName(ev.Target.Name, ev.Target.TeamId) },
                    { "part", ev.DamagedPart?.Name ?? "알 수 없는 파츠" },
                    { "damage", ev.DamageDealt.ToString("F0") },
                    { "weapon", "무기" }
                };
                string flavorText = GetRandomFlavorText(templateKey);
                string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

                if (!string.IsNullOrEmpty(formattedFlavorLog))
                {
                    logMsg = $"{iconTagFlavor}{formattedFlavorLog}";
                }
                else
                {
                    // Default message if flavor text fails
                    // string attackerNameColored = ColorizeText($"[{ev.Source?.Name ?? "Unknown Source"}]", "yellow"); // 이전 방식
                    // string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue"); // 이전 방식
                    string attackerNameColored = GetTeamColoredName(ev.Source?.Name ?? "Unknown Source", ev.Source?.TeamId ?? -1); // +++ SASHA: 팀 색상 +++
                    string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                    logMsg = $"{iconTagFlavor}{attackerNameColored}가 {targetNameColored}의 {ev.DamagedPart}에 {ev.DamageDealt:F0} 피해를 입혔습니다.";
                    Debug.LogWarning($"Flavor text not found for key: {templateKey}");
                }
            }

            // --- 델타 로그 기록 로직 ---
            if (_textLogger != null) // TextLogger 인스턴스 확인
            {
                // CombatSimulatorService에서 현재 턴과 사이클 번호 가져오기
                ICombatSimulatorService simulator = null;
                try{ // ServiceLocator 사용 시 예외 발생 가능성 고려
                    simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                } catch (Exception serviceEx) {
                    Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}");
                }

                // Fallback 값 설정 (Simulator가 null일 경우 대비)
                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                // 새 LogEntry 생성 (EventType: DamageApplied)
                var logEntry = new TextLogger.LogEntry(
                    logMsg, // 위에서 생성한 flavor text 메시지
                    LogLevel.Info,
                    currentTurn,
                    currentCycle,
                    LogEventType.DamageApplied, // 이벤트 타입 지정
                    contextUnit: ev.Target, // 피격 대상이 주요 컨텍스트
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null // 전체 스냅샷은 저장 안 함
                );

                // 델타 정보 채우기
                logEntry.Damage_SourceUnitName = ev.Source?.Name;
                logEntry.Damage_TargetUnitName = ev.Target.Name;
                logEntry.Damage_DamagedPartSlot = ev.DamagedPart.ToString(); // PartType을 string으로 변환
                logEntry.Damage_AmountDealt = ev.DamageDealt;
                logEntry.Damage_NewDurability = ev.PartCurrentDurability; // DamageAppliedEvent에 이미 적용 후 내구도 정보가 있음
                logEntry.Damage_IsCritical = ev.IsCritical;
                // PartWasDestroyed 정보는 DamageAppliedEvent에 직접적으로 없으므로,
                // NewDurability가 0 이하인지 체크하여 추론하거나, PartDestroyedEvent를 별도로 처리해야 함.
                logEntry.Damage_PartWasDestroyed = ev.PartCurrentDurability <= 0; // 간단한 추론 방식

                // TextLogger의 AddLogEntryDirectly 메서드 호출
                _textLogger.AddLogEntryDirectly(logEntry);

                // 참고: 재생 로직(CombatTextUIService 등)에서 EventType이 DamageApplied인 로그를 만나면,
                // 이 델타 정보를 사용하여 UI를 업데이트하도록 수정해야 합니다.
            }
            // --- 델타 로그 기록 로직 끝 ---

            // 이 핸들러 내의 기존 _textLogger.Log(...) 호출은 제거됨
        }

        private void HandleDamageAvoided(DamageEvents.DamageAvoidedEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 유사) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Info;
            string iconTag = _useSpriteIcons ? "<sprite index=1> " : "";

             // Check if this is avoidance from a counter-attack
            if (ev.IsCounterAttack)
            {
                // Construct specific log for counter-attack avoidance (no flavor text)
                // string counterAttackerNameColored = ColorizeText($"[{ev.Source.Name}]", "yellow"); // Source is the counter-attacker // 이전 방식
                // string counterTargetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue"); // Target is the one avoiding the counter // 이전 방식
                string counterAttackerNameColored = GetTeamColoredName(ev.Source.Name, ev.Source.TeamId); // +++ SASHA: 팀 색상 +++
                string counterTargetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId);     // +++ SASHA: 팀 색상 +++
                string prefix = _textLogger.UseIndentation ? "    " : ""; 
                logMsg =
                    $"{prefix}{iconTag}{counterTargetNameColored}(이)가 {counterAttackerNameColored}의 반격을 회피! ({ev.Type})";
            }
            else // Normal avoidance, use flavor text
            {
                string templateKey = $"DamageAvoided_{ev.Type}"; // e.g., "DamageAvoided_Dodge"
                var parameters = new Dictionary<string, string>();
                // parameters.Add("attacker", ev.Source?.Name ?? "알 수 없는 공격자"); // 이전 방식
                // parameters.Add("target", ev.Target.Name); // Avoider is the target // 이전 방식
                parameters.Add("attacker", GetTeamColoredName(ev.Source?.Name ?? "알 수 없는 공격자", ev.Source?.TeamId ?? -1, addBrackets: false)); // +++ SASHA: 팀 색상, 대괄호X +++
                parameters.Add("target", GetTeamColoredName(ev.Target.Name, ev.Target.TeamId, addBrackets: false)); // +++ SASHA: 팀 색상, 대괄호X +++

                string flavorText = GetRandomFlavorText(templateKey);
                string formattedFlavorLog = FormatFlavorText(flavorText, parameters); // +++ SASHA: 수정된 FormatFlavorText는 단순 치환만 함 +++

                if (!string.IsNullOrEmpty(formattedFlavorLog))
                {
                     logMsg = $"{iconTag}{formattedFlavorLog}";
                }
                else // Fallback
                {
                    // Default message if flavor text fails
                    // string attackerNameColored = ColorizeText($"[{ev.Source?.Name ?? "Unknown Source"}]", "yellow"); // 이전 방식
                    // string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue"); // 이전 방식
                    string attackerNameColored = GetTeamColoredName(ev.Source?.Name ?? "Unknown Source", ev.Source?.TeamId ?? -1); // +++ SASHA: 팀 색상 +++
                    string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                    logMsg = $"{iconTag}{targetNameColored}(이)가 {attackerNameColored}의 공격을 회피! ({ev.Type})";
                    Debug.LogWarning($"Flavor text not found for key: {templateKey}");
                }
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.DamageAvoided, // <<< 이벤트 타입 지정
                    contextUnit: ev.Target, // 회피한 유닛이 컨텍스트
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.Avoid_SourceName = ev.Source?.Name;
                logEntry.Avoid_TargetName = ev.Target.Name;
                logEntry.Avoid_Type = ev.Type;
                logEntry.Avoid_IsCounterAttack = ev.IsCounterAttack;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 ---

            // 기존 _textLogger?.Log(...) 호출은 제거됨
        }

        private void HandlePartDestroyed(PartEvents.PartDestroyedEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Warning;
            string iconTag = _useSpriteIcons ? "<sprite index=2> " : "";

            // 1. Determine Template Key based on PartType
            string partTypeString = ev.DestroyedPartType.ToString();
            string templateKey;
            switch (ev.DestroyedPartType)
            {
                case PartType.Head: templateKey = "PartDestroyed_Head"; break;
                case PartType.Body: templateKey = "PartDestroyed_Body"; break;
                case PartType.Arm: templateKey = "PartDestroyed_Arm"; break;
                case PartType.Legs: templateKey = "PartDestroyed_Legs"; break;
                default: templateKey = "PartDestroyed_General"; break;
            }

            // 2. Prepare parameters
            var parameters = new Dictionary<string, string>
            {
                { "target", GetTeamColoredName(ev.Frame.Name, ev.Frame.TeamId) }, // Ensure brackets
                { "part", partTypeString },
                { "destroyer", GetTeamColoredName(ev.Destroyer?.Name ?? "알 수 없는 원인", ev.Destroyer?.TeamId ?? -1) } // Ensure brackets
            };

            // 3. Get and format the flavor text
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            // 4. Log the flavor text (if found)
            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                 logMsg = $"{iconTag}{formattedFlavorLog}";
            }
            else // Fallback
            {
                // Default message if flavor text fails
                // string ownerNameColored = ColorizeText($"[{ev.Frame.Name}]", "yellow"); // Owner as yellow // 이전 방식
                string ownerNameColored = GetTeamColoredName(ev.Frame.Name, ev.Frame.TeamId); // +++ SASHA: 팀 색상 +++
                // string destroyerInfo = ev.Destroyer != null ? $" by {ColorizeText(ev.Destroyer.Name, "yellow")}" : ""; // Destroyer colored if present // 이전 방식
                string destroyerInfo = ev.Destroyer != null ? $" by {GetTeamColoredName(ev.Destroyer.Name, ev.Destroyer.TeamId)}" : ""; // +++ SASHA: 팀 색상 +++
                logMsg = $"{iconTag}<color=orange>!!! {ownerNameColored}'s {partTypeString} destroyed!{destroyerInfo}</color>";
                Debug.LogWarning($"Flavor text not found for key: {templateKey}");
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                 ICombatSimulatorService simulator = null;
                 try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                 catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                 int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                 int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                 var logEntry = new TextLogger.LogEntry(
                     logMsg,
                     logLevel,
                     currentTurn,
                     currentCycle,
                     LogEventType.PartDestroyed, // <<< 이벤트 타입 지정
                     contextUnit: ev.Frame, // 파괴된 유닛이 컨텍스트
                     shouldUpdateTargetView: true,
                     turnStartStateSnapshot: null
                 );

                 // 델타 정보 채우기
                 logEntry.PartDestroyed_OwnerName = ev.Frame.Name;
                 logEntry.PartDestroyed_PartType = ev.DestroyedPartType; // Nullable 아님
                 logEntry.PartDestroyed_DestroyerName = ev.Destroyer?.Name; // Nullable
                 logEntry.PartDestroyed_SlotId = ev.DestroyedSlotId; // <<< SASHA: SlotId 값 할당
                 logEntry.PartDestroyed_FrameWasActuallyDestroyed = ev.FrameWasDestroyed; // <<< SASHA: 플래그 값 할당

                 _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++

            // 기존 _textLogger?.Log(...) 호출은 제거됨
            // 기존 상세 로그 로직도 제거됨
        }

        private void HandleStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Info;

            string templateKey = "StatusEffectApplied";
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string durationText = ev.Duration == -1 ? "영구 지속" : $"{ev.Duration}턴";

            var parameters = new Dictionary<string, string>
            {
                { "target", GetTeamColoredName(ev.Target.Name, ev.Target.TeamId) }, // Ensure brackets
                { "effect", effectName },
                { "source", GetTeamColoredName(ev.Source?.Name ?? "알 수 없는 원인", ev.Source?.TeamId ?? -1) }, // Ensure brackets
                { "duration", durationText },
                { "magnitude", ev.Magnitude.ToString("F1") }
            };
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                string iconTag = _useSpriteIcons ? "<sprite index=4> " : "";
                logMsg = $"{iconTag}{formattedFlavorLog}";
            }
            else // Fallback
            {
                 // string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue"); // 이전 방식
                 string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                 string sourceInfo = ev.Source != null ? $" ({GetTeamColoredName(ev.Source.Name, ev.Source.TeamId)}에 의해)" : ""; // +++ SASHA: 팀 색상 +++
                 logMsg = $"{(_useSpriteIcons ? "<sprite index=4> " : "")}{targetNameColored}: 상태 효과 '{effectName}' 적용됨 {sourceInfo}({durationText})";
                 Debug.LogWarning($"Flavor text not found for key: {templateKey}");
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.StatusEffectApplied, // <<< 이벤트 타입 지정
                    contextUnit: ev.Target,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.StatusApplied_TargetName = ev.Target.Name;
                logEntry.StatusApplied_SourceName = ev.Source?.Name;
                logEntry.StatusApplied_EffectType = ev.EffectType; // Nullable 아님
                logEntry.StatusApplied_Duration = ev.Duration;
                logEntry.StatusApplied_Magnitude = ev.Magnitude;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }

        private void HandleStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Info;

            string templateKey = "StatusEffectExpired";
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string reason = ev.WasDispelled ? "해제됨" : "만료됨";

            var parameters = new Dictionary<string, string>
            {
                { "effect", effectName },
                { "reason", reason }
            };
            string flavorText = GetRandomFlavorText(templateKey);
            string iconTag = _useSpriteIcons ? "<sprite index=5> " : "";

            if (!string.IsNullOrEmpty(flavorText))
            {
                string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                string templateWithoutTarget = flavorText.Replace("{target}", "");
                string formattedRestOfString = FormatFlavorText(templateWithoutTarget, parameters);
                logMsg = $"{iconTag}{targetNameColored}{formattedRestOfString}";
            }
            else // Fallback
            {
                string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                string effectNameColored = $"<color=grey>{effectName}</color>"; // +++ SASHA: 직접 태그 사용 +++
                logMsg = $"{iconTag}{targetNameColored}의 {effectNameColored} 효과가 {reason}.";
                 Debug.LogWarning($"Flavor text not found for key: {templateKey}. Using default expiration log.");
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.StatusEffectExpired, // <<< 이벤트 타입 지정
                    contextUnit: ev.Target,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.StatusExpired_TargetName = ev.Target.Name;
                logEntry.StatusExpired_EffectType = ev.EffectType; // Nullable 아님
                logEntry.StatusExpired_WasDispelled = ev.WasDispelled;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }

        private void HandleStatusEffectTick(StatusEffectEvents.StatusEffectTickEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Info;

            string templateKey;
            string tickIconTag = "";
            if (ev.Effect.TickEffectType == TickEffectType.DamageOverTime)
            {
                templateKey = "StatusEffectTick_Damage";
                if (_useSpriteIcons) tickIconTag = "<sprite index=6> ";
            }
            else
            {
                templateKey = "StatusEffectTick_Heal";
                if (_useSpriteIcons) tickIconTag = "<sprite index=7> ";
            }

            string effectName = ev.Effect.EffectName;
            var parameters = new Dictionary<string, string>
            {
                { "target", ev.Target.Name },
                { "effect", effectName },
                { "value", ev.Effect.TickValue.ToString("F0") } 
            };
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                logMsg = $"{tickIconTag}{formattedFlavorLog}";
            }
            else // Fallback
            {
                string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId); // +++ SASHA: 팀 색상 +++
                string tickAction = ev.Effect.TickEffectType == TickEffectType.DamageOverTime ? "피해" : "회복";
                logMsg = $"{tickIconTag}{targetNameColored} < [{effectName}] 틱! ([{ev.Effect.TickValue:F0}] {tickAction})";
                Debug.LogWarning($"Flavor text not found for key: {templateKey}");
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.StatusEffectTicked, // <<< 이벤트 타입 지정
                    contextUnit: ev.Target,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.StatusTick_TargetName = ev.Target.Name;
                logEntry.StatusTick_EffectName = ev.Effect.EffectName;
                logEntry.StatusTick_Value = ev.Effect.TickValue;
                logEntry.StatusTick_TickType = ev.Effect.TickEffectType;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }
        
        // +++ 새로운 이벤트 핸들러 추가 +++
        private void HandleRepairApplied(CombatActionEvents.RepairAppliedEvent ev)
        {
            // --- Flavor Text/Log Message 생성 로직 (기존과 동일) ---
            string logMsg = "";
            LogLevel logLevel = LogLevel.Info;

            string templateKey = ev.ActionType == CombatActionEvents.ActionType.RepairAlly
                ? "RepairAlly_Success"
                : "RepairSelf_Success";
            var parameters = new Dictionary<string, string>
            {
                { "actor", ev.Actor.Name },
                { "target", ev.Target.Name },
                { "part", ev.TargetSlotIdentifier },
                { "amount", ev.AmountRepaired.ToString("F1") }
            };
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);
            string iconTag = _useSpriteIcons ? "<sprite index=7> " : "";

            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                logMsg = $"{iconTag}{formattedFlavorLog}";
            }
            else // Fallback
            {
                string actorNameColored = GetTeamColoredName(ev.Actor.Name, ev.Actor.TeamId);
                string targetNameColored = GetTeamColoredName(ev.Target.Name, ev.Target.TeamId);
                string repairedAmountColored = $"<color=lime>+{ev.AmountRepaired:F1}</color>";
                if (ev.ActionType == CombatActionEvents.ActionType.RepairAlly)
                {
                     logMsg = $"{iconTag}{actorNameColored}(이)가 {targetNameColored}의 [{ev.TargetSlotIdentifier}] 파츠 {repairedAmountColored} 수리 완료.";
                }
                else
                {
                     logMsg = $"{iconTag}자가 수리 완료: [{ev.TargetSlotIdentifier}] 파츠 {repairedAmountColored} 회복.";
                }
                 Debug.LogWarning($"Flavor text not found for key: {templateKey}. Using default repair log.");
            }
            // --- Log Message 생성 로직 끝 ---

            // +++ 델타 로그 기록 로직 +++
            if (_textLogger != null && !string.IsNullOrEmpty(logMsg))
            {
                ICombatSimulatorService simulator = null;
                try { simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); }
                catch (Exception serviceEx) { Debug.LogError($"CombatSimulatorService 가져오기 실패: {serviceEx.Message}"); }

                int currentTurn = simulator?.CurrentTurn ?? _textLogger.GetCurrentTurnForLogging();
                int currentCycle = simulator?.CurrentCycle ?? _textLogger.GetCurrentCycleForLogging();

                var logEntry = new TextLogger.LogEntry(
                    logMsg,
                    logLevel,
                    currentTurn,
                    currentCycle,
                    LogEventType.RepairApplied, // <<< 이벤트 타입 지정
                    contextUnit: ev.Target,
                    shouldUpdateTargetView: true,
                    turnStartStateSnapshot: null
                );

                // 델타 정보 채우기
                logEntry.Repair_ActorName = ev.Actor.Name;
                logEntry.Repair_TargetName = ev.Target.Name;
                logEntry.Repair_PartSlot = ev.TargetSlotIdentifier;
                logEntry.Repair_Amount = ev.AmountRepaired;
                logEntry.Repair_ActionType = ev.ActionType;

                _textLogger.AddLogEntryDirectly(logEntry);
            }
            // +++ 델타 로그 기록 로직 끝 +++
        }
        // +++ 새로운 이벤트 핸들러 추가 끝 +++
        
        // +++ SASHA: 카운터 공격 알림 이벤트 핸들러 +++
        private void HandleCounterAttackAnnounced(CombatActionEvents.CounterAttackAnnouncedEvent ev)
        {
            if (_textLogger == null) return;

            string defenderNameColored = GetTeamColoredName(ev.Defender.Name, ev.Defender.TeamId);
            string attackerNameColored = GetTeamColoredName(ev.Attacker.Name, ev.Attacker.TeamId);
            // TODO: TextLogger.cs 의 LogEventType 에 CounterAttackAnnounced 추가 후 아래 라인 수정 필요
            // 현재는 임시로 SystemMessage 사용
            string message = $"{defenderNameColored}의 <color=lightblue>카운터!</color> ({attackerNameColored}에게)";

            var logEntry = new TextLogger.LogEntry(
                message,
                LogLevel.Info, // 일반 정보 레벨
                ev.TurnNumber, // 이벤트에서 턴 번호 가져오기
                _textLogger.GetCurrentCycleForLogging(), // 현재 사이클 사용 (필요시 CombatContext 등에서 전달받도록 수정 가능)
                LogEventType.CounterAttackAnnounced, // <<< 타입 변경 
                contextUnit: ev.Defender, // 카운터 주체가 컨텍스트
                shouldUpdateTargetView: true,
                turnStartStateSnapshot: null // 이 이벤트는 상태 스냅샷을 가지지 않음
            );
            
            // 델타 정보 (필요하다면 추가)
            logEntry.Counter_DefenderName = ev.Defender.Name;
            logEntry.Counter_AttackerName = ev.Attacker.Name;

            _textLogger.AddLogEntryDirectly(logEntry);
        }
        // +++ SASHA: 카운터 공격 알림 이벤트 핸들러 끝 +++

        private Dictionary<(ArmoredFrame, string), float> _previousPartDurability = new Dictionary<(ArmoredFrame, string), float>();
        private Dictionary<ArmoredFrame, float> _previousUnitAP = new Dictionary<ArmoredFrame, float>();

        private void LogAllUnitDetailsOnInit(ArmoredFrame[] participants)
        {
            string iconTag = _useSpriteIcons ? "<sprite index=16> " : ""; // <<< 아이콘 조건부 추가
            _textLogger.Log($"{iconTag}--- Initial Units Status ---", LogLevel.Info); // <<< 아이콘 변수 사용
            _previousPartDurability.Clear();
            _previousUnitAP.Clear(); // Also clear AP tracking
            if (participants != null)
            {
                foreach (var unit in participants)
                {
                    // LogUnitDetailsInternal(unit, true); // REMOVED: Logging call removed, but tracking setup might still be useful
                    // Initialize tracking data
                    if (unit != null)
                    {
                        _previousUnitAP[unit] = unit.CurrentAP;
                        foreach (var kvp in unit.Parts)
                        {
                            _previousPartDurability[(unit, kvp.Key)] = kvp.Value.CurrentDurability;
                        }
                    }
                }
            }
        }
        
        /* // REMOVED or Commented Out: No longer needed for direct logging
        private void LogAllUnitDetailsOnTurnEnd()
        {
            // ... (Original code commented out as details are handled by UI now) ...
        }

        private void LogUnitDetailsOnTurnStart(ArmoredFrame unit)
        {
            // ... (Original code commented out) ...
        }

        private void LogUnitDetailsInternal(ArmoredFrame unit, bool isInitialLog)
        {
            // ... (Original code commented out - this formatting logic will move to UI service) ...
            // _textLogger.Log(sb.ToString().TrimEnd('\r', '\n'), LogLevel.UnitDetail); // Log details as UnitDetail -> This was the key change target
        }
        */

        private void HandleRoundStart(CombatSessionEvents.RoundStartEvent ev)
        {
            // <<< 기존 Log 호출 주석 처리 >>>
            // _textLogger?.Log($"//==[ Combat Turn {ev.RoundNumber} Initiated ]==//", LogLevel.System);

            // +++ LogEntry 직접 생성 방식으로 변경 +++
            if (_textLogger != null)
            {
                 string message = $"//==[ Combat Turn <color=orange>{ev.RoundNumber}</color> Initiated ]==//"; // +++ SASHA: 직접 태그 사용 +++

                 // 라운드 시작 시 스냅샷 생성 (UnitActivationStart와 유사하게)
                 var snapshotDict = new Dictionary<string, ArmoredFrameSnapshot>();
                 var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                 if (simulator != null)
                 {
                     // 라운드 시작 이벤트에 포함된 initiativeSequence 사용
                     var participants = ev.InitiativeSequence;
                     if (participants != null)
                     {
                         foreach (var unit in participants)
                         {
                             if (unit != null && !string.IsNullOrEmpty(unit.Name))
                             {
                                 snapshotDict[unit.Name] = new ArmoredFrameSnapshot(unit);
                             }
                         }
                     }
                 }
                 else { Debug.LogError("Could not get CombatSimulatorService to create round start snapshot."); }

                 var logEntry = new TextLogger.LogEntry(
                     message,
                     LogLevel.System,
                     ev.RoundNumber, // 이벤트의 라운드(턴) 번호 사용
                     0, // 라운드 시작은 사이클 0
                     LogEventType.RoundStart, // <<< 타입 지정
                     contextUnit: null,
                     shouldUpdateTargetView: false,
                     turnStartStateSnapshot: snapshotDict // 생성된 스냅샷 전달
                 );
                 _textLogger.AddLogEntryDirectly(logEntry);
            }
        }

        private void HandleRoundEnd(CombatSessionEvents.RoundEndEvent ev)
        {
            // 기존 주석 처리 유지
            //_textLogger?.Log($"//==[ Combat Turn {ev.RoundNumber} Complete ]==//", LogLevel.System);
        }

        // --- SASHA: 로거 설정 변경 메서드 추가 ---
        public void SetAllowedLogLevels(LogLevelFlags flags)
        {
            if (ConcreteLogger != null) ConcreteLogger.AllowedLogLevels = flags;
        }
    }
} 