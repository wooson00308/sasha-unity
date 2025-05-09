# UI 분석 (`Assets/AF/Scripts/UI`)

이 문서는 `Assets/AF/Scripts/UI` 디렉토리에 포함된 UI 관련 스크립트들의 역할과 구조를 분석합니다. 이 스크립트들은 주로 전투 화면과 전투 준비 화면의 사용자 인터페이스를 관리합니다.

## 주요 UI 컴포넌트 및 서비스

### 1. `CombatTextUIService.cs`

-   **역할**: 전투 중 발생하는 다양한 이벤트(공격, 피격, 회피, 상태 이상, 파괴 등)에 대한 텍스트 로그를 화면에 표시하는 서비스입니다. **전투 종료 시 로그 데이터를 처리하여 한 번에 애니메이션으로 보여주는 방식으로 작동합니다.**
-   **주요 기능**:
    -   `EventBus`를 통해 전투 관련 이벤트들(`CombatActionEvent`, `DamageDealtEvent`, `EvasionEvent`, `StatusEffectAppliedEvent`, `PartDestroyedEvent` 등)을 구독합니다. `PartDestroyedEvent`의 경우 `FrameWasActuallyDestroyed` 정보를 활용하고, `LogEntry`에 포함된 `PartDestroyed_SlotId` 같은 상세 정보를 통해 더욱 구체적인 파괴 로그(예: "우측 팔 파괴!") 표시가 가능합니다.
    -   **로그 처리 및 재생**: 전투 종료(`CombatEndEvent`) 시, `TextLoggerService`로부터 전체 로그(`List<LogEntry>`)를 가져옵니다. 각 로그 항목을 순회하며 상태 스냅샷과 델타 정보를 사용하여 특정 시점의 게임 상태를 복원(`UpdateSnapshotWithDelta`)하고, 이를 바탕으로 유닛 상세 정보 UI(`_unitDetailTextDisplay` 등)를 업데이트합니다. 로그 메시지는 `FlavorTextSO`를 참조하여 가공된 후, UI 텍스트 요소(`_logLinePrefab` 인스턴스)에 타이핑 애니메이션 효과와 함께 순차적으로 표시됩니다.
    -   **상세 정보 표시**: 로그 재생 중, 특정 시점의 전체 유닛 상태 스냅샷(`ArmoredFrameSnapshot`)을 사용하여 `_unitDetailTextDisplay`에 모든 유닛의 정보를 표시합니다. 또한 이벤트 대상(`_eventTargetDetailTextDisplay`)이나 피격 대상(`_damageTargetDetailTextDisplay`)의 스냅샷 정보를 별도로 표시하여 특정 유닛의 상태 변화를 강조합니다. **이때 표시되는 유닛의 내구도(DUR)는 스냅샷에 저장된 `CurrentTotalDurability`(모든 파츠의 현재 내구도 합계)와 `MaxTotalDurability`(모든 파츠의 최대 내구도 합계) 값을 사용합니다.**
-   **의존성**: `EventBusService`, `TextLoggerService`, `IService`, `FlavorTextSO` 에셋, Unity UI (TextMeshPro, ScrollRect, Prefab 등), DG.Tweening.
-   **비고**: 전투 상황을 텍스트 로그 애니메이션과 함께 상세히 전달하여 플레이어가 전투 흐름을 이해하고 분석하는 데 중요한 역할을 합니다.

### 2. `CombatRadarUIService.cs`

-   **역할**: 전투 참여 유닛(Armored Frame)들의 위치와 상태를 미니맵 또는 레이더 형태로 시각화하는 서비스입니다. `CombatLogPlaybackUpdateEvent`를 구독하여 로그 재생에 맞춰 상태를 업데이트하고, 다양한 DOTween 애니메이션을 통해 시각적 피드백을 제공합니다.
-   **주요 기능**:
    -   **스냅샷 기반 업데이트**: `CombatLogPlaybackUpdateEvent`를 통해 전달받은 `ArmoredFrameSnapshot`을 기반으로 레이더 UI에 표시되는 모든 유닛 마커의 위치와 외형을 업데이트합니다.
    -   **동적 레이더 원점 및 포커스**: 플레이어 스쿼드 유닛의 생존 여부 및 활성화 상태에 따라 레이더의 중심(`_battleCenterWorldPosition`)과 포커스 대상(`_lastRadarFocusTargetUnitName`)을 동적으로 조정하여, 플레이어에게 가장 관련성 높은 정보를 중심으로 표시합니다.
    -   **마커 텍스트(CallSignText) 표시**: 각 마커는 자식 오브젝트로 `CallSignText` (TextMeshProUGUI)를 가지며, 마커 생성 및 업데이트 시 `ArmoredFrameSnapshot`의 `Name` (기체명)을 가져와 여기에 표시합니다.
    -   **마커 애니메이션 (DOTween 사용)**:
        -   **생성/소멸**: 마커가 나타날 때는 스케일 및 페이드 인 애니메이션(`markerAppearEase`), 사라질 때는 스케일 및 페이드 아웃 애니메이션(`markerDisappearEase`)이 적용됩니다. **이때 `CallSignText`의 알파 값도 마커 `Image`의 알파 값과 동기화되어 함께 페이드 효과가 적용됩니다.**
        -   **활성 유닛 강조**: 현재 턴의 활성 유닛 마커는 부드럽게 확대/축소되어 강조 표시됩니다 (`markerActiveScaleDuration`).
        -   **탐지/가시성 변경**: 유닛이 특정 조건(플레이어 스쿼드, 레이더 포커스, 무기 사거리 내)에 의해 항상 보이게 되거나 스캔 효과 머티리얼로 전환될 때, \"탐지됨!\" 효과음과 함께 펄스 애니메이션(`PlayDetectedPulse`) 및 머티리얼 페이드 전환 효과가 적용됩니다. **텍스트 알파도 마커의 가시성 변경에 맞춰 동기화됩니다.**
        -   **피해 및 파괴**: 유닛이 피해를 입으면 마커가 지정된 색상으로 짧게 플래시하고 흔들리는 효과(`PlayFlashEffect` 강화)가 나타납니다. 프레임이 완전히 파괴되면 마커가 검게 변하며 깜빡이고 흔들리다가 축소되며 사라지는 특별 애니메이션이 재생됩니다.
        -   **이동**: 유닛 이동 시 마커가 새로운 위치로 부드럽게 이동하며 바운스 효과(`markerMoveEase`)가 적용됩니다.
        -   **빈사 상태 표시**: 바디 파츠의 내구도가 20% 미만인 유닛의 마커는 기본 팀 색상을 유지한 채 알파값이 주기적으로 깜빡이는 효과가 적용됩니다. **`CallSignText`의 알파 값도 이 깜빡임 효과와 동기화됩니다.**
    -   **머티리얼 관리**: 각 유닛 마커는 상황에 따라 기본 UI 머티리얼(항상 보임) 또는 스캔 효과가 적용된 특수 머티리얼(`markersMaterial`)을 사용하며, 전환 시 페이드 효과가 적용됩니다. **플레이어 스쿼드가 전멸하면 모든 유닛 마커는 기본 UI 머티리얼을 강제로 사용하게 되어, 스캔 효과와 관계없이 항상 선명하게 표시됩니다.**
    -   **레이더 스캔 효과 연동 및 텍스트 알파 동기화**: `LateUpdate`에서 `radarScanTransform`의 Z축 회전 값을 읽어 `markersMaterial`의 셰이더 파라미터(`_ScanCurrentAngleRad`, `_ScanArcWidthRad` 등)를 업데이트하여 레이더 스캔 빔 효과를 시각화합니다. **또한, 스캔 효과 머티리얼을 사용하는 마커의 `CallSignText` 알파 값은 레이더 스캔 빔의 위치에 따라 실시간으로 조절됩니다. 스캔 빔에 의해 마커가 가려지는 부분에서는 텍스트도 함께 투명해져 시각적 일관성을 유지합니다.**
-   **의존성**: `EventBusService` (주로 `CombatLogPlaybackUpdateEvent` 구독), `CombatTestRunner` (팀 색상 및 플레이어 스쿼드 정보 조회), `ArmoredFrameSnapshot`, DOTween, Unity UI (Image, RectTransform, Material, TextMeshProUGUI 등).
-   **비고**: 전투 상황을 시각적으로 명확하게 전달하고, 다양한 애니메이션 효과를 통해 플레이어의 몰입도를 높이는 데 기여합니다. 로그 재생 방식과 연동되어 전투 과정을 효과적으로 시각화합니다.

### 3. `CombatSetupUIService.cs`

-   **역할**: 전투 시작 전, 플레이어가 전투에 참여할 아군 유닛(및 파일럿, 장비)을 선택하고 배치하는 화면의 UI 로직을 관리합니다.
-   **주요 기능**:
    -   플레이어가 보유한 유닛, 파일럿, 프레임, 파츠, 무기 등의 목록을 표시합니다. (관련 `*SO` 에셋 또는 데이터 관리 서비스 필요)
    -   드래그 앤 드롭 또는 클릭을 통해 유닛을 전투 참여 슬롯(`ParticipantSlotUI`)에 배치하거나 제외하는 기능을 제공합니다.
    -   선택된 유닛의 상세 정보(스탯, 장착 파츠/무기, 파일럿)를 보여주는 패널을 관리합니다.
    -   유닛 조합 규칙(예: 코스트 제한, 특정 파츠 필수 장착 등)을 검사하고 유효하지 않은 조합일 경우 피드백을 제공합니다.
    -   '전투 시작' 버튼과 상호작용하여 최종 선택된 유닛 구성 정보를 `CombatSimulatorService`에 전달하고 전투 씬으로 전환하는 트리거 역할을 합니다.
-   **의존성**: 데이터 관리 서비스 (SO 로딩), `ParticipantSlotUI`, `CombatSimulatorService`, Unity UI (Button, ScrollRect, Drag & Drop 관련 컴포넌트).
-   **비고**: 플레이어가 전략적으로 전투를 준비하는 단계의 핵심 인터페이스입니다.

### 4. `ParticipantSlotUI.cs`

-   **역할**: `CombatSetupUIService` 내에서 개별 전투 참여 유닛 슬롯의 UI를 담당하는 컴포넌트입니다.
-   **주요 기능**:
    -   비어있는 슬롯 상태와 유닛이 배치된 상태를 시각적으로 구분하여 표시합니다.
    -   유닛이 배치되면 해당 유닛의 아이콘이나 간략한 정보(이름, 프레임 등)를 표시합니다.
    -   드래그 앤 드롭의 대상(Drop Zone)이 되어 유닛 배치를 처리합니다.
    -   클릭 시 해당 슬롯의 유닛을 선택 해제하거나 상세 정보 창을 여는 등의 상호작용을 지원할 수 있습니다.
-   **의존성**: `CombatSetupUIService`, Unity UI (Image, Button, IDropHandler 등).
-   **비고**: 전투 준비 화면의 구성 요소로, 유닛 선택 과정을 직관적으로 만들어줍니다.

## 시스템 흐름 및 상호작용

-   **전투 준비**: 플레이어는 `CombatSetupUIService`를 통해 유닛을 선택하고 `ParticipantSlotUI`에 배치합니다. 선택이 완료되면 `CombatSetupUIService`가 `CombatSimulatorService`에 정보를 전달하여 전투를 시작합니다.
-   **전투 중 (로그 재생 기반)**:
    -   `CombatSimulatorService`에서 발생하는 전투 이벤트는 `EventBus`를 통해 전파되어 `TextLoggerService`에 기록됩니다.
    -   전투 종료 후, `CombatTextUIService`는 `TextLoggerService`로부터 로그를 받아 재생하며 UI에 텍스트로 표시합니다.
    -   `CombatRadarUIService`는 `CombatLogPlaybackUpdateEvent`를 구독하여, 로그 재생 중 특정 시점의 `ArmoredFrameSnapshot`을 기반으로 레이더 UI와 마커 애니메이션을 업데이트합니다.

## 결론

`Assets/AF/Scripts/UI` 디렉토리의 스크립트들은 게임의 핵심적인 두 단계, 즉 전투 준비와 실제 전투 진행 상황을 플레이어에게 효과적으로 전달하고 상호작용할 수 있도록 지원하는 UI 시스템을 구성합니다. 특히 `CombatRadarUIService`는 스냅샷 기반 업데이트와 풍부한 DOTween 애니메이션을 통해 전투 상황을 역동적으로 시각화하는 핵심 역할을 수행합니다. 각 서비스와 컴포넌트는 `EventBus`와 `ServiceLocator`를 통해 다른 시스템과 긴밀하게 연동되어 작동하며, 플레이어 경험에 직접적인 영향을 미칩니다. 