# Sound 스크립트 (AF/Scripts/Sound)

> SASHA-Unity 프로젝트의 사운드 시스템 관련 C# 스크ript(.cs 파일) 문서입니다. BGM 및 SFX 재생, 볼륨 제어, SFX 풀링 기능을 제공합니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/Sound`
  - 게임 내 사운드 재생 및 관리를 위한 스크립트들이 있습니다.
  - `CombatSoundService.cs`: 전투 중 발생하는 다양한 이벤트(로그 재생 기반)에 따라 사운드를 재생하는 서비스입니다. `MonoBehaviour`와 `IService`를 상속하며, `SoundService`를 이용하여 사운드를 재생합니다. `CombatLogPlaybackUpdateEvent`를 구독하여 로그에 맞춰 사운드를 처리하며, 각 전투 이벤트에 매핑된 사운드 클립 필드들을 가집니다.
  - `SoundService.cs`: 게임의 전반적인 BGM 및 SFX 사운드 관리를 담당하는 서비스입니다. `MonoBehaviour`와 `IService`를 상속하며, `AudioMixer`와 `AudioMixerGroup`을 사용하여 볼륨 및 라우팅을 제어합니다. BGM 재생, SFX 풀링(`sfxPoolSize`, `sfxAudioSourcePrefab`), 볼륨 제어(`SetMasterVolume`, `SetBgmVolume`, `SetSfxVolume`), SFX 재생(`PlaySfxOneShot`, `PlaySfxLooping`) 등의 기능을 제공합니다. 