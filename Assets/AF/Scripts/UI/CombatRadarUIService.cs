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
    /// 전투 상황을 레이더/소나 스타일 UI로 시각화하는 서비스. (텍스트 로그 플레이백 이벤트 기반)
    /// </summary>
    public class CombatRadarUIService : MonoBehaviour, IService
    {
        [Header("UI References")]
        [SerializeField] private RectTransform radarContainer; // 레이더 UI의 기준 영역
        [SerializeField] private GameObject unitMarkerPrefab; // 유닛 표시용 프리팹
        [SerializeField] private LineRenderer targetLinePrefab; // 타겟 연결선 프리팹 (선택적) - 현재 미사용

        private EventBus.EventBus _eventBus;
        // private ICombatSimulatorService _combatSimulator; // No longer needed
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

        private Dictionary<string, GameObject> _unitMarkers = new Dictionary<string, GameObject>();
        private List<LineRenderer> _targetLines = new List<LineRenderer>(); // Currently unused

        private ArmoredFrameSnapshot _currentActiveUnitSnapshot; // <<< Store active unit for position calculations

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            // _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); // Removed

            if (_eventBus == null || radarContainer == null || unitMarkerPrefab == null)
            {
                Debug.LogError("CombatRadarUIService 초기화 실패: 필수 참조(EventBus, UI 요소)가 없습니다.");
                enabled = false; // 초기화 실패 시 비활성화
                return;
            }

            SubscribeToEvents();
            ClearRadar();
            _isInitialized = true;
            Debug.Log("CombatRadarUIService Initialized (Event-Based)..");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            UnsubscribeFromEvents();
            ClearRadar(); // 정리
            _eventBus = null;
            // _combatSimulator = null; // Removed
            _isInitialized = false;
            Debug.Log("CombatRadarUIService Shutdown.");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart); // Keep for initial clear
            // _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd); // No longer needed for playback start

            // <<< Subscribe to new Playback Events >>>
            _eventBus.Subscribe<PlaybackEvents.PlaybackSnapshotUpdateEvent>(HandlePlaybackSnapshotUpdate);
            _eventBus.Subscribe<PlaybackEvents.PlaybackUnitMoveEvent>(HandlePlaybackUnitMove);
            _eventBus.Subscribe<PlaybackEvents.PlaybackDamageEvent>(HandlePlaybackDamage);
            _eventBus.Subscribe<PlaybackEvents.PlaybackPartDestroyedEvent>(HandlePlaybackPartDestroyed);
            _eventBus.Subscribe<PlaybackEvents.PlaybackRepairEvent>(HandlePlaybackRepair);
            _eventBus.Subscribe<PlaybackEvents.PlaybackStatusEffectEvent>(HandlePlaybackStatusEffect);
            _eventBus.Subscribe<PlaybackEvents.PlaybackGenericActionEvent>(HandlePlaybackGenericAction);
            _eventBus.Subscribe<PlaybackEvents.PlaybackCompleteEvent>(HandlePlaybackComplete); // Optional: For final cleanup
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            // _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd); // Removed

             // <<< Unsubscribe from new Playback Events >>>
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackSnapshotUpdateEvent>(HandlePlaybackSnapshotUpdate);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackUnitMoveEvent>(HandlePlaybackUnitMove);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackDamageEvent>(HandlePlaybackDamage);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackPartDestroyedEvent>(HandlePlaybackPartDestroyed);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackRepairEvent>(HandlePlaybackRepair);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackStatusEffectEvent>(HandlePlaybackStatusEffect);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackGenericActionEvent>(HandlePlaybackGenericAction);
            _eventBus.Unsubscribe<PlaybackEvents.PlaybackCompleteEvent>(HandlePlaybackComplete);
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            ClearRadar();
            _currentActiveUnitSnapshot = default; // Reset active unit
        }

        // HandleCombatEnd removed - Playback is triggered by Text Service events now
        /*
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent ev)
        {
            // Start the log playback process
            // ProcessRadarPlaybackAsync(ev).Forget(); // <<< REMOVED >>>
        }
        */

        // ProcessRadarPlaybackAsync removed entirely
        /*
        private async UniTaskVoid ProcessRadarPlaybackAsync(CombatSessionEvents.CombatEndEvent endEvent)
        { ... }
        */

        // --- New Playback Event Handlers --- //

        private void HandlePlaybackSnapshotUpdate(PlaybackEvents.PlaybackSnapshotUpdateEvent ev)
        {
            if (!_isInitialized) return;
            _currentActiveUnitSnapshot = ev.ActiveUnitSnapshot; // Store active unit for centering
            UpdateRadarDisplayFromSnapshot(ev.SnapshotDict, ev.ActiveUnitSnapshot);
        }

        private void HandlePlaybackUnitMove(PlaybackEvents.PlaybackUnitMoveEvent ev)
        {
            if (!_isInitialized || _currentActiveUnitSnapshot.Name == null) return; // Need active unit for center pos
            if (_unitMarkers.TryGetValue(ev.UnitName, out GameObject targetMarker) && targetMarker != null)
            {
                 // Use the stored _currentActiveUnitSnapshot for the center position
                 Vector2 newRadarPos = CalculateRadarPosition(_currentActiveUnitSnapshot.Position, ev.NewPosition);
                 // Animate marker movement (async void, fire and forget)
                 AnimateMarkerMoveAsync(targetMarker, newRadarPos).Forget();
            }
        }

        private async UniTaskVoid AnimateMarkerMoveAsync(GameObject marker, Vector2 newPosition)
        {
             if (marker == null) return;
             await marker.GetComponent<RectTransform>().DOAnchorPos(newPosition, moveAnimDuration).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
        }

        private void HandlePlaybackDamage(PlaybackEvents.PlaybackDamageEvent ev)
        {
            if (!_isInitialized) return;
            if (_unitMarkers.TryGetValue(ev.TargetUnitName, out GameObject targetMarker) && targetMarker != null)
            {
                 // Play flash effect (async void, fire and forget)
                 PlayFlashEffect(targetMarker, damageFlashColor).Forget();
            }
        }

         private void HandlePlaybackPartDestroyed(PlaybackEvents.PlaybackPartDestroyedEvent ev)
        {
            if (!_isInitialized) return;
            if (_unitMarkers.TryGetValue(ev.OwnerName, out GameObject targetMarker) && targetMarker != null)
            {
                PlayFlashEffect(targetMarker, Color.black).Forget();
                // Potentially update appearance more drastically based on snapshot if needed
                // This might require the SnapshotUpdateEvent to be published more frequently
                // or this event needs to carry the updated snapshot data for the owner.
                // For now, just flash black.
            }
        }

        private void HandlePlaybackRepair(PlaybackEvents.PlaybackRepairEvent ev)
        {
            if (!_isInitialized) return;
             if (_unitMarkers.TryGetValue(ev.TargetName, out GameObject targetMarker) && targetMarker != null)
            {
                PlayFlashEffect(targetMarker, Color.green).Forget();
            }
        }

        private void HandlePlaybackStatusEffect(PlaybackEvents.PlaybackStatusEffectEvent ev)
        {
            if (!_isInitialized) return;
            if (_unitMarkers.TryGetValue(ev.TargetName, out GameObject targetMarker) && targetMarker != null)
            {
                PlayPulseEffect(targetMarker, 1.2f, 0.15f).Forget();
            }
        }

        private void HandlePlaybackGenericAction(PlaybackEvents.PlaybackGenericActionEvent ev)
        {
             if (!_isInitialized) return;
            if (_unitMarkers.TryGetValue(ev.ActorName, out GameObject targetMarker) && targetMarker != null)
            {
                PlayPulseEffect(targetMarker).Forget(); // Use default pulse
            }
        }

        private void HandlePlaybackComplete(PlaybackEvents.PlaybackCompleteEvent ev)
        {
            Debug.Log("Radar received Playback Complete.");
            // Optional: Add any final UI states or cleanup here
            _currentActiveUnitSnapshot = default; // Clear active unit
        }

        #endregion

        #region Radar Update Logic (Snapshot Based - Mostly Unchanged)

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

        // Renamed and modified to accept snapshot
        private void UpdateRadarDisplayFromSnapshot(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, ArmoredFrameSnapshot activeUnitSnapshot)
        {
            // Structs cannot be null, so no need to check activeUnitSnapshot for nullity
            // <<< Added check for valid active unit name >>>
            if (!_isInitialized || activeUnitSnapshot.Name == null) return;

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
                 Vector2 radarPosition = CalculateRadarPosition(activeUnitSnapshot.Position, unitSnapshot.Position);
                 bool isActive = unitSnapshot.Name == activeUnitSnapshot.Name;

                if (_unitMarkers.TryGetValue(unitSnapshot.Name, out GameObject marker))
                {
                    // Update existing marker
                    if(marker != null)
                    {
                        // <<< Don't instantly update position here, rely on move events/animations >>>
                        // marker.GetComponent<RectTransform>().anchoredPosition = radarPosition;
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive);
                    }
                    else
                    {
                        _unitMarkers.Remove(unitSnapshot.Name); // Remove null entry
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive);
                    }
                }
                else
                {
                    // Create new marker
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive);
                }
            }

            // TODO: Update target lines based on snapshot data if needed (Currently Unused)
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

        // CalculateRadarPosition remains the same
        private Vector2 CalculateRadarPosition(Vector3 centerWorldPos, Vector3 targetWorldPos)
        {
            // 월드 상의 상대 벡터 계산 (Y 무시)
            Vector3 worldOffset = targetWorldPos - centerWorldPos;
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
            // <<< Animate scale changes? Or instant? Keep instant for now >>>
            marker.transform.localScale = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;

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
             // Fallback colors (replace with actual implementation if possible)
             switch(teamId)
             {
                 case 0: return Color.cyan;  // Player team (example)
                 case 1: return Color.red;    // Enemy team 1 (example)
                 case 2: return Color.yellow; // Enemy team 2 (example)
                 default: return Color.white; // Neutral or unknown
             }
        }

        // UpdateTargetLinesFromSnapshot - Currently Unused
        /*
        ...
        */

        #endregion

        #region Visual Effects Helpers (Unchanged - except async UniTaskVoid)

        private async UniTaskVoid PlayFlashEffect(GameObject marker, Color flashColor)
        {
            if (marker == null) return;
            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color originalColor = image.color;
            // Using try-finally to ensure color reset even if sequence is interrupted
            try
            {
                await image.DOColor(flashColor, flashDuration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
                await image.DOColor(originalColor, flashDuration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
            }
            finally
            {
                 if (image != null) image.color = originalColor; // Ensure reset
            }
        }

        private async UniTaskVoid PlayPulseEffect(GameObject marker, float targetScale = 0f, float duration = 0f)
        {
            if (marker == null) return;
            if (targetScale <= 0f) targetScale = pulseScale;
            if (duration <= 0f) duration = flashDuration;

            Vector3 originalScale = marker.transform.localScale;
            // Using try-finally to ensure scale reset
            try
            {
                await marker.transform.DOScale(originalScale * targetScale, duration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
                await marker.transform.DOScale(originalScale, duration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
            }
            finally
            {
                if (marker != null) marker.transform.localScale = originalScale; // Ensure reset
            }
        }

        #endregion
    }
} 