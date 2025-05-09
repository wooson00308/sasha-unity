using UnityEngine;
using AF.Services;
using AF.Combat;
using AF.EventBus;
using AF.Models;
using System.Collections.Generic;
using System.Linq; // Added to use LINQ methods like Where
using UnityEngine.UI; // Added for Image, LineRenderer etc.
using TMPro; // Added for TextMeshPro
using Cysharp.Threading.Tasks; // Added for UniTask
using System;
using DG.Tweening; // Added for DOTween animations
using AF.Tests; // <-- SASHA: CombatTestRunner 사용을 위해 추가

namespace AF.UI
{
    /// <summary>
    /// 전투 상황을 레이더/소나 스타일 UI로 시각화하는 서비스.
    /// </summary>
    public class CombatRadarUIService : MonoBehaviour, IService
    {
        [Header("UI References")]
        [SerializeField] private RectTransform radarContainer; // 레이더 UI의 기준 영역
        [SerializeField] private GameObject unitMarkerPrefab; // 유닛 표시용 프리팹
        [SerializeField] private LineRenderer targetLinePrefab; // 타겟 연결선 프리팹 (선택적)

        private EventBus.EventBus _eventBus;
        private bool _isInitialized = false;

        // 레이더 표시 관련 설정값 (예시)
        [Header("Radar Settings")]
        [SerializeField] private float radarRadius = 100f; // UI 상의 레이더 반지름
        [SerializeField] private float maxDetectionRange = 50f; // 게임 월드 상 최대 탐지 거리 (UI 스케일링용)

        [Header("Visual Effects")]
        [SerializeField] private Color damageFlashColor = Color.magenta;
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private float pulseScale = 1.8f;
        [SerializeField] private float moveAnimDuration = 0.3f;
        [SerializeField] private float materialTransitionFadeDuration = 0.25f; // 머티리얼 변경 시 페이드 지속 시간

        // +++ SASHA: Enhanced Animation Settings +++
        [Header("Enhanced Animation Settings")]
        [SerializeField] private float markerAppearDuration = 0.35f;
        [SerializeField] private Ease markerAppearEase = Ease.OutBack;
        [SerializeField] private float markerDisappearDuration = 0.25f;
        [SerializeField] private Ease markerDisappearEase = Ease.InBack;
        [SerializeField] private Ease markerMoveEase = Ease.OutBounce; 
        [SerializeField] private Vector3 damageShakeStrength = new Vector3(3f, 3f, 0f);
        [SerializeField] private int damageShakeVibrato = 10;
        [SerializeField] private float damageShakeRandomness = 0f; 
        // +++ SASHA: End Enhanced Animation Settings +++

        // +++ SASHA: More Animation Settings +++
        [SerializeField] private float markerActiveScaleDuration = 0.2f;
        [SerializeField] private float detectedPulseScaleFactor = 1.3f;
        [SerializeField] private float detectedPulseDuration = 0.3f;
        [SerializeField] private Ease detectedPulseEaseOut = Ease.OutQuint; 
        [SerializeField] private Ease detectedPulseEaseIn = Ease.InQuint;
        // +++ SASHA: End More Animation Settings +++

        // Changed Dictionary Key from ArmoredFrame to string (Unit Name)
        private Dictionary<string, GameObject> _unitMarkers = new Dictionary<string, GameObject>();
        private List<LineRenderer> _targetLines = new List<LineRenderer>();
        private Dictionary<string, bool> _lastMarkerWasAlwaysVisible = new Dictionary<string, bool>(); // 머티리얼 상태 추적
        private Dictionary<string, Sequence> _blinkingSequences = new Dictionary<string, Sequence>(); // SASHA: 깜빡임 시퀀스 관리
        private Dictionary<string, TextMeshProUGUI> _markerTexts = new Dictionary<string, TextMeshProUGUI>(); // SASHA: 마커 텍스트 캐싱

        // +++ Add field to store the battle center +++
        private Vector3 _battleCenterWorldPosition = Vector3.zero; // 레이더 UI의 원점으로 사용될 월드 좌표
        private Vector3? _lastKnownPlayerUnitWorldPosition = null; // SASHA: 마지막 활성 플레이어 유닛 위치

        // +++ SASHA: Radar Scan Effect Settings +++
        [Header("Radar Scan Effect Settings")]
        [Tooltip("애니메이터로 Z축 회전하는 레이더 UI 오브젝트의 Transform")]
        [SerializeField] private Transform radarScanTransform;
        [Tooltip("스캔 빔의 각도상 너비 (단위: 도)")]
        [SerializeField] private float scanArcWidthDegrees = 30f;
        [Tooltip("스캔 빔 가장자리의 페이드 효과 범위 (단위: 도)")]
        [SerializeField] private float scanFadeRangeDegrees = 5f;
        [Tooltip("모든 마커 UI 이미지들이 공유할 머티리얼. 이 머티리얼에 스캔 효과 셰이더가 적용되어야 합니다.")]
        [SerializeField] private Material markersMaterial;
        private string _lastRadarFocusTargetUnitName = null; // SASHA: 마지막 레이더 포커스 유닛 이름 저장
        // +++ SASHA: End Radar Scan Effect Settings +++

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            // _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); // No longer directly needed

            // CombatSimulator is removed, adjust null check if necessary
            if (_eventBus == null || radarContainer == null || unitMarkerPrefab == null)
            {
                Debug.LogError("CombatRadarUIService 초기화 실패: 필수 참조(EventBus, UI 요소)가 없습니다.");
                enabled = false; // 초기화 실패 시 비활성화
                return;
            }

            SubscribeToEvents();
            ClearRadar();
            _isInitialized = true;
            Debug.Log("CombatRadarUIService Initialized.");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            UnsubscribeFromEvents();
            ClearRadar(); // 정리
            _eventBus = null;
            // _combatSimulator = null; // No longer needed
            _isInitialized = false;
            Debug.Log("CombatRadarUIService Shutdown.");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart); // Keep for initial clear
            // _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);     // No longer starts playback here
            _eventBus.Subscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate); // Subscribe to the new event
            // Removed real-time event subscriptions
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            // _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            _eventBus.Unsubscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate); // Unsubscribe from the new event
            // Removed real-time event unsubscriptions
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            ClearRadar();
            _battleCenterWorldPosition = Vector3.zero; // Reset battle center on new combat start
            _lastKnownPlayerUnitWorldPosition = null; // SASHA: 전투 시작 시 초기화

            // Calculate battle center immediately if possible (using start event participants)
            if (ev.Participants != null && ev.Participants.Length > 0)
            {
                Vector3 sumPositions = Vector3.zero;
                foreach(var unit in ev.Participants)
                {
                    sumPositions += unit.Position;
                }
                _battleCenterWorldPosition = sumPositions / ev.Participants.Length;
                 Debug.Log($"CombatRadarUIService: Battle Center calculated at combat start: {_battleCenterWorldPosition}");
            }
            else { Debug.LogWarning("CombatRadarUIService: No participants in CombatStartEvent to calculate initial battle center."); }
        }

        // Commented out: Playback is no longer initiated here
        /*
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            // Start the log playback process
            // ProcessRadarPlaybackAsync(ev).Forget();
        }
        */

        // +++ New Handler for CombatLogPlaybackUpdateEvent +++
        private async void HandleLogPlaybackUpdate(CombatLogPlaybackUpdateEvent ev)
        {
            if (!_isInitialized || ev.CurrentSnapshot == null || ev.CurrentLogEntry == null)
            {
                return; // Don't process if not initialized or event data is missing
            }

            var combatTestRunnerService = ServiceLocator.Instance.GetService<CombatTestRunner>();
            var playerSquadNames = combatTestRunnerService?.GetPlayerSquadUnitNames() ?? new HashSet<string>();
            string activeUnitName = ev.ActiveUnitName;
            ArmoredFrameSnapshot activeUnitSnapshotValue = default; 
            bool activeUnitSnapshotExists = false;
            if (!string.IsNullOrEmpty(activeUnitName))
            {
                activeUnitSnapshotExists = ev.CurrentSnapshot.TryGetValue(activeUnitName, out activeUnitSnapshotValue);
            }

            bool playerSquadExistsAndAtLeastOneOperational = false;
            if (playerSquadNames.Count > 0) {
                foreach (var playerName in playerSquadNames) {
                    if (ev.CurrentSnapshot.TryGetValue(playerName, out var ps) && ps.IsOperational) {
                        playerSquadExistsAndAtLeastOneOperational = true;
                        break;
                    }
                }
            }
            
            bool playerSquadAnnihilated = playerSquadNames.Count > 0 && !playerSquadExistsAndAtLeastOneOperational;
            Vector3 newRadarOrigin = _battleCenterWorldPosition; 
            string radarFocusTargetUnitName = null; 
            ArmoredFrameSnapshot? focusTargetSnapshotNullable = null; // SASHA: 포커스 타겟 스냅샷 저장 변수 (Nullable)

            if (playerSquadAnnihilated)
            {
                Vector3 allUnitsCenterSum = Vector3.zero;
                int operationalAllCount = 0;
                foreach (var snapshot in ev.CurrentSnapshot.Values)
                {
                    if (snapshot.IsOperational)
                    {
                        allUnitsCenterSum += snapshot.Position;
                        operationalAllCount++;
                    }
                }
                if (operationalAllCount > 0) newRadarOrigin = allUnitsCenterSum / operationalAllCount;
                _lastKnownPlayerUnitWorldPosition = null; 
                _lastRadarFocusTargetUnitName = null; 
                // 포커스 타겟 없음 (focusTargetSnapshotNullable = null)
            }
            else 
            {
                bool isActiveUnitPlayerSquadAndOperational = false;
                if (activeUnitSnapshotExists && activeUnitSnapshotValue.IsOperational && playerSquadNames.Contains(activeUnitName))
                {
                    isActiveUnitPlayerSquadAndOperational = true;
                }

                if (isActiveUnitPlayerSquadAndOperational)
                {
                    newRadarOrigin = activeUnitSnapshotValue.Position;
                    _lastKnownPlayerUnitWorldPosition = newRadarOrigin;
                    radarFocusTargetUnitName = activeUnitName; 
                    _lastRadarFocusTargetUnitName = activeUnitName; 
                    focusTargetSnapshotNullable = activeUnitSnapshotValue; // SASHA: 현재 활성 유닛 스냅샷을 포커스로 설정
                }
                else
                {
                    if (_lastKnownPlayerUnitWorldPosition.HasValue)
                    {
                        newRadarOrigin = _lastKnownPlayerUnitWorldPosition.Value;
                        radarFocusTargetUnitName = _lastRadarFocusTargetUnitName; 
                        // SASHA: 마지막 포커스 타겟의 스냅샷 가져오기 시도
                        if (!string.IsNullOrEmpty(radarFocusTargetUnitName) && ev.CurrentSnapshot.TryGetValue(radarFocusTargetUnitName, out var lastFocusSnapshot))
                        {
                            focusTargetSnapshotNullable = lastFocusSnapshot;
                        }
                    }
                    else
                    {
                        Vector3 allUnitsCenterSum = Vector3.zero;
                        int operationalAllCount = 0;
                        foreach (var snapshot in ev.CurrentSnapshot.Values)
                        {
                            if (snapshot.IsOperational)
                            {
                                allUnitsCenterSum += snapshot.Position;
                                operationalAllCount++;
                            }
                        }
                        if (operationalAllCount > 0) newRadarOrigin = allUnitsCenterSum / operationalAllCount;
                        _lastRadarFocusTargetUnitName = null; 
                        // 포커스 타겟 없음 (focusTargetSnapshotNullable = null)
                    }
                }
            }
            _battleCenterWorldPosition = newRadarOrigin;

            // 1. Update the base radar display (marker positions and appearances)
            UpdateRadarDisplayFromSnapshot(ev.CurrentSnapshot, ev.ActiveUnitName, focusTargetSnapshotNullable, playerSquadAnnihilated); // SASHA: playerSquadAnnihilated 전달

            // --- Small delay to allow UI to update positions before playing effects ---            
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
            await UniTask.Delay(TimeSpan.FromSeconds(0.01f)); // Tiny delay 

            // 2. Play visual effects based on the SPECIFIC log entry that just occurred
            TextLogger.LogEntry logEntry = ev.CurrentLogEntry;
            GameObject targetMarker = null;
            string targetUnitName = null;

            switch (logEntry.EventType)
            {
                case LogEventType.ActionCompleted:
                    targetUnitName = logEntry.Action_ActorName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        if (logEntry.Action_Type == CombatActionEvents.ActionType.Move && logEntry.Action_IsSuccess && logEntry.Action_NewPosition.HasValue)
                        {
                            // Animate movement - CalculateRadarPosition now takes only the target position
                            Vector2 newRadarPos = CalculateRadarPosition(logEntry.Action_NewPosition.Value);
                            // Don't await here if we want effects to potentially overlap slightly
                            targetMarker?.GetComponent<RectTransform>()?.DOAnchorPos(newRadarPos, moveAnimDuration).SetEase(markerMoveEase);
                        }
                        else if (logEntry.Action_IsSuccess && logEntry.Action_Type != CombatActionEvents.ActionType.Move) // Play pulse for other successful non-move actions
                        {
                            await PlayPulseEffect(targetMarker);
                        }
                    }
                    break;

                case LogEventType.DamageApplied:
                    targetUnitName = logEntry.Damage_TargetUnitName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        await PlayFlashEffect(targetMarker, damageFlashColor);
                    }
                    break;

                case LogEventType.PartDestroyed:
                    targetUnitName = logEntry.PartDestroyed_OwnerName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        if (logEntry.PartDestroyed_FrameWasActuallyDestroyed)
                        {
                            // +++ SASHA: 프레임 완전 파괴 시 특별 애니메이션 +++
                            if (targetMarker != null)
                            {
                                DOTween.Kill(targetMarker.transform); // 기존 스케일/위치 트윈 중지
                                Image markerImage = targetMarker.GetComponent<Image>();
                                if (markerImage != null) DOTween.Kill(markerImage); // 기존 이미지 트윈 중지

                                Sequence destructionSequence = DOTween.Sequence();
                                await destructionSequence.Append(markerImage.DOColor(Color.black, 0.1f)) // 즉시 검은색으로
                                    .Append(markerImage.DOFade(0.3f, 0.08f).SetLoops(6, LoopType.Yoyo)) // 빠르게 깜빡임 (0.3 알파 <-> 현재 알파)
                                    .Append(targetMarker.transform.DOShakePosition(0.3f, damageShakeStrength * 0.7f, 15, damageShakeRandomness)) // 살짝 흔들림
                                    .Append(targetMarker.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
                                    .Join(markerImage.DOFade(0f, 0.3f))
                                    .OnComplete(() =>
                                    {
                                        if (targetMarker != null) Destroy(targetMarker);
                                        _unitMarkers.Remove(targetUnitName);
                                        _lastMarkerWasAlwaysVisible.Remove(targetUnitName);
                                    });
                                await destructionSequence.Play();
                            }
                            // +++ SASHA: 애니메이션 끝 +++
                        }
                        else
                        {
                            // 프레임이 파괴되지 않은 일반 파츠 파괴 시에는 기존 플래시 효과만.
                            await PlayFlashEffect(targetMarker, Color.gray); // 일반 파츠 파괴는 회색 플래시
                        }
                        // Marker appearance was already updated by UpdateRadarDisplayFromSnapshot based on the IsOperational flag in the snapshot
                    }
                    break;

                case LogEventType.StatusEffectApplied:
                    targetUnitName = logEntry.StatusApplied_TargetName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        await PlayPulseEffect(targetMarker, 1.2f, 0.15f);
                    }
                    break;

                case LogEventType.RepairApplied:
                    targetUnitName = logEntry.Repair_TargetName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        await PlayFlashEffect(targetMarker, Color.green);
                    }
                    break;

                // Add cases for other EventTypes if visual feedback is desired
                // case LogEventType.DamageAvoided: ...
                // case LogEventType.WeaponFired: ...

                default:
                    // No specific visual effect for this log type
                    break;
            }
        }
        // +++ End New Handler +++

        /// <summary>
        /// Processes the combat log asynchronously to playback radar states.
        /// </summary>
        // Commented out - No longer used
        /*
        private async UniTaskVoid ProcessRadarPlaybackAsync(CombatSessionEvents.CombatEndEvent endEvent) // Accept end event if needed
        {
            // ... (Original method content removed or commented out) ...
        }
        */

        #endregion

        #region Radar Update Logic (Snapshot Based)

        // Renamed method
        private void ClearRadar()
        {
            foreach (var marker in _unitMarkers.Values)
            {
                if (marker != null)
                {
                    Image image = marker.GetComponent<Image>();
                    if (image != null) DOTween.Kill(image); // Kill tween before destroying
                    DOTween.Kill(marker.transform); // SASHA: 마커 트랜스폼에 걸린 모든 트윈 제거 (스케일, 이동, 깜빡임 등)
                    Destroy(marker);
                }
            }
            _unitMarkers.Clear();
            _lastMarkerWasAlwaysVisible.Clear(); // 이전 상태 추적 딕셔너리도 클리어
            _markerTexts.Clear(); // SASHA: 마커 텍스트 캐시 클리어
            // SASHA: 깜빡임 시퀀스도 모두 정리
            foreach (var seq in _blinkingSequences.Values)
            {
                seq?.Kill();
            }
            _blinkingSequences.Clear();

            foreach (var line in _targetLines)
            {
                 if (line != null) Destroy(line.gameObject); 
            }
             _targetLines.Clear();
        }

        // Renamed and modified to accept snapshot and active unit name for highlight
        // SASHA: radarFocusTargetUnitName 파라미터 대신 focusTargetSnapshotNullable 파라미터 사용
        private void UpdateRadarDisplayFromSnapshot(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, string activeUnitName, ArmoredFrameSnapshot? focusTargetSnapshotNullable, bool playerSquadAnnihilated)
        {
            if (!_isInitialized) return;

            HashSet<string> currentOperationalUnitNames = new HashSet<string>(
                snapshotDict.Values.Where(s => s.IsOperational).Select(s => s.Name)
            );
            List<string> unitsToRemoveFromMarkers = new List<string>();

            // --- SASHA: Get player squad unit names for passing to UpdateMarkerAppearanceFromSnapshot --- 
            var combatTestRunnerService = ServiceLocator.Instance.GetService<CombatTestRunner>();
            var playerSquadNames = combatTestRunnerService?.GetPlayerSquadUnitNames() ?? new HashSet<string>();
            // --- End SASHA ---

            foreach (var existingUnitName in _unitMarkers.Keys)
            {
                if (!currentOperationalUnitNames.Contains(existingUnitName))
                {
                    unitsToRemoveFromMarkers.Add(existingUnitName);
                }
            }

            foreach(var unitName in unitsToRemoveFromMarkers)
            {
                if (_unitMarkers.TryGetValue(unitName, out var marker))
                {
                     if(marker != null)
                     {
                         Image image = marker.GetComponent<Image>();
                         if (image != null) DOTween.Kill(image); // 트윈 중지
                         DOTween.Kill(marker.transform); // SASHA: 마커 트랜스폼에 걸린 모든 트윈 제거

                         // +++ SASHA: Enhanced Disappear Animation (Image & Text) +++
                         TextMeshProUGUI callsignTMP = null;
                         if (_markerTexts.TryGetValue(unitName, out var tmp)) { callsignTMP = tmp; }

                         Sequence disappearSequence = DOTween.Sequence();
                         disappearSequence.Append(marker.transform.DOScale(Vector3.zero, markerDisappearDuration).SetEase(markerDisappearEase))
                                        .Join(image.DOFade(0f, markerDisappearDuration));
                         
                         if (callsignTMP != null)
                         {
                            disappearSequence.Join(callsignTMP.DOFade(0f, markerDisappearDuration));
                         }

                         disappearSequence.OnComplete(() => {
                                            if (marker != null) Destroy(marker); 
                                        });
                         // +++ SASHA: End Enhanced Disappear Animation +++
                     }
                     _unitMarkers.Remove(unitName);
                     _lastMarkerWasAlwaysVisible.Remove(unitName); // 상태 제거
                     _markerTexts.Remove(unitName); // SASHA: 마커 텍스트 캐시에서 제거
                     // SASHA: 깜빡임 시퀀스 정리
                     if (_blinkingSequences.TryGetValue(unitName, out var blinkingSeq))
                     {
                         blinkingSeq?.Kill();
                         _blinkingSequences.Remove(unitName);
                     }
                }
            }

            foreach (var unitSnapshot in snapshotDict.Values)
            {
                 if (!unitSnapshot.IsOperational) continue;

                 Vector2 radarPosition = CalculateRadarPosition(unitSnapshot.Position);
                 bool isActive = unitSnapshot.Name == activeUnitName; 
                 bool isRadarFocus = unitSnapshot.Name == focusTargetSnapshotNullable?.Name; // SASHA: 포커스 타겟 여부 확인 (Nullable 안전 접근)
                 bool isInWeaponRange = false; // SASHA: 무기 사거리 내 여부 초기화

                 // SASHA: 무기 사거리 계산 로직 추가
                 if (focusTargetSnapshotNullable.HasValue && !isRadarFocus) // 포커스 타겟이 있고, 현재 유닛이 포커스 타겟이 아닐 때
                 {
                     float distance = Vector3.Distance(focusTargetSnapshotNullable.Value.Position, unitSnapshot.Position);
                     if (distance <= focusTargetSnapshotNullable.Value.PrimaryWeaponRange)
                     {
                         isInWeaponRange = true;
                     }
                 }

                if (_unitMarkers.TryGetValue(unitSnapshot.Name, out GameObject marker))
                {
                    if(marker != null)
                    {
                        marker.GetComponent<RectTransform>().anchoredPosition = radarPosition;
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated); // SASHA: playerSquadAnnihilated 전달
                    }
                    else
                    {
                        _unitMarkers.Remove(unitSnapshot.Name);
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated); // SASHA: playerSquadAnnihilated 전달
                    }
                }
                else
                {
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated); // SASHA: playerSquadAnnihilated 전달
                }
            }
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRange 파라미터 추가
        private void CreateNewMarkerFromSnapshot(ArmoredFrameSnapshot unitSnapshot, Vector2 position, bool isActive, bool isRadarFocus, bool isInWeaponRange, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
        {
            GameObject marker = Instantiate(unitMarkerPrefab, radarContainer);
            marker.name = $"Marker_{unitSnapshot.Name}";
            marker.GetComponent<RectTransform>().anchoredPosition = position;

            Image image = marker.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"Marker {unitSnapshot.Name} is missing Image component.");
                Destroy(marker);
                return;
            }

            // SASHA: Callsign 표시 로직 (unitSnapshot.Name 사용) 및 TMP 캐싱, 초기 알파 설정
            TextMeshProUGUI callsignTMP = null;
            Transform callsignTextTransform = marker.transform.Find("CallSignText");
            if (callsignTextTransform != null)
            {
                callsignTMP = callsignTextTransform.GetComponent<TextMeshProUGUI>();
                if (callsignTMP != null)
                {
                    callsignTMP.text = unitSnapshot.Name;
                    callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, 0f); // 초기 알파 0
                    _markerTexts[unitSnapshot.Name] = callsignTMP;
                }
                else
                {
                    Debug.LogWarning($"[CombatRadarUIService] Marker '{marker.name}'의 자식 'CallSignText'에서 TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[CombatRadarUIService] Marker '{marker.name}'에서 'CallSignText' 자식 오브젝트를 찾을 수 없습니다.");
            }
            // SASHA: End Callsign 표시 로직

            // +++ SASHA: Enhanced Appear Animation (Image & Text) +++
            bool isInitiallyAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange; // SASHA: playerSquadAnnihilated 조건 추가
            Color initialTeamColor = GetTeamColor(unitSnapshot.TeamId);

            image.color = new Color(initialTeamColor.r, initialTeamColor.g, initialTeamColor.b, 0f); // Start fully transparent for fade
            image.material = isInitiallyAlwaysVisible ? null : markersMaterial; // SASHA: playerSquadAnnihilated가 true면 항상 null (기본 머티리얼)
            marker.transform.localScale = Vector3.zero; // Start from zero scale for appear animation

            Sequence appearSequence = DOTween.Sequence();
            appearSequence.Append(image.DOFade(1f, markerAppearDuration)) // Fade in alpha for image
                          .Join(marker.transform.DOScale(isActive ? Vector3.one * 1.5f : Vector3.one, markerAppearDuration).SetEase(markerAppearEase)); // Scale up

            if (callsignTMP != null)
            {
                appearSequence.Join(callsignTMP.DOFade(1f, markerAppearDuration)); // Fade in alpha for text
            }
            
            appearSequence.SetId(marker.GetInstanceID() + "_appear"); 
            appearSequence.OnComplete(() => {
                // 애니메이션 완료 후 UpdateMarkerAppearance를 호출하여 최종 상태(깜빡임 등) 반영
                // 이 시점에는 마커와 텍스트가 완전히 표시된 상태여야 함.
                // UpdateMarkerAppearanceFromSnapshot는 알파를 직접 제어하므로, 여기서 알파가 1로 설정된 후 호출되도록.
                 UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated);
            });
            // +++ SASHA: End Enhanced Appear Animation +++
            
            _unitMarkers[unitSnapshot.Name] = marker; // 마커 저장
            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = isInitiallyAlwaysVisible; // SASHA: playerSquadAnnihilated가 true면 항상 true로 저장됨
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRange 파라미터 추가
        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit, bool isRadarFocus, bool isInWeaponRange, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
        {
            if (marker == null) return;

            // SASHA: 변수 선언을 메서드 최상단으로 통합하고 중복 제거
            TextMeshProUGUI callsignTMP = null;
            Transform callsignTextTransform = null; // 미리 선언

            if (!_markerTexts.TryGetValue(unitSnapshot.Name, out callsignTMP)) 
            {
                callsignTextTransform = marker.transform.Find("CallSignText");
                if (callsignTextTransform != null) 
                {
                    callsignTMP = callsignTextTransform.GetComponent<TextMeshProUGUI>();
                    if (callsignTMP != null) 
                    {
                        _markerTexts[unitSnapshot.Name] = callsignTMP; // 찾았으면 캐시에 추가
                    }
                }
            }
            // 이제 callsignTMP는 값을 가지고 있거나, 못 찾았다면 null 상태임.

            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color baseTeamColor = GetTeamColor(unitSnapshot.TeamId); // Get base team color

            // +++ SASHA: 바디 파츠 손상도에 따른 색상 조정 +++
            float bodyDamageRatio = 0f; // 기본값: 손상 없음
            string bodySlotId = "Body"; // TODO: 실제 Body 슬롯 ID를 FrameSO 등에서 가져오거나 설정할 수 있도록 하는 것이 좋음

            if (unitSnapshot.PartSnapshots != null && unitSnapshot.PartSnapshots.TryGetValue(bodySlotId, out PartSnapshot bodySnapshot))
            {
                if (bodySnapshot.MaxDurability > 0)
                {
                    bodyDamageRatio = 1f - (bodySnapshot.CurrentDurability / bodySnapshot.MaxDurability);
                }
                else if (!bodySnapshot.IsOperational)
                {
                    bodyDamageRatio = 1f; // 최대 내구도가 0인데 작동 불능이면 완전 손상으로 간주
                }
            }
            else
            {
                // 바디 파츠 정보가 없으면 (이론상 드묾), 또는 유닛 자체가 비작동 상태면 최대 손상으로 처리
                if (!unitSnapshot.IsOperational) bodyDamageRatio = 1f; 
            }

            // 손상 비율에 따라 검은색을 얼마나 섞을지 결정 (0.0 ~ 0.6 사이로 제한) -> SASHA: 이 로직 제거하고 baseTeamColor 사용
            // float lerpFactor = Mathf.Clamp(bodyDamageRatio * 0.7f, 0f, 0.6f); 
            // Color displayColor = Color.Lerp(baseTeamColor, Color.black, lerpFactor);
            Color displayColor = baseTeamColor; // SASHA: 항상 기본 팀 색상 사용
            // +++ SASHA: 바디 파츠 손상도 색상 조정 끝 +++

            // +++ SASHA: stateChangedThisFrame 계산 로직을 깜빡임 로직 앞으로 이동 +++
            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange; // SASHA: playerSquadAnnihilated 조건 추가
            Material targetMaterial = shouldBeAlwaysVisible ? null : markersMaterial;

            bool stateChangedThisFrame = false;
            if (_lastMarkerWasAlwaysVisible.TryGetValue(unitSnapshot.Name, out bool wasAlwaysVisible))
            {
                if (wasAlwaysVisible != shouldBeAlwaysVisible)
                {
                    stateChangedThisFrame = true;
                }
            }
            else
            {
                stateChangedThisFrame = true;
            }
            // +++ SASHA: stateChangedThisFrame 계산 로직 이동 끝 +++

            // +++ SASHA: 빈사 상태 깜빡임 로직 +++
            bool isNearDeath = bodyDamageRatio >= 0.8f; // 내구도 20% 미만 (1.0 - 0.2 = 0.8)
            // callsignTMP는 이미 위에서 처리되었으므로 여기서는 사용만 함.

            if (isNearDeath)
            {
                if (!_blinkingSequences.ContainsKey(unitSnapshot.Name) || _blinkingSequences[unitSnapshot.Name] == null || !_blinkingSequences[unitSnapshot.Name].IsActive())
                {
                    if (_blinkingSequences.TryGetValue(unitSnapshot.Name, out var oldSeq)) { oldSeq?.Kill(); }

                    image.DOKill(); 
                    if (callsignTMP != null) callsignTMP.DOKill();
                    
                    Sequence blinkingSequence = DOTween.Sequence();
                    blinkingSequence.Append(image.DOFade(0.3f, 0.4f).SetEase(Ease.InOutSine))
                                  .Append(image.DOFade(1.0f, 0.4f).SetEase(Ease.InOutSine));
                    if (callsignTMP != null)
                    {
                        blinkingSequence.Join(callsignTMP.DOFade(0.3f, 0.4f).SetEase(Ease.InOutSine))
                                      .Join(callsignTMP.DOFade(1.0f, 0.4f).SetEase(Ease.InOutSine));
                    }
                    blinkingSequence.SetLoops(-1, LoopType.Restart)
                                  .SetId(marker.GetInstanceID() + "_blinking");
                    _blinkingSequences[unitSnapshot.Name] = blinkingSequence;
                    blinkingSequence.Play();
                }
            }
            else
            {
                if (_blinkingSequences.TryGetValue(unitSnapshot.Name, out var blinkingSeq))
                {
                    blinkingSeq?.Kill();
                    _blinkingSequences.Remove(unitSnapshot.Name);
                    if (!stateChangedThisFrame) 
                    {
                         image.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1f);
                         if (callsignTMP != null) callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, 1f);
                    }
                }
            }

            if (stateChangedThisFrame)
            {
                DOTween.Kill(image, false); 
                if (callsignTMP != null) DOTween.Kill(callsignTMP, false);

                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, 0f);
                if (callsignTMP != null) callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, 0f);
                
                image.material = targetMaterial; 
                image.DOFade(1f, materialTransitionFadeDuration).SetId(image); 
                if (callsignTMP != null) callsignTMP.DOFade(1f, materialTransitionFadeDuration).SetId(callsignTMP);

                if (shouldBeAlwaysVisible && !wasAlwaysVisible) 
                {
                    PlayDetectedPulse(marker).Forget(); 
                }
            }
            else
            {
                if (image.material != targetMaterial) { image.material = targetMaterial; }
                Color currentColor = image.color;
                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, currentColor.a);
                if (callsignTMP != null && !isNearDeath && !stateChangedThisFrame) 
                {
                    Color currentTextColor = callsignTMP.color;
                }
            }

            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = shouldBeAlwaysVisible; 

            //marker.transform.localScale = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            // +++ SASHA: Smooth Scale Transition for Active/Inactive State +++
            Vector3 targetScaleVec = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            if (marker.transform.localScale != targetScaleVec)
            {
                DOTween.Kill(marker.transform, true); // Complete current scale tween before starting new one
                marker.transform.DOScale(targetScaleVec, markerActiveScaleDuration).SetEase(Ease.OutQuad).SetId(marker.transform);
            }
            // +++ SASHA: End Smooth Scale Transition +++
            
            // SASHA: Callsign 업데이트 로직 (unitSnapshot.Name 사용, 메서드 상단에서 이미 할당된 callsignTMP 사용)
            if (callsignTMP != null) // callsignTMP는 메서드 상단에서 이미 값을 할당받았거나 null일 것임
            {
                // 이름이 변경될 가능성은 거의 없지만, 만약을 위해 업데이트
                if (callsignTMP.text != unitSnapshot.Name) 
                {
                    callsignTMP.text = unitSnapshot.Name;
                }
            }
        }

        // GetTeamColor remains the same
        private Color GetTeamColor(int teamId)
        {
            // --- SASHA: CombatTestRunner에서 색상 가져오기 시도 ---
            var combatTestRunnerService = ServiceLocator.Instance.GetService<CombatTestRunner>();
            if (combatTestRunnerService != null && combatTestRunnerService.TryGetTeamColor(teamId, out Color color))
            {
                return color;
            }
            // --- SASHA: 수정 끝 ---

            // Fallback colors (CombatTestRunner가 없거나 해당 팀 ID 색상이 없을 경우)
            switch(teamId)
            {
                case 0: return Color.cyan;  // Player team (example)
                case 1: return Color.red;    // Enemy team 1 (example)
                case 2: return Color.yellow; // Enemy team 2 (example)
                default: return Color.white; // Neutral or unknown
            }
        }

        // TODO: Update target lines based on snapshot
        /*
        private void UpdateTargetLinesFromSnapshot(ArmoredFrameSnapshot activeUnitSnapshot, Dictionary<string, ArmoredFrameSnapshot> snapshotDict)
        {
             // Clear existing lines
             foreach (var line in _targetLines) { if(line != null) Destroy(line.gameObject); }
             _targetLines.Clear();

             // Get current target info (this needs a way to be stored/retrieved for playback)
             string currentTargetName = GetCurrentTargetNameForPlayback(activeUnitSnapshot.Name, snapshotDict); // How to get target info?

             if (!string.IsNullOrEmpty(currentTargetName) &&
                 _unitMarkers.TryGetValue(activeUnitSnapshot.Name, out var activeMarker) &&
                 _unitMarkers.TryGetValue(currentTargetName, out var targetMarker))
             {
                  if (activeMarker == null || targetMarker == null) return;

                  LineRenderer newLine = Instantiate(targetLinePrefab, radarContainer);
                  if(newLine == null) return;

                  newLine.positionCount = 2;
                  Vector3 startPos = activeMarker.GetComponent<RectTransform>().anchoredPosition;
                  Vector3 endPos = targetMarker.GetComponent<RectTransform>().anchoredPosition;
                  newLine.SetPosition(0, startPos);
                  newLine.SetPosition(1, endPos);
                  _targetLines.Add(newLine);
             }
        }

        // Placeholder - How to determine the target during playback?
        private string GetCurrentTargetNameForPlayback(string activeUnitName, Dictionary<string, ArmoredFrameSnapshot> snapshot)
        {
             // This is tricky. The target choice isn't typically in the TurnStart snapshot.
             // Maybe needs to be inferred from subsequent ActionCompletedEvents?
             // Or the LogEntry needs to store target info explicitly for UI playback.
             return null;
        }
        */

        #endregion

        // +++ SASHA: LateUpdate for Shader Parameters +++
        private void LateUpdate()
        {
            if (radarScanTransform == null || markersMaterial == null)
            {
                // 필수 참조가 없으면 아무것도 하지 않음
                // 필요하다면 초기화 시점에 경고 로그를 남길 수 있음
                return;
            }

            // 1. 레이더의 현재 Z축 회전 각도 (애니메이터에 의해 업데이트된 값)
            float currentScanAngleDegrees = radarScanTransform.localEulerAngles.z;
            float scanCurrentAngleRad = currentScanAngleDegrees * Mathf.Deg2Rad;

            // 2. 스캔 빔 너비 및 페이드 범위 (라디안으로 변환)
            float scanArcWidthRad = scanArcWidthDegrees * Mathf.Deg2Rad;
            float scanFadeRangeRad = scanFadeRangeDegrees * Mathf.Deg2Rad;

            // 3. 레이더 중심 UI 좌표 계산 (셰이더는 UI 좌표계 기준일 가능성이 높음)
            // TODO: _RadarCenterUIPos 계산:
            // - radarContainer 또는 radarScanTransform의 RectTransform을 기준으로 계산.
            // - 캔버스의 Render Mode (Screen Space - Overlay, Camera, World Space)에 따라 좌표 변환 방식이 달라짐.
            // - Screen Space - Overlay 라면 RectTransform의 position이 이미 UI 좌표일 수 있음 (피봇 주의).
            // - Camera 또는 World Space 라면, 월드 좌표를 UI 좌표로 변환해야 할 수 있음 (RectTransformUtility.WorldToScreenPoint 또는 유사한 방법).
            // - 셰이더에서 사용할 좌표계와 일치시키는 것이 중요. 
            // 기본 가정: 각 마커의 로컬 (0,0)이 레이더 회전의 중심점이라고 가정 (즉, 마커들이 회전 중심의 직계 자식일 때).
            // 만약 아니라면, 이 값을 정확히 계산해야 함.
            Vector2 radarCenterUIPos = Vector2.zero; 

            // 4. 계산된 값들을 마커 머티리얼의 셰이더 유니폼 변수로 전달
            markersMaterial.SetFloat("_ScanCurrentAngleRad", scanCurrentAngleRad);
            markersMaterial.SetFloat("_ScanArcWidthRad", scanArcWidthRad);
            markersMaterial.SetFloat("_FadeRangeRad", scanFadeRangeRad);
            markersMaterial.SetVector("_RadarCenterUIPos", new Vector4(radarCenterUIPos.x, radarCenterUIPos.y, 0, 0));

            // 디버깅용 로그 (필요시 활성화)
            // Debug.Log($"ScanAngleDeg: {currentScanAngleDegrees}, ScanAngleRad: {scanCurrentAngleRad}, ArcWidthRad: {scanArcWidthRad}, CenterUIPos: {radarCenterUIPos}");

            // 5. 각 마커를 순회하며 텍스트 알파 값 조정
            foreach (var entry in _unitMarkers)
            {
                GameObject markerObject = entry.Value;
                if (markerObject == null) continue;

                Image markerImage = markerObject.GetComponent<Image>();
                if (markerImage == null) continue;

                // TextMeshProUGUI 텍스트 가져오기
                if (!_markerTexts.TryGetValue(entry.Key, out TextMeshProUGUI callsignTMP) || callsignTMP == null)
                {
                    continue; // 텍스트가 없거나 캐시에 없으면 건너뜀
                }

                if (markerImage.material == markersMaterial) // 스캔 효과를 받는 마커인가?
                {
                    // 현재 마커의 UI상 위치 (레이더 중심 기준)
                    Vector2 markerUIPosition = markerObject.GetComponent<RectTransform>().anchoredPosition;

                    // 마커의 각도 계산 (라디안 단위, Y축이 위쪽일 때 0도, 시계방향으로 증가)
                    // Atan2의 결과는 -PI ~ PI 범위. Unity의 각도는 보통 0~360. 정규화 필요.
                    float markerAngleRad = Mathf.Atan2(markerUIPosition.x, markerUIPosition.y); 

                    // 스캔 빔 중심과의 각도 차이 (라디안, 절대값, 최단거리)
                    // scanCurrentAngleRad는 0부터 2PI로 가정 (또는 -PI ~ PI로 변환 필요)
                    // Mathf.DeltaAngle은 degree를 사용하므로 변환 후 사용
                    float markerAngleDeg = markerAngleRad * Mathf.Rad2Deg;
                    float currentScanAngleDegForDelta = currentScanAngleDegrees; // localEulerAngles.z는 0-360 범위일 가능성 높음

                    float angleDifferenceDeg = Mathf.DeltaAngle(markerAngleDeg, currentScanAngleDegForDelta);
                    float angleDifferenceRad = Mathf.Abs(angleDifferenceDeg * Mathf.Deg2Rad);
                    
                    float shaderAlphaMultiplier = 0f;
                    float halfArcWidthRad = scanArcWidthRad / 2f;

                    if (angleDifferenceRad < halfArcWidthRad)
                    {
                        float fadeEffectStartAngle = halfArcWidthRad - scanFadeRangeRad;
                        if (fadeEffectStartAngle < 0) fadeEffectStartAngle = 0; // 페이드 범위가 아크 절반보다 클 경우 대비

                        if (angleDifferenceRad < fadeEffectStartAngle)
                        {
                            shaderAlphaMultiplier = 1f; 
                        }
                        else
                        {
                            if (scanFadeRangeRad > 0) // 0으로 나누기 방지
                            {
                                shaderAlphaMultiplier = 1f - ((angleDifferenceRad - fadeEffectStartAngle) / scanFadeRangeRad);
                            }
                            else // 페이드 범위가 0이면 스텝 함수처럼 동작 (빔 안에 있으면 1, 아니면 0)
                            {
                                shaderAlphaMultiplier = 1f;
                            }
                            shaderAlphaMultiplier = Mathf.Clamp01(shaderAlphaMultiplier);
                        }
                    }

                    float currentProgrammaticAlpha = callsignTMP.color.a;
                    callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, currentProgrammaticAlpha * shaderAlphaMultiplier);
                }
                // 스캔 효과를 받지 않는 마커의 텍스트 알파는 이미 다른 로직(UpdateMarkerAppearanceFromSnapshot 등)에서 관리됨.
            }
        }
        // +++ SASHA: End LateUpdate +++

        #region Visual Effects Helpers

        private async UniTask PlayFlashEffect(GameObject marker, Color flashColor)
        {
            if (marker == null) return;
            var image = marker.GetComponent<Image>();
            if (image == null) return;

            // DOTween.Kill(image, true); // Complete any existing tween before starting new one
            Color originalColor = image.color;
            // For flash, we want to preserve the target alpha (usually 1.0 after fade-in)
            // So, we flash RGB components, then restore. Alpha is controlled by material transition or other effects.
            Color flashRgbColor = new Color(flashColor.r, flashColor.g, flashColor.b, originalColor.a);
            Color originalRgbColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a);

            // +++ SASHA: Add Shake Effect +++
            marker.transform.DOShakePosition(flashDuration, damageShakeStrength, damageShakeVibrato, damageShakeRandomness, false, true).SetId(marker.transform); // Shake position
            // +++ SASHA: End Shake Effect +++

            await image.DOColor(flashRgbColor, flashDuration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await image.DOColor(originalRgbColor, flashDuration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        private async UniTask PlayPulseEffect(GameObject marker, float targetScale = 0f, float duration = 0f)
        {
            if (marker == null) return;
            // DOTween.Kill(marker.transform, true); // Complete any existing scale tween

            if (targetScale <= 0f) targetScale = pulseScale;
            if (duration <= 0f) duration = flashDuration;

            Vector3 originalScale = marker.transform.localScale;
            // Ensure the original scale is set before pulsing if needed
            // This might fight with the isActiveUnit scale setting.
            // For pulse, it's usually a brief effect on top of current scale.
            // Let's assume originalScale is the base (e.g. 1 or 1.5)
            // await marker.transform.DOScale(originalScale * targetScale, duration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            // await marker.transform.DOScale(originalScale, duration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
            
            // Better: Pulse relative to current scale if active, or base scale if not.
            // The current logic already sets scale based on isActiveUnit. Pulse should be additive or multiplicative to that.
            // For simplicity, let's make DOScale work on the current scale.
            Vector3 currentMarkerScale = marker.transform.localScale;
            await marker.transform.DOScale(currentMarkerScale * targetScale, duration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await marker.transform.DOScale(currentMarkerScale, duration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        // +++ SASHA: New Helper for "Detected!" Pulse +++
        private async UniTask PlayDetectedPulse(GameObject marker)
        {
            if (marker == null) return;

            Vector3 originalScale = marker.transform.localScale; // Get current scale (could be 1.0f or 1.5f if active)
            Vector3 pulseTargetScale = originalScale * detectedPulseScaleFactor;

            // Ensure no other scale tween is running on this marker before starting a new one
            DOTween.Kill(marker.transform, false); // false: don't complete, just kill

            Sequence pulseSequence = DOTween.Sequence();
            pulseSequence.Append(marker.transform.DOScale(pulseTargetScale, detectedPulseDuration / 2).SetEase(detectedPulseEaseOut))
                         .Append(marker.transform.DOScale(originalScale, detectedPulseDuration / 2).SetEase(detectedPulseEaseIn));
            pulseSequence.SetId(marker.transform); // Set ID for potential kill later

            await pulseSequence.AsyncWaitForCompletion();
        }
        // +++ SASHA: End New Helper +++

        #endregion

        // +++ SASHA: CalculateRadarPosition 메서드 복원 +++
        // Modified to accept only targetWorldPos, uses _battleCenterWorldPosition internally
        private Vector2 CalculateRadarPosition(Vector3 targetWorldPos)
        {
            // 월드 상의 상대 벡터 계산 (Y 무시) - 기준점은 _battleCenterWorldPosition
            Vector3 worldOffset = targetWorldPos - _battleCenterWorldPosition;
            Vector2 relativePos2D = new Vector2(worldOffset.x, worldOffset.z); // Z축을 UI의 Y로 사용

            float worldDistance = relativePos2D.magnitude;
            float scaleFactor = radarRadius / maxDetectionRange;

            // 레이더 UI 상의 위치 계산 (거리가 maxDetectionRange 넘어가면 가장자리에 표시)
            Vector2 radarPosition = relativePos2D.normalized * Mathf.Min(worldDistance, maxDetectionRange) * scaleFactor;

            // 최대 반경 보정 (원이 아닌 경우 필요 없을 수 있음)
            if (radarPosition.magnitude > radarRadius)
            {
                radarPosition = radarPosition.normalized * radarRadius;
            }

            return radarPosition;
        }
        // +++ SASHA: CalculateRadarPosition 메서드 복원 끝 +++
    }
} 