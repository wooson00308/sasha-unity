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

        // Changed Dictionary Key from ArmoredFrame to string (Unit Name)
        private Dictionary<string, GameObject> _unitMarkers = new Dictionary<string, GameObject>();
        private List<LineRenderer> _targetLines = new List<LineRenderer>();
        private Dictionary<string, bool> _lastMarkerWasAlwaysVisible = new Dictionary<string, bool>(); // 머티리얼 상태 추적

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
            UpdateRadarDisplayFromSnapshot(ev.CurrentSnapshot, ev.ActiveUnitName, focusTargetSnapshotNullable); // SASHA: focusTargetSnapshotNullable 전달 (이름 대신)

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
                            targetMarker?.GetComponent<RectTransform>()?.DOAnchorPos(newRadarPos, moveAnimDuration).SetEase(Ease.OutQuad);
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
                        await PlayFlashEffect(targetMarker, Color.black);
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
                    Destroy(marker);
                }
            }
            _unitMarkers.Clear();
            _lastMarkerWasAlwaysVisible.Clear(); // 이전 상태 추적 딕셔너리도 클리어

            foreach (var line in _targetLines)
            {
                 if (line != null) Destroy(line.gameObject); 
            }
             _targetLines.Clear();
        }

        // Renamed and modified to accept snapshot and active unit name for highlight
        // SASHA: radarFocusTargetUnitName 파라미터 대신 focusTargetSnapshotNullable 파라미터 사용
        private void UpdateRadarDisplayFromSnapshot(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, string activeUnitName, ArmoredFrameSnapshot? focusTargetSnapshotNullable)
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
                         Destroy(marker);
                     }
                     _unitMarkers.Remove(unitName);
                     _lastMarkerWasAlwaysVisible.Remove(unitName); // 상태 제거
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
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRange, playerSquadNames); // SASHA: isInWeaponRange, playerSquadNames 전달
                    }
                    else
                    {
                        _unitMarkers.Remove(unitSnapshot.Name);
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRange, playerSquadNames); // SASHA: isInWeaponRange, playerSquadNames 전달
                    }
                }
                else
                {
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRange, playerSquadNames); // SASHA: isInWeaponRange, playerSquadNames 전달
                }
            }
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRange 파라미터 추가
        private void CreateNewMarkerFromSnapshot(ArmoredFrameSnapshot unitSnapshot, Vector2 position, bool isActive, bool isRadarFocus, bool isInWeaponRange, HashSet<string> playerSquadNames)
        {
             GameObject newMarker = Instantiate(unitMarkerPrefab, radarContainer);
             if(newMarker == null)
             {
                  Debug.LogError($"Failed to instantiate unit marker for {unitSnapshot.Name}");
                  return;
             }
             newMarker.name = $"Marker_{unitSnapshot.Name}";
             Image image = newMarker.GetComponent<Image>();
             if (image == null)
             {
                 Debug.LogError($"Marker {unitSnapshot.Name} is missing Image component.");
                 Destroy(newMarker);
                 return;
             }
            
             newMarker.GetComponent<RectTransform>().anchoredPosition = position;

             bool isInitiallyAlwaysVisible = playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange;
             Color initialTeamColor = GetTeamColor(unitSnapshot.TeamId);

             image.color = new Color(initialTeamColor.r, initialTeamColor.g, initialTeamColor.b, 0f); // Start transparent
             image.material = isInitiallyAlwaysVisible ? null : markersMaterial;
             image.DOFade(1f, materialTransitionFadeDuration).SetId(image);

             _unitMarkers.Add(unitSnapshot.Name, newMarker); 
             _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = isInitiallyAlwaysVisible; // 초기 상태 저장

             // UpdateMarkerAppearanceFromSnapshot 호출은 여기서 제거하고, UpdateRadarDisplayFromSnapshot의 루프에서 처리하도록 함
             // 이렇게 하면 isActive, isRadarFocus, isInWeaponRange 등의 최신 상태를 항상 반영 가능
             // 또한, 새로 생성된 마커에 대해 중복으로 UpdateMarkerAppearanceFromSnapshot가 호출되는 것을 방지.
             // 단, 최초 스케일 및 텍스트 설정은 여기서 필요하면 수행 (현재는 UpdateMarkerAppearanceFromSnapshot 에서 처리)
             newMarker.transform.localScale = isActive ? Vector3.one * 1.5f : Vector3.one;
             var text = newMarker.GetComponentInChildren<TMP_Text>();
             if (text != null)
             {
                 // text.text = unitSnapshot.Name; 
             }
        }

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

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRange 파라미터 추가
        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit, bool isRadarFocus, bool isInWeaponRange, HashSet<string> playerSquadNames)
        {
            if (marker == null) return;

            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color teamColor = GetTeamColor(unitSnapshot.TeamId); // Get base team color

            bool shouldBeAlwaysVisible = playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange;
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
                // Marker state not found, assume it's new to this logic or state was lost.
                // Treat as a change to establish the correct state with a fade.
                stateChangedThisFrame = true;
            }

            if (stateChangedThisFrame)
            {
                DOTween.Kill(image, false); // Kill any ongoing tween on the image (false = don't complete)

                // Set base color (RGB from team, Alpha to 0 for fade-in)
                image.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
                image.material = targetMaterial; // Apply the new material
                image.DOFade(1f, materialTransitionFadeDuration).SetId(image); // Fade alpha to 1
            }
            else
            {
                // State hasn't changed. Ensure material is correct (should be, but as a safe guard)
                // and team color is up-to-date. Preserve current alpha as it might be mid-fade.
                if (image.material != targetMaterial) {
                    image.material = targetMaterial; // Correct material if somehow mismatched without state change
                }
                // Update team color, but keep current alpha (이미지 트위닝에 영향 없도록)
                Color currentColor = image.color;
                image.color = new Color(teamColor.r, teamColor.g, teamColor.b, currentColor.a);
            }

            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = shouldBeAlwaysVisible; // 항상 최신 상태로 업데이트

            marker.transform.localScale = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            
            var text = marker.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                // text.text = unitSnapshot.Name; 
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

        #endregion
    }
} 