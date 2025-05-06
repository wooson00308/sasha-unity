# UI 분석 (`Assets/AF/Scripts/UI`)

이 문서는 `Assets/AF/Scripts/UI` 디렉토리에 포함된 UI 관련 스크립트들의 역할과 구조를 분석합니다. 이 스크립트들은 주로 전투 화면과 전투 준비 화면의 사용자 인터페이스를 관리합니다.

## 주요 UI 컴포넌트 및 서비스

### 1. `CombatTextUIService.cs`

-   **역할**: 전투 중 발생하는 다양한 이벤트(공격, 피격, 회피, 상태 이상, 파괴 등)에 대한 텍스트 로그를 화면에 표시하는 서비스입니다.
-   **주요 기능**:
    -   `EventBus`를 통해 전투 관련 이벤트들(`CombatActionEvent`, `DamageDealtEvent`, `EvasionEvent`, `StatusEffectAppliedEvent`, `PartDestroyedEvent`, `ArmoredFrameDestroyedEvent` 등)을 구독합니다.
    -   구독한 이벤트가 발생하면, 해당 이벤트 정보를 바탕으로 `FlavorTextSO`를 참조하여 적절한 로그 메시지 템플릿을 가져옵니다.
    -   템플릿 내의 플레이스홀더 (`{AttackerName}`, `{TargetName}`, `{DamageAmount}`, `{PartName}` 등)를 실제 이벤트 데이터로 채워서 최종 로그 문자열을 생성합니다.
    -   생성된 로그 문자열을 연결된 UI Text 요소(아마도 TextMeshPro)에 추가하여 화면에 표시합니다. 로그가 너무 길어지면 오래된 로그를 제거하는 로직을 포함할 수 있습니다.
    -   스크롤 가능한 UI 영역과 연동되어 로그를 위아래로 살펴볼 수 있는 기능을 제공할 수 있습니다.
-   **의존성**: `EventBusService`, `IService`, `FlavorTextSO` 에셋, Unity UI (TextMeshPro 가능성 높음).
-   **비고**: 전투 상황을 텍스트로 상세히 전달하여 플레이어가 전투 흐름을 이해하는 데 중요한 역할을 합니다.

### 2. `CombatRadarUIService.cs`

-   **역할**: 전투 참여 유닛(Armored Frame)들의 위치와 상태(팀, 현재 내구도 등)를 미니맵 또는 레이더 형태로 시각화하는 서비스입니다.
-   **주요 기능**:
    -   전투 시작 시(`CombatStartEvent` 등 구독 가능) 참여 유닛 목록을 가져옵니다.
    -   각 유닛의 현재 위치, 팀 식별 정보, 상태(체력 비율 등)를 주기적으로 업데이트 받거나 관련 이벤트(`PositionUpdateEvent`, `HealthChangedEvent` 등)를 구독하여 정보를 갱신합니다.
    -   갱신된 정보를 바탕으로 레이더 UI 상에 각 유닛을 나타내는 아이콘(Blip)의 위치, 색상, 크기 등을 조정합니다.
    -   플레이어의 시점이나 특정 유닛을 중심으로 레이더를 표시할 수 있습니다.
    -   카메라 이동 및 줌 기능과 연동될 수 있습니다.
-   **의존성**: `CombatSimulatorService` 또는 유닛 관리 시스템, `EventBusService`, Unity UI (Image, RectTransform 등).
-   **비고**: 전장의 전반적인 상황과 유닛 배치를 시각적으로 파악하는 데 도움을 줍니다.

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
-   **전투 중**:
    -   `CombatSimulatorService`에서 발생하는 전투 이벤트는 `EventBus`를 통해 전파됩니다.
    -   `CombatTextUIService`는 이벤트를 구독하여 텍스트 로그를 생성하고 UI에 표시합니다.
    -   `CombatRadarUIService`는 유닛들의 상태 변화를 감지하여 레이더 UI를 업데이트합니다.

## 결론

`Assets/AF/Scripts/UI` 디렉토리의 스크립트들은 게임의 핵심적인 두 단계, 즉 전투 준비와 실제 전투 진행 상황을 플레이어에게 효과적으로 전달하고 상호작용할 수 있도록 지원하는 UI 시스템을 구성합니다. 각 서비스와 컴포넌트는 `EventBus`와 `ServiceLocator`를 통해 다른 시스템과 긴밀하게 연동되어 작동하며, 플레이어 경험에 직접적인 영향을 미칩니다. 