using UnityEngine;
using AF.Services;
using AF.Combat;
using AF.EventBus;
using AF.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using DG.Tweening;
using AF.Tests;

namespace AF.UI.Combat
{
    /// <summary>
    /// 전투 상황을 레이더/소나 스타일 UI로 시각화하는 서비스.
    /// </summary>
    public class CombatRadarUIService : MonoBehaviour, IService
    {
        [Header("UI References")]
        [SerializeField] private RectTransform radarContainer;
        [SerializeField] private GameObject unitMarkerPrefab;
        [SerializeField] private UILineRenderer uiLinePrefab;

        [Header("Target Line Animation Settings")]
        [SerializeField] private float lineInitialFadeInDuration = 0.05f;
        [SerializeField] private float lineDrawDuration = 0.2f;
        [SerializeField] private float lineSustainDuration = 0.1f;
        [SerializeField] private float lineFadeOutDuration = 0.2f;

        [Header("Target Line Dynamic Speed Settings")]
        [SerializeField] private float minLineDrawDuration = 0.05f;
        [SerializeField] private float maxLineDrawDuration = 0.2f;

        [Header("Target Line Animation Colors")]
        [SerializeField] private Color lineInitialStartColor = Color.yellow;
        [SerializeField] private Color lineFinalStartColor = Color.yellow;
        [SerializeField] private Color lineFinalEndColor = Color.red;
        [SerializeField] private Color lineFadeOutTargetColor = new Color(0.5f, 0, 0, 0);

        [Header("Evasion Effect Settings")]
        [SerializeField] private float evasionEffectDuration = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float evasionEffectMinAlpha = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float evasionEffectMinScaleFactor = 0.7f;

        private EventBus.EventBus _eventBus;
        private bool _isInitialized = false;

        [Header("Radar Settings")]
        [SerializeField] private float radarRadius = 100f;
        [SerializeField] private float maxDetectionRange = 50f;

        [Header("Visual Effects")]
        [SerializeField] private Color damageFlashColor = Color.magenta;
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private float pulseScale = 1.8f;
        [SerializeField] private float moveAnimDuration = 0.3f;
        [SerializeField] private float materialTransitionFadeDuration = 0.25f;

        [Header("Enhanced Animation Settings")]
        [SerializeField] private float markerAppearDuration = 0.35f;
        [SerializeField] private Ease markerAppearEase = Ease.OutBack;
        [SerializeField] private float markerDisappearDuration = 0.25f;
        [SerializeField] private Ease markerDisappearEase = Ease.InBack;
        [SerializeField] private Ease markerMoveEase = Ease.OutBounce; 
        [SerializeField] private Vector3 damageShakeStrength = new Vector3(3f, 3f, 0f);
        [SerializeField] private int damageShakeVibrato = 10;
        [SerializeField] private float damageShakeRandomness = 0f; 

        [Header("More Animation Settings")]
        [SerializeField] private float markerActiveScaleDuration = 0.2f;
        [SerializeField] private float detectedPulseScaleFactor = 1.3f;
        [SerializeField] private float detectedPulseDuration = 0.3f;
        [SerializeField] private Ease detectedPulseEaseOut = Ease.OutQuint; 
        [SerializeField] private Ease detectedPulseEaseIn = Ease.InQuint;

        private Dictionary<string, GameObject> _unitMarkers = new Dictionary<string, GameObject>();
        private List<LineRenderer> _targetLines = new List<LineRenderer>();
        private Dictionary<string, bool> _lastMarkerWasAlwaysVisible = new Dictionary<string, bool>();
        private Dictionary<string, Sequence> _blinkingSequences = new Dictionary<string, Sequence>();
        private Dictionary<string, TextMeshProUGUI> _markerTexts = new Dictionary<string, TextMeshProUGUI>();

        private Vector3 _battleCenterWorldPosition = Vector3.zero;
        private Vector3? _lastKnownPlayerUnitWorldPosition = null;

        private UILineRenderer _activeUILine;
        private Sequence _targetLineSequence;

        private string _lastRadarFocusTargetUnitName = null;

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;

            if (_eventBus == null || radarContainer == null || unitMarkerPrefab == null || uiLinePrefab == null)
            {
                Debug.LogError("CombatRadarUIService 초기화 실패: 필수 참조(EventBus, UI 요소, uiLinePrefab)가 없습니다.");
                enabled = false;
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
            ClearRadar();
            _eventBus = null;
            _isInitialized = false;

            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill();
            _targetLineSequence = null;

            Debug.Log("CombatRadarUIService Shutdown.");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate);
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            ClearRadar();
            _battleCenterWorldPosition = Vector3.zero;
            _lastKnownPlayerUnitWorldPosition = null;

            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill();
            _targetLineSequence = null;

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

        private async void HandleLogPlaybackUpdate(CombatLogPlaybackUpdateEvent ev)
        {
            if (!_isInitialized || ev.CurrentSnapshot == null || ev.CurrentLogEntry == null)
            {
                return;
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
            ArmoredFrameSnapshot? focusTargetSnapshotNullable = null;

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
                    focusTargetSnapshotNullable = activeUnitSnapshotValue;
                }
                else
                {
                    if (_lastKnownPlayerUnitWorldPosition.HasValue)
                    {
                        newRadarOrigin = _lastKnownPlayerUnitWorldPosition.Value;
                        radarFocusTargetUnitName = _lastRadarFocusTargetUnitName; 
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
                    }
                }
            }
            _battleCenterWorldPosition = newRadarOrigin;

            UpdateRadarDisplayFromSnapshot(ev.CurrentSnapshot, ev.ActiveUnitName, focusTargetSnapshotNullable, playerSquadAnnihilated);

            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
            await UniTask.Delay(TimeSpan.FromSeconds(0.01f));

            TextLogger.LogEntry logEntry = ev.CurrentLogEntry;
            GameObject targetMarker = null;
            string targetUnitName = null;

            string attackerNameForLine = null;
            string targetNameForLine = null;

            if (logEntry.EventType == LogEventType.WeaponFired)
            {
                attackerNameForLine = logEntry.Weapon_AttackerName;
                targetNameForLine = logEntry.Weapon_TargetName;
            }
            else if (logEntry.EventType == LogEventType.ActionCompleted && logEntry.Action_Type == CombatActionEvents.ActionType.Attack)
            {
                attackerNameForLine = logEntry.Action_ActorName;
                targetNameForLine = logEntry.Action_TargetName;
            }

            if (!string.IsNullOrEmpty(attackerNameForLine) && !string.IsNullOrEmpty(targetNameForLine) && uiLinePrefab != null)
            {
                if (ev.CurrentSnapshot.TryGetValue(attackerNameForLine, out var attackerSnapshot) &&
                    ev.CurrentSnapshot.TryGetValue(targetNameForLine, out var targetSnapshot) &&
                    _unitMarkers.TryGetValue(attackerNameForLine, out var attackerMarkerGO) &&
                    _unitMarkers.TryGetValue(targetNameForLine, out var targetMarkerGO) &&
                    attackerMarkerGO != null && targetMarkerGO != null)
                {
                    Vector2 attackerRadarPos = CalculateRadarPosition(attackerSnapshot.Position);
                    Vector2 targetRadarPos = CalculateRadarPosition(targetSnapshot.Position);
                    DrawTargetingLine(attackerRadarPos, targetRadarPos);
                }
            }

            switch (logEntry.EventType)
            {
                case LogEventType.ActionCompleted:
                    targetUnitName = logEntry.Action_ActorName;
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        if (logEntry.Action_Type == CombatActionEvents.ActionType.Move && logEntry.Action_IsSuccess && logEntry.Action_NewPosition.HasValue)
                        {
                            Vector2 newRadarPos = CalculateRadarPosition(logEntry.Action_NewPosition.Value);
                            targetMarker?.GetComponent<RectTransform>()?.DOAnchorPos(newRadarPos, moveAnimDuration).SetEase(markerMoveEase);
                        }
                        else if (logEntry.Action_IsSuccess && logEntry.Action_Type != CombatActionEvents.ActionType.Move)
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
                            if (targetMarker != null)
                            {
                                DOTween.Kill(targetMarker.transform);
                                Image markerImage = targetMarker.GetComponent<Image>();
                                if (markerImage != null) DOTween.Kill(markerImage);

                                Sequence destructionSequence = DOTween.Sequence();
                                destructionSequence.Append(markerImage.DOColor(Color.black, 0.1f))
                                    .Append(markerImage.DOFade(0.3f, 0.08f).SetLoops(6, LoopType.Yoyo))
                                    .Append(targetMarker.transform.DOShakePosition(0.3f, damageShakeStrength * 0.7f, 15, damageShakeRandomness))
                                    .Append(targetMarker.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
                                    .Join(markerImage.DOFade(0f, 0.3f))
                                    .OnComplete(() =>
                                    {
                                        if (targetMarker != null) Destroy(targetMarker);
                                        _unitMarkers.Remove(targetUnitName);
                                        _lastMarkerWasAlwaysVisible.Remove(targetUnitName);
                                    });
                                destructionSequence.Play();
                            }
                        }
                        else
                        {
                            await PlayFlashEffect(targetMarker, Color.gray);
                        }
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

                case LogEventType.DamageAvoided:
                    targetUnitName = logEntry.Avoid_TargetName; 
                    
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        PlayEvasionEffect(targetMarker).Forget();
                    }
                    break;

                default:
                    break;
            }

            foreach (var seq in _blinkingSequences.Values)
            {
                seq?.Kill();
            }
            _blinkingSequences.Clear();
        }

        #endregion

        #region Radar Update Logic (Snapshot Based)

        private void ClearRadar()
        {
            foreach (var markerKvp in _unitMarkers)
            {
                GameObject marker = markerKvp.Value;
                string unitName = markerKvp.Key;

                if (marker != null)
                {
                    DOTween.Kill(marker.transform, true);
                    Image image = marker.GetComponent<Image>();
                    if (image != null) DOTween.Kill(image, true);
                    
                    if (_markerTexts.TryGetValue(unitName, out TextMeshProUGUI callsignTMP) && callsignTMP != null)
                    {                        
                        DOTween.Kill(callsignTMP, true);
                    }

                    Destroy(marker);
                }
            }
            _unitMarkers.Clear();
            _lastMarkerWasAlwaysVisible.Clear(); 
            _markerTexts.Clear(); 

            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill(); 
            _targetLineSequence = null;
        }

        private void UpdateRadarDisplayFromSnapshot(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, string activeUnitName, ArmoredFrameSnapshot? focusTargetSnapshotNullable, bool playerSquadAnnihilated)
        {
            if (!_isInitialized) return;

            HashSet<string> currentOperationalUnitNames = new HashSet<string>(
                snapshotDict.Values.Where(s => s.IsOperational).Select(s => s.Name)
            );
            List<string> unitsToRemoveFromMarkers = new List<string>();

            var combatTestRunnerService = ServiceLocator.Instance.GetService<CombatTestRunner>();
            var playerSquadNames = combatTestRunnerService?.GetPlayerSquadUnitNames() ?? new HashSet<string>();

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
                         if (image != null) DOTween.Kill(image); 
                         DOTween.Kill(marker.transform); 

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
                     }
                     _unitMarkers.Remove(unitName);
                     _lastMarkerWasAlwaysVisible.Remove(unitName); 
                     _markerTexts.Remove(unitName); 
                }
            }

            foreach (var unitSnapshot in snapshotDict.Values)
            {
                 if (!unitSnapshot.IsOperational) continue;

                 Vector2 radarPosition = CalculateRadarPosition(unitSnapshot.Position);
                 bool isActive = unitSnapshot.Name == activeUnitName; 
                 bool isRadarFocus = unitSnapshot.Name == focusTargetSnapshotNullable?.Name; 
                 bool isInWeaponRangeOfFocusTarget = false; 
                 bool isVisibleDueToAllyIntel = false;

                 if (focusTargetSnapshotNullable.HasValue && !isRadarFocus) 
                 {
                     float distanceToFocus = Vector3.Distance(focusTargetSnapshotNullable.Value.Position, unitSnapshot.Position);
                     if (distanceToFocus <= focusTargetSnapshotNullable.Value.PrimaryWeaponRange)
                     {
                         isInWeaponRangeOfFocusTarget = true;
                     }
                 }

                 if (!playerSquadNames.Contains(unitSnapshot.Name))
                 {
                     foreach (var playerUnitName in playerSquadNames)
                     {
                         if (snapshotDict.TryGetValue(playerUnitName, out var playerUnitSnapshot) && playerUnitSnapshot.IsOperational)
                         {
                             float distanceToPlayerUnit = Vector3.Distance(playerUnitSnapshot.Position, unitSnapshot.Position);
                             if (distanceToPlayerUnit <= playerUnitSnapshot.PrimaryWeaponRange)
                             {
                                 isVisibleDueToAllyIntel = true;
                                 break;
                             }
                         }
                     }
                 }

                if (_unitMarkers.TryGetValue(unitSnapshot.Name, out GameObject marker))
                {
                    if(marker != null)
                    {
                        marker.GetComponent<RectTransform>().anchoredPosition = radarPosition;
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                    }
                    else
                    {
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                    }
                }
                else
                {
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                }
            }
        }

        private void CreateNewMarkerFromSnapshot(ArmoredFrameSnapshot unitSnapshot, Vector2 position, bool isActive, bool isRadarFocus, bool isInWeaponRangeOfFocusTarget, bool isVisibleDueToAllyIntel, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
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

            TextMeshProUGUI callsignTMP = null;
            Transform callsignTextTransform = marker.transform.Find("CallSignText");
            if (callsignTextTransform != null)
            {
                callsignTMP = callsignTextTransform.GetComponent<TextMeshProUGUI>();
                if (callsignTMP != null)
                {
                    callsignTMP.text = unitSnapshot.Name;
                    _markerTexts[unitSnapshot.Name] = callsignTMP;
                }
            }
            
            bool isInitiallyAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Color initialTeamColor = GetTeamColor(unitSnapshot.TeamId);

            image.color = new Color(initialTeamColor.r, initialTeamColor.g, initialTeamColor.b, 1f);
            image.material = null;
            marker.transform.localScale = Vector3.zero; 

            Sequence appearSequence = DOTween.Sequence();
            appearSequence.Append(marker.transform.DOScale(isActive ? Vector3.one * 1.5f : Vector3.one, markerAppearDuration).SetEase(markerAppearEase));

            appearSequence.SetId(marker.GetInstanceID() + "_appear"); 
            appearSequence.OnComplete(() => {
                 UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated);
            });
            
            _unitMarkers[unitSnapshot.Name] = marker;
            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = isInitiallyAlwaysVisible;
        }

        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit, bool isRadarFocus, bool isInWeaponRangeOfFocusTarget, bool isVisibleDueToAllyIntel, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
        {
            if (marker == null) return;

            TextMeshProUGUI callsignTMP = _markerTexts.TryGetValue(unitSnapshot.Name, out var tmp) ? tmp : null;
            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color baseTeamColor = GetTeamColor(unitSnapshot.TeamId);
            Color displayColor = baseTeamColor;

            float bodyDamageRatio = 0f;
            string bodySlotId = "Body"; 
            if (unitSnapshot.PartSnapshots != null && unitSnapshot.PartSnapshots.TryGetValue(bodySlotId, out PartSnapshot bodySnapshot))
            {
                if (bodySnapshot.MaxDurability > 0) bodyDamageRatio = 1f - (bodySnapshot.CurrentDurability / bodySnapshot.MaxDurability);
                else if (!bodySnapshot.IsOperational) bodyDamageRatio = 1f;
            }
            else if (!unitSnapshot.IsOperational) bodyDamageRatio = 1f;

            bool isNearDeath = bodyDamageRatio >= 0.8f;
            if (callsignTMP != null)
            {
                Color defaultTextColor = Color.white;
                callsignTMP.color = new Color(
                    isNearDeath ? Color.red.r : defaultTextColor.r,
                    isNearDeath ? Color.red.g : defaultTextColor.g,
                    isNearDeath ? Color.red.b : defaultTextColor.b,
                    1f
                );
            }

            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Material targetMaterial = null;
            
            bool stateChangedThisFrame = false;
            if (_lastMarkerWasAlwaysVisible.TryGetValue(unitSnapshot.Name, out bool wasAlwaysVisible))
            {
                if (wasAlwaysVisible != shouldBeAlwaysVisible) stateChangedThisFrame = true;
            }
            else stateChangedThisFrame = true;

            if (stateChangedThisFrame)
            {
                image.material = targetMaterial;
                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1f);

                if (shouldBeAlwaysVisible && !wasAlwaysVisible) PlayDetectedPulse(marker).Forget();
            }
            else
            {
                if (image.material != targetMaterial) image.material = targetMaterial;
                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, 1f);
            }
             _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = shouldBeAlwaysVisible;


            Vector3 targetScaleVec = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            if (marker.transform.localScale != targetScaleVec)
            {
                marker.transform.localScale = targetScaleVec;
            }
            
            if (callsignTMP != null && callsignTMP.text != unitSnapshot.Name)
            {
                callsignTMP.text = unitSnapshot.Name;
            }
        }

        private Color GetTeamColor(int teamId)
        {
            var combatTestRunnerService = ServiceLocator.Instance.GetService<CombatTestRunner>();
            if (combatTestRunnerService != null && combatTestRunnerService.TryGetTeamColor(teamId, out Color color))
            {
                return color;
            }

            switch(teamId)
            {
                case 0: return Color.cyan;
                case 1: return Color.red;
                case 2: return Color.yellow;
                default: return Color.white;
            }
        }

        #endregion

        private Vector2 CalculateRadarPosition(Vector3 targetWorldPos)
        {
            Vector3 worldOffset = targetWorldPos - _battleCenterWorldPosition;
            Vector2 relativePos2D = new Vector2(worldOffset.x, worldOffset.z);

            float worldDistance = relativePos2D.magnitude;
            float scaleFactor = radarRadius / maxDetectionRange;

            Vector2 radarPosition = relativePos2D.normalized * Mathf.Min(worldDistance, maxDetectionRange) * scaleFactor;

            if (radarPosition.magnitude > radarRadius)
            {
                radarPosition = radarPosition.normalized * radarRadius;
            }

            return radarPosition;
        }

        private void DrawTargetingLine(Vector2 attackerUIPos, Vector2 targetUIPos)
        {
            _targetLineSequence?.Kill();
            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }

            if (uiLinePrefab == null) return;

            _activeUILine = Instantiate(uiLinePrefab, radarContainer);
            if (_activeUILine == null) return;

            float uiDistance = Vector2.Distance(attackerUIPos, targetUIPos);
            float effectiveMaxDistance = radarRadius * 2f;
            if (effectiveMaxDistance <= 0) effectiveMaxDistance = 0.01f;

            float distanceRatio = Mathf.Clamp01(uiDistance / effectiveMaxDistance);
            float dynamicDrawDuration = Mathf.Lerp(minLineDrawDuration, maxLineDrawDuration, distanceRatio);

            _activeUILine.points.Clear();
            _activeUILine.AddPoint(attackerUIPos);
            _activeUILine.AddPoint(attackerUIPos);

            float finalThickness = uiLinePrefab.thickness;
            _activeUILine.thickness = finalThickness / 3f;

            _activeUILine.SetGradientColors(lineInitialStartColor, lineInitialStartColor);
            _activeUILine.color = new Color(1,1,1,0);

            _targetLineSequence = DOTween.Sequence();

            _targetLineSequence.Append(_activeUILine.DOFade(1f, lineInitialFadeInDuration));
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.points[1], x => {
                                _activeUILine.points[1] = x;
                                _activeUILine.SetVerticesDirty();
                            }, targetUIPos, dynamicDrawDuration).SetEase(Ease.OutSine)
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.thickness, x => _activeUILine.SetThickness(x), finalThickness, dynamicDrawDuration).SetEase(Ease.OutCubic)
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.startColor, x => _activeUILine.startColor = x, lineFinalStartColor, dynamicDrawDuration).OnUpdate(() => _activeUILine.SetVerticesDirty())
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.endColor, x => _activeUILine.endColor = x, lineFinalEndColor, dynamicDrawDuration).OnUpdate(() => _activeUILine.SetVerticesDirty())
            );

            _targetLineSequence.AppendInterval(lineSustainDuration);

            Sequence fadeOutSequence = DOTween.Sequence();
            fadeOutSequence.Append(_activeUILine.DOFade(0f, lineFadeOutDuration).SetEase(Ease.InQuad));
            fadeOutSequence.Join(
                DOTween.To(() => _activeUILine.startColor, x => _activeUILine.startColor = x, lineFadeOutTargetColor, lineFadeOutDuration).OnUpdate(() => _activeUILine.SetVerticesDirty())
            );
            fadeOutSequence.Join(
                DOTween.To(() => _activeUILine.endColor, x => _activeUILine.endColor = x, lineFadeOutTargetColor, lineFadeOutDuration).OnUpdate(() => _activeUILine.SetVerticesDirty())
            );

            _targetLineSequence.Append(fadeOutSequence);

            _targetLineSequence.OnComplete(() =>
            {
                if (_activeUILine != null)
                {
                    Destroy(_activeUILine.gameObject);
                    _activeUILine = null;
                }
            });

            _targetLineSequence.Play();
        }

        #region Visual Effects Helpers

        private async UniTask PlayFlashEffect(GameObject marker, Color flashColor)
        {
            if (marker == null) return;
            var image = marker.GetComponent<Image>();
            if (image == null) return;

            Color originalColor = image.color;
            Color flashRgbColor = new Color(flashColor.r, flashColor.g, flashColor.b, originalColor.a);
            Color originalRgbColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a);

            marker.transform.DOShakePosition(flashDuration, damageShakeStrength, damageShakeVibrato, damageShakeRandomness, false, true).SetId(marker.transform);

            await image.DOColor(flashRgbColor, flashDuration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await image.DOColor(originalRgbColor, flashDuration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        private async UniTask PlayPulseEffect(GameObject marker, float targetScale = 0f, float duration = 0f)
        {
            if (marker == null) return;

            if (targetScale <= 0f) targetScale = pulseScale;
            if (duration <= 0f) duration = flashDuration;

            Vector3 currentMarkerScale = marker.transform.localScale;
            await marker.transform.DOScale(currentMarkerScale * targetScale, duration / 2).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await marker.transform.DOScale(currentMarkerScale, duration / 2).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }

        private async UniTask PlayDetectedPulse(GameObject marker)
        {
            if (marker == null) return;

            Vector3 originalScale = marker.transform.localScale;
            Vector3 pulseTargetScale = originalScale * detectedPulseScaleFactor;

            DOTween.Kill(marker.transform, false);

            Sequence pulseSequence = DOTween.Sequence();
            pulseSequence.Append(marker.transform.DOScale(pulseTargetScale, detectedPulseDuration / 2).SetEase(detectedPulseEaseOut))
                         .Append(marker.transform.DOScale(originalScale, detectedPulseDuration / 2).SetEase(detectedPulseEaseIn));
            pulseSequence.SetId(marker.transform);

            await pulseSequence.AsyncWaitForCompletion();
        }

        private async UniTask PlayEvasionEffect(GameObject marker)
        {
            if (marker == null) return;

            Image image = marker.GetComponent<Image>();
            if (image == null) return;

            float originalAlpha = image.color.a;
            Vector3 originalScale = marker.transform.localScale;

            DOTween.Kill(image, false); 
            DOTween.Kill(marker.transform, false);

            Sequence evasionSequence = DOTween.Sequence();
            evasionSequence.Append(image.DOFade(evasionEffectMinAlpha, evasionEffectDuration / 2f).SetEase(Ease.OutQuad))
                         .Join(marker.transform.DOScale(originalScale * evasionEffectMinScaleFactor, evasionEffectDuration / 2f).SetEase(Ease.OutQuad))
                         .Append(image.DOFade(originalAlpha, evasionEffectDuration / 2f).SetEase(Ease.InQuad))
                         .Join(marker.transform.DOScale(originalScale, evasionEffectDuration / 2f).SetEase(Ease.InQuad));
            
            evasionSequence.SetId(marker.GetInstanceID() + "_evasion");

            await evasionSequence.Play().AsyncWaitForCompletion();
        }

        #endregion
    }
} 