using AF.Combat;
using AF.EventBus;
using AF.Models;
using AF.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List 사용 위해 추가
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System; // TimeSpan 사용 위해 추가
using System.Linq;
using System.Text;

namespace AF.UI
{
    /// <summary>
    /// 전투 로그를 UI에 표시하는 서비스 (전투 종료 시 한 번에 표시)
    /// </summary>
    public class CombatTextUIService : MonoBehaviour, IService
    {
        [Header("UI References")] 
        // 기존 _logTextDisplay는 프리팹 방식으로 대체될 수 있으므로 주석 처리 또는 제거 고려
        // [SerializeField] private TMP_Text _logTextDisplay; 
        [SerializeField] private ScrollRect _scrollRect; // 자동 스크롤용
        [SerializeField] private GameObject _logLinePrefab; // 로그 한 줄을 표시할 프리팹
        [SerializeField] private Transform _logContainer; // 프리팹이 생성될 부모 컨테이너 (VerticalLayoutGroup 등)
        [SerializeField] private TMP_Text _unitDetailTextDisplay; // 유닛 상세 정보를 표시할 텍스트 (이제 전체 유닛용)
        [SerializeField] private TMP_Text _eventTargetDetailTextDisplay; // 최근 이벤트 대상 유닛 상세 정보 표시용 (추가)

        [Header("Animation Settings")]
        // [SerializeField] private float _fadeInDuration = 0.3f; // 각 라인 페이드인 시간 (이제 사용 안 함)
        [SerializeField] private float _textAnimationDuration = 0.5f; // 텍스트 타이핑 애니메이션 시간
        [SerializeField] private float _lineDelay = 0.1f; // 다음 라인 표시 전 딜레이

        private EventBus.EventBus _eventBus;
        private bool _isInitialized = false;
        // StringBuilder 와 라인 카운트는 프리팹 방식에서는 관리 방식 변경 필요
        // private System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();
        // private int _currentLogLines = 0;
        // private int _maxLogLines = 1000; // 최대 라인 수 제한 로직도 변경 필요 (오브젝트 삭제 등)
        // private bool _isInitialized = false;
        // // private ArmoredFrame _currentlyDisplayedUnit = null; // 제거
        // private List<ArmoredFrame> _finalParticipants = null; // 제거: 스냅샷 방식으로 변경
        
        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            if (_eventBus == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: EventBus 참조를 가져올 수 없습니다.");
                return;
            }
            if (_logLinePrefab == null || _logContainer == null || _scrollRect == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: 필요한 UI 참조(프리팹, 컨테이너, 스크롤렉트)가 설정되지 않았습니다.");
                return;
            }
            if (_unitDetailTextDisplay == null) // 상세 정보 텍스트 필드 null 체크 추가
            {
                Debug.LogWarning("CombatTextUIService 초기화 경고: Unit Detail Text Display가 설정되지 않았습니다.");
                // 상세 정보 표시 기능은 작동하지 않지만, 서비스 자체는 계속 진행하도록 할 수 있음
            }

            SubscribeToEvents();
            ClearLog(); 
            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            UnsubscribeFromEvents();
            _eventBus = null;
            _isInitialized = false;
        }

        #endregion

        #region Unit Detail Display Logic (New)

        /// <summary>
        /// Updates the primary unit detail display using the provided snapshot data.
        /// </summary>
        private void UpdateAllUnitsDetailDisplay(Dictionary<string, ArmoredFrameSnapshot> snapshot)
        {
            if (_unitDetailTextDisplay == null) return; // UI 없으면 중단
            
            if (snapshot == null || snapshot.Count == 0)
            {
                _unitDetailTextDisplay.text = "No snapshot data available."; // 스냅샷 데이터 없을 때
                return;
            }

            _unitDetailTextDisplay.text = FormatAllUnitDetails(snapshot.Values); // 스냅샷 값들 사용
        }

        /// <summary>
        /// Formats the detailed status of all units from snapshot data.
        /// </summary>
        private string FormatAllUnitDetails(IEnumerable<ArmoredFrameSnapshot> snapshots)
        {
            StringBuilder allDetails = new StringBuilder();
            foreach (var snapshot in snapshots.OrderBy(s => s.Name)) // 이름 순 정렬
            {
                // Format snapshot data using a dedicated or adapted method
                allDetails.AppendLine(FormatUnitDetailsFromSnapshot(snapshot)); // Use snapshot formatter
                allDetails.AppendLine(new string('-', 20)); // 구분선 추가
            }
             // 마지막 구분선 제거
            if (allDetails.Length > 20) 
            {
                allDetails.Length -= (20 + Environment.NewLine.Length);
            }
            return allDetails.ToString();
        }

        /// <summary>
        /// Updates the event target detail display with the latest info for the given unit.
        /// </summary>
        private void UpdateEventTargetDetailDisplay(ArmoredFrame affectedUnit)
        {
            if (affectedUnit == null || _eventTargetDetailTextDisplay == null) 
            {
                 if (_eventTargetDetailTextDisplay != null) _eventTargetDetailTextDisplay.text = ""; // Clear if no target
                 return;
            }
             // Use the existing formatter for live ArmoredFrame data
            _eventTargetDetailTextDisplay.text = FormatUnitDetails(affectedUnit); 
        }

        /// <summary>
        /// Formats the detailed status from an ArmoredFrameSnapshot into a string.
        /// Similar to FormatUnitDetails but uses snapshot data.
        /// </summary>
        private string FormatUnitDetailsFromSnapshot(ArmoredFrameSnapshot snapshot)
        {
            StringBuilder sb = new StringBuilder();
            string statusTag = snapshot.IsOperational ? "<sprite index=17>" : "<sprite index=2>";
            sb.AppendLine($"<b>{snapshot.Name}</b> {statusTag} | AP: {snapshot.CurrentAP:F1}/{snapshot.MaxAP:F1} | DUR: {snapshot.CurrentTotalDurability:F0}/{snapshot.MaxTotalDurability:F0}");
            
            // Append Stats from snapshot
            var stats = snapshot.CombinedStats;
            sb.AppendLine($"  <i>Stats: Atk:{stats.AttackPower:F1}/Def:{stats.Defense:F1}/Spd:{stats.Speed:F1}/Acc:{stats.Accuracy:F1}/Eva:{stats.Evasion:F1}</i>");

            // Append Parts from snapshot
            sb.AppendLine("  <b>Parts:</b>");
            if (snapshot.PartSnapshots != null && snapshot.PartSnapshots.Count > 0)
            {
                // Assuming PartSnapshots dictionary keys are the slot IDs and they are ordered reasonably
                // Or potentially get slot order from FrameBase if that info is also snapshotted (currently not)
                foreach (var kvp in snapshot.PartSnapshots.OrderBy(p => p.Key)) // Order by slot ID
                {
                    string partLine;
                    var partSnapshot = kvp.Value;
                    float currentDurability = partSnapshot.CurrentDurability;
                    float maxDurability = partSnapshot.MaxDurability;
                    float durabilityPercent = maxDurability > 0 ? currentDurability / maxDurability : 0;
                    string partStatusIcon;
                    string statusText = $"({currentDurability:F0}/{maxDurability:F0})";

                    if (!partSnapshot.IsOperational || currentDurability <= 0)
                    {
                        partStatusIcon = "<sprite index=2>"; // DESTROYED
                        statusText = "(Destroyed)";
                    }
                    else if (durabilityPercent <= 0.3f) partStatusIcon = "<sprite index=19>"; // PART CRIT
                    else if (durabilityPercent <= 0.7f) partStatusIcon = "<sprite index=18>"; // PART DMG
                    else partStatusIcon = "<sprite index=17>"; // PART OK

                    // Use slotId (kvp.Key) and part name from snapshot
                    partLine = $"    {partStatusIcon} {kvp.Key} ({partSnapshot.Name}): {statusText}";
                    sb.AppendLine(partLine);
                }
            }
            else
            {
                sb.AppendLine("    (Part data not available in snapshot)");
            }

            // Append Weapons from snapshot
            sb.AppendLine("  <b>Weapons:</b>");
            if (snapshot.WeaponSnapshots != null && snapshot.WeaponSnapshots.Count > 0)
            {
                foreach (var weaponSnapshot in snapshot.WeaponSnapshots)
                {
                    string weaponStatusTag = weaponSnapshot.IsOperational ? "<sprite index=17>" : "<sprite index=3>"; // PART OK / SYS FAIL
                    string ammoStatus = weaponSnapshot.MaxAmmo <= 0 ? "(∞)" : $"({weaponSnapshot.CurrentAmmo}/{weaponSnapshot.MaxAmmo})";
                    sb.AppendLine($"    {weaponStatusTag} {weaponSnapshot.Name} {ammoStatus}");
                }
            }
            else
            {
                sb.AppendLine("    (Weapon data not available in snapshot)");
            }
            
            // TODO: Add Status Effects display here if needed from snapshot

            return sb.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Updates the unit detail display with the latest info for the given unit.
        /// </summary>
        private void UpdateUnitDetailDisplay(ArmoredFrame unit) // 이제 사용 안 함 -> UpdateAllUnitsDetailDisplay 또는 UpdateEventTargetDetailDisplay 사용
        {
            // if (unit == null || _unitDetailTextDisplay == null) return;

            // // Only update if the provided unit is the one currently being displayed
            // // (or if no unit is currently displayed, e.g., on Turn Start)
            // // We might remove this check later if we always want to show the affected unit.
            // if (_currentlyDisplayedUnit == null || _currentlyDisplayedUnit == unit)
            // {
            //      _unitDetailTextDisplay.text = FormatUnitDetails(unit);
            // }
        }

        /// <summary>
        /// Formats the detailed status of an ArmoredFrame into a string for UI display.
        /// Adapted from TextLoggerService.LogUnitDetailsInternal logic.
        /// </summary>
        private string FormatUnitDetails(ArmoredFrame unit)
        {
             if (unit == null) return "No unit selected.";

            StringBuilder sb = new StringBuilder();

            // Use sprite indices defined in TextLogger or a shared location
            string statusTag = unit.IsOperational ? "<sprite index=17>" : "<sprite index=2>"; // PART OK / DESTROYED
            sb.AppendLine($"<b>{unit.Name}</b> {statusTag} | AP: {unit.CurrentAP:F1} / {unit.CombinedStats.MaxAP:F1}");
            
            var stats = unit.CombinedStats;
            sb.AppendLine($"  <i>Stats: Atk:{stats.AttackPower:F1}/Def:{stats.Defense:F1}/Spd:{stats.Speed:F1}/Acc:{stats.Accuracy:F1}/Eva:{stats.Evasion:F1}</i>");

            sb.AppendLine("  <b>Parts:</b>");
            // Use FrameBase slots if available for consistent order and empty slots
            IReadOnlyDictionary<string, PartSlotDefinition> slots = unit.FrameBase?.GetPartSlots();
            IEnumerable<string> slotOrder = slots?.Keys ?? unit.Parts.Keys.OrderBy(k => k); // Fallback to sorted keys
            
            foreach (var slotId in slotOrder)
            {
                string partLine = "";
                if (unit.Parts.TryGetValue(slotId, out Part part))
                {
                    float currentDurability = part.CurrentDurability;
                    float maxDurability = part.MaxDurability;
                    float durabilityPercent = maxDurability > 0 ? currentDurability / maxDurability : 0;
                    string partStatusIcon;
                    string statusText = $"({currentDurability:F0}/{maxDurability:F0})";

                    if (!part.IsOperational || currentDurability <= 0)
                    {
                        partStatusIcon = "<sprite index=2>"; // DESTROYED
                        statusText = "(Destroyed)";
                    }
                    else if (durabilityPercent <= 0.3f) partStatusIcon = "<sprite index=19>"; // PART CRIT
                    else if (durabilityPercent <= 0.7f) partStatusIcon = "<sprite index=18>"; // PART DMG
                    else partStatusIcon = "<sprite index=17>"; // PART OK

                    partLine = $"    {partStatusIcon} {slotId} ({part.Name}): {statusText}";
                }
                else
                {
                    // Part is missing from this slot
                    partLine = $"    <sprite index=20> {slotId}: (Empty)"; // PART EMPTY
                }
                sb.AppendLine(partLine);
            }

            sb.AppendLine("  <b>Weapons:</b>");
            var weapons = unit.GetAllWeapons();
            if (weapons != null && weapons.Count > 0)
            {
                foreach (var weapon in weapons)
                {
                    string weaponStatusTag = weapon.IsOperational ? "<sprite index=17>" : "<sprite index=3>"; // PART OK / SYS FAIL
                    string ammoStatus = weapon.MaxAmmo <= 0 ? "(∞)" : $"({weapon.CurrentAmmo}/{weapon.MaxAmmo})";
                    sb.AppendLine($"    {weaponStatusTag} {weapon.Name} {ammoStatus}");
                }
            }
            else
            {
                sb.AppendLine("    (None)");
            }
            
            // TODO: Add Status Effects display here if needed

            return sb.ToString().TrimEnd('\r', '\n');
        }


        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEndUpdateDetails); // 이전 핸들러 제거

            // 실시간 업데이트를 위한 이벤트 구독 추가 -> 제거
            // _eventBus.Subscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStartUpdateDetails); 
            // _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageAppliedUpdateDetails);
            // _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyedUpdateDetails);
            // _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompletedUpdateDetails);
            // 필요시 StatusEffect 이벤트 등 추가 구독...
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEndUpdateDetails); // 이전 핸들러 제거

            // 실시간 업데이트 이벤트 구독 해제 -> 제거
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStartUpdateDetails);
            // _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageAppliedUpdateDetails);
            // _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyedUpdateDetails);
            // _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompletedUpdateDetails);
            // 필요시 StatusEffect 이벤트 등 구독 해제...
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            ClearLog();
            // _finalParticipants = null; // 제거
            if (_unitDetailTextDisplay != null) 
                _unitDetailTextDisplay.text = "Waiting for combat data..."; // 초기 메시지 설정
            if (_eventTargetDetailTextDisplay != null) 
                _eventTargetDetailTextDisplay.text = ""; // 대상 창도 초기화
            
            // 전투 시작 시 초기 참가자 정보 표시 시도 -> 제거: ProcessLogsAsync에서 첫 로그 처리 시 업데이트됨
            /*
            UniTask.Void(async () => 
            { 
                await UniTask.DelayFrame(1); // 한 프레임 지연
                UpdateAllUnitsDetailDisplay(); 
            });
            */
        }

        // HandleCombatEnd는 메인 로그 표시에만 집중 -> 참가자 목록 저장 로직 제거
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent evt)
        {
            // 로그 재생 전에 최종 참가자 목록 저장 -> 제거
            /*
            var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            if (simulator != null) 
            {
                var currentParticipants = simulator.GetParticipants(); 
                _finalParticipants = currentParticipants?.ToList(); 
            }
            else
            {
                Debug.LogError("CombatTextUIService: Combat Simulator not found at combat end. Cannot store final participants.");
                _finalParticipants = new List<ArmoredFrame>();
            }
            */
            ProcessLogsAsync(evt).Forget();
        }

        // --- 실시간 업데이트 핸들러들 --- -> 제거
        /*
        private void HandleTurnStartUpdateDetails(CombatSessionEvents.TurnStartEvent evt)
        {
            UpdateAllUnitsDetailDisplay();
            UpdateEventTargetDetailDisplay(evt.ActiveUnit); // 이벤트 대상 창에는 현재 턴 유닛 표시
        }

        private void HandleDamageAppliedUpdateDetails(DamageEvents.DamageAppliedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Target); // 이벤트 대상 창에는 피격 유닛 표시
        }

        private void HandlePartDestroyedUpdateDetails(PartEvents.PartDestroyedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Frame); // 이벤트 대상 창에는 파츠 파괴된 유닛 표시
        }

        private void HandleActionCompletedUpdateDetails(CombatActionEvents.ActionCompletedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Actor); // 이벤트 대상 창에는 행동 완료한 유닛 표시
        }
        */

        // +++ 이전 TurnEnd 핸들러 제거 +++
        /*
        private void HandleTurnEndUpdateDetails(CombatSessionEvents.TurnEndEvent evt)
        {
            if (evt.ActiveUnit == null || _unitDetailTextDisplay == null) return; // 유닛이나 UI 없으면 중단
            UpdateUnitDetailDisplay(evt.ActiveUnit); // Turn End 시 해당 유닛 정보로 업데이트 (추후 로직 변경 예정)
        }
        */
        // +++ 핸들러 추가 끝 +++

        // 로그 처리 로직 (로그 재생 및 상세 정보 동기화)
        private async UniTaskVoid ProcessLogsAsync(CombatSessionEvents.CombatEndEvent evt)
        {
            ITextLogger textLogger = ServiceLocator.Instance.GetService<TextLoggerService>()?.TextLogger;
            if (textLogger == null)
            {
                Debug.LogError("CombatTextUIService: TextLogger 참조를 가져올 수 없습니다.");
                await CreateAndAnimateLogLine("오류: 전투 로그를 불러올 수 없습니다.");
                return;
            }

            // 원본 LogEntry 리스트 가져오기
            List<TextLogger.LogEntry> logEntries = textLogger.GetLogEntries(); 

            if (logEntries == null || logEntries.Count == 0)
            {
                Debug.LogWarning("표시할 전투 로그가 없습니다.");
                return;
            }
            
            // 전투 시작 시 초기 상태로 UI 클리어 (메인 로그 제외)
            if (_unitDetailTextDisplay != null) _unitDetailTextDisplay.text = "";
            if (_eventTargetDetailTextDisplay != null) _eventTargetDetailTextDisplay.text = "";

            // 로그 항목들을 순차적으로 처리하며 UI 업데이트 및 애니메이션 재생
            foreach (var logEntry in logEntries)
            {
                // 1. 전체 유닛 상세 정보 UI 업데이트 (턴 시작 로그일 때만)
                if (logEntry.TurnStartStateSnapshot != null)
                {
                    UpdateAllUnitsDetailDisplay(logEntry.TurnStartStateSnapshot);
                }

                // 2. 이벤트 대상 상세 정보 UI 업데이트 (로그 애니메이션 전에!)
                if (logEntry.ShouldUpdateTargetView && logEntry.ContextUnit != null)
                {
                    // 이벤트 대상 상세 정보 업데이트
                    UpdateEventTargetDetailDisplay(logEntry.ContextUnit);
                    // 전체 유닛 상세 정보도 함께 업데이트 (가장 최신 상태 반영) -> 제거: 턴 시작 시에만 업데이트
                    // UpdateAllUnitsDetailDisplay(); 
                }
                
                // 3. 메인 로그 애니메이션 재생 및 대기
                string messageToDisplay = logEntry.Message;
                if (logEntry.Level == LogLevel.System)
                {
                    messageToDisplay = $"<size=30>{logEntry.Message}</size>";
                }
                await CreateAndAnimateLogLine(messageToDisplay);
            }

            Debug.Log("전투 로그 재생 완료.");
        }

        #endregion

        #region Logging Methods

        private async UniTask CreateAndAnimateLogLine(string message)
        {
            if (_logLinePrefab == null || _logContainer == null || _scrollRect == null) return;

            GameObject logInstance = Instantiate(_logLinePrefab, _logContainer);
            TMP_Text logText = logInstance.GetComponentInChildren<TMP_Text>();

            if (_scrollRect != null)
            {
                 await _scrollRect.DOVerticalNormalizedPos(0f, 0.1f).SetEase(Ease.OutQuad).AsyncWaitForCompletion(); // <<< DOTween 애니메이션 추가
            }

            if (logText != null)
            {
                logText.text = "";
                await logText.DOText(message, _textAnimationDuration).SetEase(Ease.Linear).AsyncWaitForCompletion();
            }
            else
            {
                 Debug.LogWarning("로그 라인 프리팹에 TMP_Text 컴포넌트를 찾을 수 없습니다.");
            }

            if (_lineDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_lineDelay));
            }
            else {
                await UniTask.Yield(PlayerLoopTiming.LastUpdate); // Wait one frame for rebuild to settle
            }
        }

        public void ClearLog()
        {
            if (_logContainer == null) return;

            foreach (Transform child in _logContainer)
            {
                Destroy(child.gameObject);
            }

            if (_scrollRect != null)
            {
                 _scrollRect.normalizedPosition = new Vector2(0, 1);
            }
            if (_unitDetailTextDisplay != null)
            {
                _unitDetailTextDisplay.text = "";
            }
        }

        #endregion
    }
} 