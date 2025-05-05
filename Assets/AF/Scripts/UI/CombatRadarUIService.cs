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

        // Changed Dictionary Key from ArmoredFrame to string (Unit Name)
        private Dictionary<string, GameObject> _unitMarkers = new Dictionary<string, GameObject>();
        private List<LineRenderer> _targetLines = new List<LineRenderer>();

        // +++ Add field to store the battle center +++
        private Vector3 _battleCenterWorldPosition = Vector3.zero;

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

            // 1. Update the base radar display (marker positions and appearances)
            // This uses the CURRENT state from the snapshot
            UpdateRadarDisplayFromSnapshot(ev.CurrentSnapshot, ev.ActiveUnitName);

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
                if (marker != null) Destroy(marker); // Null 체크 추가
            }
            _unitMarkers.Clear(); // Now clears Dictionary<string, GameObject>

            foreach (var line in _targetLines)
            {
                 if (line != null) Destroy(line.gameObject); // Null 체크 추가
            }
             _targetLines.Clear();
        }

        // Renamed and modified to accept snapshot and active unit name for highlight
        private void UpdateRadarDisplayFromSnapshot(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, string activeUnitName)
        {
            if (!_isInitialized) return;

            // --- Marker Management (Using Snapshot Names) ---
            HashSet<string> currentOperationalUnitNames = new HashSet<string>(
                snapshotDict.Values.Where(s => s.IsOperational).Select(s => s.Name)
            );
            List<string> unitsToRemoveFromMarkers = new List<string>();

            // 1. Identify markers to remove
            foreach (var existingUnitName in _unitMarkers.Keys)
            {
                if (!currentOperationalUnitNames.Contains(existingUnitName))
                {
                    unitsToRemoveFromMarkers.Add(existingUnitName);
                }
            }

            // 2. Remove markers
            foreach(var unitName in unitsToRemoveFromMarkers)
            {
                if (_unitMarkers.TryGetValue(unitName, out var marker))
                {
                     if(marker != null) Destroy(marker);
                     _unitMarkers.Remove(unitName);
                }
            }

            // 3. Update or create markers for current operational units
            foreach (var unitSnapshot in snapshotDict.Values)
            {
                 if (!unitSnapshot.IsOperational) continue;

                 // Assuming ArmoredFrameSnapshot has a Position property
                 // Use the modified CalculateRadarPosition
                 Vector2 radarPosition = CalculateRadarPosition(unitSnapshot.Position);
                 bool isActive = unitSnapshot.Name == activeUnitName; // Compare with active unit name

                if (_unitMarkers.TryGetValue(unitSnapshot.Name, out GameObject marker))
                {
                    // Update existing marker
                    if(marker != null)
                    {
                        marker.GetComponent<RectTransform>().anchoredPosition = radarPosition;
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive);
                    }
                    else
                    {
                        _unitMarkers.Remove(unitSnapshot.Name);
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive);
                    }
                }
                else
                {
                    // Create new marker
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive);
                }
            }

            // TODO: Update target lines based on snapshot data if needed
            // UpdateTargetLinesFromSnapshot(activeUnitSnapshot, snapshotDict);
        }

        // Modified to accept snapshot
        private void CreateNewMarkerFromSnapshot(ArmoredFrameSnapshot unitSnapshot, Vector2 position, bool isActive)
        {
             GameObject newMarker = Instantiate(unitMarkerPrefab, radarContainer);
             if(newMarker == null)
             {
                  Debug.LogError($"Failed to instantiate unit marker for {unitSnapshot.Name}");
                  return;
             }
             newMarker.name = $"Marker_{unitSnapshot.Name}";
             newMarker.GetComponent<RectTransform>().anchoredPosition = position;
             _unitMarkers.Add(unitSnapshot.Name, newMarker); // Use Name as key
             UpdateMarkerAppearanceFromSnapshot(newMarker, unitSnapshot, isActive);
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
        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit)
        {
            // Structs cannot be null, removed null check for unitSnapshot
            if (marker == null) return;

            // --- Image component (Color, Sprite) ---
            var image = marker.GetComponent<Image>();
            if (image != null)
            {
                 // Assuming ArmoredFrameSnapshot has a TeamId property
                Color teamColor = GetTeamColor(unitSnapshot.TeamId);
                image.color = teamColor;
                // TODO: Use different sprites based on unit type/status from snapshot
            }

            // --- Active unit highlight ---
            marker.transform.localScale = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            // TODO: Add other visual effects for active unit

            // --- TextMeshPro component (Unit Name, etc.) ---
            var text = marker.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                // text.text = unitSnapshot.Name; // Display unit name (optional)
            }
        }

        // GetTeamColor remains the same
        private Color GetTeamColor(int teamId)
        {
            // ... (Implementation is unchanged) ...
             /* // Commented out due to context errors - Needs proper implementation later
             if (CombatTestRunner.Instance != null && CombatTestRunner.Instance.TryGetTeamColor(teamId, out Color color))
             {
                 return color;
             }
             */
             // Fallback colors
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

        #region Visual Effects Helpers

        private async UniTask PlayFlashEffect(GameObject marker, Color flashColor)
        {
            if (marker == null) return;
            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color originalColor = image.color;
            await image.DOColor(flashColor, flashDuration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await image.DOColor(originalColor, flashDuration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        private async UniTask PlayPulseEffect(GameObject marker, float targetScale = 0f, float duration = 0f)
        {
            if (marker == null) return;
            if (targetScale <= 0f) targetScale = pulseScale;
            if (duration <= 0f) duration = flashDuration;

            Vector3 originalScale = marker.transform.localScale;
            // Ensure the original scale is set before pulsing if needed (might be redundant)
            // marker.transform.localScale = originalScale; 
            await marker.transform.DOScale(originalScale * targetScale, duration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await marker.transform.DOScale(originalScale, duration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        #endregion
    }
} 