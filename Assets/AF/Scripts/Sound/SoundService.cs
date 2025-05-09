using AF.Services;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

namespace AF.Sound
{
    /// <summary>
    /// 게임의 전반적인 사운드(BGM, SFX)를 관리하는 서비스입니다.
    /// SFX 재생 시 AudioSource 풀링 및 AudioMixer 그룹을 사용합니다.
    /// </summary>
    public class SoundService : MonoBehaviour, IService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mainAudioMixer; // 메인 오디오 믹서
        [SerializeField] private AudioMixerGroup masterMixerGroup; // 마스터 믹서 그룹 (선택적, 보통 Mixer자체가 Master 역할)
        [SerializeField] private AudioMixerGroup bgmMixerGroup;    // BGM 전용 믹서 그룹
        [SerializeField] private AudioMixerGroup sfxMixerGroup;    // SFX 전용 믹서 그룹

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmAudioSource; 

        [Header("SFX Pool Settings")]
        [SerializeField] private GameObject sfxAudioSourcePrefab; 
        [SerializeField] private int sfxPoolSize = 10; 

        [Header("Exposed Mixer Parameter Names")]
        [Tooltip("AudioMixer에 노출된 Master 볼륨 파라미터 이름")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [Tooltip("AudioMixer에 노출된 BGM 볼륨 파라미터 이름")]
        [SerializeField] private string bgmVolumeParam = "BGMVolume";
        [Tooltip("AudioMixer에 노출된 SFX 볼륨 파라미터 이름")]
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        // 내부 볼륨 상태 저장 (0.0 ~ 1.0 범위)
        private float _currentMasterVolume = 1f;
        private float _currentBgmVolume = 0.7f;
        private float _currentSfxVolume = 1f;

        private List<AudioSource> _sfxPool;
        private Queue<AudioSource> _availableSfxSources;
        // private Dictionary<AudioSource, Coroutine> _activeLoopingSfx; // 루핑 관리 방식 변경 가능성

        private bool _isInitialized = false;

        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            if (mainAudioMixer == null)
            {
                Debug.LogError("[SoundService] Main AudioMixer가 설정되지 않았습니다. 볼륨 제어 기능이 제한됩니다.");
            }
            if (bgmAudioSource == null)
            {
                Debug.LogWarning("[SoundService] BGM AudioSource가 설정되지 않았습니다.");
            }
            else if (bgmMixerGroup != null)
            {
                bgmAudioSource.outputAudioMixerGroup = bgmMixerGroup;
            }
            else Debug.LogWarning("[SoundService] BGM Mixer Group이 설정되지 않았습니다. BGM이 Master로 출력됩니다.");
            
            if (sfxAudioSourcePrefab == null)
            {
                Debug.LogError("[SoundService] SFX AudioSource Prefab이 설정되지 않았습니다. SFX 기능 사용 불가.");
                enabled = false;
                return;
            }
            if (sfxMixerGroup == null) Debug.LogWarning("[SoundService] SFX Mixer Group이 설정되지 않았습니다. SFX가 Master로 출력됩니다.");

            InitializeSfxPool();
            // 초기 볼륨 적용 (Mixer 파라미터 사용)
            SetMasterVolume(_currentMasterVolume); 
            SetBgmVolume(_currentBgmVolume);
            SetSfxVolume(_currentSfxVolume);

            _isInitialized = true;
            Debug.Log("[SoundService] Initialized.");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            // StopAllSfx(); // 풀링된 소스 정리 방식 개선 필요
            if (bgmAudioSource != null && bgmAudioSource.isPlaying) { bgmAudioSource.Stop(); }
            _isInitialized = false;
            Debug.Log("[SoundService] Shutdown.");
        }

        #endregion

        #region Volume Control (Using AudioMixer)

        public void SetMasterVolume(float volume)
        {
            _currentMasterVolume = Mathf.Clamp01(volume);
            if (mainAudioMixer != null && !string.IsNullOrEmpty(masterVolumeParam))
            {
                mainAudioMixer.SetFloat(masterVolumeParam, ConvertVolumeToDb(_currentMasterVolume));
            }
        }

        public void SetBgmVolume(float volume) // BGM 그룹 자체의 볼륨 (0~1)
        {
            _currentBgmVolume = Mathf.Clamp01(volume);
            if (mainAudioMixer != null && !string.IsNullOrEmpty(bgmVolumeParam))
            {
                mainAudioMixer.SetFloat(bgmVolumeParam, ConvertVolumeToDb(_currentBgmVolume));
            }
        }

        public void SetSfxVolume(float volume) // SFX 그룹 자체의 볼륨 (0~1)
        {
            _currentSfxVolume = Mathf.Clamp01(volume);
            if (mainAudioMixer != null && !string.IsNullOrEmpty(sfxVolumeParam))
            {
                mainAudioMixer.SetFloat(sfxVolumeParam, ConvertVolumeToDb(_currentSfxVolume));
            }
        }

        private float ConvertVolumeToDb(float volume)
        {
            // Mathf.Log10(0) is -infinity. Clamp to avoid issues.
            return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        }
        
        // ApplyAllVolumes, ApplyBgmVolume, ApplySfxVolume는 Mixer를 사용하면서 불필요해지거나 역할 변경

        #endregion

        #region BGM Control

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0.5f)
        {
            if (bgmAudioSource == null || clip == null) return;

            bgmAudioSource.DOKill(); 
            if (bgmAudioSource.isPlaying && bgmAudioSource.clip == clip) return; 

            // Mixer를 사용하므로, AudioSource의 볼륨은 1로 두고 Mixer 그룹 볼륨으로 제어.
            // 페이드 효과는 AudioSource의 볼륨을 잠시 조절했다가 원래대로(1로) 복구하는 방식 사용 가능
            // 또는 Mixer의 Exposed Parameter를 DOTween으로 직접 트위닝 (더 고급)
            // 여기서는 AudioSource 자체 볼륨 페이드로 구현하고, 최종 볼륨은 Mixer 따름을 가정.
            float targetMixerControlledVolume = 1f; // Mixer가 최종 볼륨을 결정하므로 소스 자체는 최대로.

            if (bgmAudioSource.isPlaying)
            {
                bgmAudioSource.DOFade(0, fadeDuration).OnComplete(() =>
                {
                    bgmAudioSource.clip = clip;
                    bgmAudioSource.loop = loop;
                    bgmAudioSource.volume = 0; // 페이드 인 시작을 위해 잠시 0으로
                    bgmAudioSource.Play();
                    bgmAudioSource.DOFade(targetMixerControlledVolume, fadeDuration); // 다시 최대로 페이드 인
                });
            }
            else
            {
                bgmAudioSource.clip = clip;
                bgmAudioSource.loop = loop;
                bgmAudioSource.volume = 0; 
                bgmAudioSource.Play();
                bgmAudioSource.DOFade(targetMixerControlledVolume, fadeDuration);
            }
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            if (bgmAudioSource == null || !bgmAudioSource.isPlaying) return;
            bgmAudioSource.DOKill();
            // 여기서도 AudioSource 볼륨 페이드 아웃 후 Stop
            bgmAudioSource.DOFade(0, fadeDuration).OnComplete(() => 
            { 
                bgmAudioSource.Stop();
                bgmAudioSource.volume = 1f; // 다음에 재생될 때를 위해 볼륨 원복 (선택적)
            });
        }

        #endregion

        #region SFX Control (Pooling with Mixer Output)

        private void InitializeSfxPool()
        {
            _sfxPool = new List<AudioSource>();
            _availableSfxSources = new Queue<AudioSource>();
            // _activeLoopingSfx = new Dictionary<AudioSource, Coroutine>(); // 루핑 관리는 필요시 다시 구현

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxSourceGO = Instantiate(sfxAudioSourcePrefab, transform); 
                sfxSourceGO.name = $"SFXSource_{i}";
                AudioSource source = sfxSourceGO.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.playOnAwake = false;
                    if (sfxMixerGroup != null) // <<< SASHA: SFX Mixer 그룹 할당
                    {
                        source.outputAudioMixerGroup = sfxMixerGroup;
                    }
                    source.volume = 1f; // <<< SASHA: AudioSource 자체 볼륨은 최대로, 실제 조절은 Mixer에서
                    _sfxPool.Add(source);
                    _availableSfxSources.Enqueue(source);
                }
                else
                {
                    Debug.LogError($"[SoundService] SFX AudioSource Prefab '{sfxAudioSourcePrefab.name}'에 AudioSource 컴포넌트가 없습니다.");
                    Destroy(sfxSourceGO);
                }
            }
        }

        public AudioSource PlaySfxOneShot(AudioClip clip, float volumeScale = 1f, float pitch = 1f, Vector3? position = null)
        {
            if (clip == null || _availableSfxSources.Count == 0)
            {
                if (_availableSfxSources.Count == 0) Debug.LogWarning("[SoundService] SFX Pool이 가득 찼습니다. 효과음 재생 건너뜁니다.");
                return null;
            }

            AudioSource source = _availableSfxSources.Dequeue();
            source.clip = clip;
            // <<< SASHA: Mixer를 사용하므로, source.volume은 개별 클립의 상대적 크기(volumeScale)만 반영 >>>
            // 최종 볼륨은 Mixer의 SFX 그룹 볼륨과 Master 그룹 볼륨에 의해 결정됨.
            source.volume = Mathf.Clamp01(volumeScale); 
            source.pitch = pitch;
            source.loop = false;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1f; 
            }
            else
            {
                source.spatialBlend = 0f; 
            }

            source.Play();
            StartCoroutine(ReturnSourceToPoolWhenFinished(source));
            return source;
        }

        public AudioSource PlaySfxLooping(AudioClip clip, float volumeScale = 1f, float pitch = 1f, Vector3? position = null)
        {
            if (clip == null || _availableSfxSources.Count == 0) return null;

            AudioSource source = _availableSfxSources.Dequeue();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volumeScale); // Mixer 사용 시 상대적 크기만
            source.pitch = pitch;
            source.loop = true;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1f;
            }
            else
            {
                source.spatialBlend = 0f;
            }
            
            source.Play();
            return source; 
        }

        public void StopLoopingSfx(AudioSource sourceInstance)
        {
            if (sourceInstance != null && sourceInstance.isPlaying && sourceInstance.loop)
            {
                sourceInstance.Stop();
                if (!_availableSfxSources.Contains(sourceInstance) && _sfxPool.Contains(sourceInstance))
                {
                    _availableSfxSources.Enqueue(sourceInstance);
                }
            }
        }

        public void StopAllLoopingSfx() // 네이밍 일관성 및 모든 풀 소스 검사
        {
            foreach (var source in _sfxPool) // 전체 풀 검사
            {
                if (source.isPlaying && source.loop)
                {
                    source.Stop();
                    if (!_availableSfxSources.Contains(source)) // 중복 추가 방지
                    {
                        _availableSfxSources.Enqueue(source);
                    }
                }
            }
        }
        
        // 모든 SFX(원샷 포함) 중지 후 풀 정리 (조심해서 사용)
        public void StopAllSfxAndResetPool() 
        {
            StopAllCoroutines(); // ReturnSourceToPoolWhenFinished 코루틴 모두 중지
            _availableSfxSources.Clear(); // 큐 비우기

            foreach (var source in _sfxPool)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
                source.clip = null; // 클립 참조 해제
                _availableSfxSources.Enqueue(source); // 모든 소스를 사용 가능하도록 큐에 다시 추가
            }
        }

        private System.Collections.IEnumerator ReturnSourceToPoolWhenFinished(AudioSource source)
        {
            // 루핑 사운드가 아니고, 실제로 재생 중일 때만 대기
            if (source != null && !source.loop && source.isPlaying) 
            {
                 yield return new WaitWhile(() => source.isPlaying);
            }
            
            // 코루틴이 중단되지 않고 정상 종료된 경우 (StopAllSfxAndResetPool 등으로 중단되지 않은 경우)
            // 그리고 아직 풀에 반환되지 않은 경우에만 반환
            if (source != null && !_availableSfxSources.Contains(source) && _sfxPool.Contains(source)) 
            {
                 _availableSfxSources.Enqueue(source);
            }
        }

        #endregion
    }
} 