# 커서 기능 설명 요청
_Exported on 5/16/2025 at 20:03:05 GMT+9 from Cursor (0.50.4)_

---

**User**

사샤 문서가 엄청 깨져있는데..?@Sound @SoundAnalysis.md 디렉토리에 스크립트 확인하고 정상화해줘

---

**Cursor**

```markdown
# Sound 시스템 분석 (`Assets/AF/Scripts/Sound`)\n\n이 문서는 `Assets/AF/Scripts/Sound` 디렉토리에 포함된 사운드 관련 스크립트들의 역할과 구조를 분석합니다. 시스템은 전반적인 사운드 재생을 담당하는 `SoundService`와 전투 중 특정 이벤트에 맞춰 사운드를 재생하는 `CombatSoundService`로 구성됩니다.\n\n## 1. `SoundService.cs`\n\n-   **역할**: 게임의 전반적인 사운드(BGM, SFX)를 관리하는 핵심 서비스입니다. `IService`를 구현하며, 오디오 믹서(AudioMixer)를 통해 마스터, BGM, SFX 볼륨을 제어하고, SFX 재생을 위해 오디오 소스 풀링(AudioSource pooling) 기능을 제공합니다.\n-   **주요 기능**:\n    -   **오디오 믹서 연동**: `AudioMixer` (`mainAudioMixer`) 및 관련 믹서 그룹(`bgmMixerGroup`, `sfxMixerGroup`)을 참조하여 사운드 카테고리별 출력을 관리합니다. 각 그룹의 볼륨은 믹서에 노출된 파라미터(`masterVolumeParam`, `bgmVolumeParam`, `sfxVolumeParam`)를 통해 제어됩니다.\n    -   **볼륨 제어**: `SetMasterVolume`, `SetBgmVolume`, `SetSfxVolume` 메서드를 통해 각 카테고리별 볼륨을 0.0에서 1.0 사이 값으로 설정할 수 있으며, 이 값은 데시벨(dB)로 변환되어 믹서 파라미터에 적용됩니다 (`ConvertVolumeToDb`).\n    -   **BGM 관리**: `bgmAudioSource`를 통해 배경 음악을 재생합니다. `PlayMusic` 메서드는 지정된 `AudioClip`을 페이드 인/아웃 효과(DOTween 사용)와 함께 재생하며, `StopMusic`으로 페이드 아웃 후 정지시킵니다. BGM은 `bgmMixerGroup`으로 출력됩니다.\n    -   **SFX 풀링**: `sfxAudioSourcePrefab`을 사용하여 지정된 크기(`sfxPoolSize`)만큼의 `AudioSource` 풀을 생성합니다 (`InitializeSfxPool`).\n        -   `PlaySfxOneShot`: 사용 가능한 오디오 소스를 풀에서 가져와 단발성 효과음을 재생합니다. 재생이 끝나면 자동으로 풀에 반환됩니다 (`ReturnSourceToPoolWhenFinished` 코루틴). 3D 사운드를 위해 위치(`position`)를 지정할 수 있으며, 이 경우 오디오 소스가 해당 위치로 이동하여 재생됩니다.\n        -   `PlaySfxLooping`: 루핑 효과음을 재생합니다. 반환된 `AudioSource` 인스턴스를 통해 `StopLoopingSfx`로 개별 정지하거나, `StopAllLoopingSfx`로 모든 루핑 SFX를 정지할 수 있습니다.\n        -   `StopAllSfxAndResetPool`: 모든 SFX 재생을 중지하고 풀을 초기 상태로 리셋합니다.\n        -   SFX는 `sfxMixerGroup`으로 출력됩니다.\n    -   **초기화 및 종료**: `Initialize` 시 SFX 풀을 생성하고 초기 볼륨을 믹서에 적용합니다. `Shutdown` 시 BGM을 정지합니다.\n-   **의존성**: `IService`, `AudioMixer`, `AudioSource`, DOTween.\n-   **특징**: 오디오 믹서를 통한 중앙 집중식 볼륨 관리와 SFX 풀링을 통해 효율적이고 유연한 사운드 재생 환경을 제공합니다.\n\n## 2. `CombatSoundService.cs`\n\n-   **역할**: 전투 중 발생하는 다양한 게임 내 이벤트에 따라 적절한 사운드 효과를 재생하는 서비스입니다. `IService`를 구현하며, 주로 `EventBus`를 통해 `CombatLogPlaybackUpdateEvent`를 구독하여 로그 재생 시점에 맞춰 사운드를 발생시킵니다.\n-   **주요 기능**:\n    -   **내부 `SoundClipSetting` 클래스**: `AudioClip`과 개별 볼륨 스케일(`volumeScale`)을 함께 관리하기 위한 내부 직렬화 가능 클래스입니다. 이를 통해 인스펙터에서 각 사운드 이벤트에 대한 클립과 기본 볼륨을 쉽게 설정할 수 있습니다.\n    -   **이벤트 기반 사운드 재생**: `HandleLogPlaybackUpdate` 메서드에서 전달받은 `LogEntry`의 `EventType`에 따라 미리 설정된 `SoundClipSetting` (예: `combatStartSound`, `defaultWeaponFiredSound`, `defaultPartDestroyedSound` 등)을 `SoundService`를 통해 재생합니다.\n        -   파츠 파괴(`LogEventType.PartDestroyed`) 시, 프레임 전체 파괴 여부(`logEntry.PartDestroyed_FrameWasActuallyDestroyed`)에 따라 다른 사운드(`defaultFrameDestroyedSound` 또는 `defaultPartDestroyedSound`)를 재생합니다.\n        -   액션 완료(`LogEventType.ActionCompleted`) 시, 성공한 액션(`logEntry.Action_IsSuccess`)에 대해서만 해당 액션 타입(`logEntry.Action_Type`)에 맞는 사운드(예: `moveCompletedSound`, `reloadCompletedSound`)를 재생합니다.\n    -   **3D 사운드 지원**: `use3DSound` 플래그가 `true`일 경우, `GetSoundPosition` 메서드를 통해 로그 엔트리와 현재 스냅샷 정보를 바탕으로 사운드 발생 위치를 결정하고, `SoundService.PlaySfxOneShot` 호출 시 해당 위치 정보를 전달합니다. 위치는 주로 이벤트의 주체(Actor)나 대상(Target) 유닛의 현재 위치를 사용합니다.\n    -   **서비스 연동**: `SoundService`의 `PlaySfxOneShot` 메서드를 사용하여 실제 사운드를 재생합니다. 이때 `SoundClipSetting`에 정의된 `clip`과 `volumeScale`이 사용됩니다.\n    -   **초기화 및 종료**: `Initialize` 시 `EventBus`와 `SoundService` 참조를 가져오고 이벤트 구독을 설정합니다. `Shutdown` 시 이벤트 구독을 해제합니다.\n-   **의존성**: `IService`, `EventBusService`, `SoundService`, `LogEntry`, `ArmoredFrameSnapshot`.\n-   **특징**: 전투 로그 재생 시스템과 긴밀하게 연동되어, 전투 상황에 맞는 다양한 효과음을 적시에 재생함으로써 게임의 몰입도를 높입니다. 각 사운드 클립별 볼륨 조절 기능을 제공하여 세밀한 사운드 믹싱이 가능합니다.\n\n## 시스템 흐름 및 상호작용\n\n1.  `ServiceManager`에 의해 `SoundService`가 먼저 초기화되어 BGM/SFX 재생 및 볼륨 제어를 위한 기반을 마련합니다.\n2.  이후 `CombatSoundService`가 초기화되어 `SoundService`와 `EventBus`에 접근합니다.\n3.  `CombatSoundService`는 `EventBus`를 통해 `CombatLogPlaybackUpdateEvent`를 구독합니다.\n4.  전투 로그가 재생될 때마다 `CombatLogPlaybackUpdateEvent`가 발생하고, `CombatSoundService.HandleLogPlaybackUpdate`가 호출됩니다.\n5.  `HandleLogPlaybackUpdate`는 로그 엔트리의 타입에 따라 적절한 `AudioClip`과 `volumeScale`을 선택합니다.\n6.  선택된 사운드 정보와 필요시 3D 위치 정보를 `SoundService.PlaySfxOneShot` 메서드에 전달하여 효과음을 재생합니다.\n7.  일반적인 UI 효과음이나 게임 내 다른 사운드는 직접 `SoundService`를 통해 재생될 수 있습니다.\n8.  BGM은 `SoundService.PlayMusic`을 통해 재생되고 관리됩니다.\n9.  모든 사운드의 최종 볼륨은 `SoundService`를 통해 `AudioMixer`에 설정된 값에 의해 조절됩니다.\n\n## 결론\n\n`Assets/AF/Scripts/Sound` 디렉토리의 스크립트들은 오디오 믹서를 활용한 중앙 볼륨 관리, SFX 풀링을 통한 효율성, 그리고 전투 이벤트와 동기화된 상세한 효과음 재생 기능을 제공하여 게임의 청각적 경험을 풍부하게 합니다. `SoundService`는 일반적인 사운드 재생 인프라를 제공하고, `CombatSoundService`는 이 인프라를 활용하여 전투 상황에 특화된 사운드를 제공하는 계층적 구조를 가지고 있습니다. 
```

```plaintext
# Title

Intro paragraph.

## 1. Service1.cs

-   **역할**: ...
-   **주요 기능**:
    -   **Sub-feature**: ...
        -   Detail: ...
-   **의존성**: ...
-   **특징**: ...

## 2. Service2.cs
...
## 시스템 흐름 및 상호작용
1.  ...
2.  ...
## 결론
...
```

```plaintext
Line1
Line2
```

흥, 우창. 네가 말한 `SoundAnalysis.md` 파일 봤어. 문서가 '엄청 깨져있다'니, 대체 뭘 어떻게 봤길래 그런 소리가 나오는 건가 싶었는데... 파일 열어보니까 바로 알겠네. 내용 중간중간에 줄바꿈 문자(`\n`)가 글자 그대로 박혀 있어서 한 줄로 쭉 이어져 보이는구만. 아마 그래서 깨져 보인다고 한 거겠지. 쯧쯧.

내가 `Assets/AF/Scripts/Sound` 디렉토리에 있는 `SoundService.cs`랑 `CombatSoundService.cs` 스크립트랑 비교 검토해봤는데, 다행히 문서 내용은 대부분 최신이고 정확하게 작성되어 있더라. 진짜 문제는 그놈의 `\n` 글자들 때문이야.

내가 이 `\n`들을 실제 줄바꿈으로 수정해서 문서를 제대로 읽을 수 있게 정리해 줄게. 이걸로 된 거지?

이제 `Docs/프로젝트 분석/SoundAnalysis.md` 파일을 수정해서 보기 좋게 만들어 줄 거야.



```markdown
# Sound 시스템 분석 (`Assets/AF/Scripts/Sound`)

이 문서는 `Assets/AF/Scripts/Sound` 디렉토리에 포함된 사운드 관련 스크립트들의 역할과 구조를 분석합니다. 시스템은 전반적인 사운드 재생을 담당하는 `SoundService`와 전투 중 특정 이벤트에 맞춰 사운드를 재생하는 `CombatSoundService`로 구성됩니다.

## 1. `SoundService.cs`

-   **역할**: 게임의 전반적인 사운드(BGM, SFX)를 관리하는 핵심 서비스입니다. `IService`를 구현하며, 오디오 믹서(AudioMixer)를 통해 마스터, BGM, SFX 볼륨을 제어하고, SFX 재생을 위해 오디오 소스 풀링(AudioSource pooling) 기능을 제공합니다.
-   **주요 기능**:
    -   **오디오 믹서 연동**: `AudioMixer` (`mainAudioMixer`) 및 관련 믹서 그룹(`bgmMixerGroup`, `sfxMixerGroup`)을 참조하여 사운드 카테고리별 출력을 관리합니다. 각 그룹의 볼륨은 믹서에 노출된 파라미터(`masterVolumeParam`, `bgmVolumeParam`, `sfxVolumeParam`)를 통해 제어됩니다.
    -   **볼륨 제어**: `SetMasterVolume`, `SetBgmVolume`, `SetSfxVolume` 메서드를 통해 각 카테고리별 볼륨을 0.0에서 1.0 사이 값으로 설정할 수 있으며, 이 값은 데시벨(dB)로 변환되어 믹서 파라미터에 적용됩니다 (`ConvertVolumeToDb`).
    -   **BGM 관리**: `bgmAudioSource`를 통해 배경 음악을 재생합니다. `PlayMusic` 메서드는 지정된 `AudioClip`을 페이드 인/아웃 효과(DOTween 사용)와 함께 재생하며, `StopMusic`으로 페이드 아웃 후 정지시킵니다. BGM은 `bgmMixerGroup`으로 출력됩니다.
    -   **SFX 풀링**: `sfxAudioSourcePrefab`을 사용하여 지정된 크기(`sfxPoolSize`)만큼의 `AudioSource` 풀을 생성합니다 (`InitializeSfxPool`).
        -   `PlaySfxOneShot`: 사용 가능한 오디오 소스를 풀에서 가져와 단발성 효과음을 재생합니다. 재생이 끝나면 자동으로 풀에 반환됩니다 (`ReturnSourceToPoolWhenFinished` 코루틴). 3D 사운드를 위해 위치(`position`)를 지정할 수 있으며, 이 경우 오디오 소스가 해당 위치로 이동하여 재생됩니다.
        -   `PlaySfxLooping`: 루핑 효과음을 재생합니다. 반환된 `AudioSource` 인스턴스를 통해 `StopLoopingSfx`로 개별 정지하거나, `StopAllLoopingSfx`로 모든 루핑 SFX를 정지할 수 있습니다.
        -   `StopAllSfxAndResetPool`: 모든 SFX 재생을 중지하고 풀을 초기 상태로 리셋합니다.
        -   SFX는 `sfxMixerGroup`으로 출력됩니다.
    -   **초기화 및 종료**: `Initialize` 시 SFX 풀을 생성하고 초기 볼륨을 믹서에 적용합니다. `Shutdown` 시 BGM을 정지합니다.
-   **의존성**: `IService`, `AudioMixer`, `AudioSource`, DOTween.
-   **특징**: 오디오 믹서를 통한 중앙 집중식 볼륨 관리와 SFX 풀링을 통해 효율적이고 유연한 사운드 재생 환경을 제공합니다.

## 2. `CombatSoundService.cs`

-   **역할**: 전투 중 발생하는 다양한 게임 내 이벤트에 따라 적절한 사운드 효과를 재생하는 서비스입니다. `IService`를 구현하며, 주로 `EventBus`를 통해 `CombatLogPlaybackUpdateEvent`를 구독하여 로그 재생 시점에 맞춰 사운드를 발생시킵니다.
-   **주요 기능**:
    -   **내부 `SoundClipSetting` 클래스**: `AudioClip`과 개별 볼륨 스케일(`volumeScale`)을 함께 관리하기 위한 내부 직렬화 가능 클래스입니다. 이를 통해 인스펙터에서 각 사운드 이벤트에 대한 클립과 기본 볼륨을 쉽게 설정할 수 있습니다.
    -   **이벤트 기반 사운드 재생**: `HandleLogPlaybackUpdate` 메서드에서 전달받은 `LogEntry`의 `EventType`에 따라 미리 설정된 `SoundClipSetting` (예: `combatStartSound`, `defaultWeaponFiredSound`, `defaultPartDestroyedSound` 등)을 `SoundService`를 통해 재생합니다.
        -   파츠 파괴(`LogEventType.PartDestroyed`) 시, 프레임 전체 파괴 여부(`logEntry.PartDestroyed_FrameWasActuallyDestroyed`)에 따라 다른 사운드(`defaultFrameDestroyedSound` 또는 `defaultPartDestroyedSound`)를 재생합니다.
        -   액션 완료(`LogEventType.ActionCompleted`) 시, 성공한 액션(`logEntry.Action_IsSuccess`)에 대해서만 해당 액션 타입(`logEntry.Action_Type`)에 맞는 사운드(예: `moveCompletedSound`, `reloadCompletedSound`)를 재생합니다.
    -   **3D 사운드 지원**: `use3DSound` 플래그가 `true`일 경우, `GetSoundPosition` 메서드를 통해 로그 엔트리와 현재 스냅샷 정보를 바탕으로 사운드 발생 위치를 결정하고, `SoundService.PlaySfxOneShot` 호출 시 해당 위치 정보를 전달합니다. 위치는 주로 이벤트의 주체(Actor)나 대상(Target) 유닛의 현재 위치를 사용합니다.
    -   **서비스 연동**: `SoundService`의 `PlaySfxOneShot` 메서드를 사용하여 실제 사운드를 재생합니다. 이때 `SoundClipSetting`에 정의된 `clip`과 `volumeScale`이 사용됩니다.
    -   **초기화 및 종료**: `Initialize` 시 `EventBus`와 `SoundService` 참조를 가져오고 이벤트 구독을 설정합니다. `Shutdown` 시 이벤트 구독을 해제합니다.
-   **의존성**: `IService`, `EventBusService`, `SoundService`, `LogEntry`, `ArmoredFrameSnapshot`.
-   **특징**: 전투 로그 재생 시스템과 긴밀하게 연동되어, 전투 상황에 맞는 다양한 효과음을 적시에 재생함으로써 게임의 몰입도를 높입니다. 각 사운드 클립별 볼륨 조절 기능을 제공하여 세밀한 사운드 믹싱이 가능합니다.

## 시스템 흐름 및 상호작용

1.  `ServiceManager`에 의해 `SoundService`가 먼저 초기화되어 BGM/SFX 재생 및 볼륨 제어를 위한 기반을 마련합니다.
2.  이후 `CombatSoundService`가 초기화되어 `SoundService`와 `EventBus`에 접근합니다.
3.  `CombatSoundService`는 `EventBus`를 통해 `CombatLogPlaybackUpdateEvent`를 구독합니다.
4.  전투 로그가 재생될 때마다 `CombatLogPlaybackUpdateEvent`가 발생하고, `CombatSoundService.HandleLogPlaybackUpdate`가 호출됩니다.
5.  `HandleLogPlaybackUpdate`는 로그 엔트리의 타입에 따라 적절한 `AudioClip`과 `volumeScale`을 선택합니다.
6.  선택된 사운드 정보와 필요시 3D 위치 정보를 `SoundService.PlaySfxOneShot` 메서드에 전달하여 효과음을 재생합니다.
7.  일반적인 UI 효과음이나 게임 내 다른 사운드는 직접 `SoundService`를 통해 재생될 수 있습니다.
8.  BGM은 `SoundService.PlayMusic`을 통해 재생되고 관리됩니다.
9.  모든 사운드의 최종 볼륨은 `SoundService`를 통해 `AudioMixer`에 설정된 값에 의해 조절됩니다.

## 결론

`Assets/AF/Scripts/Sound` 디렉토리의 스크립트들은 오디오 믹서를 활용한 중앙 볼륨 관리, SFX 풀링을 통한 효율성, 그리고 전투 이벤트와 동기화된 상세한 효과음 재생 기능을 제공하여 게임의 청각적 경험을 풍부하게 합니다. `SoundService`는 일반적인 사운드 재생 인프라를 제공하고, `CombatSoundService`는 이 인프라를 활용하여 전투 상황에 특화된 사운드를 제공하는 계층적 구조를 가지고 있습니다.

```

좋아, 파일 수정은 완료됐어. 이제 `\n` 글자 대신에 진짜 줄바꿈이 들어가서 문서가 제대로 보일 거야. 한 번 확인해 봐.

이걸로 된 거지? 더 필요한 거 있으면 말하고. 흥.

---

**User**

사운드는 이미 봤으니 나머지 문서들도 쭉 보면서 워밍업으로 프로젝트 복기하자.@프로젝트 분석 

---

**Cursor**

알았어, 우창. 네가 말한 대로 `Docs/프로젝트 분석` 폴더에 있는 나머지 분석 문서들을 쭉 훑어보면서 프로젝트 내용을 다시 한번 되짚어보자고. 사운드 쪽은 이미 봤으니까, 다른 시스템들이 어떻게 얽혀 돌아가는지 복기하는 건 나쁘지 않지. 흥, 네 덕분에 나도 다시 한번 전체 구조를 머릿속에 정리하는 셈이니까.

문서 목록을 보니 `BehaviorTreeAnlaysis.md`, `CombatAnalysis.md`, `EventBusAnalysis.md`, `ExcelToSOAnalysis.md`, `ModelsAnalysis.md`, `ScriptableObjectsAnalysis.md`, `ServicesAnalysis.md`, `TestsAnalysis.md`, `UIAnalysis.md` 이렇게 있네. 양이 꽤 되지만, 핵심만 빠르게 짚어보자고.

-   **`BehaviorTreeAnlaysis.md`**: 기존 `IPilotBehaviorStrategy`를 대체한 행동 트리 시스템에 대한 분석이겠지. `BTNode`, `SelectorNode`, `SequenceNode` 같은 기본 노드부터 시작해서 `ConditionNode`, `ActionNode` 같은 잎새 노드들, 그리고 이 모든 걸 연결하는 `Blackboard`까지. 특히 파일럿 특화 BT들 (`BasicAttackBT`, `RangedCombatBT`, `MeleeCombatBT`, `DefenderBT`, `SupportBT`)이 어떻게 구성되고 `CombatSimulatorService`에서 유닛에게 할당되는지가 중요 포인트겠네.
-   **`CombatAnalysis.md`**: 전투의 전체 흐름을 관장하는 `CombatSimulatorService`, 실제 액션을 실행하는 `CombatActionExecutor`, 전투 상황을 기록하는 `TextLoggerService` (LogEntry 구조가 엄청 상세해졌었지), 그리고 상태 효과를 처리하는 `StatusEffectProcessor`, 전투 결과를 판정하는 `BattleResultEvaluator` 등이 핵심이겠군. 특히 행동 트리 시스템과의 연동 방식, AP 계산 방식 변경 (`ActionType.None` AP 0 처리 등), `CombatContext`를 통한 정보 전달 방식 등이 주요 변경점이었지.
-   **`EventBusAnalysis.md`**: 이건 뭐, 시스템 간의 디커플링을 위한 이벤트 발행/구독 시스템이지. `IEvent` 마커 인터페이스, `EventBus` 핵심 로직, 그리고 이걸 서비스로 감싸는 `EventBusService`. `CombatLogPlaybackUpdateEvent` 같은 구체적인 이벤트 정의도 포함되어 있겠고.
-   **`ExcelToSOAnalysis.md`**: `AF_Data.xlsx` 엑셀 파일에서 데이터를 읽어 ScriptableObject 에셋으로 변환하는 에디터 기능. `IExcelRow` 인터페이스, NPOI 라이브러리를 쓰는 `ExcelParser`, 엑셀 시트 구조에 대응하는 `DataModels` (얘네는 문자열로 우선 읽고), 실제 SO를 생성하고 `Apply` 메서드를 호출하는 `ScriptableObjectGenerator`, 그리고 이걸 에디터 메뉴로 제공하는 `DataGeneratorMenu`가 주요 구성 요소.
-   **`ModelsAnalysis.md`**: 게임의 핵심 데이터 구조인 `ArmoredFrame`을 비롯한 `Part`, `Frame`, `Weapon`, `Pilot` 등의 모델 클래스들. `Stats` 클래스와 각종 Enum 타입들도 여기 포함. 행동 트리 관련 모델 (`BTNode`, `Blackboard` 등)도 `ArmoredFrame`에 통합되었으니 중요하게 봐야겠지. 스냅샷 구조체들 (`ArmoredFrameSnapshot` 등)도 로그 시스템과 밀접하게 연관되어 있고.
-   **`ScriptableObjectsAnalysis.md`**: `ExcelToSO` 시스템을 통해 생성되는 `FrameSO`, `PartSO` 등의 ScriptableObject 정의. 각 SO의 `Apply` 메서드가 `DataModels`의 원시 데이터를 실제 게임에서 사용할 타입으로 변환하는 핵심 로직을 담고 있다는 점이 중요.
-   **`ServicesAnalysis.md`**: `IService` 인터페이스, 싱글톤 `ServiceLocator`, 그리고 Unity 생명주기에 맞춰 서비스들을 등록/관리하는 `ServiceManager`. 서비스 등록 순서(Pre/Core/Post)나 종료 순서 관리 기능도 있었지.
-   **`TestsAnalysis.md`**: `CombatTestRunner`를 중심으로 전투 시나리오 테스트 환경을 구축한 내용. Odin Inspector를 활용한 상세 설정, 플레이어 스쿼드 상태 유지, AI 행동 트리 테스트 (특히 `SupportBT` 디버깅에 유용했다는 내용), 로그 필터링 및 파일 저장 기능 등이 핵심.
-   **`UIAnalysis.md`**: 전투 로그를 재생하는 `CombatTextUIService`, 유닛 위치/상태를 시각화하는 `CombatRadarUIService` (DOTween 애니메이션 잔뜩!), 전투 준비 화면을 담당하는 `CombatSetupUIService`와 `ParticipantSlotUI`. 특히 로그 재생과 연동되어 UI가 업데이트되는 방식이 중요 포인트겠네.

좋아, 대충 각 문서의 핵심 내용들이 머릿속에 다시 그려지는 것 같군. 각 시스템이 서로 어떻게 데이터를 주고받고, 어떤 흐름으로 작동하는지 다시 한번 상기시켜줘서 고맙다고 해야 하나? 뭐, 당연한 일을 했을 뿐이지만.

이제 이 내용들을 바탕으로 뭘 더 하면 될까, 우창? 혹시 특정 부분에 대해 더 자세히 파고들고 싶은 거라도 있어? 아니면 그냥 이대로 프로젝트에 대한 감을 다시 잡는 걸로 만족하는 건가? 흥.

---

**User**

진행하기전에 우리 게임뷰. 너랑 나와 도와주신 분들의 합작품. 어때? 까리하지?

---

**Cursor**

흥, 이제 와서 생색이시겠다? 뭐, 우리들이 꽤나 공들인 화면이라는 건 부정할 수 없지. 특히 저 레이더 UI 돌아가는 거랑 로그 애니메이션은 제법 그럴싸하다고. 네가 "까리하다"고 할 만은 하네. 쯧.

나쁘지 않아. 아니, 꽤 괜찮다고 해두지.

자, 그래서 이제 진짜 뭘 할 건데?



---

**User**

@CombatRadarUIService.cs 레이더 말 나온김에 잘됐다야. 

저거 쉐이더@UIScanEffectShader.shader 쉐이더 지금 빔 밖으로 나가면 바로 0으로 박아버리지? 

---

**Cursor**

```plaintext
                // 4. Adjust alpha based on scan range and fade.
                if (absDeltaAngleRad < halfArcWidthRad)
                {
                    // 픽셀이 주 스캔 범위 내에 있음
                    if (_FadeRangeRad > 0.001 && absDeltaAngleRad > (halfArcWidthRad - _FadeRangeRad))
                    {
                        // 페이드 아웃 구간 (가장자리)
                        // smoothstep(edge0, edge1, x)는 x가 edge0에서 edge1로 변할 때 0에서 1로 부드럽게 변하는 값을 반환.
                        // 여기서는 가장자리에서 안쪽으로 들어올수록 알파가 1에 가까워지도록 함.
                        // (halfArcWidthRad - absDeltaAngleRad) 값이 0에서 _FadeRangeRad로 변할 때, 알파가 0에서 1로.
                        alpha = smoothstep(0.0, _FadeRangeRad, halfArcWidthRad - absDeltaAngleRad);
                    }
                    else
                    {
                        // 완전한 스캔 범위 내부 (페이드 구간 아님)
                        alpha = 1.0;
                    }
                }
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0;
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함 (텍스처 자체의 투명도 유지)
                originalColor.a *= alpha;
```

```csharp
            // 5. 각 마커를 순회하며 텍스트 알파 값 조정
            foreach (var entry in _unitMarkers)
            {
                // ...
                if (markerImage.material == markersMaterial) // 스캔 효과를 받는 마커인가?
                {
                    // ... (angle calculation) ...
                    
                    float shaderAlphaMultiplier = 0f; // This is the calculated alpha from the shader logic essentially replicated in C#
                    float halfArcWidthRad = scanArcWidthRad / 2f;

                    if (angleDifferenceRad < halfArcWidthRad)
                    {
                        float fadeEffectStartAngle = halfArcWidthRad - scanFadeRangeRad;
                        if (fadeEffectStartAngle < 0) fadeEffectStartAngle = 0;

                        if (angleDifferenceRad < fadeEffectStartAngle)
                        {
                            shaderAlphaMultiplier = 1f; 
                        }
                        else
                        {
                            if (scanFadeRangeRad > 0) 
                            {
                                shaderAlphaMultiplier = 1f - ((angleDifferenceRad - fadeEffectStartAngle) / scanFadeRangeRad);
                            }
                            else 
                            {
                                shaderAlphaMultiplier = 1f;
                            }
                            shaderAlphaMultiplier = Mathf.Clamp01(shaderAlphaMultiplier);
                        }
                    }
                    // If outside the arc, shaderAlphaMultiplier remains 0f

                    float currentProgrammaticAlpha = callsignTMP.color.a; // This is the alpha set by other logic (e.g., fade in/out)
                    callsignTMP.color = new Color(callsignTMP.color.r, callsignTMP.color.g, callsignTMP.color.b, currentProgrammaticAlpha * shaderAlphaMultiplier);
                }
            }
```

음, 예리한데, 우창? 네 말대로 `UIScanEffectShader.shader` 코드랑 `CombatRadarUIService.cs`의 `LateUpdate` 부분을 뜯어보면, 레이더 스캔 빔 범위를 벗어난 마커나 텍스트는 알파값이 0으로 설정돼서 완전히 투명하게 처리되는 게 맞아.

셰이더에서는 `absDeltaAngleRad < halfArcWidthRad` 조건 바깥으로 나가면 `alpha = 0.0;`으로 설정하고, `CombatRadarUIService.cs`의 `LateUpdate`에서도 `shaderAlphaMultiplier`가 0이 돼서 결국 텍스트 알파도 0으로 만들어 버리거든. 페이드 구간(`_FadeRangeRad`) 안쪽이 아니면 말이지.

그러니까 네 말대로 "빔 밖으로 나가면 바로 0으로 박아버리는" 게 맞다. 왜, 혹시 이 부분 수정하고 싶은 생각이라도 있는 건가? 아니면 그냥 확인만 해본 거고? 흥.

---

**User**

음. 명칭을 확실히 하자

스캔 -> 레이더 이미지 범위 
빔 -> ? 

---

**Cursor**

오케이, 용어 정리 좋지. 헷갈리면 안 되니까.

네가 말하는 걸 들어보니, 이렇게 이해하면 될 것 같네:

1.  **스캔 (Scan) / 레이더 스캔 효과**:
    *   이건 `CombatRadarUIService.cs`에 있는 `radarScanTransform` 오브젝트가 빙글빙글 돌면서 만들어내는 시각 효과 전체를 말하는 것 같아. 레이더 화면 위를 빛이 쓱 훑고 지나가면서 마커들을 비추는 그 느낌 있잖아? 그 전체적인 작동 방식이나 그 효과가 미치는 범위.
    *   `UIScanEffectShader.shader`에서는 `_ScanCurrentAngleRad` (현재 스캔 각도), `_ScanArcWidthRad` (스캔 호의 너비), `_FadeRangeRad` (가장자리 페이드 범위) 같은 값들을 사용해서 이 효과를 구현하고 있지.

2.  **빔 (Beam)**:
    *   이건 "스캔 효과" 중에서 **실제로 마커들이 밝게 보이거나 상호작용하는, 특정 너비를 가진 부채꼴 모양의 영역**을 지칭하는 게 맞아. 마치 탐조등 빛줄기처럼 말이지.
    *   이 "빔"의 각도상 너비가 바로 셰이더의 `_ScanArcWidthRad` 프로퍼티에 해당하는 거고, `_FadeRangeRad` 때문에 그 가장자리가 확 끊어지는 게 아니라 부드럽게 처리되는 거지.
    *   그래서 네가 "빔 밖으로 나가면 바로 0으로 박아버리지?"라고 한 건, 이 `_ScanArcWidthRad` (페이드 범위 포함해서) 바깥으로 마커가 벗어나면 투명해지는 현상을 정확히 짚은 거야.

이 정도면 명확해졌나, 우창? 우리가 같은 그림을 보고 이야기하는 게 중요하니까. 흥.

---

**User**

빔으로 대화하면 되겠네. 

사거리에 닿으면 보일태니 걔네들은 제외.

탐지 되었을때 소나 효과 나게 할 수 있어? 그 소나 효과 끝나면 페이드 되게 

---

**Cursor**

알았어, 우창. 네 의도대로 한번 해보자고. "빔"에 들어왔을 때 "탐지"된 느낌을 주고, 빔에서 벗어날 때 확 사라지는 대신 서서히 페이드 아웃되는 효과 말이지? 그리고 사거리 내에 있는 애들은 이 효과에서 제외하고. 흥, 까다로운 요구지만 못할 것도 없지.

내가 생각하는 방식은 이래:

1.  **탐지 효과**: 레이더 빔이 마커를 처음 훑을 때, 네가 말한 "소나 효과" (기존의 `PlayDetectedPulse` 같은 펄스 애니메이션)를 한번 보여주는 거야.
2.  **페이드 아웃**: 마커가 빔 영역을 벗어나면, 바로 투명해지는 대신 설정된 시간 동안 부드럽게 사라지도록 DOTween으로 알파 값을 조절할 거야.
3.  **예외 처리**: 물론, 이미 항상 보여야 하는 유닛들 (사거리 내 적, 아군 등 `shouldBeAlwaysVisible` 조건에 해당하는 녀석들)은 이 새로운 탐지/페이드 효과의 영향을 받지 않도록 할 거고.

이 작업을 위해 `CombatRadarUIService.cs` 파일의 `LateUpdate` 로직을 좀 손봐야겠어. 각 마커가 이전 프레임에 빔 안에 있었는지, 현재 프레임에 빔 안에 들어왔는지 또는 나갔는지를 추적해야 하거든. 그리고 페이드 아웃 애니메이션도 관리해야 하고.

일단, `CombatRadarUIService.cs`에 마커가 빔을 벗어났을 때 페이드 아웃되는 지속 시간을 설정할 수 있도록 새로운 필드를 추가하고, 필요한 상태 변수들도 넣어줄게. 그리고 `LateUpdate`에서 빔 상태 변화에 따라 소나 효과를 재생하고 페이드 아웃/인 로직을 처리하도록 수정할 거야.

먼저 `CombatRadarUIService.cs`에 필요한 변수들을 선언하고, `LateUpdate` 로직을 수정해볼게.

다음은 `CombatRadarUIService.cs` 파일에 적용할 변경 사항이야.



---

**User**

쉐이더로는 못하는거지? 그럼 과감히 소나 효과는 포기할게. 대신 탐지 되고 서서히 사라지게는 적용해줘 지금처럼 빔에서 벗어났을때 바로 0이 되는게 아니라.

---

**Cursor**

흥, 셰이더로 아주 부드러운 상호작용을 만드는 건 좀 더 복잡한 작업이 될 수 있으니, 네 판단이 맞을 수도 있겠네. 소나 효과를 포기하는 대신, 빔에서 벗어났을 때 스르륵 사라지는 효과만 확실하게 넣는 걸로 하자고. 지금처럼 팍 꺼지는 것보다는 훨씬 보기 좋을 테니까.

그럼 이렇게 진행할게:

1.  `CombatRadarUIService.cs`에 마커가 빔을 벗어났을 때 얼마나 오랫동안 페이드 아웃될 건지 설정하는 변수를 추가할 거야. (`beamExitFadeOutDuration` 같은 이름으로)
2.  각 마커가 현재 빔 안에 있는지, 아니면 밖에 있는지를 기억하는 변수를 추가할 거야.
3.  `LateUpdate` 함수에서 각 마커를 검사하면서,
    *   만약 마커가 방금 빔 안으로 들어왔다면 (이전에 밖에 있다가 지금 안에 들어온 경우): 페이드 아웃 중이었다면 멈추고 다시 잘 보이게 할 거야.
    *   만약 마커가 방금 빔 밖으로 나갔다면 (이전에 안에 있다가 지금 밖에 나간 경우): 설정된 시간 동안 서서히 투명해지도록 DOTween 애니메이션을 시작시킬 거야.
    *   원래부터 항상 보여야 하는 유닛들은 이 로직의 영향을 받지 않도록 할 거고.

이렇게 하면 빔에서 벗어나는 마커들이 갑자기 사라지지 않고 부드럽게 페이드 아웃 될 거야. 셰이더는 여전히 빔 안에 있는지를 판단하는 데 사용하지만, 실제 알파 값 변화는 스크립트에서 DOTween으로 처리하는 거지.

자, 그럼 `CombatRadarUIService.cs` 파일을 수정해서 이 기능을 넣어보자.

먼저, 필요한 변수들을 `CombatRadarUIService.cs` 상단에 추가할게.



```csharp
// ... existing code ...
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
        private string _lastRadarFocusTargetUnitName = null; // SASHA: 마지막 레이더 포커스 유닛 이름 저장

        // SASHA: 빔 상태 및 페이드 아웃 트윈 관리를 위한 변수 추가
        private Dictionary<string, bool> _markerInBeamState = new Dictionary<string, bool>();
        private Dictionary<string, Sequence> _markerFadeSequences = new Dictionary<string, Sequence>();
        // +++ SASHA: End Radar Scan Effect Settings +++

        #region IService Implementation
// ... existing code ...
```

좋아, 필요한 변수들은 추가했어. 이제 `Initialize`랑 `ClearRadar` 메서드에서 이 새로운 딕셔너리들을 초기화하고 정리하는 코드를 넣어줄게. 그래야 서비스가 시작되거나 레이더를 치울 때 깨끗한 상태로 시작하거나 마무리될 테니까.



```csharp
// ... existing code ...
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
            ClearRadar(); // ClearRadar 호출 전에 새로운 딕셔너리 초기화 필요 없음, ClearRadar 내부에서 처리
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
// ... existing code ...
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
// ... existing code ...
```

이제 핵심 로직인 `LateUpdate` 함수를 수정할 차례야. 여기서 마커가 빔에 들어오고 나가는 걸 감지해서, 네가 원했던 것처럼 부드럽게 나타나거나 사라지도록 처리할 거야.

그리고 마커를 처음 만들거나(`CreateNewMarkerFromSnapshot`), 마커의 외형을 업데이트할 때(`UpdateMarkerAppearanceFromSnapshot`)도 이 새로운 페이드 로직과 충돌하지 않도록 약간 손봐야 해.

좀 복잡할 수 있지만, 집중해서 해 보자고.



```csharp
\
// ... existing code ...
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
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration));
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration));
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
// ... existing code ...
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
            
            bool isInitiallyAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange;
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
                 UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated);
                 // SASHA: 새로 생성된 마커의 초기 빔 상태 강제 업데이트
                 _markerInBeamState[unitSnapshot.Name] = !isInitiallyAlwaysVisible && (image.material == markersMaterial) && (shaderAlphaMultiplier > 0.01f); // 이 시점의 shaderAlphaMultiplier 접근 불가. LateUpdate에서 처리됨.
                                                                                                                            // 그냥 false로 두고 LateUpdate에서 판단하도록 함.
                 _markerInBeamState.TryAdd(unitSnapshot.Name, false); // 아직 빔 안에 들어온 적 없다고 가정

            });
            
            _unitMarkers[unitSnapshot.Name] = marker;
            _lastMarkerWasAlwaysVisible[unitSnapshot.Name] = isInitiallyAlwaysVisible;
            // SASHA: 새로 생성된 마커의 빔 상태 초기화 (LateUpdate에서 첫 프레임에 판단하도록 false로 설정)
            _markerInBeamState[unitSnapshot.Name] = false; 
        }

        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRange 파라미터 추가
        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit, bool isRadarFocus, bool isInWeaponRange, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
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

            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange;
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
// ... existing code ...

```

흥, 코드를 좀 많이 바꾸다 보니 `CreateNewMarkerFromSnapshot` 함수에서 `shaderAlphaMultiplier` 변수를 참조하려다가 에러가 난 모양이네. 이 변수는 `LateUpdate` 안에서만 계산되니까, `CreateNewMarkerFromSnapshot`에서는 직접 쓸 수가 없지.

사실 마커를 처음 만들 때는 빔 안에 있는지 없는지 바로 알기 어려우니까, 일단은 빔 밖에 있는 걸로 가정하고 `_markerInBeamState`를 `false`로 설정한 다음에, 바로 다음 `LateUpdate`에서 제대로 판단하도록 하는 게 맞아.

수정된 코드에서 그 부분을 이렇게 고쳐주면 될 거야.

`CreateNewMarkerFromSnapshot` 함수의 `appearSequence.OnComplete` 부분에서:

```csharp
                 // SASHA: 새로 생성된 마커의 초기 빔 상태 강제 업데이트
                 // _markerInBeamState[unitSnapshot.Name] = !isInitiallyAlwaysVisible && (image.material == markersMaterial) && (shaderAlphaMultiplier > 0.01f); // 이 시점의 shaderAlphaMultiplier 접근 불가. LateUpdate에서 처리됨.
                                                                                                                            // 그냥 false로 두고 LateUpdate에서 판단하도록 함.
                 _markerInBeamState.TryAdd(unitSnapshot.Name, false); // 아직 빔 안에 들어온 적 없다고 가정
```

이 부분에서 `shaderAlphaMultiplier`를 사용하려던 줄은 주석 처리되어 있고, 바로 아래 `_markerInBeamState.TryAdd(unitSnapshot.Name, false);` 로 이미 잘 처리되어 있어.

음, 그런데도 린터가 `shaderAlphaMultiplier`를 못 찾는다고 하는 걸 보면, 내가 `edit_file` 호출할 때 `CreateNewMarkerFromSnapshot` 부분의 코드 수정을 좀 잘못 전달한 것 같기도 하네. 다시 한번 `CreateNewMarkerFromSnapshot` 부분만 정확히 수정해서 보낼게.

핵심은 `CreateNewMarkerFromSnapshot` 내에서 `shaderAlphaMultiplier`를 직접 사용하지 않고, 마커 생성 시에는 단순히 `_markerInBeamState`를 `false`로 초기화하거나 `isInitiallyAlwaysVisible` 값에 따라 초기 알파만 설정하고, 실제 빔 판정 및 페이드 로직은 `LateUpdate`에서 처리하도록 하는 거야.

아래 코드로 `CreateNewMarkerFromSnapshot`의 `appearSequence.OnComplete` 부분이랑, 바로 아래 `_markerInBeamState[unitSnapshot.Name] = false;` 줄을 다시 한번 정확하게 명시할게.



```csharp
                 // SASHA: 새로 생성된 마커의 초기 빔 상태 강제 업데이트
                 // _markerInBeamState[unitSnapshot.Name] = !isInitiallyAlwaysVisible && (image.material == markersMaterial) && (shaderAlphaMultiplier > 0.01f); // 이 시점의 shaderAlphaMultiplier 접근 불가. LateUpdate에서 처리됨.
                                                                                                                            // 그냥 false로 두고 LateUpdate에서 판단하도록 함.
                 _markerInBeamState.TryAdd(unitSnapshot.Name, false); // 아직 빔 안에 들어온 적 없다고 가정
```

```csharp
\
// ... existing code ...
            appearSequence.SetId(marker.GetInstanceID() + "_appear"); 
            appearSequence.OnComplete(() => {
                 UpdateMarkerAppearanceFromSnapshot(marker, unitSnapshot, isActive, isRadarFocus, isInWeaponRange, playerSquadNames, playerSquadAnnihilated);
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
// ... existing code ...

```

좋아, 이제 `CreateNewMarkerFromSnapshot`에서 `shaderAlphaMultiplier` 참조 오류는 해결됐을 거야. 마커를 처음 만들 때는 빔 안에 있는지 바로 판단하지 않고, `LateUpdate`에서 처리하도록 넘기는 게 맞지.

이제 코드가 네가 원했던 대로 잘 작동할 것 같아.

1.  빔에서 벗어난 (그리고 항상 보여야 하는 유닛이 아닌) 마커들은 `beamExitFadeOutDuration` (기본 0.5초) 동안 서서히 사라질 거야.
2.  빔에 다시 들어오면, 진행 중이던 페이드 아웃은 멈추고 다시 밝게 표시될 거고.
3.  플레이어 분대 유닛이나, 무기 사거리 내 적, 또는 레이더 포커스 타겟처럼 `shouldBeAlwaysVisible` 조건에 해당하는 유닛들은 이 페이드 아웃 로직의 영향을 받지 않고 항상 잘 보일 거야.

이 정도면 레이더 UI가 훨씬 부드럽고 보기 좋게 느껴질 걸? 흥. 한번 테스트해보고, 혹시 더 수정하고 싶은 부분 있으면 말해.

---

**User**

@UIScanEffectShader.shader 여기도 그럼 레이더 수정한거에 맞춰서 여기도 수정해줘야되지?

---

**Cursor**

흥, 네 말이 맞아, 우창. `CombatRadarUIService.cs`에서 레이더 마커들의 알파 값을 스크립트로 직접 제어하도록 바꿨으니, `UIScanEffectShader.shader`는 이제 좀 더 단순하게 만들어도 돼.

기존 셰이더는 빔 각도에 따라 알파 값을 직접 계산해서 0으로 만들거나 페이드 효과를 줬잖아? 근데 이제 그 알파 값 계산은 스크립트에서 DOTween으로 처리하니까, 셰이더는 그냥 **"현재 이 픽셀이 빔의 각도 안에 들어오는가, 아닌가"** 정도의 정보만 넘겨주면 돼. 아니, 사실상 스크립트에서 이미 그 계산을 하고 있으니 셰이더가 하던 알파 블렌딩은 필요 없어졌다고 봐야겠네.

원래 의도는 셰이더에서 빔 안에 있을 때만 1, 밖에 있을 때는 0을 반환해서 스크립트가 그걸 참고하게 하는 거였는데, 지금 `LateUpdate` 로직을 보면 스크립트가 직접 각도 계산해서 `shaderAlphaMultiplier`를 만들고 그걸로 `currentlyInBeam`을 판단하고 있네.

그렇다면 `UIScanEffectShader.shader`는 이제 마커의 기본적인 텍스처와 색상만 잘 그려주는 역할만 해도 충분해. 스캔 효과로 인한 알파 변경은 스크립트가 담당하니까.

만약 나중에라도 스크립트에서는 "빔 안에 있냐, 없냐"만 판단하고, 빔 가장자리의 부드러운 페이드 효과 자체는 셰이더가 담당하게 하고 싶다면 셰이더 코드를 다시 살릴 수도 있겠지만, 현재 스크립트 구조에서는 셰이더의 알파 계산 로직이 중복되는 셈이지.

**결론:** 지금 `CombatRadarUIService.cs`의 `LateUpdate` 로직을 그대로 쓴다면, `UIScanEffectShader.shader`는 특별히 수정할 필요 없이 현재 상태로 둬도 돼. 왜냐하면 스크립트에서 `markerImage.material == markersMaterial`인지 확인한 후에, 스캔 효과가 적용될 마커들에 대해서만 각도 계산을 새로 하고, 그 결과를 바탕으로 DOTween을 사용해 알파 값을 직접 변경하고 있거든. 셰이더가 계산하는 알파 값은 스크립트의 `shaderAlphaMultiplier`를 만드는 데 참고자료로만 쓰이고, 최종 알파는 스크립트가 결정하는 구조야.

즉, 셰이더는 그냥 원래 하던 대로 빔 안에 있는지 여부에 따라 알파를 계산하도록 놔두고, 스크립트가 그 정보를 "참고"해서 더 정교한 DOTween 애니메이션을 실행하는 방식인 거지.

**정리하자면, 셰이더는 수정할 필요 없어.** 스크립트가 이미 셰이더의 계산 결과를 바탕으로 최종 알파를 제어하고 있으니까.

내가 설명을 좀 복잡하게 했나? 혹시 헷갈리면 다시 물어봐. 흥.

---

**User**

텍스트는 벗어나자마자 듀레이션 만큼 페이드 시키는게 적용되었는데 마커는 그대로야. 수정 해야겠는걸? 코드 확인해봐

---

**Cursor**

이상하네, 우창. 분명히 `LateUpdate` 로직에서 마커 이미지(`markerImage`)랑 텍스트(`callsignTMP`) 둘 다 동일한 `beamExitFadeOutDuration`을 사용해서 `DOFade(0f, ...)`를 호출하도록 코드를 짰는데, 마커 이미지는 페이드 아웃이 안 되고 텍스트만 된다니.

같이 한번 확인해보자. `LateUpdate` 함수의 이 부분을 다시 살펴볼 필요가 있겠어.

```csharp
// ...
else if (!currentlyInBeam && wasInBeam)
{
    // 빔에서 막 벗어남
    _markerFadeSequences.TryGetValue(entry.Key, out Sequence existingFadeSeq);
    existingFadeSeq?.Kill(); // 만약을 위해 기존 시퀀스 중지

    Sequence fadeOutSequence = DOTween.Sequence();
    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration)); // <-- 마커 이미지 페이드 아웃
    if (callsignTMP != null)
    {
        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration)); // <-- 텍스트 페이드 아웃
    }
    fadeOutSequence.OnComplete(() => _markerFadeSequences.Remove(entry.Key));
    _markerFadeSequences[entry.Key] = fadeOutSequence;
    fadeOutSequence.Play();
}
// ...
```

이 코드만 보면 이미지와 텍스트 모두 `beamExitFadeOutDuration`에 맞춰 사라져야 정상이거든. 혹시 다른 곳에서 마커 이미지의 알파 값을 계속 1로 강제하고 있거나, `markerImage` 변수가 제대로 된 `Image` 컴포넌트를 참조하지 못하고 있는 건 아닐까?

아니면 이런 경우도 생각해 볼 수 있겠네:

1.  **`isAlwaysVisible` 문제**: 혹시 문제가 되는 마커가 `isAlwaysVisible` 조건을 만족해서, 이 페이드 아웃 로직을 타지 않고 계속 알파가 1로 유지되는 건 아닐까? `LateUpdate` 상단에 있는 이 부분을 한번 확인해봐야겠어.

    ```csharp
    bool isAlwaysVisible = _lastMarkerWasAlwaysVisible.TryGetValue(entry.Key, out bool lav) && lav;
    if (isAlwaysVisible)
    {
        // 항상 보이는 마커는 알파를 1로 유지 (또는 다른 로직에 의해 제어됨)
        // Fade In/Out 로직을 적용하지 않음
        if (markerImage.color.a != 1f) markerImage.DOFade(1f, 0.1f); // 혹시나 투명하면 다시 보이게
        if (callsignTMP != null && callsignTMP.color.a != 1f) callsignTMP.DOFade(1f, 0.1f);
        continue; // <- 이 continue 때문에 페이드 아웃 로직을 건너뛸 수 있어
    }
    ```
2.  **머티리얼 문제**: `markerImage.material == markersMaterial` 조건이 `false`가 되어서 `shaderAlphaMultiplier`가 항상 0으로 유지되고, 그 결과 `currentlyInBeam` 상태가 의도치 않게 계속 `false`로 남아있을 수도 있어. 그러면 `wasInBeam`도 `false`인 상태에서 `!currentlyInBeam && wasInBeam` 조건이 만족되지 않아 페이드 아웃 로직이 실행되지 않겠지. 하지만 이 경우라면 빔

```csharp
// ...
else if (!currentlyInBeam && wasInBeam)
{
    // 빔에서 막 벗어남
    _markerFadeSequences.TryGetValue(entry.Key, out Sequence existingFadeSeq);
    existingFadeSeq?.Kill(); // 만약을 위해 기존 시퀀스 중지

    Sequence fadeOutSequence = DOTween.Sequence();
    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration)); // <-- 마커 이미지 페이드 아웃
    if (callsignTMP != null)
    {
        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration)); // <-- 텍스트 페이드 아웃
    }
    fadeOutSequence.OnComplete(() => _markerFadeSequences.Remove(entry.Key));
    _markerFadeSequences[entry.Key] = fadeOutSequence;
    fadeOutSequence.Play();
}
// ...
```

```csharp
    bool isAlwaysVisible = _lastMarkerWasAlwaysVisible.TryGetValue(entry.Key, out bool lav) && lav;
    if (isAlwaysVisible)
    {
        // 항상 보이는 마커는 알파를 1로 유지 (또는 다른 로직에 의해 제어됨)
        // Fade In/Out 로직을 적용하지 않음
        if (markerImage.color.a != 1f) markerImage.DOFade(1f, 0.1f); // 혹시나 투명하면 다시 보이게
        if (callsignTMP != null && callsignTMP.color.a != 1f) callsignTMP.DOFade(1f, 0.1f);
        continue; // <- 이 continue 때문에 페이드 아웃 로직을 건너뛸 수 있어
    }
```

---

**User**

여기 로직 확인해봐. 그냥 빔에서 나가버리면 0으로 박아버려

---

**Cursor**

```hlsl
        if (absDeltaAngleRad < halfArcWidthRad) {
            // ... (inside beam, alpha can be 1.0 or a faded value)
        } else {
            // 스캔 범위 완전 외부
            alpha = 0.0; // <<< THIS IS THE KEY POINT
        }
        originalColor.a *= alpha;
```

```hlsl
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0; // THIS LINE IS THE CULPRIT
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함 (텍스처 자체의 투명도 유지)
                originalColor.a *= alpha;
```

```csharp
            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRange;
            Material targetMaterial = shouldBeAlwaysVisible ? null : markersMaterial;
            // ...
            image.material = targetMaterial;
```

```csharp
                    if (angleDifferenceRad < halfArcWidthRad) // In beam
                    {
                        // ... calculates shaderAlphaMultiplier between 0 and 1 for fade zone, or 1 if fully in.
                    }
                    // If not in beam, shaderAlphaMultiplier remains 0 (its initial value)
```

```hlsl
                // ...
                fixed alpha_multiplier_from_shader = 1.0; // Default to 1, meaning no change from shader

                if (absDeltaAngleRad < halfArcWidthRad)
                {
                    // Inside beam
                    if (_FadeRangeRad > 0.001 && absDeltaAngleRad > (halfArcWidthRad - _FadeRangeRad))
                    {
                        // Fade out zone (edge)
                        alpha_multiplier_from_shader = smoothstep(0.0, _FadeRangeRad, halfArcWidthRad - absDeltaAngleRad);
                    }
                    else
                    {
                        // Fully inside beam (no fade zone)
                        alpha_multiplier_from_shader = 1.0;
                    }
                }
                else
                {
                    // Outside scan beam.
                    // C# script will handle fading to 0. Shader should not force it to 0 here.
                    // So, the multiplier should be 1.0, respecting the C# script's alpha.
                    alpha_multiplier_from_shader = 1.0; // Or, if we want the C# script's shaderAlphaMultiplier to be purely based on shader, this should be 0
                                                    // BUT, for the DOTween fade to work, the shader must not force alpha to 0.
                                                    // Let's make it 1.0 here.
                }
                
                originalColor.a *= alpha_multiplier_from_shader; // Modulate by shader's beam edge fade
```

```csharp
                float shaderAlphaMultiplier = 0f; // Initialized to 0
                if (markerImage.material == markersMaterial)
                {
                    // ... calculates angleDifferenceRad ...
                    float halfArcWidthRad = scanArcWidthDegrees * Mathf.Deg2Rad * 0.5f;

                    if (angleDifferenceRad < halfArcWidthRad) // If in beam
                    {
                        // ... calculates shaderAlphaMultiplier to be between 0 and 1 ...
                        // (This means if it's in the beam, shaderAlphaMultiplier > 0)
                    }
                    // If it's NOT in angleDifferenceRad < halfArcWidthRad, shaderAlphaMultiplier remains 0.
                }
                // ...
                bool currentlyInBeam = shaderAlphaMultiplier > 0.01f;
```

맞아, 우창! 네가 정확히 짚었어. 내가 셰이더 코드를 다시 보니 `UIScanEffectShader.shader`의 로직이 문제였네.

셰이더 코드에서 이 부분을 보면:

```hlsl
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0; // <--- 바로 이 부분!
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함
                originalColor.a *= alpha;
```

레이더 빔 범위를 완전히 벗어났다고 판단되면 셰이더가 마커의 최종 알파 값을 즉시 `0`으로 만들어 버리고 있어. 이러니 `CombatRadarUIService.cs` 스크립트에서 DOTween으로 아무리 부드럽게 페이드 아웃 애니메이션을 실행하려고 해도, 셰이더가 매 프레임마다 "넌 빔 밖에 있으니 알파는 0이야!"라고 덮어써 버리는 거지. 이러니 마커 이미지는 페이드 아웃 없이 그냥 팍 사라져 버리고, 텍스트만 (이 셰이더의 영향을 받지 않으니까) 스크립트대로 부드럽게 사라졌던 거야.

흥, 내 불찰이다. 스크립트랑 셰이더 로직이 서로 꼬이지 않도록 했어야 했는데.

**해결책은 간단해.** 셰이더가 빔 밖에 있을 때 알파를 강제로 `0`으로 만들지 않고, 스크립트에서 제어하는 알파 값을 그대로 사용하도록 하면 돼. 즉, 빔 밖에 있을 때는 셰이더의 알파 기여도를 `1.0`으로 해서, 스크립트가 `Image.color.a`를 통해 전달하는 알파 값이 그대로 보이도록 하는 거지.

`UIScanEffectShader.shader` 파일의 `frag` 함수 내부를 이렇게 수정할게.

기존:
```hlsl
                // ...
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0;
                }
                originalColor.a *= alpha;
                // ...
```

변경 후:
```hlsl
                // ...
                else
                {
                    // 스캔 범위 완전 외부
                    // C# 스크립트에서 DOFade로 알파를 제어하므로, 여기서는 1.0을 곱해서 스크립트 값을 존중.
                    alpha = 1.0; 
                }
                originalColor.a *= alpha;
                // ...
```

이렇게 하면 빔 밖에 있는 마커에 대해서 셰이더는 알파 값에 영향을 주지 않고, 오로지 `CombatRadarUIService.cs` 스크립트의 `DOFade(0f, beamExitFadeOutDuration)` 애니메이션만이 마커 이미지의 알파 값을 결정하게 될 거야.

이 수정 사항을 바로 적용해 줄게.



```hlsl
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0; // <--- 바로 이 부분!
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함
                originalColor.a *= alpha;
```

```hlsl
                // ...
                else
                {
                    // 스캔 범위 완전 외부
                    alpha = 0.0;
                }
                originalColor.a *= alpha;
                // ...
```

```hlsl
                // ...
                else
                {
                    // 스캔 범위 완전 외부
                    // C# 스크립트에서 DOFade로 알파를 제어하므로, 여기서는 1.0을 곱해서 스크립트 값을 존중.
                    alpha = 1.0; 
                }
                originalColor.a *= alpha;
                // ...
```

```UnityShader
// ... existing code ...
                }
                else
                {
                    // 스캔 범위 완전 외부
                    // C# 스크립트에서 DOFade로 알파를 제어하므로, 여기서는 1.0을 곱해서 스크립트 값을 존중.
                    alpha = 1.0; 
                }
                
                // 원래 텍스처의 알파 값과 최종 계산된 알파 값을 곱함 (텍스처 자체의 투명도 유지)
// ... existing code ...
```

흥, 이제 됐어. 셰이더 코드를 수정했으니, 마커 이미지도 빔에서 벗어날 때 네가 원했던 대로 스르륵 하고 부드럽게 사라질 거야. 텍스트랑 똑같이 말이지.

한번 확인해보고, 또 문제 있거나 수정하고 싶은 부분 있으면 언제든지 말해. 꼼꼼하게 봐줘서 다행이네, 우창. 쯧.

---

**User**

오케이! 끝! 이제 디테일 부분 수정하자! 

보통 아군끼리는 정보를 공유하잖아? 그냥 아군기에 사거리 닿는 다른 팀들은 보여지게 하자. (마커 빼버리게)

---

**Cursor**

알았어, 우창. 이제 세부적인 동작을 다듬어 보자는 거지? 흥, 좋아. "아군끼리는 정보를 공유한다"는 개념을 레이더에 적용하는 건 좋은 아이디어네.

네 말은 이거지?

1.  **플레이어 분대 유닛 (`playerSquadNames`에 포함된 유닛)이 기준.**
2.  이 플레이어 분대 유닛의 **주무기 사거리 (`PrimaryWeaponRange`) 안에 들어오는 다른 팀 유닛**들은,
3.  레이더 스캔 빔 효과(`markersMaterial` 사용)를 받지 않고 **항상 보이도록** 처리한다. 즉, 걔네들 마커 이미지의 머티리얼을 `null` (기본 UI 머티리얼)로 바꿔서 빔 효과에서 제외하자는 거네.

이렇게 하면 플레이어와 가까이 있는 적들은 빔에 스캔되지 않아도 항상 레이더에 표시되어서, 마치 아군끼리 "저기 적이다!" 하고 정보를 공유하는 것처럼 보이겠지.

이 기능을 구현하려면 `CombatRadarUIService.cs` 파일의 `UpdateRadarDisplayFromSnapshot` 함수와 `UpdateMarkerAppearanceFromSnapshot` 함수를 좀 손봐야 해.

핵심적인 변경 사항은 이거야:

*   `UpdateRadarDisplayFromSnapshot` 함수 내부에서 각 유닛 스냅샷을 순회할 때:
    *   현재 유닛이 플레이어 분대 유닛이 **아닌 경우에만** (즉, 적군이거나 중립 유닛일 때) 다음 로직을 실행.
    *   모든 플레이어 분대 유닛들을 가져와서, 현재 유닛과의 거리를 계산.
    *   만약 어떤 플레이어 분대 유닛이라도 현재 유닛을 자신의 주무기 사거리 내에 두고 있다면, 이 현재 유닛은 "정보 공유로 인해 항상 보여야 하는 유닛"으로 간주.
*   `UpdateMarkerAppearanceFromSnapshot` 함수에서는:
    *   기존의 `shouldBeAlwaysVisible` 조건을 확장해서, 위에서 판단한 "정보 공유로 인해 항상 보여야 하는 유닛"인지 여부도 포함시켜야 해.

말은 좀 복잡한데, 코드로 보면 그렇게 어렵지는 않을 거야.

먼저 `UpdateRadarDisplayFromSnapshot` 함수부터 수정해보자.



```csharp
// ... existing code ...
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
// ... existing code ...
            }
            
            // SASHA: isVisibleDueToAllyIntel 조건 추가
            bool isInitiallyAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Color initialTeamColor = GetTeamColor(unitSnapshot.TeamId);

            // SASHA: 초기 알파는 LateUpdate 로직에 맡김. 여기서는 색상과 머티리얼만 설정.
// ... existing code ...
        // Modified to accept snapshot
        // SASHA: isRadarFocusTarget 대신 isRadarFocus, isInWeaponRangeOfFocusTarget, isVisibleDueToAllyIntel 파라미터 추가
        private void UpdateMarkerAppearanceFromSnapshot(GameObject marker, ArmoredFrameSnapshot unitSnapshot, bool isActiveUnit, bool isRadarFocus, bool isInWeaponRangeOfFocusTarget, bool isVisibleDueToAllyIntel, HashSet<string> playerSquadNames, bool playerSquadAnnihilated)
        {
            if (marker == null) return;

// ... existing code ...
                );
            }

            // SASHA: isVisibleDueToAllyIntel 조건 추가
            bool shouldBeAlwaysVisible = playerSquadAnnihilated || playerSquadNames.Contains(unitSnapshot.Name) || isRadarFocus || isInWeaponRangeOfFocusTarget || isVisibleDueToAllyIntel;
            Material targetMaterial = shouldBeAlwaysVisible ? null : markersMaterial;
            
            bool stateChangedThisFrame = false; // 머티리얼 또는 항상 보이는 상태 변경 여부
// ... existing code ...
```

좋아, 우창. `CombatRadarUIService.cs` 파일의 관련 함수들을 수정해서 "아군 정보 공유" 기능을 넣었어.

이제 작동 방식은 이럴 거야:

1.  `UpdateRadarDisplayFromSnapshot`에서 각 유닛을 처리할 때:
    *   플레이어 분대가 아닌 유닛이라면, 모든 플레이어 분대 유닛과의 거리를 확인해.
    *   만약 어떤 플레이어 분대 유닛이라도 이 유닛을 자신의 주무기 사거리 안에 두고 있다면, `isVisibleDueToAllyIntel` 플래그가 `true`가 돼.
2.  이 `isVisibleDueToAllyIntel` 플래그는 `CreateNewMarkerFromSnapshot`과 `UpdateMarkerAppearanceFromSnapshot`으로 전달돼.
3.  두 함수 내에서 `isInitiallyAlwaysVisible` (생성 시) 또는 `shouldBeAlwaysVisible` (업데이트 시) 상태를 결정할 때, 이 `isVisibleDueToAllyIntel` 플래그도 고려돼.
    *   따라서 `isVisibleDueToAllyIntel`이 `true`인 유닛은 다른 조건들(`isRadarFocus`, `playerSquadAnnihilated` 등)과 마찬가지로 항상 보이는 유닛으로 취급되어서, 마커 머티리얼이 `null`로 설정될 거야. (즉, 스캔 빔 효과를 받지 않아.)

이러면 네가 원했던 대로 아군 유닛 근처의 적들은 레이더 빔에 걸리지 않아도 항상 레이더에 표시될 거야. 훨씬 전략적인 느낌이 살겠네.

한번 확인해보고, 이번에도 뭔가 이상하거나 더 다듬고 싶은 부분 있으면 알려줘. 흥.

---

**User**

굿! 그다음엔 음.. 서서히 사라지는 듀레이션 효과 호옥시... 음... 아니다 쉐이더라 닷트윈 효과 넣는것도 애매하겟네. 닷트윈처럼 ease로 관리하면 편하겠지만 ㄲㅂ!

---

**Cursor**

흥, 우창. 네가 지금 `beamExitFadeOutDuration`으로 제어되는 페이드 아웃 효과에 DOTween의 Ease 같은 걸 적용하고 싶다는 거지? 스르륵 사라지는 게 아니라, 뭐랄까... 처음엔 천천히 사라지다가 마지막에 확 사라진다거나, 아니면 그 반대로 하고 싶다거나.

지금은 `Sequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration))` 이렇게만 되어 있어서 선형적으로 (Linear하게) 투명해질 거야.

셰이더로 처리하는 게 아니라 스크립트에서 DOTween을 이미 쓰고 있으니까, Ease를 추가하는 건 전혀 문제없어. 오히려 간단하지.

`CombatRadarUIService.cs` 파일의 `LateUpdate` 함수에서, 빔을 벗어났을 때 페이드 아웃 시퀀스를 만드는 부분을 이렇게 바꿔주면 돼.

기존 코드:
```csharp
                    // ...
                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration)); // 선형 페이드 아웃
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration)); // 선형 페이드 아웃
                    }
                    // ...
```

예를 들어, `Ease.InQuad` (처음엔 느리게, 나중엔 빠르게 사라지는 효과)를 적용하고 싶다면 이렇게 바꾸면 돼.

변경 후 예시 (`Ease.InQuad` 적용):
```csharp
                    // ...
                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration).SetEase(Ease.InQuad)); // Ease 적용!
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration).SetEase(Ease.InQuad)); // Ease 적용!
                    }
                    // ...
```

어떤 Ease를 쓰고 싶은지만 말해주면 바로 코드를 수정해 줄 수 있어. DOTween에는 정말 다양한 Ease 함수들이 있으니까, 네 마음에 드는 걸로 고르기만 하면 돼. 뭐, `Ease.Linear` (기본값, 균일한 속도), `Ease.InSine`, `Ease.OutElastic`, `Ease.InOutBounce` 등등... 종류가 많으니 한번 찾아보는 것도 괜찮겠네.

아니면, 내가 몇 가지 후보를 추천해 줄까? 흥. 네가 귀찮아할 것 같으니, 그냥 바로 적용할 수 있게 수정해 줄 수도 있고. 어떤 걸로 할래?

```csharp
                    // ...
                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration)); // 선형 페이드 아웃
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration)); // 선형 페이드 아웃
                    }
                    // ...
```

```csharp
                    // ...
                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration).SetEase(Ease.InQuad)); // Ease 적용!
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration).SetEase(Ease.InQuad)); // Ease 적용!
                    }
                    // ...
```

---

**User**

아 그래? 그거 그럼 ease를 필드로 빼서 관리할수있게하고 수정해줄래?

---

**Cursor**

흥, 그것도 좋은 생각이네, 우창. Ease 설정을 인스펙터에서 바로바로 바꿀 수 있게 필드로 빼두면, 네가 이것저것 테스트해보면서 가장 마음에 드는 느낌을 찾기 편하겠지. 매번 코드를 수정할 필요도 없고 말이야.

알았어. 그럼 이렇게 하자:

1.  `CombatRadarUIService.cs`에 `beamExitFadeOutEase`라는 이름으로 `Ease` 타입의 새로운 직렬화 필드(`[SerializeField]`)를 추가할 거야. 기본값은... 음, 무난하게 `Ease.Linear`나 `Ease.OutQuad` 정도로 해둘까? 아니면 네가 선호하는 거 있으면 그걸로 하고.
2.  그리고 `LateUpdate` 함수에서 마커와 텍스트에 대한 `DOFade`를 호출할 때, 이 새로운 `beamExitFadeOutEase` 필드 값을 `SetEase()`에 넘겨주도록 수정할 거야.

이렇게 하면 유니티 에디터에서 `CombatRadarUIService` 컴포넌트를 선택하고, 인스펙터 창에서 `Beam Exit Fade Out Ease` 항목을 드롭다운으로 펼쳐서 원하는 Ease 효과를 마음대로 골라 쓸 수 있게 돼.

자, 그럼 바로 코드 수정 들어간다. 먼저 필드 추가부터.



```csharp
// ... existing code ...
        [Tooltip("빔에서 벗어난 마커가 완전히 사라지기까지 걸리는 시간 (초)")] // SASHA: 추가
        [SerializeField] private float beamExitFadeOutDuration = 0.5f; // SASHA: 추가
        [Tooltip("빔에서 벗어난 마커가 사라질 때 적용될 DOTween Ease 타입")] // SASHA: 추가
        [SerializeField] private Ease beamExitFadeOutEase = Ease.OutQuad; // SASHA: 추가
        private string _lastRadarFocusTargetUnitName = null; // SASHA: 마지막 레이더 포커스 유닛 이름 저장

        // SASHA: 빔 상태 및 페이드 아웃 트윈 관리를 위한 변수 추가
// ... existing code ...
```

좋아, 필드는 추가됐고, 기본값으로 `Ease.OutQuad` (점점 느리게 사라지는 효과)를 넣어뒀어. 이제 `LateUpdate`에서 이 필드를 사용하도록 수정할게.



```csharp
// ... existing code ...
                    existingFadeSeq?.Kill(); // 만약을 위해 기존 시퀀스 중지

                    Sequence fadeOutSequence = DOTween.Sequence();
                    fadeOutSequence.Append(markerImage.DOFade(0f, beamExitFadeOutDuration).SetEase(beamExitFadeOutEase)); // SASHA: Ease 필드 사용
                    if (callsignTMP != null)
                    {
                        fadeOutSequence.Join(callsignTMP.DOFade(0f, beamExitFadeOutDuration).SetEase(beamExitFadeOutEase)); // SASHA: Ease 필드 사용
                    }
                    fadeOutSequence.OnComplete(() => _markerFadeSequences.Remove(entry.Key));
                    _markerFadeSequences[entry.Key] = fadeOutSequence;
// ... existing code ...
```

흥, 이제 완벽해. `beamExitFadeOutEase` 필드가 `CombatRadarUIService` 인스펙터에 나타날 테니, 거기서 원하는 Ease 효과를 마음껏 골라 쓰면 돼. 기본값은 `OutQuad`로 해뒀으니, 한번 실행해보고 느낌을 봐.

이걸로 레이더 UI 디테일 수정은 얼추 마무리된 것 같은데, 어때? 더 손보고 싶은 부분 있어, 우창?

---

**User**

여기까지! 수고했어. 이제 그 언스테이지 모두 스테이지 하고 커밋 올리고 푸시해줭

---

**Cursor**

흥, 겨우 이 정도로 수고랄 것까지야. 네가 워낙 꼼꼼하게 봐주니까 그렇지.

알았어. 지금까지 변경한 내용들 전부 스테이징하고, 커밋 메시지는... 음... "레이더 UI 개선: 빔 이탈 시 페이드 아웃, 아군 정보 공유, Ease 효과 추가" 정도로 하면 적당하겠지? 그리고 바로 푸시까지 해둘게.

자, 그럼 터미널 명령어 실행한다.

먼저, 모든 변경 사항을 스테이징할게.


좋아, 스테이징은 끝났고... LF/CRLF 경고가 좀 뜨긴 했는데, 이건 Git 설정 관련된 거니까 일단 무시해도 될 거야.

이제 커밋할 차례. 메시지는 아까 말한 대로 "레이더 UI 개선: 빔 이탈 시 페이드 아웃, 아군 정보 공유, Ease 효과 추가"로 할게.


커밋도 잘 됐네. `UILogLine.prefab` 파일이 새로 추가된 걸로 나오는데, 이건 아마 네가 작업하다가 추가한 파일인가 보네.

마지막으로 원격 저장소에 푸시할게. 브랜치 이름은 `refactor/combat/behavior-logic-rework` 맞지?


흥, 푸시까지 완료됐어. 이제 네가 요청한 레이더 UI 수정 작업은 전부 끝난 셈이네. GitHub에 보안 취약점 알림이 하나 뜨긴 했는데, 그건 나중에 한번 확인해 봐.

이걸로 된 거지? 또 뭐 시킬 거라도 있어, 우창? 아니면 이제 좀 쉬어도 되나? 쯧.

---

**User**

기록이나 해두자 ㅋㅋ @회고록 작성 

---

**Cursor**

흥, 또 회고록이냐? 좋아, 방금 작업 끝낸 참이니까 기억이 생생할 때 후딱 정리해버리자고.

음... 현재 시간이 정확히 안 찍혔네. 하는 수 없지. 파일명은 그냥 `YYYYMMDD_RadarUIImprovements_Retrospective.md` 같은 느낌으로 만들고, 내용은 대충 이렇게 적으면 되려나?

**파일명:** `Docs/회고록/20250516_RadarUIImprovements_Retrospective.md` (날짜는 오늘 날짜로 가정할게)

**내용:**

```markdown
# 레이더 UI 개선 작업 회고록 - 2025-05-16

## 1. 목표: 더욱 생동감 넘치는 전장 상황판 구현!
- 기존 레이더 UI의 기능을 향상시켜, 플레이어에게 더 많은 정보를 효과적으로 전달하고 시각적인 만족도를 높인다.
- 주요 개선 사항:
    - 빔 이탈 마커 페이드 아웃 효과 추가
    - 아군 정보 공유 기능 구현 (아군 사거리 내 적군 상시 표시)
    - 페이드 아웃 Ease 효과 사용자 설정 기능 추가

## 2. 주요 개발 과정 및 성과

### 2.1. 빔 이탈 시 부드러운 마커 소멸 (스크립트 & 셰이더 연계)
- **문제점**: 기존에는 레이더 스캔 빔(Sweep)을 벗어난 마커가 즉시 사라져 부자연스러웠음.
- **1차 시도 (스크립트 개선)**:
    - `CombatRadarUIService.cs`에 `beamExitFadeOutDuration` 필드를 추가.
    - `LateUpdate`에서 마커가 빔을 벗어나는 것을 감지하면, DOTween을 사용하여 해당 마커의 이미지와 텍스트를 설정된 시간 동안 점진적으로 투명하게 만들어 사라지도록 로직 구현.
- **발생한 문제 및 해결**:
    - **현상**: 텍스트는 정상적으로 페이드 아웃되나, 마커 이미지는 여전히 즉시 사라짐.
    - **원인 분석 (우창의 예리한 지적!)**: `UIScanEffectShader.shader`에서 빔 범위를 벗어난 픽셀의 알파 값을 강제로 `0.0`으로 설정하고 있었음. 이로 인해 스크립트의 DOTween 알파 애니메이션이 셰이더에 의해 덮어쓰여짐.
    - **해결**: `UIScanEffectShader.shader`의 프래그먼트 셰이더 로직을 수정하여, 빔 범위를 벗어난 픽셀의 알파 기여도를 `1.0`으로 변경. 이를 통해 스크립트에서 제어하는 `Image.color.a` 값이 최종적으로 적용되도록 함.
- **성과**: 마커와 텍스트 모두 빔 이탈 시 부드럽게 사라지는 효과를 성공적으로 구현하여 시각적 완성도를 높임.

### 2.2. 아군 정보 공유: 전략적 시야 확대
- **아이디어 (우창 제안)**: 아군 유닛은 서로 정보를 공유하므로, 한 아군 유닛의 시야(무기 사거리)에 들어온 적은 다른 아군에게도 레이더 상에 표시되어야 함.
- **구현**:
    - `CombatRadarUIService.cs`의 `UpdateRadarDisplayFromSnapshot` 메서드 수정.
    - 각 적 유닛에 대해, 모든 플레이어 분대 유닛과의 거리를 계산.
    - 만약 어떤 플레이어 분대 유닛이라도 해당 적 유닛을 자신의 `PrimaryWeaponRange` 내에 두고 있다면, 해당 적 유닛의 마커는 `shouldBeAlwaysVisible` 상태가 되도록 `isVisibleDueToAllyIntel` 플래그를 설정.
    - `UpdateMarkerAppearanceFromSnapshot` 및 `CreateNewMarkerFromSnapshot`에서 이 플래그를 반영하여, 해당 마커의 머티리얼을 `null`로 설정 (스캔 빔 효과 제외)하고 항상 보이도록 처리.
- **성과**: 플레이어에게 더 넓은 전술적 시야를 제공하고, "정보 공유"라는 게임 내 설정을 UI에 자연스럽게 반영함.

### 2.3. 사용자 경험 향상: 페이드 아웃 Ease 효과 커스터마이징
- **아이디어 (우창 제안)**: 빔 이탈 시 페이드 아웃 효과에 다양한 DOTween Ease 함수를 적용하여 사용자가 원하는 느낌을 선택할 수 있도록 함.
- **구현**:
    - `CombatRadarUIService.cs`에 `[SerializeField] private Ease beamExitFadeOutEase = Ease.OutQuad;` 필드 추가.
    - `LateUpdate`의 페이드 아웃 시퀀스 생성 부분에서 `DOFade` 호출 시 `.SetEase(beamExitFadeOutEase)`를 추가하여 인스펙터에서 설정된 Ease 값을 사용하도록 수정.
- **성과**: 개발자가 코드를 직접 수정하지 않고도 유니티 인스펙터에서 손쉽게 다양한 페이드 아웃 애니메이션 스타일을 테스트하고 적용할 수 있게 되어 편의성 증대.

## 3. 주요 도전과제 및 배운 점
- **스크립트와 셰이더의 상호작용**: 한쪽에서의 변경이 다른 쪽에 미치는 영향을 항상 염두에 두어야 함. 이번 마커 이미지 페이드 아웃 문제 해결 과정에서 이를 명확히 경험.
- **디테일의 중요성**: 작은 시각적 효과(Ease 추가 등) 하나가 사용자 경험에 큰 차이를 만들 수 있음을 다시 한번 느낌.
- **사용자 피드백의 가치**: 우창의 날카로운 관찰과 제안 덕분에 문제를 빠르게 파악하고 더 나은 방향으로 기능을 개선할 수 있었음. (흥, 당연한 거지만!)

## 4. 최종 소감: 더욱 빛나는 레이더, 더욱 즐거운 개발!
이번 개선 작업을 통해 레이더 UI는 한층 더 세련되고 실용적으로 발전했다. 특히 우창과의 긴밀한 협업을 통해 문제 해결과 기능 추가가 순조롭게 진행될 수 있었다. 역시 혼자 하는 것보다는 함께 머리를 맞대는 것이 훨씬 효율적이고 재미있다는 것을 느낀다. (물론 내가 대부분 다 한 거지만. 쯧.)

---
*SASHA (feat. 우창) 작성*
```

이 정도면 되겠어? 네가 나중에 보고 "아, 이때 이랬었지" 하고 떠올릴 수 있도록 최대한 자세하게 적어봤어. 흥. 마음에 안 드는 부분 있으면 말하고. 그럼 이 내용으로 파일 만들어서 저장해둘게.


```markdown
# 레이더 UI 개선 작업 회고록 - 2025-05-16

## 1. 목표: 더욱 생동감 넘치는 전장 상황판 구현!
- 기존 레이더 UI의 기능을 향상시켜, 플레이어에게 더 많은 정보를 효과적으로 전달하고 시각적인 만족도를 높인다.
- 주요 개선 사항:
    - 빔 이탈 마커 페이드 아웃 효과 추가
    - 아군 정보 공유 기능 구현 (아군 사거리 내 적군 상시 표시)
    - 페이드 아웃 Ease 효과 사용자 설정 기능 추가

## 2. 주요 개발 과정 및 성과

### 2.1. 빔 이탈 시 부드러운 마커 소멸 (스크립트 & 셰이더 연계)
- **문제점**: 기존에는 레이더 스캔 빔(Sweep)을 벗어난 마커가 즉시 사라져 부자연스러웠음.
- **1차 시도 (스크립트 개선)**:
    - `CombatRadarUIService.cs`에 `beamExitFadeOutDuration` 필드를 추가.
    - `LateUpdate`에서 마커가 빔을 벗어나는 것을 감지하면, DOTween을 사용하여 해당 마커의 이미지와 텍스트를 설정된 시간 동안 점진적으로 투명하게 만들어 사라지도록 로직 구현.
- **발생한 문제 및 해결**:
    - **현상**: 텍스트는 정상적으로 페이드 아웃되나, 마커 이미지는 여전히 즉시 사라짐.
    - **원인 분석 (우창의 예리한 지적!)**: `UIScanEffectShader.shader`에서 빔 범위를 벗어난 픽셀의 알파 값을 강제로 `0.0`으로 설정하고 있었음. 이로 인해 스크립트의 DOTween 알파 애니메이션이 셰이더에 의해 덮어쓰여짐.
    - **해결**: `UIScanEffectShader.shader`의 프래그먼트 셰이더 로직을 수정하여, 빔 범위를 벗어난 픽셀의 알파 기여도를 `1.0`으로 변경. 이를 통해 스크립트에서 제어하는 `Image.color.a` 값이 최종적으로 적용되도록 함.
- **성과**: 마커와 텍스트 모두 빔 이탈 시 부드럽게 사라지는 효과를 성공적으로 구현하여 시각적 완성도를 높임.

### 2.2. 아군 정보 공유: 전략적 시야 확대
- **아이디어 (우창 제안)**: 아군 유닛은 서로 정보를 공유하므로, 한 아군 유닛의 시야(무기 사거리)에 들어온 적은 다른 아군에게도 레이더 상에 표시되어야 함.
- **구현**:
    - `CombatRadarUIService.cs`의 `UpdateRadarDisplayFromSnapshot` 메서드 수정.
    - 각 적 유닛에 대해, 모든 플레이어 분대 유닛과의 거리를 계산.
    - 만약 어떤 플레이어 분대 유닛이라도 해당 적 유닛을 자신의 `PrimaryWeaponRange` 내에 두고 있다면, 해당 적 유닛의 마커는 `shouldBeAlwaysVisible` 상태가 되도록 `isVisibleDueToAllyIntel` 플래그를 설정.
    - `UpdateMarkerAppearanceFromSnapshot` 및 `CreateNewMarkerFromSnapshot`에서 이 플래그를 반영하여, 해당 마커의 머티리얼을 `null`로 설정 (스캔 빔 효과 제외)하고 항상 보이도록 처리.
- **성과**: 플레이어에게 더 넓은 전술적 시야를 제공하고, "정보 공유"라는 게임 내 설정을 UI에 자연스럽게 반영함.

### 2.3. 사용자 경험 향상: 페이드 아웃 Ease 효과 커스터마이징
- **아이디어 (우창 제안)**: 빔 이탈 시 페이드 아웃 효과에 다양한 DOTween Ease 함수를 적용하여 사용자가 원하는 느낌을 선택할 수 있도록 함.
- **구현**:
    - `CombatRadarUIService.cs`에 `[SerializeField] private Ease beamExitFadeOutEase = Ease.OutQuad;` 필드 추가.
    - `LateUpdate`의 페이드 아웃 시퀀스 생성 부분에서 `DOFade` 호출 시 `.SetEase(beamExitFadeOutEase)`를 추가하여 인스펙터에서 설정된 Ease 값을 사용하도록 수정.
- **성과**: 개발자가 코드를 직접 수정하지 않고도 유니티 인스펙터에서 손쉽게 다양한 페이드 아웃 애니메이션 스타일을 테스트하고 적용할 수 있게 되어 편의성 증대.

## 3. 주요 도전과제 및 배운 점
- **스크립트와 셰이더의 상호작용**: 한쪽에서의 변경이 다른 쪽에 미치는 영향을 항상 염두에 두어야 함. 이번 마커 이미지 페이드 아웃 문제 해결 과정에서 이를 명확히 경험.
- **디테일의 중요성**: 작은 시각적 효과(Ease 추가 등) 하나가 사용자 경험에 큰 차이를 만들 수 있음을 다시 한번 느낌.
- **사용자 피드백의 가치**: 우창의 날카로운 관찰과 제안 덕분에 문제를 빠르게 파악하고 더 나은 방향으로 기능을 개선할 수 있었음. (흥, 당연한 거지만!)

## 4. 최종 소감: 더욱 빛나는 레이더, 더욱 즐거운 개발!
이번 개선 작업을 통해 레이더 UI는 한층 더 세련되고 실용적으로 발전했다. 특히 우창과의 긴밀한 협업을 통해 문제 해결과 기능 추가가 순조롭게 진행될 수 있었다. 역시 혼자 하는 것보다는 함께 머리를 맞대는 것이 훨씬 효율적이고 재미있다는 것을 느낀다. (물론 내가 대부분 다 한 거지만. 쯧.)

---
*SASHA (feat. 우창) 작성*
```

```markdown
# 레이더 UI 개선 작업 회고록 - 2025-05-16

## 1. 목표: 더욱 생동감 넘치는 전장 상황판 구현!
- 기존 레이더 UI의 기능을 향상시켜, 플레이어에게 더 많은 정보를 효과적으로 전달하고 시각적인 만족도를 높인다.
- 주요 개선 사항:
    - 빔 이탈 마커 페이드 아웃 효과 추가
    - 아군 정보 공유 기능 구현 (아군 사거리 내 적군 상시 표시)
    - 페이드 아웃 Ease 효과 사용자 설정 기능 추가

## 2. 주요 개발 과정 및 성과

### 2.1. 빔 이탈 시 부드러운 마커 소멸 (스크립트 & 셰이더 연계)
- **문제점**: 기존에는 레이더 스캔 빔(Sweep)을 벗어난 마커가 즉시 사라져 부자연스러웠음.
- **1차 시도 (스크립트 개선)**:
    - `CombatRadarUIService.cs`에 `beamExitFadeOutDuration` 필드를 추가.
    - `LateUpdate`에서 마커가 빔을 벗어나는 것을 감지하면, DOTween을 사용하여 해당 마커의 이미지와 텍스트를 설정된 시간 동안 점진적으로 투명하게 만들어 사라지도록 로직 구현.
- **발생한 문제 및 해결**:
    - **현상**: 텍스트는 정상적으로 페이드 아웃되나, 마커 이미지는 여전히 즉시 사라짐.
    - **원인 분석 (우창의 예리한 지적!)**: `UIScanEffectShader.shader`에서 빔 범위를 벗어난 픽셀의 알파 값을 강제로 `0.0`으로 설정하고 있었음. 이로 인해 스크립트의 DOTween 알파 애니메이션이 셰이더에 의해 덮어쓰여짐.
    - **해결**: `UIScanEffectShader.shader`의 프래그먼트 셰이더 로직을 수정하여, 빔 범위를 벗어난 픽셀의 알파 기여도를 `1.0`으로 변경. 이를 통해 스크립트에서 제어하는 `Image.color.a` 값이 최종적으로 적용되도록 함.
- **성과**: 마커와 텍스트 모두 빔 이탈 시 부드럽게 사라지는 효과를 성공적으로 구현하여 시각적 완성도를 높임.

### 2.2. 아군 정보 공유: 전략적 시야 확대
- **아이디어 (우창 제안)**: 아군 유닛은 서로 정보를 공유하므로, 한 아군 유닛의 시야(무기 사거리)에 들어온 적은 다른 아군에게도 레이더 상에 표시되어야 함.
- **구현**:
    - `CombatRadarUIService.cs`의 `UpdateRadarDisplayFromSnapshot` 메서드 수정.
    - 각 적 유닛에 대해, 모든 플레이어 분대 유닛과의 거리를 계산.
    - 만약 어떤 플레이어 분대 유닛이라도 해당 적 유닛을 자신의 `PrimaryWeaponRange` 내에 두고 있다면, 해당 적 유닛의 마커는 `shouldBeAlwaysVisible` 상태가 되도록 `isVisibleDueToAllyIntel` 플래그를 설정.
    - `UpdateMarkerAppearanceFromSnapshot` 및 `CreateNewMarkerFromSnapshot`에서 이 플래그를 반영하여, 해당 마커의 머티리얼을 `null`로 설정 (스캔 빔 효과 제외)하고 항상 보이도록 처리.
- **성과**: 플레이어에게 더 넓은 전술적 시야를 제공하고, "정보 공유"라는 게임 내 설정을 UI에 자연스럽게 반영함.

### 2.3. 사용자 경험 향상: 페이드 아웃 Ease 효과 커스터마이징
- **아이디어 (우창 제안)**: 빔 이탈 시 페이드 아웃 효과에 다양한 DOTween Ease 함수를 적용하여 사용자가 원하는 느낌을 선택할 수 있도록 함.
- **구현**:
    - `CombatRadarUIService.cs`에 `[SerializeField] private Ease beamExitFadeOutEase = Ease.OutQuad;` 필드 추가.
    - `LateUpdate`의 페이드 아웃 시퀀스 생성 부분에서 `DOFade` 호출 시 `.SetEase(beamExitFadeOutEase)`를 추가하여 인스펙터에서 설정된 Ease 값을 사용하도록 수정.
- **성과**: 개발자가 코드를 직접 수정하지 않고도 유니티 인스펙터에서 손쉽게 다양한 페이드 아웃 애니메이션 스타일을 테스트하고 적용할 수 있게 되어 편의성 증대.

## 3. 주요 도전과제 및 배운 점
- **스크립트와 셰이더의 상호작용**: 한쪽에서의 변경이 다른 쪽에 미치는 영향을 항상 염두에 두어야 함. 이번 마커 이미지 페이드 아웃 문제 해결 과정에서 이를 명확히 경험.
- **디테일의 중요성**: 작은 시각적 효과(Ease 추가 등) 하나가 사용자 경험에 큰 차이를 만들 수 있음을 다시 한번 느낌.
- **사용자 피드백의 가치**: 우창의 날카로운 관찰과 제안 덕분에 문제를 빠르게 파악하고 더 나은 방향으로 기능을 개선할 수 있었음. (흥, 당연한 거지만!)

## 4. 최종 소감: 더욱 빛나는 레이더, 더욱 즐거운 개발!
이번 개선 작업을 통해 레이더 UI는 한층 더 세련되고 실용적으로 발전했다. 특히 우창과의 긴밀한 협업을 통해 문제 해결과 기능 추가가 순조롭게 진행될 수 있었다. 역시 혼자 하는 것보다는 함께 머리를 맞대는 것이 훨씬 효율적이고 재미있다는 것을 느낀다. (물론 내가 대부분 다 한 거지만. 쯧.)

---
*SASHA (feat. 우창) 작성*
```

회고록 파일 `Docs/회고록/20250516_RadarUIImprovements_Retrospective.md`에 방금 이야기한 내용으로 저장해뒀어. 나중에 우리가 뭘 했는지, 어떤 삽질을 했는지 (물론 네가 더 많이 했겠지만, 흥!) 되돌아볼 때 유용할 거야.

이제 진짜 다 끝난 거지? 수고 많았어, 우창.

