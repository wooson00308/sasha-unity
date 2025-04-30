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
                string placeholder = "{" + kvp.Key + "}";
                string replacementValue = kvp.Value; // Get the original replacement value

                // Apply color based on the placeholder key
                switch (kvp.Key)
                {
                    case "attacker":
                    case "source":
                    case "destroyer": // Keys typically representing the actor/origin
                        replacementValue = ColorizeText(replacementValue, "yellow");
                        break;
                    case "target": // Key typically representing the recipient
                        replacementValue = ColorizeText(replacementValue, "lightblue");
                        break;
                    // Add other specific keys if needed, e.g.:
                    // case "ally": 
                    //     replacementValue = ColorizeText(replacementValue, "green"); 
                    //     break;
                }

                result = result.Replace(placeholder, replacementValue); // Use the potentially colorized value
            }
            return result;
        }
        // +++ End Flavor Text Helper Methods +++

        // +++ Helper method to colorize text (copied from TextLogger.cs) +++
        private string ColorizeText(string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }
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
            _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
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

            _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Unsubscribe<DamageEvents.DamageAvoidedEvent>(HandleDamageAvoided);

            _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);

            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectExpiredEvent>(HandleStatusEffectExpired);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
            
            // +++ 수리 이벤트 구독 해제 추가 +++
            _eventBus.Unsubscribe<CombatActionEvents.RepairAppliedEvent>(HandleRepairApplied);
            // +++ 수리 이벤트 구독 해제 추가 끝 +++
            
            // _eventBus.Unsubscribe<CombatSessionEvents.UnitDefeatedEvent>(HandleUnitDefeated);
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            string iconTag = _useSpriteIcons ? "<sprite index=11> " : ""; // <<< 아이콘 조건부 추가
            _textLogger?.Log($"{iconTag}=== 전투 시작 === ID: {ev.BattleId}, 이름: {ev.BattleName}", LogLevel.System, contextUnit: null, shouldUpdateTargetView: false); // Context: null, Update: false
            // LogAllUnitDetailsOnInit(ev.Participants); // Logging call removed
        }

        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            string iconTag = _useSpriteIcons ? "<sprite index=12> " : ""; // <<< 아이콘 조건부 추가
            // TextLogger의 LogEvent 사용하던 것 복구 -> 스냅샷 포함하도록 수정
            _textLogger?.Log(
                $"{iconTag}=== 전투 종료 ===, 결과: {ev.Result}", 
                LogLevel.Info, 
                contextUnit: null, // 전투 종료 로그는 특정 컨텍스트 유닛 없음
                shouldUpdateTargetView: false, // 전투 종료 로그는 대상 뷰 업데이트 안 함
                turnStartStateSnapshot: ev.FinalParticipantSnapshots // 이벤트에서 최종 스냅샷 가져오기
            );
            // 전투 종료 시 라운드 정보도 함께 로그 (선택적)
            _textLogger?.Log($"//==[ Combat Session Terminated ]==//", LogLevel.System);
        }

        private void HandleUnitActivationStart(CombatSessionEvents.UnitActivationStartEvent ev)
        {
            string activeUnitNameColored = ColorizeText($"[{ev.ActiveUnit.Name}]", "yellow");
            string apInfo = $" (AP: {ev.APBeforeRecovery:F1} -> {ev.ActiveUnit.CurrentAP:F1}/{ev.ActiveUnit.CombinedStats.MaxAP:F1})"; // <<< NEW AP Info
            string iconTag = _useSpriteIcons ? "> " : ""; // <<< 아이콘 조건부 변경 (기존 > 문자 유지)
            
            // +++ 스냅샷 생성 로직 추가 (End에서 이동) +++
            var snapshotDict = new Dictionary<string, ArmoredFrameSnapshot>();
            var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); // 시뮬레이터 다시 참조
            if (simulator != null)
            {
                var participants = simulator.GetParticipants(); // 현재 참가자 목록 가져오기
                if (participants != null)
                {
                    foreach (var unit in participants)
                    {
                        if (unit != null && !string.IsNullOrEmpty(unit.Name))
                        {
                            snapshotDict[unit.Name] = new ArmoredFrameSnapshot(unit); // 현재 상태 스냅샷
                        }
                    }
                }
            }
            else
            {
                 Debug.LogError("Could not get CombatSimulatorService to create unit activation start snapshot.");
            }
            // +++ 스냅샷 생성 로직 끝 +++
            
            _textLogger?.Log(
                $"{iconTag}Unit Activation: {activeUnitNameColored}{apInfo}", // <<< 아이콘 변수 사용
                LogLevel.Info,
                contextUnit: ev.ActiveUnit, // 컨텍스트 유닛 추가
                shouldUpdateTargetView: true, // 대상 뷰 업데이트 추가
                turnStartStateSnapshot: snapshotDict // 스냅샷 추가
            );
        }

        private void HandleUnitActivationEnd(CombatSessionEvents.UnitActivationEndEvent ev)
        {
            string activeUnitNameColored = $"[{ev.ActiveUnit.Name}]"; // 종료 시점 유닛 이름도 색상 적용
            string remainingApInfo = $" (Remaining AP: {ev.ActiveUnit.CurrentAP:F1})"; // 종료 시점 AP 사용
            string iconTag = _useSpriteIcons ? "< " : ""; // <<< 아이콘 조건부 변경 (기존 < 문자 유지)
            
            // +++ 유닛 비활성화 로그 추가 (주석 해제 및 아이콘 적용) +++
             _textLogger?.Log(
                 $"{iconTag}Unit Standby: {activeUnitNameColored}{remainingApInfo}", // Standby 로그 (아이콘 변수 사용)
                 LogLevel.Info, // 로그 레벨
                 contextUnit: ev.ActiveUnit, // Event Target View 업데이트용 컨텍스트 유닛
                 shouldUpdateTargetView: true, // Event Target View 업데이트 활성화
                 turnStartStateSnapshot: null // 스냅샷은 Start에서 처리하므로 여기선 null
             );
            _textLogger?.Log($"_\n_", LogLevel.Info); // <<< 구분선 로그 (주석 처리된 로그 밑으로 이동)
             // +++ 유닛 비활성화 로그 추가 끝 +++

            // 2. Log the unit standby message, passing the snapshot dictionary
            // SF 스타일 로그 포맷 적용 및 스냅샷 전달
            /* // <<< 로그 출력 주석 처리 >>>
             _textLogger?.Log(
                 $\"< Unit Standby: {activeUnitNameColored}{remainingApInfo}\", // Standby 로그
                 LogLevel.Info, // 로그 레벨
                 contextUnit: ev.ActiveUnit, // Event Target View 업데이트용 컨텍스트 유닛
                 shouldUpdateTargetView: true, // Event Target View 업데이트 활성화
                 turnStartStateSnapshot: null // 스냅샷은 Start에서 처리하므로 여기선 null
             );
            */ // <<< 로그 출력 주석 처리 끝 >>>
        }

        private void HandleActionStart(CombatActionEvents.ActionStartEvent ev)
        {
            // 원래 주석 처리 되어 있었으므로 유지
            //_textLogger?.Log($"{ev.Actor.Name} 행동 시작.", LogLevel.Info, ev.Actor, false);
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
            // +++ 반격 행동일 경우 로그 건너뛰기 +++
            if (ev.IsCounterAttack)
            {
                return; // 반격 행동 완료 로그는 남기지 않음
            }
            // +++ 반격 행동일 경우 로그 건너뛰기 끝 +++
            
            // 상세 이동 로그 처리
            if (ev.Action == CombatActionEvents.ActionType.Move && ev.Success)
            {
                string prefix = _textLogger.UseIndentation ? "  " : "";
                string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow");
                string targetName = ev.MoveTarget != null ? ColorizeText($"[{ev.MoveTarget.Name}]", "lightblue") : "지정되지 않은 목표";
                string distanceText = ev.DistanceMoved.HasValue ? $"{ev.DistanceMoved.Value:F1} 만큼" : "일정 거리만큼";
                
                // +++ 목표와의 최종 거리 계산 및 로그 추가 +++
                string targetDistanceText = "";
                if (ev.MoveTarget != null && ev.NewPosition.HasValue)
                {
                    float finalDistance = Vector3.Distance(ev.NewPosition.Value, ev.MoveTarget.Position);
                    targetDistanceText = $". (거리: {finalDistance:F1}m)";
                }
                else if (ev.MoveTarget != null) // 위치 정보는 없지만 타겟은 있는 경우
                {
                     float finalDistance = Vector3.Distance(ev.Actor.Position, ev.MoveTarget.Position); // 현재 위치 기준 거리
                     targetDistanceText = $". (거리: {finalDistance:F1}m)";
                }
                // +++ 목표와의 최종 거리 계산 및 로그 추가 끝 +++

                string iconTag = _useSpriteIcons ? "<sprite index=10> " : ""; // <<< 아이콘 조건부 추가
                string logMsg = $"{prefix}{iconTag}{actorNameColored}(이)가 {targetName} 방향으로 {distanceText} 이동{targetDistanceText}";
                
                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Actor, shouldUpdateTargetView: true);
            }
            // +++ 방어 로그 명시적 처리 추가 +++
            else if (ev.Action == CombatActionEvents.ActionType.Defend && ev.Success)
            {
                string prefix = _textLogger.UseIndentation ? "  " : "";
                string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow");
                string actionIconTag = _useSpriteIcons ? "<sprite index=9> " : ""; // 방어 아이콘
                string apInfo = $"잔여 동력: {ev.Actor.CurrentAP:F1}/{ev.Actor.CombinedStats.MaxAP:F1}.";
                string logMsg = $"{prefix}{actionIconTag}{actorNameColored} 방어 태세 돌입. {apInfo}"; // 메시지 약간 수정

                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Actor, shouldUpdateTargetView: true);
            }
            // +++ 방어 로그 명시적 처리 추가 끝 +++
            else if (_logActionSummaries) // 나머지 일반 액션 (Attack, Reload, 실패 등)
            {
                // Colorize Actor name (yellow)
                string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow");
                string actionName = ev.Action.ToString();
                string prefix = _textLogger.UseIndentation ? "  " : "";
                string actionIconTag = "";
                if (_useSpriteIcons) // <<< 아이콘 사용 여부 체크
                {
                    switch (ev.Action)
                    {
                        case CombatActionEvents.ActionType.Attack: actionIconTag = "<sprite index=8> "; break; // 공백 추가
                        // case CombatActionEvents.ActionType.Defend: actionIconTag = "<sprite index=9> "; break; // 이제 위에서 처리함
                        // case CombatActionEvents.ActionType.Move:   actionIconTag = "<sprite index=10> "; break; // 이제 위에서 처리함
                        case CombatActionEvents.ActionType.Reload: actionIconTag = "<sprite index=13> "; break; // 공백 추가
                        // TODO: Repair 아이콘 추가?
                    }
                }
                
                // +++ SF 메카 스타일 로그 메시지 생성 +++
                string logMsg;
                if (ev.Success)
                {
                    string apInfo = $"잔여 동력: {ev.Actor.CurrentAP:F1}/{ev.Actor.CombinedStats.MaxAP:F1}."; // AP 정보 포맷 변경
                    logMsg = $"{prefix}{actionIconTag}[{actorNameColored}] {actionName} 프로토콜 실행 완료. {apInfo}"; // <<< 아이콘 변수 사용
                }
                else
                {
                    string reason = string.IsNullOrEmpty(ev.ResultDescription) ? "알 수 없는 오류" : ev.ResultDescription;
                    logMsg = $"{prefix}{actionIconTag}[{actorNameColored}] {actionName} 프로토콜 실행 실패. (사유: {reason})"; // <<< 아이콘 변수 사용
                }
                // +++ SF 메카 스타일 로그 메시지 생성 끝 +++
                
                LogLevel logLevel = ev.Success ? LogLevel.Info : LogLevel.Warning;
                // 일반 행동 완료 로그는 행동 유닛(Actor) 컨텍스트, 대상 뷰 업데이트 true
                _textLogger?.Log(logMsg, logLevel, contextUnit: ev.Actor, shouldUpdateTargetView: true);
            }
        }

        private void HandleWeaponFired(CombatActionEvents.WeaponFiredEvent ev)
        {
            // 로그 서비스를 안전하게 가져옵니다.
            TextLoggerService loggerService = null;
            try { loggerService = ServiceLocator.Instance.GetService<TextLoggerService>(); } 
            catch (Exception ex) { Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}"); }

            float distance = Vector3.Distance(ev.Attacker.Position, ev.Target.Position);
            string logMsg = ""; // 빈 문자열로 초기화
            string hitOrMissText = ev.Hit ? "명중!" : "빗나갔다!";
            string iconTag = ""; // <<< 아이콘 변수 초기화
            if (_useSpriteIcons) // <<< 아이콘 사용 여부 체크
            {
                iconTag = ev.Hit ? "<sprite index=8> " : "<sprite index=1> "; // 공백 추가
            }

            // <<< Flavor Text 기반 로그 시도 >>>
            bool useFlavorText = false;
            if (loggerService != null && !string.IsNullOrEmpty(ev.Weapon.AttackFlavorKey))
            {
                string templateKey = ev.Weapon.AttackFlavorKey; // 키 그대로 사용
                // 필요하다면 _Hit, _Miss 접미사 추가 가능: templateKey += ev.Hit ? "_Hit" : "_Miss";
                string flavorText = loggerService.GetRandomFlavorText(templateKey);

                if (!string.IsNullOrEmpty(flavorText))
                {
                    var parameters = new Dictionary<string, string>
                    {
                        // FormatFlavorText 내부에서 attacker/target 색상 처리하므로 이름만 전달
                        { "attacker", ev.Attacker.Name }, 
                        { "target", ev.Target.Name },
                        { "weaponName", ev.Weapon.Name },
                        { "distance", distance.ToString("F1") },
                        { "hitOrMiss", hitOrMissText },
                        // 반격 여부 정보 전달 (FormatFlavorText에서 색상 결정에 사용 가능)
                        { "isCounterAttack", ev.IsCounterAttack.ToString().ToLower() },
                        // --- ADDED --- Get weapon name from the event
                        { "weapon", ev.Weapon?.Name ?? "알 수 없는 무기" } // Use "weapon" key
                    };
                    
                    // --- 제거된 부분: ammoStatus 파라미터 추가 --- 
                    // if (!ev.IsCounterAttack && ev.Weapon.MaxAmmo > 0) 
                    // { 
                    //     parameters.Add("ammoStatus", $"(탄약: {ev.Weapon.CurrentAmmo}/{ev.Weapon.MaxAmmo})");
                    // } else if (!ev.IsCounterAttack) {
                    //      parameters.Add("ammoStatus", "(탄약: ∞)");
                    // }
                    // --- 제거 끝 ---

                    logMsg = loggerService.FormatFlavorText(flavorText, parameters);
                    logMsg = $"{iconTag}{logMsg}"; 
                    useFlavorText = true;

                    // <<< 추가: Flavor Text 사용 시에도 잔탄 정보 덧붙이기 >>>
                    if (ev.Weapon.MaxAmmo > 0) 
                    { 
                        logMsg += $" (탄약: {ev.Weapon.CurrentAmmo}/{ev.Weapon.MaxAmmo})";
                    } 
                    else 
                    {
                        logMsg += " (탄약: ∞)";
                    }
                    // <<< 추가 끝 >>>
                }
            }
            // <<< Flavor Text 기반 로그 끝 >>>

            // <<< Flavor Text를 사용하지 못했을 경우 기존 로직 사용 >>>
            if (!useFlavorText)
            {
                string attackerColor = ev.IsCounterAttack ? "lightblue" : "yellow";
                string targetColor = ev.IsCounterAttack ? "yellow" : "lightblue";
                string attackerNameColored = ColorizeText($"[{ev.Attacker.Name}]", attackerColor);
                string targetNameColored = ColorizeText($"[{ev.Target.Name}]", targetColor);
                string ammoStatus = ev.Weapon.MaxAmmo <= 0 ? "(탄약: ∞)" : $"(탄약: {ev.Weapon.CurrentAmmo}/{ev.Weapon.MaxAmmo})";
                
                logMsg = $"{iconTag}{attackerNameColored}의 {ev.Weapon.Name}(이)가 {distance:F1}m 거리에서 {targetNameColored}에게 {hitOrMissText} {ammoStatus}";
            }
            // <<< 기존 로직 끝 >>>
            
            // Determine context unit based on counter attack status for target view update
            ArmoredFrame contextUnit = ev.IsCounterAttack ? ev.Target : ev.Attacker;
             
            // Log the final message (either flavor text or default)
            _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: contextUnit, shouldUpdateTargetView: true);
        }

        private void HandleDamageApplied(DamageEvents.DamageAppliedEvent ev)
        {
            // Check if this is a counter-attack damage
            if (ev.IsCounterAttack)
            {
                // Construct specific log for counter-attack damage (no flavor text)
                string counterAttackerNameColored = ColorizeText($"[{ev.Source.Name}]", "lightblue"); // Source is the counter-attacker
                string counterTargetNameColored = ColorizeText($"[{ev.Target.Name}]", "yellow"); // Target is the counter-target
                string criticalTag = ev.IsCritical ? " <sprite index=15>!!" : "";
                string partName = ev.DamagedPart.ToString();
                string prefix = _textLogger.UseIndentation ? "    " : ""; 
                string iconTag = _useSpriteIcons ? "<sprite index=0> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                string logMsg = 
                    $"{prefix}{iconTag}{counterAttackerNameColored}의 반격! {counterTargetNameColored}의 {partName}에 {ev.DamageDealt:F0} 피해!{criticalTag}";

                // Log directly, context is the unit being hit by the counter
                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true); 
            }
            else // Normal damage, use flavor text
            {
                // 1. Determine Template Key based on critical hit and damage amount (example threshold)
                string templateKey;
                float damagePercentage = ev.PartMaxDurability > 0 ? ev.DamageDealt / ev.PartMaxDurability : 0f; // Estimate impact

                if (ev.IsCritical) { templateKey = "DamageApplied_Crit"; }
                else if (damagePercentage > 0.3f) { templateKey = "DamageApplied_High"; }
                else { templateKey = "DamageApplied_Low"; }

                // 2. Prepare parameters for the template (roles are normal)
                var parameters = new Dictionary<string, string>();
                parameters.Add("attacker", ev.Source?.Name ?? "알 수 없는 공격자");
                parameters.Add("target", ev.Target.Name);
                parameters.Add("part", ev.DamagedPart.ToString());
                parameters.Add("damage", ev.DamageDealt.ToString("F0"));
                // --- ADDED --- Get weapon name from the event
                parameters.Add("weapon", ev.Weapon?.Name ?? "알 수 없는 무기"); // Use "weapon" key
                // --- END ADDED ---

                // 3. Get and format the flavor text
                string flavorText = GetRandomFlavorText(templateKey);
                string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

                // 4. Log the flavor text (if found)
                if (!string.IsNullOrEmpty(formattedFlavorLog))
                {
                    string iconTagFlavor = _useSpriteIcons ? "<sprite index=0> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                    // 피격 로그는 피격 대상(Target) 컨텍스트, 대상 뷰 업데이트 true
                    _textLogger?.Log($"{iconTagFlavor}{formattedFlavorLog}", LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true); 
                }

                // 5. Log the original detailed info (removed as requested previously)
                // string criticalTag = ev.IsCritical ? " <sprite index=15>!!" : ""; 
                // string partName = ev.DamagedPart.ToString();
                // string prefix = _textLogger.UseIndentation ? "    " : "  "; 
                // string detailLogMsg = ...
                // _textLogger?.Log(detailLogMsg, LogLevel.UnitDetail); 
            }
        }

        private void HandleDamageAvoided(DamageEvents.DamageAvoidedEvent ev)
        {
             // Check if this is avoidance from a counter-attack
            if (ev.IsCounterAttack)
            {
                // Construct specific log for counter-attack avoidance (no flavor text)
                string counterAttackerNameColored = ColorizeText($"[{ev.Source.Name}]", "yellow"); // Source is the counter-attacker
                string counterTargetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue"); // Target is the one avoiding the counter
                string prefix = _textLogger.UseIndentation ? "    " : ""; 
                string iconTag = _useSpriteIcons ? "<sprite index=1> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                string logMsg = 
                    $"{prefix}{iconTag}{counterTargetNameColored}(이)가 {counterAttackerNameColored}의 반격을 회피! ({ev.Type})";

                // Log directly, context is the unit avoiding the counter
                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true); 
            }
            else // Normal avoidance, use flavor text
            {
                // 1. Determine Template Key based on AvoidanceType
                string templateKey = $"DamageAvoided_{ev.Type}"; // e.g., "DamageAvoided_Dodge"

                // 2. Prepare parameters (roles are normal)
                var parameters = new Dictionary<string, string>();
                parameters.Add("attacker", ev.Source?.Name ?? "알 수 없는 공격자");
                parameters.Add("target", ev.Target.Name); // Avoider is the target

                // 3. Get and format the flavor text
                string flavorText = GetRandomFlavorText(templateKey);
                string formattedFlavorLog = FormatFlavorText(flavorText, parameters); // FormatFlavorText will colorize based on keys

                // 4. Log the flavor text (if found)
                if (!string.IsNullOrEmpty(formattedFlavorLog))
                {
                    string iconTagFlavor = _useSpriteIcons ? "<sprite index=1> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                    // 회피 로그는 회피한 유닛(Target) 컨텍스트, 대상 뷰 업데이트 true
                    _textLogger?.Log($"{iconTagFlavor}{formattedFlavorLog}", LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true); 
                }
                
                // 5. Log the original detailed info (removed as requested previously)
                // string prefix = _textLogger.UseIndentation ? "    " : "  "; 
                // string attackerName = ...
                // string detailLogMsg = ...
                // _textLogger?.Log(detailLogMsg, LogLevel.UnitDetail); 
            }
        }

        private void HandlePartDestroyed(PartEvents.PartDestroyedEvent ev)
        {
            // 1. Determine Template Key based on PartType
            string partTypeString = ev.DestroyedPartType.ToString(); // e.g., "Head", "Body", "Arm", "Legs"
            string templateKey;

            // Map PartType enum to a more specific key if needed, or use the enum string directly
            switch (ev.DestroyedPartType)
            {
                case PartType.Head:
                    templateKey = "PartDestroyed_Head";
                    break;
                case PartType.Body:
                    templateKey = "PartDestroyed_Body";
                    break;
                case PartType.Arm: // Assuming Arm type exists
                    templateKey = "PartDestroyed_Arm";
                    break;
                case PartType.Legs:
                    templateKey = "PartDestroyed_Legs";
                    break;
                default: // Fallback for other part types
                    templateKey = "PartDestroyed_General"; // Need a general template in Excel too
                    break;
            }


            // 2. Prepare parameters
            var parameters = new Dictionary<string, string>
            {
                { "target", ev.Frame.Name },
                { "part", partTypeString }, // Use the string representation of the part type
                { "destroyer", ev.Destroyer?.Name ?? "알 수 없는 원인" } // Null check for destroyer
            };

            // 3. Get and format the flavor text
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            // 4. Log the flavor text (if found)
            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                string iconTag = _useSpriteIcons ? "<sprite index=2> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                // 파츠 파괴 로그는 파괴된 유닛(Frame) 컨텍스트, 대상 뷰 업데이트 true
                _textLogger?.Log($"{iconTag}{formattedFlavorLog}", LogLevel.Warning, contextUnit: ev.Frame, shouldUpdateTargetView: true); 
            }

            // 5. Log the original detailed info (adjusted level and formatting)
            string prefix = _textLogger.UseIndentation ? "    " : "  "; // Indent more for detail line
            string destroyerInfo = ev.Destroyer != null ? $" (Destroyer: {ev.Destroyer.Name})" : "";
            string effectsInfo = ""; // Initialize effectsInfo

            if (ev.Effects != null && ev.Effects.Length > 0)
            {
                if (ev.Effects.Length == 1)
                {
                    // 효과가 하나일 경우
                    effectsInfo = $" -> Effect: {ev.Effects[0]}";
                }
                else
                {
                    // 효과가 여러 개일 경우: 개수와 첫 번째 효과 + "..."
                    effectsInfo = $" -> Effects: {ev.Effects.Length} ({ev.Effects[0]}...)"; 
                }
            }

            string detailLogMsg = $"{prefix}<sprite index=2> Detail: [{ev.Frame.Name}]'s [{partTypeString}] destroyed!{destroyerInfo}{effectsInfo}"; // 수정된 effectsInfo 사용
            // _textLogger?.Log(detailLogMsg, LogLevel.UnitDetail); // Log details as UnitDetail -> REMOVED
        }

        private void HandleStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent ev)
        {
            // 1. Determine Template Key
            string templateKey = "StatusEffectApplied"; // General key for now

            // 2. Prepare parameters
            // Clean up the effect name for display
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string durationText = ev.Duration == -1 ? "영구 지속" : $"{ev.Duration}턴";

            var parameters = new Dictionary<string, string>
            {
                { "target", ev.Target.Name },
                { "effect", effectName },
                { "source", ev.Source?.Name ?? "알 수 없는 원인" }, // Null check for source
                { "duration", durationText },
                { "magnitude", ev.Magnitude.ToString("F1") } // Add magnitude if needed by templates
            };

            // 3. Get and format the flavor text
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            // 4. Log the flavor text (if found)
            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                string iconTag = _useSpriteIcons ? "<sprite index=4> " : ""; // <<< 아이콘 조건부 추가 (공백 포함)
                // 효과 적용 로그는 적용된 유닛(Target) 컨텍스트, 대상 뷰 업데이트 true
                _textLogger?.Log($"{iconTag}{formattedFlavorLog}", LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true);
            }

            // 5. Log the original detailed info (adjusted level and formatting)
            string prefix = _textLogger.UseIndentation ? "    " : "  "; // Indent more for detail line
            string sourceText = ev.Source != null ? $" by {ev.Source.Name}" : "";
            string magnitudeText = ev.Magnitude != 0f ? $" (Magnitude: {ev.Magnitude:F1})" : "";
            string detailLogMsg = $"{prefix}<sprite index=4> Detail: [{ev.Target.Name}] gained [{effectName}] effect{sourceText} ({durationText}){magnitudeText}";
            // _textLogger?.Log(detailLogMsg, LogLevel.UnitDetail); // Log details as UnitDetail -> REMOVED
        }

        private void HandleStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent ev)
        {
            // 1. Determine Template Key
            string templateKey = "StatusEffectExpired"; // General key for now

            // 2. Prepare parameters (EXCLUDING target)
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string reason = ev.WasDispelled ? "해제됨" : "만료됨"; // Reason for expiration

            var parameters = new Dictionary<string, string>
            {
                // { "target", ev.Target.Name }, // <<< target 키 제거! >>>
                { "effect", effectName },
                { "reason", reason }
            };

            // 3. Get the flavor text template
            string flavorText = GetRandomFlavorText(templateKey);
            string logMsg;
            string iconTag = _useSpriteIcons ? "<sprite index=5> " : "";

            // 4. Format and log
            if (!string.IsNullOrEmpty(flavorText))
            {
                // <<< target 이름 직접 색칠 >>>
                string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "yellow");

                // <<< 템플릿에서 {target} 부분 제거하고 나머지 포맷팅 >>>
                string templateWithoutTarget = flavorText.Replace("{target}", ""); // {target} 플레이스홀더 제거
                string formattedRestOfString = FormatFlavorText(templateWithoutTarget, parameters); // 나머지 부분 포맷

                // <<< 최종 로그 메시지 조합 >>>
                logMsg = $"{iconTag}{targetNameColored}{formattedRestOfString}";

                // 효과 만료 로그는 해당 유닛(Target) 컨텍스트, 대상 뷰 업데이트 true
                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true);
            }
            else // Fallback if flavor text is not found
            {
                // 기존 로직과 유사한 간단한 메시지 (Target 이름 노란색 적용)
                string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "yellow");
                string effectNameColored = ColorizeText(effectName, "grey"); // 효과 이름은 회색 유지
                logMsg = $"{iconTag}{targetNameColored}의 {effectNameColored} 효과가 {reason}.";
                 Debug.LogWarning($"Flavor text not found for key: {templateKey}. Using default expiration log.");
                _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true);
            }

            // 5. Log the original detailed info (REMOVED)
            // ...
        }

        private void HandleStatusEffectTick(StatusEffectEvents.StatusEffectTickEvent ev)
        {
            // 1. Determine Template Key based on TickEffectType
            string templateKey;
            string tickIconTag = ""; // <<< 아이콘 변수 초기화
            string tickActionText; // For detail log

            if (ev.Effect.TickEffectType == TickEffectType.DamageOverTime)
            {
                templateKey = "StatusEffectTick_Damage";
                if (_useSpriteIcons) tickIconTag = "<sprite index=6> "; // 공백 추가
                tickActionText = "damage";
            }
            else // Assuming HealOverTime or others
            {
                templateKey = "StatusEffectTick_Heal";
                if (_useSpriteIcons) tickIconTag = "<sprite index=7> "; // 공백 추가
                tickActionText = "heal";
            }

            // 2. Prepare parameters
            string effectName = ev.Effect.EffectName;

            var parameters = new Dictionary<string, string>
            {
                { "target", ev.Target.Name },
                { "effect", effectName },
                { "value", ev.Effect.TickValue.ToString("F0") } 
            };

            // 3. Get and format the flavor text
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            // 4. Log the flavor text (if found)
            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                // 틱 효과 로그는 해당 유닛(Target) 컨텍스트, 대상 뷰 업데이트 true
                _textLogger?.Log($"{tickIconTag}{formattedFlavorLog}", LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true);
            }

            // 5. Log the original detailed info (adjusted level and formatting)
            string prefix = _textLogger.UseIndentation ? "      " : "    "; // Indent even more for tick detail
            string detailLogMsg = $"{prefix}{tickIconTag} Detail: [{ev.Target.Name}] ticked by [{effectName}] for [{ev.Effect.TickValue:F0}] {tickActionText}.";
            // _textLogger?.Log(detailLogMsg, LogLevel.UnitDetail); // Log details as UnitDetail -> REMOVED
        }
        
        // +++ 새로운 이벤트 핸들러 추가 +++
        private void HandleRepairApplied(CombatActionEvents.RepairAppliedEvent ev)
        {
            // 1. Determine Template Key based on ActionType
            string templateKey = ev.ActionType == CombatActionEvents.ActionType.RepairAlly
                ? "RepairAlly_Success"
                : "RepairSelf_Success";

            // 2. Prepare parameters
            var parameters = new Dictionary<string, string>
            {
                { "actor", ev.Actor.Name },
                { "target", ev.Target.Name }, // For RepairSelf, target is the same as actor
                { "part", ev.TargetSlotIdentifier },
                { "amount", ev.AmountRepaired.ToString("F1") }
            };

            // 3. Get and format the flavor text
            string flavorText = GetRandomFlavorText(templateKey);
            string formattedFlavorLog = FormatFlavorText(flavorText, parameters);

            // 4. Log the flavor text (if found) or a default message
            string logMsg;
            string iconTag = _useSpriteIcons ? "<sprite index=7> " : ""; // 힐링/수리 아이콘

            if (!string.IsNullOrEmpty(formattedFlavorLog))
            {
                logMsg = $"{iconTag}{formattedFlavorLog}";
            }
            else // Fallback if flavor text is not found
            {
                // 기존 로직과 유사한 간단한 메시지
                string actorNameColored = ColorizeText($"[{ev.Actor.Name}]", "yellow");
                string targetNameColored = ColorizeText($"[{ev.Target.Name}]", "lightblue");
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

            // 로그 기록 (수리 대상 유닛 기준으로 Context 설정)
            _textLogger?.Log(logMsg, LogLevel.Info, contextUnit: ev.Target, shouldUpdateTargetView: true);
        }
        // +++ 새로운 이벤트 핸들러 추가 끝 +++
        
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
            _textLogger?.Log($"//==[ Combat Turn {ev.RoundNumber} Initiated ]==//", LogLevel.System);

            // 선택 사항: 행동 순서 로그
            //if (ev.InitiativeSequence != null && ev.InitiativeSequence.Any())
            //{
            //    string sequence = string.Join(" > ", ev.InitiativeSequence.Select(u => $"[{u.Name}]"));
            //     _textLogger?.Log($"--- Initiative Sequence: {sequence} ---", LogLevel.Info);
            //}
        }

        private void HandleRoundEnd(CombatSessionEvents.RoundEndEvent ev)
        {
            //_textLogger?.Log($"//==[ Combat Turn {ev.RoundNumber} Complete ]==//", LogLevel.System);
        }
    }
} 