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
        [SerializeField] private UILineRenderer uiLinePrefab; // 네임스페이스 명시

        [Header("Target Line Animation Settings")] // SASHA: 타겟 라인 애니메이션 속도 조절용 필드 추가
        [SerializeField] private float lineInitialFadeInDuration = 0.05f;
        [SerializeField] private float lineDrawDuration = 0.2f; // 선이 길어지고 두꺼워지며 색이 변하는 시간
        [SerializeField] private float lineSustainDuration = 0.1f;
        [SerializeField] private float lineFadeOutDuration = 0.2f;

        [Header("Target Line Dynamic Speed Settings")] // SASHA: 타겟 라인 동적 속도 조절용 필드 추가
        [SerializeField] private float minLineDrawDuration = 0.05f;
        [SerializeField] private float maxLineDrawDuration = 0.2f; // 기존 lineDrawDuration의 역할
        // radarRadius는 이미 UI 상의 레이더 반지름으로 존재함.
        // UI상의 최대 유효 거리는 radarRadius * 2 (지름) 정도로 생각할 수 있음.

        [Header("Target Line Animation Colors")] // SASHA: 타겟 라인 애니메이션 색상 설정용 필드 추가
        [SerializeField] private Color lineInitialStartColor = Color.yellow;
        [SerializeField] private Color lineFinalStartColor = Color.yellow;
        [SerializeField] private Color lineFinalEndColor = Color.red;
        [SerializeField] private Color lineFadeOutTargetColor = new Color(0.5f, 0, 0, 0); // 알파 포함된 최종 목표색

        [Header("Evasion Effect Settings")] // SASHA: 회피 효과 설정용 필드 추가
        [SerializeField] private float evasionEffectDuration = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float evasionEffectMinAlpha = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float evasionEffectMinScaleFactor = 0.7f;

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

        // SASHA: Target line management
        private AF.UI.UILineRenderer _activeUILine; // 네임스페이스 명시
        private Sequence _targetLineSequence; // 타겟 라인 애니메이션 시퀀스

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
        [Tooltip("빔에서 벗어난 마커가 완전히 사라지기까지 걸리는 시간 (초)")] // SASHA: 추가
        [SerializeField] private float beamExitFadeOutDuration = 0.5f; // SASHA: 추가
        [Tooltip("빔에서 벗어난 마커가 사라질 때 적용될 DOTween Ease 타입")] // SASHA: 추가
        [SerializeField] private Ease beamExitFadeOutEase = Ease.OutQuad; // SASHA: 추가
        private string _lastRadarFocusTargetUnitName = null; // SASHA: 마지막 레이더 포커스 유닛 이름 저장

        // SASHA: 빔 상태 및 페이드 아웃 트윈 관리를 위한 변수 추가
        private Dictionary<string, bool> _markerInBeamState = new Dictionary<string, bool>();
        private Dictionary<string, Sequence> _markerFadeSequences = new Dictionary<string, Sequence>();
        // +++ SASHA: End Radar Scan Effect Settings +++

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            // _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>(); // No longer directly needed

            // CombatSimulator is removed, adjust null check if necessary
            if (_eventBus == null || radarContainer == null || unitMarkerPrefab == null || uiLinePrefab == null)
            {
                Debug.LogError("CombatRadarUIService 초기화 실패: 필수 참조(EventBus, UI 요소, uiLinePrefab)가 없습니다.");
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

            // SASHA: 타겟 라인 정리
            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill();
            _targetLineSequence = null;

            // SASHA: 추가된 딕셔너리 정리 (ClearRadar에서 이미 처리)
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

            // SASHA: 전투 시작 시 기존 타겟 라인 제거
            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill();
            _targetLineSequence = null;

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

            // SASHA: 공격/타겟팅 라인 처리
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
            // SASHA: 공격/타겟팅 라인 처리 끝

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
                                destructionSequence.Append(markerImage.DOColor(Color.black, 0.1f)) // 즉시 검은색으로
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
                                destructionSequence.Play();
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
                    targetUnitName = logEntry.Repair_TargetName; // 수정 전: targetName 사용 오류
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker)) // targetUnitName으로 변경
                    {
                        await PlayFlashEffect(targetMarker, Color.green);
                    }
                    break;

                // Add cases for other EventTypes if visual feedback is desired
                case LogEventType.DamageAvoided: // SASHA: 회피 이벤트 처리 추가
                    // 실제 TextLogger.LogEntry에 정의된, 회피한 유닛의 이름이 담긴 필드명을 사용해야 합니다.
                    // TextLogger.cs 확인 결과, Avoid_TargetName 필드를 사용합니다.
                    targetUnitName = logEntry.Avoid_TargetName; 
                    
                    if (!string.IsNullOrEmpty(targetUnitName) && _unitMarkers.TryGetValue(targetUnitName, out targetMarker))
                    {
                        PlayEvasionEffect(targetMarker).Forget(); // async UniTask 호출이므로 Forget() 처리
                    }
                    break;

                // case LogEventType.WeaponFired: ...

                default:
                    // No specific visual effect for this log type
                    break;
            }

            // SASHA: 깜빡임 시퀀스도 모두 정리
            foreach (var seq in _blinkingSequences.Values)
            {
                seq?.Kill();
            }
            _blinkingSequences.Clear();
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

        private void ClearRadar()
        {
            foreach (var markerKvp in _unitMarkers) // SASHA: 순회 방식 변경 및 키 가져오기
            {
                GameObject marker = markerKvp.Value;
                string unitName = markerKvp.Key;

                if (marker != null)
                {
                    // SASHA: 마커 및 자식 텍스트의 DOTween 애니메이션 명시적 중지
                    DOTween.Kill(marker.transform, true); // true: complete a.s.a.p.
                    Image image = marker.GetComponent<Image>();
                    if (image != null) DOTween.Kill(image, true);
                    
                    if (_markerTexts.TryGetValue(unitName, out TextMeshProUGUI callsignTMP) && callsignTMP != null)
                    {                        
                        DOTween.Kill(callsignTMP, true);
                    }
                    // --- SASHA: 끝 ---

                    // SASHA: 페이드 시퀀스 중지 및 제거
                    if (_markerFadeSequences.TryGetValue(unitName, out Sequence fadeSeq))
                    {
                        fadeSeq?.Kill();
                    }

                    Destroy(marker);
                }
            }
            _unitMarkers.Clear();
            _lastMarkerWasAlwaysVisible.Clear(); 
            _markerTexts.Clear(); 
            _markerInBeamState.Clear(); // SASHA: 추가
            _markerFadeSequences.Clear(); // SASHA: 추가

            // _blinkingSequences 관련 코드 완전 제거

            // _activeUILine 정리 (Shutdown 및 HandleCombatStart에서 이미 처리)
            if (_activeUILine != null)
            {
                Destroy(_activeUILine.gameObject);
                _activeUILine = null;
            }
            _targetLineSequence?.Kill(); 
            _targetLineSequence = null;
        }

        // Renamed and modified to accept snapshot and active unit name for highlight
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
                 // SASHA: 아군 정보 공유로 인한 가시성 플래그 추가
                 bool isVisibleDueToAllyIntel = false;

                 // 기존: 포커스 타겟의 무기 사거리 내 다른 유닛 (적군/아군 무관)
                 if (focusTargetSnapshotNullable.HasValue && !isRadarFocus) 
                 {
                     float distanceToFocus = Vector3.Distance(focusTargetSnapshotNullable.Value.Position, unitSnapshot.Position);
                     if (distanceToFocus <= focusTargetSnapshotNullable.Value.PrimaryWeaponRange)
                     {
                         isInWeaponRangeOfFocusTarget = true;
                     }
                 }

                 // SASHA: 새로운 아군 정보 공유 로직
                 // 현재 유닛(unitSnapshot)이 플레이어 분대가 아니고, 어떤 플레이어 분대 유닛의 사거리 안에 있다면 항상 보이게 함.
                 if (!playerSquadNames.Contains(unitSnapshot.Name)) // 현재 유닛이 플레이어 분대 유닛이 아닐 때
                 {
                     foreach (var playerUnitName in playerSquadNames)
                     {
                         if (snapshotDict.TryGetValue(playerUnitName, out var playerUnitSnapshot) && playerUnitSnapshot.IsOperational)
                         {
                             float distanceToPlayerUnit = Vector3.Distance(playerUnitSnapshot.Position, unitSnapshot.Position);
                             if (distanceToPlayerUnit <= playerUnitSnapshot.PrimaryWeaponRange)
                             {
                                 isVisibleDueToAllyIntel = true;
                                 break; // 한 명의 아군이라도 탐지했으면 더 볼 필요 없음
                             }
                         }
                     }
                 }
                 // --- SASHA: 로직 끝 ---

                if (_unitMarkers.TryGetValue(unitSnapshot.Name, out GameObject marker))
                {
                    if(marker != null)
                    {
                        marker.GetComponent<RectTransform>().anchoredPosition = radarPosition;
                        // SASHA: isVisibleDueToAllyIntel 파라미터 추가
                        UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                    }
                    else
                    {
                        // SASHA: isVisibleDueToAllyIntel 파라미터 추가
                        CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                    }
                }
                else
                {
                    // SASHA: isVisibleDueToAllyIntel 파라미터 추가
                    CreateNewMarkerFromSnapshot(unitSnapshot, radarPosition, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated); 
                }
            }
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel 파라미터 추가
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
                    // SASHA: 초기 알파는 LateUpdate 로직에 맡기므로 여기서 0으로 설정하지 않음.
                    // LateUpdate에서 빔 상태에 따라 적절히 처리될 것임.
                    // callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, 0f); 
                    _markerTexts[unitSnapshot.Name] = callsignTMP;
                }
            }
            
            // SASHA: isVisibleDueToAllyIntel 조건 추가
            bool isInitiallyAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Color initialTeamColor = GetTeamColor(unitSnapshot.TeamId);

            // SASHA: 초기 알파는 LateUpdate 로직에 맡김. 여기서는 색상과 머티리얼만 설정.
            image.color = new Color(initialTeamColor.r, initialTeamColor.g, initialTeamColor.b, isInitiallyAlwaysVisible ? 1f : 0f); 
            image.material = isInitiallyAlwaysVisible ? null : markersMaterial;
            marker.transform.localScale = Vector3.zero; 

            Sequence appearSequence = DOTween.Sequence();
            // SASHA: 초기 알파가 LateUpdate에서 결정되므로, appearSequence의 Fade 효과는 조건부로 적용하거나,
            // LateUpdate가 첫 프레임에 올바른 값을 설정하도록 대기.
            // 여기서는 일단 스케일 애니메이션만 적용하고, 알파는 LateUpdate에 맡기는 방향으로 단순화.
            appearSequence.Append(marker.transform.DOScale(isActive ? Vector3.one * 1.5f : Vector3.one, markerAppearDuration).SetEase(markerAppearEase));
            
            // if (!isInitiallyAlwaysVisible) // 항상 보이지 않는 마커만 초기 페이드 인 (LateUpdate가 처리하도록 둘 수도 있음)
            // {
            //     appearSequence.Join(image.DOFade(1f, markerAppearDuration)); 
            //     if (callsignTMP != null) appearSequence.Join(callsignTMP.DOFade(1f, markerAppearDuration));
            // }


            appearSequence.SetId(marker.GetInstanceID() + "_appear"); 
            appearSequence.OnComplete(() => {
                 UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel, playerSquadNames, playerSquadAnnihilated);
                 // SASHA: 새로 생성된 마커의 초기 빔 상태 설정. shaderAlphaMultiplier는 이 컨텍스트에 없음.
                 // isInitiallyAlwaysVisible 마커는 LateUpdate의 빔 로직을 타지 않으므로 상태 설정 불필요.
                 if (!isInitiallyAlwaysVisible)
                 {
                    _markerInBeamState.TryAdd(unitSnapshot.Name, false); // 아직 빔 안에 들어온 적 없다고 가정, LateUpdate에서 판단.
                 }
            });
            
            _unitMarkers[unitSnapshot.Name] = marker;
            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = isInitiallyAlwaysVisible;
            // SASHA: 새로 생성된 마커의 빔 상태 초기화 (isInitiallyAlwaysVisible가 아닌 경우)
            // LateUpdate에서 첫 프레임에 판단하도록 false로 설정
            if (!isInitiallyAlwaysVisible) 
            {
                 _markerInBeamState[unitSnapshot.Name] = false; 
            }
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel 파라미터 추가
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
                    callsignTMP.color.a // 현재 알파는 유지 (LateUpdate에서 제어)
                );
            }

            // SASHA: isVisibleDueToAllyIntel 조건 추가
            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Material targetMaterial = shouldBeAlwaysVisible ? null : markersMaterial;
            
            bool stateChangedThisFrame = false; // 머티리얼 또는 항상 보이는 상태 변경 여부
            if (_lastMarkerWasAlwaysVisible.TryGetValue(unitSnapshot.Name, out bool wasAlwaysVisible))
            {
                if (wasAlwaysVisible != shouldBeAlwaysVisible) stateChangedThisFrame = true;
            }
            else stateChangedThisFrame = true;

            // SASHA: 알파 설정은 LateUpdate로 이전. 여기서는 색상과 머티리얼, 스케일만 관리.
            if (stateChangedThisFrame)
            {
                // 머티리얼 변경 시, 짧은 페이드로 전환 (알파는 LateUpdate에서 관리하므로 여기서는 색상만)
                // DOTween.Kill(image, false); // Stop any existing color/fade tweens
                // image.material = targetMaterial;
                // image.DOColor(new Color(displayColor.r, displayColor.g, displayColor.b, image.color.a), materialTransitionFadeDuration).SetId(image);

                // 대신 즉시 머티리얼 변경 및 색상 적용
                image.material = targetMaterial;
                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, image.color.a);


                if (shouldBeAlwaysVisible && !wasAlwaysVisible) PlayDetectedPulse(marker).Forget();
            }
            else
            {
                if (image.material != targetMaterial) image.material = targetMaterial;
                image.color = new Color(displayColor.r, displayColor.g, displayColor.b, image.color.a);
            }
             _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = shouldBeAlwaysVisible;


            Vector3 targetScaleVec = isActiveUnit ? Vector3.one * 1.5f : Vector3.one;
            if (marker.transform.localScale != targetScaleVec)
            {
                // DOTween.Kill(marker.transform, true); // 이전 스케일 트윈 중지
                // marker.transform.DOScale(targetScaleVec, markerActiveScaleDuration).SetEase(Ease.OutQuad).SetId(marker.transform);
                // 스케일 변경은 즉시 또는 짧은 트윈으로. 여기서는 일단 즉시 변경.
                marker.transform.localScale = targetScaleVec;
            }
            
            if (callsignTMP != null && callsignTMP.text != unitSnapshot.Name)
            {
                callsignTMP.text = unitSnapshot.Name;
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

                if (!_markerTexts.TryGetValue(entry.Key, out TextMeshProUGUI callsignTMP) || callsignTMP == null)
                {
                    // TextMeshProUGUI가 없는 마커는 일단 건너뛰거나, 이미지에 대해서만 처리할 수 있음.
                    // 여기서는 텍스트가 없으면 일단 다음 마커로 넘어감.
                    // continue; 
                }

                // SASHA: shouldBeAlwaysVisible 마커는 이 로직에서 제외
                bool isAlwaysVisible = _lastMarkerWasAlwaysVisible.TryGetValue(entry.Key, out bool lav) && lav;
                if (isAlwaysVisible)
                {
                    // 항상 보이는 마커는 알파를 1로 유지 (또는 다른 로직에 의해 제어됨)
                    // Fade In/Out 로직을 적용하지 않음
                    if (markerImage.color.a != 1f) markerImage.DOFade(1f, 0.1f); // 혹시나 투명하면 다시 보이게
                    if (callsignTMP != null && callsignTMP.color.a != 1f) callsignTMP.DOFade(1f, 0.1f);
                    continue;
                }

                // 스캔 효과를 받는 마커인가? (셰이더의 알파 계산 로직은 유지)
                float shaderAlphaMultiplier = 0f;
                if (markerImage.material == markersMaterial)
                {
                    Vector2 markerUIPosition = markerObject.GetComponent<RectTransform>().anchoredPosition;
                    float markerAngleRad = Mathf.Atan2(markerUIPosition.y, markerUIPosition.x);
                    float currentScanAngleDegForDelta = radarScanTransform.localEulerAngles.z;
                    float markerAngleDeg = markerAngleRad * Mathf.Rad2Deg;
                    float angleDifferenceDeg = Mathf.DeltaAngle(markerAngleDeg, currentScanAngleDegForDelta);
                    float angleDifferenceRad = Mathf.Abs(angleDifferenceDeg * Mathf.Deg2Rad);
                    float halfArcWidthRad = scanArcWidthDegrees * Mathf.Deg2Rad * 0.5f; // _ScanArcWidthRad 를 사용하도록 수정

                    if (angleDifferenceRad < halfArcWidthRad)
                    {
                        float fadeEffectStartAngle = halfArcWidthRad - (scanFadeRangeDegrees * Mathf.Deg2Rad); // _FadeRangeRad 를 사용하도록 수정
                        if (fadeEffectStartAngle < 0) fadeEffectStartAngle = 0;

                        if (angleDifferenceRad < fadeEffectStartAngle)
                        {
                            shaderAlphaMultiplier = 1f;
                        }
                        else
                        {
                            if ((scanFadeRangeDegrees * Mathf.Deg2Rad) > 0.0001f) // _FadeRangeRad 사용 및 0으로 나누기 방지
                            {
                                shaderAlphaMultiplier = 1f - ((angleDifferenceRad - fadeEffectStartAngle) / (scanFadeRangeDegrees * Mathf.Deg2Rad)); // _FadeRangeRad 사용
                            }
                            else
                            {
                                shaderAlphaMultiplier = 1f;
                            }
                            shaderAlphaMultiplier = Mathf.Clamp01(shaderAlphaMultiplier);
                        }
                    }
                } else { // 스캔 머티리얼이 아니면 항상 빔 안에 있는 것으로 간주 (예: 플레이어 유닛)
                    // 이 경우는 isAlwaysVisible 에서 이미 처리되었어야 함.
                    // 하지만 방어적으로, 스캔 머티리얼이 아닌데 isAlwaysVisible이 false인 마커는 없다고 가정.
                    // 만약 그런 경우가 있다면, 기본 알파를 1로 처리하거나 다른 로직 필요.
                }


                bool currentlyInBeam = shaderAlphaMultiplier > 0.01f; // 빔 안에 있는지 여부 (셰이더 계산 결과 기반)
                _markerInBeamState.TryGetValue(entry.Key, out bool wasInBeam);

                if (currentlyInBeam && !wasInBeam)
                {
                    // 빔에 새로 진입
                    _markerFadeSequences.TryGetValue(entry.Key, out Sequence existingFadeSeq);
                    existingFadeSeq?.Kill(); // 진행 중인 페이드 아웃 중지
                    _markerFadeSequences.Remove(entry.Key);

                    // 즉시 또는 짧은 페이드 인으로 보이게 함
                    markerImage.DOFade(1f, 0.1f); // 짧은 페이드 인
                    if (callsignTMP != null) callsignTMP.DOFade(1f, 0.1f);
                }
                else if (!currentlyInBeam && wasInBeam)
                {
                    // 빔에서 막 벗어남
                    _markerFadeSequences.TryGetValue(entry.Key, out Sequence existingFadeSeq);
                    existingFadeSeq?.Kill(); // 만약을 위해 기존 시퀀스 중지

                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration).SetEase(beamExitFadeOutEase));
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration).SetEase(beamExitFadeOutEase));
                    }
                    fadeOutSequence.OnComplete(() => _markerFadeSequences.Remove(entry.Key));
                    _markerFadeSequences[entry.Key] = fadeOutSequence;
                    fadeOutSequence.Play();
                }
                // else: 계속 빔 안에 있거나, 계속 빔 밖에 있는 경우 (페이드 아웃 진행 중이거나 이미 완료)
                // 이 경우 DOTween이 알파를 관리하므로 직접 설정하지 않음.
                // 단, 계속 빔 안에 있을 때 셰이더 알파가 1이 아니라면 (페이드 구간) 그 값을 존중해야 할 수 있음.
                // 현재 로직은 빔에 들어오면 즉시 알파 1로 만듦. 셰이더의 페이드 구간 효과를 스크립트에서 덮어쓰고 있음.
                // 만약 셰이더의 페이드 효과를 그대로 쓰고 싶다면, currentlyInBeam && !wasInBeam 부분 수정 필요.
                // 여기서는 "빔 안에 들어오면 확실히 보인다"는 기조로 감.

                _markerInBeamState[entry.Key] = currentlyInBeam;
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

        // SASHA: 회피 효과 재생 메서드 추가
        private async UniTask PlayEvasionEffect(GameObject marker)
        {
            if (marker == null) return;

            Image image = marker.GetComponent<Image>();
            if (image == null) return;

            // DOTween.Kill을 호출하기 전에 원래 값들을 저장합니다.
            // Graphic.color는 전체 알파에 영향을 주므로, 개별 마커 이미지의 현재 알파를 기준으로 해야 할 수 있습니다.
            // 다만, 대부분의 경우 회피 직전 마커는 불투명(알파1) 상태일 가능성이 높습니다.
            // 여기서는 간단하게 Graphic.color.a를 사용하지만, 더 복잡한 알파 관리가 있다면 수정이 필요할 수 있습니다.
            float originalAlpha = image.color.a; // 현재 이미지 알파 (또는 _activeUILine.color.a 와 같은 Graphic의 알파)
            Vector3 originalScale = marker.transform.localScale;

            // 기존 트윈 중지 (เฉพาะ image 와 transform 관련)
            DOTween.Kill(image, false); 
            DOTween.Kill(marker.transform, false);

            Sequence evasionSequence = DOTween.Sequence();
            evasionSequence.Append(image.DOFade(evasionEffectMinAlpha, evasionEffectDuration / 2f).SetEase(Ease.OutQuad))
                         .Join(marker.transform.DOScale(originalScale * evasionEffectMinScaleFactor, evasionEffectDuration / 2f).SetEase(Ease.OutQuad))
                         .Append(image.DOFade(originalAlpha, evasionEffectDuration / 2f).SetEase(Ease.InQuad))
                         .Join(marker.transform.DOScale(originalScale, evasionEffectDuration / 2f).SetEase(Ease.InQuad));
            
            evasionSequence.SetId(marker.GetInstanceID() + "_evasion"); // 고유 ID로 트윈 관리 용이

            await evasionSequence.Play().AsyncWaitForCompletion();
        }

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

        // +++ SASHA: 타겟팅 라인 그리기 및 애니메이션 메서드 +++
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

            // --- SASHA: 동적 lineDrawDuration 계산 --- 
            float uiDistance = Vector2.Distance(attackerUIPos, targetUIPos);
            // radarRadius * 2 를 최대 유효 UI 거리로 사용 (레이더 지름)
            float effectiveMaxDistance = radarRadius * 2f;
            if (effectiveMaxDistance <= 0) effectiveMaxDistance = 0.01f; // 0으로 나누기 방지 및 매우 작은 기본값
            
            float distanceRatio = Mathf.Clamp01(uiDistance / effectiveMaxDistance);
            float dynamicDrawDuration = Mathf.Lerp(minLineDrawDuration, maxLineDrawDuration, distanceRatio);
            // --- SASHA: 계산 끝 ---

            _activeUILine.points.Clear();
            _activeUILine.AddPoint(attackerUIPos); 
            _activeUILine.AddPoint(attackerUIPos); 
            
            float finalThickness = uiLinePrefab.thickness; 
            _activeUILine.thickness = finalThickness / 3f; 

            _activeUILine.SetGradientColors(lineInitialStartColor, lineInitialStartColor); // 인스펙터 값 사용
            _activeUILine.color = new Color(1,1,1,0); 

            _targetLineSequence = DOTween.Sequence();

            _targetLineSequence.Append(_activeUILine.DOFade(1f, lineInitialFadeInDuration)); 
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.points[1], x => {
                                _activeUILine.points[1] = x;
                                _activeUILine.SetVerticesDirty(); 
                            }, targetUIPos, dynamicDrawDuration).SetEase(Ease.OutSine) // lineDrawDuration 대신 dynamicDrawDuration 사용
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.thickness, x => _activeUILine.SetThickness(x), finalThickness, dynamicDrawDuration).SetEase(Ease.OutCubic) // dynamicDrawDuration 사용
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.startColor, x => _activeUILine.startColor = x, lineFinalStartColor, dynamicDrawDuration).OnUpdate(() => _activeUILine.SetVerticesDirty()) // dynamicDrawDuration 사용
            );
            _targetLineSequence.Join(
                DOTween.To(() => _activeUILine.endColor, x => _activeUILine.endColor = x, lineFinalEndColor, dynamicDrawDuration).OnUpdate(() => _activeUILine.SetVerticesDirty()) // dynamicDrawDuration 사용
            );

            _targetLineSequence.AppendInterval(lineSustainDuration);

            Sequence fadeOutSequence = DOTween.Sequence();
            fadeOutSequence.Append(_activeUILine.DOFade(0f, lineFadeOutDuration).SetEase(Ease.InQuad));
            fadeOutSequence.Join(
                DOTween.To(() => _activeUILine.startColor, x => _activeUILine.startColor = x, lineFadeOutTargetColor, lineFadeOutDuration).OnUpdate(() => _activeUILine.SetVerticesDirty()) // 인스펙터 값 사용
            );
            fadeOutSequence.Join(
                DOTween.To(() => _activeUILine.endColor, x => _activeUILine.endColor = x, lineFadeOutTargetColor, lineFadeOutDuration).OnUpdate(() => _activeUILine.SetVerticesDirty()) // 인스펙터 값 사용, _activeUILine 오타 수정
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
        // +++ SASHA: 타겟팅 라인 그리기 및 애니메이션 메서드 끝 +++
    }
} 