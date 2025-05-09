using AF.Combat;
using AF.EventBus;
using AF.Models;
using AF.Services;
using UnityEngine;
using System.Collections.Generic; // 필요시 주석 해제
// using Cysharp.Threading.Tasks; // 필요시 주석 해제

namespace AF.Sound
{
    /// <summary>
    /// 전투 중 발생하는 이벤트에 따라 사운드를 재생하는 서비스입니다.
    /// CombatLogPlaybackUpdateEvent를 구독하여 로그 재생에 맞춰 사운드를 처리합니다.
    /// </summary>
    public class CombatSoundService : MonoBehaviour, IService
    {
        // <<< SASHA: AudioClip과 VolumeScale을 함께 관리하는 내부 클래스 정의 >>>
        [System.Serializable] // Inspector에 표시되도록 Serializable 속성 추가
        public class SoundClipSetting
        {
            public AudioClip clip;
            [Range(0f, 5f)] // 볼륨 스케일 범위를 0에서 2까지로 설정 (기본 1)
            public float volumeScale = 1f;
        }
        // <<< SASHA: 정의 끝 >>>

        [Header("Sound Settings")]
        [SerializeField] private bool use3DSound = true; // 3D 사운드 사용 여부 토글

        [Header("General Combat Sounds")]
        [SerializeField] private SoundClipSetting combatStartSound;
        [SerializeField] private SoundClipSetting roundStartSound;
        [SerializeField] private SoundClipSetting unitActivationSound;
        [SerializeField] private SoundClipSetting combatEndVictorySound;
        [SerializeField] private SoundClipSetting combatEndDefeatSound;
        [SerializeField] private SoundClipSetting combatEndDrawSound;

        [Header("Weapon & Damage Sounds")]
        [SerializeField] private SoundClipSetting defaultWeaponFiredSound;
        [SerializeField] private SoundClipSetting defaultDamageAppliedSound;
        [SerializeField] private SoundClipSetting heavyDamageAppliedSound; // 큰 피해 시
        [SerializeField] private SoundClipSetting defaultPartDestroyedSound;
        [SerializeField] private SoundClipSetting defaultFrameDestroyedSound; // 프레임 전체 파괴 시 사운드
        [SerializeField] private SoundClipSetting defaultEvasionSound;

        [Header("Action Specific Sounds")]
        [SerializeField] private SoundClipSetting moveCompletedSound;
        [SerializeField] private SoundClipSetting reloadCompletedSound;
        [SerializeField] private SoundClipSetting defendActionSound;
        [SerializeField] private SoundClipSetting repairCompletedSound;
        // TODO: 무기 타입별, 파츠 타입별, 액션 타입별 상세 사운드 클립 추가 가능

        private EventBus.EventBus _eventBus;
        private SoundService _soundService;
        private bool _isInitialized = false;

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            if (_eventBus == null)
            {
                Debug.LogError("[CombatSoundService] 초기화 실패: EventBus 참조를 가져올 수 없습니다.");
                enabled = false;
                return;
            }

            _soundService = ServiceLocator.Instance.GetService<SoundService>();
            if (_soundService == null)
            {
                Debug.LogError("[CombatSoundService] 초기화 실패: SoundService 참조를 가져올 수 없습니다.");
                enabled = false;
                return;
            }

            SubscribeToEvents();
            _isInitialized = true;
            Debug.Log("[CombatSoundService] Initialized.");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;

            UnsubscribeFromEvents();
            _eventBus = null;
            _soundService = null;
            _isInitialized = false;
            Debug.Log("[CombatSoundService] Shutdown.");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate);
            // CombatStartEvent, CombatEndEvent는 LogEntry를 통해 처리하므로 직접 구독 불필요
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatLogPlaybackUpdateEvent>(HandleLogPlaybackUpdate);
        }

        private void HandleLogPlaybackUpdate(CombatLogPlaybackUpdateEvent ev)
        {
            if (!_isInitialized || ev.CurrentLogEntry == null || _soundService == null) return;

            TextLogger.LogEntry logEntry = ev.CurrentLogEntry;
            SoundClipSetting soundSettingToPlay = null; // <<< SASHA: 타입을 SoundClipSetting으로 변경
            Vector3? soundPosition = null;

            if (use3DSound)
            {
                soundPosition = GetSoundPosition(logEntry, ev.CurrentSnapshot);
            }

            switch (logEntry.EventType)
            {
                case LogEventType.CombatStart:
                    soundSettingToPlay = combatStartSound;
                    break;
                case LogEventType.RoundStart:
                    soundSettingToPlay = roundStartSound;
                    break;
                case LogEventType.UnitActivationStart:
                    soundSettingToPlay = unitActivationSound;
                    break;
                case LogEventType.WeaponFired:
                    // TODO: logEntry.Weapon_WeaponName 에 따라 다른 사운드 선택 로직 추가 가능
                    soundSettingToPlay = defaultWeaponFiredSound;
                    break;
                case LogEventType.DamageApplied:
                    // TODO: logEntry.Damage_AmountDealt (또는 IsCritical)에 따라 heavyDamageAppliedSound와 구분
                    soundSettingToPlay = defaultDamageAppliedSound;
                    break;
                case LogEventType.DamageAvoided:
                    soundSettingToPlay = defaultEvasionSound;
                    break;
                case LogEventType.PartDestroyed:
                    soundSettingToPlay = logEntry.PartDestroyed_FrameWasActuallyDestroyed ? defaultFrameDestroyedSound : defaultPartDestroyedSound;
                    break;
                case LogEventType.ActionCompleted:
                    if (logEntry.Action_IsSuccess) // 성공한 액션에 대해서만 사운드 재생
                    {
                        switch (logEntry.Action_Type)
                        {
                            case CombatActionEvents.ActionType.Move:
                                soundSettingToPlay = moveCompletedSound;
                                break;
                            case CombatActionEvents.ActionType.Reload:
                                soundSettingToPlay = reloadCompletedSound;
                                break;
                            case CombatActionEvents.ActionType.Defend:
                                soundSettingToPlay = defendActionSound;
                                break;
                            case CombatActionEvents.ActionType.RepairAlly:
                            case CombatActionEvents.ActionType.RepairSelf:
                                soundSettingToPlay = repairCompletedSound;
                                break;
                            // 다른 액션 타입에 대한 사운드 추가 가능
                        }
                    }
                    break;
                case LogEventType.CombatEnd:
                    // CombatEndEvent는 LogEntry에 직접적인 Result 정보가 없을 수 있음.
                    // CombatSimulatorService에서 발행하는 CombatEndEvent를 직접 구독하거나,
                    // TextLoggerService가 CombatEnd 로그를 생성할 때 Result 정보를 포함하도록 해야 함.
                    // 여기서는 일단 가장 기본적인 CombatEnd 사운드를 재생하거나, Result에 따른 분기는 CombatEndEvent 직접 구독으로 처리하는 것을 권장.
                    // 임시로 default 사운드 처리 (CombatTestRunner의 evt.Result 사용 가능성 확인 필요)
                    // if (ev.CurrentLogEntry.Message.Contains("Victory")) clipToPlay = combatEndVictorySound;
                    // else if (ev.CurrentLogEntry.Message.Contains("Defeat")) clipToPlay = combatEndDefeatSound;
                    // else clipToPlay = combatEndDrawSound; 
                    // --> 위 방식은 불안정. CombatEndEvent 직접 구독 또는 LogEntry 개선 필요.
                    break;

                // TODO: StatusEffectApplied, StatusEffectExpired 등 추가

                default:
                    break;
            }

            if (soundSettingToPlay != null && soundSettingToPlay.clip != null) // <<< SASHA: clip null 체크 추가
            {
                _soundService.PlaySfxOneShot(soundSettingToPlay.clip, soundSettingToPlay.volumeScale, 1f, soundPosition);
                // Debug.Log($"[CombatSoundService] Played '{soundSettingToPlay.clip.name}' for event '{logEntry.EventType}' at position {soundPosition}");
            }
            // else if (logEntry.EventType != LogEventType.Unknown && logEntry.EventType != LogEventType.SystemMessage && (soundSettingToPlay == null || soundSettingToPlay.clip == null) )
            // {
            //     Debug.LogWarning($"[CombatSoundService] SoundClipSetting or its clip is null for event type: {logEntry.EventType}");
            // }
        }

        // 3D 사운드를 위한 위치 결정 헬퍼 메서드
        private Vector3? GetSoundPosition(TextLogger.LogEntry logEntry, Dictionary<string, ArmoredFrameSnapshot> currentSnapshots)
        {
            string unitNameForPosition = null;
            // ArmoredFrameSnapshot snapshot = null; // <<< SASHA: 이 부분 원복

            switch (logEntry.EventType)
            {
                case LogEventType.CombatStart:
                case LogEventType.RoundStart:
                case LogEventType.CombatEnd:
                    return null; // 전투 시작/종료, 라운드 시작 등은 보통 2D UI 사운드

                case LogEventType.UnitActivationStart:
                    unitNameForPosition = logEntry.ContextUnit?.Name;
                    break;
                case LogEventType.WeaponFired:
                    unitNameForPosition = logEntry.Weapon_AttackerName;
                    break;
                case LogEventType.DamageApplied:
                    unitNameForPosition = logEntry.Damage_TargetUnitName; 
                    break;
                case LogEventType.DamageAvoided:
                    unitNameForPosition = logEntry.Avoid_TargetName; 
                    break;
                case LogEventType.PartDestroyed:
                    unitNameForPosition = logEntry.PartDestroyed_OwnerName;
                    break;
                case LogEventType.ActionCompleted:
                    unitNameForPosition = logEntry.Action_ActorName;
                    if (logEntry.Action_Type == CombatActionEvents.ActionType.Move && logEntry.Action_NewPosition.HasValue)
                    {
                        return logEntry.Action_NewPosition.Value;
                    }
                    break;
                // TODO: StatusEffect 등 다른 이벤트에 대한 위치 결정 로직 추가
            }

            if (!string.IsNullOrEmpty(unitNameForPosition) && 
                currentSnapshots != null && 
                currentSnapshots.TryGetValue(unitNameForPosition, out ArmoredFrameSnapshot snapshot)) // <<< SASHA: out 변수 인라인 선언으로 복원
            {
                return snapshot.Position;
            }
            return null; 
        }

        #endregion
    }
} 