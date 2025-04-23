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

        private bool _logActionSummaries = true; // 행동 요약 로그 표시 여부 필드 추가

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

        #endregion
        
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
            // TextLogger의 LogEvent 사용하던 것 복구
            _textLogger?.Log($"<sprite index=11> === 전투 시작 === ID: {ev.BattleId}, 이름: {ev.BattleName}", LogLevel.Info); // BATTLE START 아이콘
            LogAllUnitDetailsOnInit(ev.Participants);
        }

        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            _textLogger?.Log($"<sprite index=12> === 전투 종료 === ID: {ev.BattleId}, 결과: {ev.Result}, 지속시간: {ev.Duration:F1}초", LogLevel.Info); // BATTLE END 아이콘
        }

        private void HandleTurnStart(CombatSessionEvents.TurnStartEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            _textLogger?.Log($"<sprite index=13> --- Turn {ev.TurnNumber} 시작: [{ev.ActiveUnit.Name}] ---", LogLevel.Info); // TURN START 아이콘
            LogUnitDetailsOnTurnStart(ev.ActiveUnit);
        }

        private void HandleTurnEnd(CombatSessionEvents.TurnEndEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            _textLogger?.Log($"<sprite index=14> --- Turn {ev.TurnNumber} 종료: [{ev.ActiveUnit.Name}] ---", LogLevel.Info); // TURN END 아이콘
            LogAllUnitDetailsOnTurnEnd();
        }

        private void HandleActionStart(CombatActionEvents.ActionStartEvent ev)
        {
            // 원래 주석 처리 되어 있었으므로 유지
            //_textLogger?.Log($"{ev.Actor.Name} 행동 시작.", LogLevel.Info);
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
            // 상세 이동 로그 처리
            if (ev.Action == CombatActionEvents.ActionType.Move && ev.Success)
            {
                string prefix = _textLogger.UseIndentation ? "  " : "";
                string targetName = ev.MoveTarget != null ? ev.MoveTarget.Name : "지정되지 않은 목표";
                string distanceText = ev.DistanceMoved.HasValue ? $"{ev.DistanceMoved.Value:F1} 만큼" : "일정 거리만큼";
                // Vector3 포맷팅 개선 (소수점 한 자리)
                string positionText = ev.NewPosition.HasValue ? $"({ev.NewPosition.Value.x:F1}, {ev.NewPosition.Value.y:F1}, {ev.NewPosition.Value.z:F1})" : "알 수 없는 위치";

                // <<< 이동 아이콘 추가 >>>
                string logMsg = $"{prefix}<sprite index=10> {ev.Actor.Name}(이)가 {targetName} 방향으로 {distanceText} 이동. 새 위치: {positionText}"; // MOVE 아이콘
                _textLogger?.Log(logMsg, LogLevel.Info);
            }
            // 이동 성공 외의 경우 + 행동 요약 로그 토글이 켜진 경우에만 일반 요약 로그 출력
            else if (_logActionSummaries) 
            {
                string actionName = ev.Action.ToString();
                string successText = ev.Success ? "성공" : "실패";
                string prefix = _textLogger.UseIndentation ? "  " : "";
                // 실패 이유(ResultDescription)는 포함하지 않음 (필요시 추가)
                
                // <<< 행동 타입별 아이콘 추가 >>>
                string actionIconTag = "";
                switch (ev.Action)
                {
                    case CombatActionEvents.ActionType.Attack:
                        actionIconTag = "<sprite index=8>"; // ATK 아이콘
                        break;
                    case CombatActionEvents.ActionType.Defend:
                        actionIconTag = "<sprite index=9>"; // DEF 아이콘
                        break;
                    // 다른 ActionType에 대한 아이콘 추가 가능
                }

                string logMsg = $"{prefix}{actionIconTag} {ev.Actor.Name}: {actionName} {successText}.";
                LogLevel logLevel = ev.Success ? LogLevel.Info : LogLevel.Warning;
                _textLogger?.Log(logMsg, logLevel);
            }
        }

        private void HandleWeaponFired(CombatActionEvents.WeaponFiredEvent ev)
        {
            float distance = Vector3.Distance(ev.Attacker.Position, ev.Target.Position);
            string logMsg;
            if (ev.Hit)
            {
                // <<< 공격 성공 아이콘 추가 >>>
                logMsg = $"<sprite index=8> {ev.Attacker.Name}의 {ev.Weapon.Name}(이)가 {distance:F1}m 거리에서 {ev.Target.Name}에게 명중!"; // ATK 아이콘 (명중)
            }
            else
            {
                // <<< 공격 실패 아이콘 추가 (Miss와 구분 위해 ATK 사용) >>>
                logMsg = $"<sprite index=8> {ev.Attacker.Name}의 {ev.Weapon.Name} 발사! 하지만 {distance:F1}m 거리의 {ev.Target.Name}(은)는 빗나갔다!"; // ATK 아이콘 (빗나감)
            }
            _textLogger?.Log(logMsg, LogLevel.Info);
        }

        private void HandleDamageApplied(DamageEvents.DamageAppliedEvent ev)
        {
            // <<< 크리티컬 태그 수정: 인덱스 15 사용 >>>
            string criticalTag = ev.IsCritical ? " <sprite index=15>!!" : ""; // CRIT! 아이콘
            string partName = ev.DamagedPart.ToString();
            // UseIndentation 플래그 확인하여 들여쓰기 적용 및 아이콘 제거
            string prefix = _textLogger.UseIndentation ? "  " : ""; 
            // <<< 메시지 생성 시 criticalTag 사용 >>>
            // <<< 데미지 아이콘 추가 >>>
            string logMsg = $"{prefix}<sprite index=0> {ev.Target.Name}의 [{partName}]에 충격! [{ev.DamageDealt:F0}] 피해!{criticalTag} (내구도: {ev.PartCurrentDurability:F0}/{ev.PartMaxDurability:F0})"; // HIT 아이콘
            _textLogger?.Log(logMsg, LogLevel.Warning);
        }

        private void HandleDamageAvoided(DamageEvents.DamageAvoidedEvent ev)
        {
            string avoidanceText;
            switch (ev.Type)
            {
                case DamageEvents.DamageAvoidedEvent.AvoidanceType.Dodge:
                    avoidanceText = "날렵하게 회피!";
                    break;
                // 다른 회피 타입에 대한 메시지 추가 가능 (Deflect, Shield 등)
                default:
                    avoidanceText = "공격을 피했다!"; // 기본 메시지 변경
                    break;
            }
            // UseIndentation 플래그 확인 및 아이콘 제거, 공격자 정보 추가 (Source가 있다고 가정)
            string prefix = _textLogger.UseIndentation ? "  " : "";
            string attackerName = ev.Source != null ? ev.Source.Name : "알 수 없는 공격자"; // Null 체크 추가
            // <<< 회피 아이콘 추가 >>>
            string logMsg = $"{prefix}<sprite index=1> {ev.Target.Name}(이)가 {attackerName}의 공격을 {avoidanceText} ({ev.Type})"; // MISS 아이콘
            _textLogger?.Log(logMsg, LogLevel.Info);
        }

        private void HandlePartDestroyed(PartEvents.PartDestroyedEvent ev)
        {
            // 잘못 수정된 내용 복구: 원래 파츠 파괴 로직으로 되돌림
            StringBuilder sb = new StringBuilder();
            string prefix = _textLogger.UseIndentation ? "  " : ""; 
            // <<< 파츠 파괴 태그 수정: 인덱스 2 사용 >>>
            sb.Append($"{prefix}*** <sprite index=2> 파츠 파괴됨! *** "); // DESTROYED 아이콘
            sb.Append($"[{ev.Frame.Name}]의 [{ev.DestroyedPartType}]");

            if (ev.Destroyer != null)
            {
                sb.Append($" (파괴자: [{ev.Destroyer.Name}])");
            }
            if (ev.Effects != null && ev.Effects.Length > 0)
            {
                sb.Append($" -> 결과: {string.Join(", ", ev.Effects)} ");
            }
            _textLogger?.Log(sb.ToString(), LogLevel.Error);
        }

        private void HandleStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            string durationText = ev.Duration == -1 ? "영구 지속" : $"{ev.Duration}턴 지속";
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string sourceText = ev.Source != null ? $"[{ev.Source.Name}]의 효과로 " : "";
            string magnitudeText = ev.Magnitude != 0f ? $" (강도: {ev.Magnitude:F1})" : "";
            // UseIndentation 플래그 확인하여 들여쓰기 적용 (✨ 앞)
            string prefix = _textLogger.UseIndentation ? "  " : ""; 
            // <<< 효과 적용 태그 수정: 인덱스 4 사용 >>>
            string logMsg = $"{prefix}<sprite index=4> {sourceText}[{ev.Target.Name}]에게 [{effectName}] 효과 적용! ({durationText}){magnitudeText}"; // EFFECT+ 아이콘
            _textLogger?.Log(logMsg, LogLevel.Info);
        }

        private void HandleStatusEffectExpired(StatusEffectEvents.StatusEffectExpiredEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            string effectName = ev.EffectType.ToString().Replace("Buff_", "").Replace("Debuff_", "").Replace("Environmental_", "");
            effectName = System.Text.RegularExpressions.Regex.Replace(effectName, "([A-Z])", " $1").Trim();
            string reason = ev.WasDispelled ? " (해제됨)" : "";
            // UseIndentation 플래그 확인하여 들여쓰기 적용 (💨 앞)
            string prefix = _textLogger.UseIndentation ? "  " : ""; 
            // <<< 효과 만료 태그 수정: 인덱스 5 사용 >>>
            string logMsg = $"{prefix}<sprite index=5> [{ev.Target.Name}]의 [{effectName}] 효과 만료{reason}."; // EFFECT- 아이콘
            _textLogger?.Log(logMsg, LogLevel.Info);
        }

        private void HandleStatusEffectTick(StatusEffectEvents.StatusEffectTickEvent ev)
        {
            // TextLogger의 LogEvent 사용하던 것 복구
            string effectName = ev.Effect.EffectName;
            string tickAction = ev.Effect.TickEffectType == TickEffectType.DamageOverTime ? "피해" : "회복";
            // <<< DoT/HoT 태그 수정: 인덱스 6(DoT), 7(HoT) 사용 >>>
            string tickIconTag = ev.Effect.TickEffectType == TickEffectType.DamageOverTime ? "<sprite index=6>" : "<sprite index=7>"; // TICK / HEAL TICK 아이콘
            // UseIndentation 플래그 확인하여 들여쓰기 적용
            string prefix = _textLogger.UseIndentation ? "  ㄴ" : "";
            string logMsg = $"{prefix}{tickIconTag} [{ev.Target.Name}] < [{effectName}] 틱! ([{ev.Effect.TickValue:F0}] {tickAction})";
            _textLogger?.Log(logMsg, LogLevel.Info);
        }
        
        private Dictionary<(ArmoredFrame, string), float> _previousPartDurability = new Dictionary<(ArmoredFrame, string), float>();
        private Dictionary<ArmoredFrame, float> _previousUnitAP = new Dictionary<ArmoredFrame, float>();

        private void LogAllUnitDetailsOnInit(ArmoredFrame[] participants)
        {
            // <<< 유닛 상태 아이콘 추가 >>>
            _textLogger.Log("<sprite index=16> --- Initial Units Status ---", LogLevel.Info); // UNIT 아이콘
            _previousPartDurability.Clear();
            if (participants != null)
            {
                foreach (var unit in participants)
                {
                    LogUnitDetailsInternal(unit, true);
                }
            }
        }
        
        private void LogAllUnitDetailsOnTurnEnd()
        {
            // <<< 유닛 상태 아이콘 추가 >>>
            _textLogger.Log("<sprite index=16> --- End of Turn Units Status ---", LogLevel.Info); // UNIT 아이콘

            // 참가자 목록 가져오기 (CombatSimulatorService에서 가져오는 것이 더 안정적일 수 있음)
            var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            if (simulator == null) return;
            var currentParticipants = simulator.GetParticipants(); 

            bool anyChangeLogged = false;
            foreach (var unit in currentParticipants)
            {
                if (unit == null) continue;

                // 이전 상태와 비교하여 변경 여부 확인
                bool apChanged = _previousUnitAP.TryGetValue(unit, out float previousAP) && Mathf.Abs(unit.CurrentAP - previousAP) > 0.01f;
                bool durabilityChanged = false;
                foreach (var kvp in unit.Parts)
                {
                    var key = (unit, kvp.Key);
                    if (_previousPartDurability.TryGetValue(key, out float previousDurability) && 
                        Mathf.Abs(kvp.Value.CurrentDurability - previousDurability) > 0.01f)
                    {
                        durabilityChanged = true;
                        break; // 하나라도 변경되었으면 더 볼 필요 없음
                    }
                    // 파츠가 새로 생기거나 파괴된 경우도 변경으로 간주 (선택적)
                    if (!_previousPartDurability.ContainsKey(key) && kvp.Value.IsOperational) durabilityChanged = true; 
                    // if (_previousPartDurability.ContainsKey(key) && !kvp.Value.IsOperational) durabilityChanged = true;
                }

                // AP 또는 내구도에 변화가 있었던 유닛만 로그 기록
                if (apChanged || durabilityChanged)
                {
                    LogUnitDetailsInternal(unit, false); // 변경된 유닛 상세 정보 로깅
                    anyChangeLogged = true;
                }
            }
            
            // 아무 변경 사항도 없었으면 메시지 출력 (선택적)
            if (!anyChangeLogged)
            {
                 _textLogger.Log("  (No significant status changes this turn)", LogLevel.Info);
            }
            
            // 턴 종료 시 다음 턴 비교를 위해 현재 상태 기록 (LogUnitDetailsInternal에서 이미 처리됨)
        }

        private void LogUnitDetailsOnTurnStart(ArmoredFrame unit)
        {
            // <<< 유닛 상태 아이콘 추가 >>>
            _textLogger.Log($"<sprite index=16> --- Turn Start: {unit?.Name} Status ---", LogLevel.Info); // UNIT 아이콘
            LogUnitDetailsInternal(unit, false);
        }

        private void LogUnitDetailsInternal(ArmoredFrame unit, bool isInitialLog)
        {
            if (unit == null) return;

            // StringBuilder를 사용하여 여러 줄 로그를 하나의 문자열로 만들기
            StringBuilder sb = new StringBuilder();

            // 이전 AP 기록용 (메소드 내 임시 변수 또는 클래스 멤버로 관리 필요)
            float previousAP = -1f; // 초기값 -1 또는 다른 방식으로 관리
            if (!isInitialLog && _previousUnitAP.TryGetValue(unit, out float prevAP)) // _previousUnitAP 딕셔너리 필요
            {
                previousAP = prevAP;
            }

            // 1. 기본 유닛 정보 추가 (AP 변화량 포함)
            string apChangeIndicator = "";
            if (previousAP >= 0 && Mathf.Abs(unit.CurrentAP - previousAP) > 0.01f)
            {
                apChangeIndicator = $" [{(unit.CurrentAP - previousAP):+0.0;-0.0}]"; // 부호 표시 (+/-)
            }
            // <<< 유닛 상태 태그 수정: 인덱스 17(정상), 2(파괴) 사용 >>>
            string statusTag = unit.IsOperational ? "<sprite index=17>" : "<sprite index=2>"; // PART OK / DESTROYED 아이콘
            sb.AppendLine($"  Unit: {unit.Name} {statusTag} | AP: {unit.CurrentAP:F1}/{unit.CombinedStats.MaxAP:F1}{apChangeIndicator}"); // 이모지 변경 및 AP 변화량 추가
            
            // 현재 AP 기록 업데이트
            _previousUnitAP[unit] = unit.CurrentAP; // _previousUnitAP 딕셔너리 필요

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
                            // <<< 내구도 변화량 태그 수정: 인덱스 4(증가), 0(감소) 사용 >>>
                            string sign = durabilityChange > 0 ? "+" : "";
                            string changeIconTag = durabilityChange > 0 ? "<sprite index=4>" : "<sprite index=0>"; // EFFECT+ / HIT 아이콘
                            changeIndicator = $" [{sign}{durabilityChange:F0}{changeIconTag}]"; 
                        }
                    }
                    status = $"OK ({currentDurability:F0}/{part.MaxDurability:F0}){changeIndicator}";
                    _previousPartDurability[key] = currentDurability;
                }
                else
                {
                    // <<< 파츠 파괴 태그 수정: 인덱스 2 사용 >>>
                    status = "DESTROYED <sprite index=2>"; // DESTROYED 아이콘
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
                    // <<< 무기 상태 태그 수정: 인덱스 17(정상), 3(고장) 사용 >>>
                    string weaponStatusTag = weapon.IsOperational ? "<sprite index=17>" : "<sprite index=3>"; // PART OK / SYS FAIL 아이콘
                    // 무기 정보 각 줄 추가
                    sb.AppendLine($"      - {weapon.Name}: {weaponStatusTag}");
                }
            } else {
                sb.AppendLine("      - None"); // 무기 없을 때
            }

            // 5. 최종적으로 모든 내용을 담은 문자열을 한 번만 로그로 출력
            _textLogger.Log(sb.ToString().TrimEnd('\r', '\n'), LogLevel.Info); // 마지막 줄바꿈 제거 (선택적)
        }
    }
} 