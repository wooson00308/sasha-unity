# 리팩토링 업보: 모델-SO-데이터 간 강결합 문제 및 개선 방향

## 1. 문제 상황: 너무 끈끈한 우리 사이

현재 프로젝트의 핵심 데이터 관리 방식은 다음과 같은 심각한 강결합(tight coupling) 문제를 안고 있습니다.

-   **삼위일체 구조**: 런타임 모델 클래스 (예: `Frame.cs`), Excel 데이터 구조를 반영하는 데이터 모델 클래스 (예: `FrameData.cs`), 그리고 ScriptableObject 데이터 에셋 (예: `FrameSO.cs`)이 서로 너무 많은 책임을 공유하고 필드를 중복으로 정의하고 있습니다.
    -   `Frame.cs`는 `FrameData`의 거의 모든 필드를 가지고 있으며, 자체적으로 로직도 수행합니다.
    -   `FrameSO.cs` 역시 `FrameData`의 필드들을 대부분 복제하여 가지고 있습니다.
    -   이는 `Part.cs`/`PartData.cs`/`PartSO.cs`, `Weapon.cs`/`WeaponData.cs`/`WeaponSO.cs`, `Pilot.cs`/`PilotData.cs`/`PilotSO.cs` 등 다른 핵심 모델에서도 유사하게 반복되는 패턴입니다.

-   **스탯 데이터 중복 및 간접 초기화**: `Stats.cs` 자체는 여러 스탯을 모아놓은 데이터 클래스이지만, 이를 사용하는 `FrameSO`, `PartSO`, `PilotSO` 등은 `Stats` 객체를 직접 필드로 갖는 대신, 개별 스탯 필드(예: `Stat_AttackPower`, `Stat_Defense`)를 나열하여 가지고 있습니다. 런타임 모델(`Frame.cs`, `Pilot.cs` 등)에서 `Stats` 객체를 사용할 때, 이 SO의 개별 필드 값들을 일일이 읽어와 `Stats` 객체를 새로 생성하여 초기화하는 과정을 거칩니다. 이는 데이터 흐름을 간접적으로 만들고, SO 레벨에서 `Stats` 객체를 직접 관리하는 것보다 번거롭습니다.

-   **파급 효과**: 이로 인해 하나의 데이터 필드를 수정하거나 추가하려고 하면, 관련된 모든 클래스 (`*SO.cs`, `*.cs` 모델, `*Data.cs`)와 더불어, 이들을 사용하는 시스템 (`DataGeneratorMenu.cs`, `ScriptableObjectGenerator.cs`, `CombatTestRunner.cs` 등)까지 연쇄적으로 수정해야 하는 상황이 발생합니다.
    -   예시: `Frame`에 '제조사' 정보를 추가하려면, `Frames` Excel 시트, `FrameData.cs`, `FrameSO.cs`, `Frame.cs` (만약 직접 저장한다면), 그리고 `FrameSO.Apply()` 메서드, `CombatTestRunner`의 프레임 생성 로직 등을 모두 변경해야 합니다.

## 2. 이 구조의 문제점 (우리가 겪고 있는 고통)

-   **유지보수 비용 증가**: 작은 변경에도 여러 파일을 수정해야 하므로 작업 시간이 늘어나고 실수가 발생할 가능성이 커집니다.
-   **오류 발생 가능성 증가**: 여러 곳에 동일한 정보가 중복으로 존재하므로, 데이터 동기화가 누락되거나 불일치가 발생하기 쉽습니다.
-   **확장성 저해**: 새로운 프레임 타입이나 파츠 특성을 추가하는 작업이 매우 복잡하고 어려워집니다. (우리가 `FrameType` Enum 대신 데이터 기반으로 가려는 이유)
-   **책임 분리 모호**: 각 클래스의 역할과 책임이 불분명해져 코드 이해와 관리가 어려워집니다.

## 3. 이상적인 구조 (우리가 나아가야 할 길)

우창님이 제시한 바람직한 구조는 다음과 같습니다.

-   **ScriptableObject (`*SO.cs`)는 데이터의 "단일 진실 공급원(Single Source of Truth)"**:
    -   `FrameSO.cs`는 특정 프레임의 모든 정적 데이터(예: `Stats` 타입의 기본 스탯, 파츠 슬롯 정의, 호환성 규칙, 제조사, 등급 등)를 책임지고 소유합니다.
    -   `PartSO.cs`는 파츠의 모든 정적 데이터(예: `Stats` 타입의 스탯, 무게, 특수 능력 등)를 가집니다.
    -   `PilotSO.cs`는 파일럿의 초기 정적 데이터(예: `Stats` 타입의 기본 스탯, 초기 전문화, 습득 가능 스킬 목록 등)를 포함합니다.
    -   `WeaponSO.cs`는 무기의 모든 정적 정보(데미지, 사거리, 타입, 발사 효과음 키, 재장전 효과음 키, `Stats` 형태의 스탯 보너스가 있다면 그것까지도)를 포함합니다.
    -   Excel에서 가져온 데이터는 각 타입에 맞는 `*SO.cs` 에셋에 저장됩니다.

-   **런타임 모델 (`*.cs`, 예: `Frame.cs`)은 로직 중심, 데이터는 참조 또는 초기화**:
    -   `Frame.cs`는 더 이상 모든 데이터 필드를 직접 소유하지 않습니다. 자신에 해당하는 `FrameSO` 에셋을 참조하거나, 생성 시점에서 `FrameSO`로부터 필요한 데이터(예: `Stats` 객체)를 받아 초기화합니다. `Frame.cs`는 주로 런타임 상태(현재 내구도, 장착된 파츠 목록 등)와 행위(메서드)에 집중합니다.
    -   `Part.cs`, `Pilot.cs`, `Weapon.cs` 역시 각각 해당하는 `*SO` 에셋으로부터 정적 데이터를 받아 초기화하고, 자신들의 동적인 런타임 상태(예: `Part`의 현재 내구도, `Pilot`의 현재 경험치/레벨, `Weapon`의 현재 탄약/과열도)와 로직 처리에 집중합니다.

-   **데이터 모델 (`*Data.cs`, 예: `FrameData.cs`)은 순수 Excel 파싱용**:
    -   `FrameData.cs`는 NPOI 라이브러리를 통해 Excel 시트의 데이터를 C# 객체로 변환하는 역할만 담당합니다. (`Stats` 관련 필드도 Excel 시트 구조에 따라 개별 필드로 유지될 수 있습니다.)
    -   `ScriptableObjectGenerator`는 이 `FrameData` 객체를 받아, `*SO.cs` 에셋 내부의 `Stats` 객체 등을 포함한 모든 필드를 채우는 데 사용합니다.

-   **책임 분리 명확화**:
    -   **`*Data.cs`**: Excel -> C# 객체 변환 (데이터 입력)
    -   **`*SO.cs`**: 게임 내 정적 데이터 저장 및 관리 (데이터 원본, `Stats` 객체 등 복합 데이터 포함)
    -   **`*.cs` (런타임 모델)**: 게임 로직 및 동적 상태 관리 (데이터 활용)

## 4. 리팩토링 방향 제안

1.  **`*SO.cs` 클래스 강화**: 각 `FrameSO`, `PartSO`, `WeaponSO`, `PilotSO` 등이 해당 에셋 타입의 모든 정적 데이터를 포함하도록 필드를 확장합니다. 특히, 개별 스탯 필드 대신 `Stats` 객체를 직접 필드로 갖도록 수정하고, 관련 로직(예: 호환성 계산에 필요한 데이터 제공)을 가질 수 있도록 합니다.
    -   **예시 (`FrameSO.cs`)**: 기존의 `Stat_AttackPower`, `Stat_Defense` 등의 개별 스탯 필드를 제거하고, `public Stats BaseStats;`와 같이 `Stats` 타입의 필드를 직접 선언합니다. 프레임의 고유 특성(제조사, 등급, 기술 레벨 등)을 나타내는 새로운 필드들을 추가합니다. `public FrameType FrameType;` 필드는 클래스 인스턴스화 분기용으로 유지될 수 있습니다. 파츠 슬롯 정의(`public List<PartSlotDefinition> PartSlots;`)나 특정 파츠 타입과의 호환성 정보를 담는 복합 데이터 구조(`public List<PartCompatibilityRule> CompatibilityRules;`)도 `FrameSO` 내부에 직접 정의하여, 기존 `Frame.cs` 내 하드코딩된 슬롯 정보나 `AdjustCompatibilityByType` 로직을 대체합니다.

2.  **런타임 모델 수정**: `Frame.cs`, `Part.cs`, `Weapon.cs`, `Pilot.cs` 등이 생성자나 초기화 메서드를 통해 해당 `*SO`를 전달받고, 필요한 데이터를 가져오거나 참조하도록 변경합니다. SO에서 `Stats` 객체를 직접 받을 수 있게 되면, 런타임 모델에서의 불필요한 `Stats` 객체 재생성 코드를 제거합니다. 중복 필드를 제거합니다.
    -   **예시 (`Frame.cs` 및 자식 클래스들)**:
        -   `Frame.cs`의 생성자를 `protected Frame(FrameSO frameSO)` 형태로 변경합니다.
        -   생성자 내부에서 `_name = frameSO.FrameName;`, `_baseStats = frameSO.BaseStats;` (복사 또는 참조), `_weight = frameSO.FrameWeight;`, `_type = frameSO.FrameType;` (또는 `frameSO.FrameIdentifierTag`) 등으로 핵심 정보를 `FrameSO`에서 직접 초기화합니다. 이 `_baseStats`는 `FrameSO`에 정의된 `Stats` 객체를 직접 참조하거나 필요에 따라 복사하여 사용합니다.
        -   `GetPartSlots()` 메서드는 `frameSO.PartSlots`에 저장된 정보를 기반으로 슬롯 데이터를 반환하도록 하고, `CanEquipPart()` 메서드는 `frameSO.CompatibilityRules`와 `frameSO.PartSlots`를 참조하여 파츠 장착 가능 여부 로직을 수행하도록 변경합니다.
        -   `LightFrame`, `StandardFrame`, `HeavyFrame` 등의 자식 클래스들은 생성자를 `public StandardFrame(FrameSO frameSO) : base(frameSO)` 형태로 통일합니다.
        -   각 프레임 타입별 `GetPartSlots()`의 기존 `static readonly` 딕셔너리 정의는 `FrameSO`로 이전되므로, 이 메서드들은 `base.frameSO.PartSlots` (또는 `this.frameDataSource.PartSlots` 등 `FrameSO`에서 파생된 데이터를 가리키는 내부 멤버를 통해)를 반환하도록 단순화됩니다.
    -   **`ArmoredFrame.cs` 와의 연동**: 
        -   `ArmoredFrame`은 생성 시 여전히 `Frame` 타입의 객체를 (`_frameBase` 필드에) 주입받습니다. 이 `Frame` 객체는 `CombatTestRunner` 등에서 `FrameSO`를 기반으로 생성된 인스턴스가 됩니다.
        -   `ArmoredFrame` 내에서 `_frameBase.BaseStats`, `_frameBase.Weight`, `_frameBase.CanEquipPart()`, `_frameBase.GetPartSlots()` 등을 호출하는 기존 코드는 대부분 변경 없이 유지될 가능성이 높습니다. 왜냐하면 `Frame` 클래스가 `FrameSO`의 데이터를 내부적으로 어떻게 사용하는지에 대한 세부 사항을 캡슐화하고, 일관된 공개 API(메서드 및 속성)를 `ArmoredFrame`에 제공하기 때문입니다.
        -   결과적으로, `ArmoredFrame`은 `FrameSO`의 구체적인 존재를 직접 알 필요 없이, 잘 정의된 `Frame` 클래스의 인터페이스를 통해 필요한 모든 프레임 관련 정적 데이터를 간접적으로 활용하게 됩니다. 이는 `ArmoredFrame`과 프레임 데이터 소스 간의 결합도를 낮추고, 각 클래스의 책임을 명확하게 분리하는 데 기여합니다.

3.  **`DataGeneratorMenu.cs` 및 `ScriptableObjectGenerator.cs` 수정**: Excel에서 읽어온 `*Data.cs` 객체의 정보를 새롭게 확장된 `*SO.cs` 에셋에 올바르게 매핑하도록 `Apply` 메서드 등을 수정합니다. (예: `PartData`의 개별 스탯 필드들을 `PartSO` 내부의 `Stats` 객체에 할당)
    -   **예시 (`ScriptableObjectGenerator.cs` 내 `Apply` 호출부 또는 각 `*SO.cs`의 `Apply` 메서드)**: `FrameData`의 개별 스탯 필드들을 `FrameSO`의 `BaseStats` 객체 내부 필드에 각각 채워 넣도록 수정합니다. `FrameSO`에 새로 추가된 제조사, 등급, `PartSlotDefinition` 리스트, 호환성 규칙 등의 필드도 `FrameData` (Excel 시트에 해당 컬럼 추가 후)에서 읽어와 매핑합니다.

4.  **`CombatTestRunner.cs` (및 기타 시스템) 수정**: `ArmoredFrame` 등의 런타임 객체를 생성할 때, 하드코딩된 값이나 `FrameType` enum 기반의 `switch` 문 대신, 해당 `*SO` 에셋을 로드하여 전달하는 방식으로 변경합니다. `Pilot`, `Weapon` 등의 객체 생성 시에도 해당 `*SO`에서 직접 `Stats` 객체를 가져와 전달하도록 수정합니다.
    -   **예시 (`CombatTestRunner.cs`의 `CreateTestArmoredFrame` 메서드)**:
        -   `FrameSO frameSO = ...;` 로 `FrameSO`를 가져온 후, 더 이상 `Stats frameBaseStats = new Stats(frameSO.Stat_AttackPower, ...)`와 같이 `Stats` 객체를 별도로 생성하지 않습니다.
        -   `Frame` 객체 생성 시 `switch (frameSO.FrameType)` 구문은 유지하되, 각 case에서 `new StandardFrame(frameSO);`, `new LightFrame(frameSO);` 와 같이 `FrameSO` 객체 자체를 생성자에 전달합니다. 이렇게 하면 각 프레임 클래스 내부에서 `FrameSO`로부터 `BaseStats`를 포함한 모든 필요한 정보를 가져와 설정하게 됩니다.
        -   `Pilot` 객체 생성 시: `PilotSO pilotData = ...; Pilot pilot = new Pilot(pilotData);` 와 같이 `PilotSO`를 직접 전달하고, `Pilot` 클래스 내부에서 `pilotData.PilotName`, `pilotData.BaseStats`, `pilotData.Specialization` 등을 사용해 초기화합니다.
        -   파츠 및 무기 장착 로직(`AttachPartFromSO`, `AttachWeaponFromSO` 등)도 유사하게, 각 `PartSO`, `WeaponSO`를 해당 런타임 모델의 생성자나 초기화 메서드에 전달하여 내부에서 `Stats` 객체를 포함한 필요 데이터를 설정하도록 수정합니다.

## 5. 기대 효과

-   **유지보수성 향상**: 데이터 변경 시 주로 `*SO.cs`와 Excel 파일만 수정하면 되므로 작업 범위가 명확해지고 줄어듭니다.
-   **오류 감소**: 데이터 중복이 최소화되어 불일치 가능성이 낮아집니다.
-   **확장성 증대**: 새로운 종류의 프레임, 파츠, 무기 등을 데이터(Excel 및 SO) 추가/수정만으로 쉽게 구현할 수 있게 됩니다.
-   **코드 가독성 및 이해도 증가**: 각 클래스의 역할이 명확해집니다.

이 작업은 상당한 노력이 필요하겠지만, 장기적으로 프로젝트의 건강성과 개발 효율성을 크게 높일 수 있는 중요한 투자입니다. 