# ScriptableObjects 분석 (`Assets/AF/Data/ScriptableObjects`)

이 문서는 `Assets/AF/Data/ScriptableObjects` 디렉토리에 포함된 ScriptableObject 정의 스크립트들의 역할과 구조를 분석합니다. 이 스크립트들은 `ExcelToSO` 시스템에 의해 생성된 `.asset` 파일의 데이터 구조를 정의하며, Excel로부터 파싱된 임시 데이터(`DataModels`)를 실제 게임에서 사용 가능한 데이터 형식으로 변환하고 저장하는 역할을 합니다.

## 주요 ScriptableObject 클래스

각 SO 클래스는 `UnityEngine.ScriptableObject`를 상속받고, `ExcelToSO.DataModels` 네임스페이스에 정의된 해당 데이터 모델 클래스(예: `FrameData`)를 파라미터로 받는 `Apply` 메서드를 가지고 있습니다. 이 `Apply` 메서드는 `ExcelToSO.ScriptableObjectGenerator`에 의해 리플렉션으로 호출됩니다.

### 1. `FrameSO.cs`

-   **역할**: 프레임 데이터를 저장하는 ScriptableObject입니다.
-   **주요 필드**: `FrameID`, `FrameName`, `FrameType` (Enum), 스탯 값들, `FrameWeight`, 슬롯 정보(문자열), `Notes`.
-   **`Apply(FrameData data)` 메서드**: `FrameData` 객체로부터 데이터를 받아 `FrameSO`의 필드를 채웁니다.
    -   `FrameType` 문자열을 `AF.Models.FrameType` Enum으로 파싱합니다 (실패 시 경고 로그 및 기본값 `Standard` 사용).
    -   나머지 필드는 직접 매핑합니다.
-   **기타**: 에디터 전용 프리뷰 기능(`#if UNITY_EDITOR`, Odin Inspector 사용)을 포함하여 해당 ID의 스프라이트 이미지를 보여줍니다.

### 2. `PartSO.cs`

-   **역할**: 파츠 데이터를 저장하는 ScriptableObject입니다.
-   **주요 필드**: `PartID`, `PartName`, `PartType` (Enum), 스탯 값들, `MaxDurability`, `PartWeight`, `Abilities` (List<string>), `Notes`.
-   **`Apply(PartData data)` 메서드**: `PartData` 객체로부터 데이터를 받아 `PartSO`의 필드를 채웁니다.
    -   `PartType` 문자열을 `AF.Models.PartType` Enum으로 파싱합니다 (실패 시 경고 로그 및 기본값 `Body` 사용).
    -   `Abilities` 문자열을 쉼표(`,`) 기준으로 분리하고 공백을 제거하여 `List<string>`으로 변환합니다.
    -   나머지 필드는 직접 매핑합니다.
-   **기타**: 에디터 전용 프리뷰 기능 포함.

### 3. `WeaponSO.cs`

-   **역할**: 무기 데이터를 저장하는 ScriptableObject입니다.
-   **주요 필드**: `WeaponID`, `WeaponName`, `WeaponType` (Enum), `DamageType` (Enum), 스탯/정보 값들, 탄약/재장전 정보, AP 비용, `SpecialEffects` (List<string>), `AttackFlavorKey`, `ReloadFlavorKey`, `Notes`.
-   **`Apply(WeaponData data)` 메서드**: `WeaponData` 객체로부터 데이터를 받아 `WeaponSO`의 필드를 채웁니다.
    -   `WeaponType` 문자열을 `AF.Models.WeaponType` Enum으로 파싱합니다 (실패 시 경고 로그 및 기본값 `MidRange` 사용).
    -   `DamageType` 문자열을 `AF.Models.DamageType` Enum으로 파싱합니다 (실패 시 경고 로그 및 기본값 `Physical` 사용).
    -   `SpecialEffects` 문자열을 쉼표(`,`) 기준으로 분리하여 `List<string>`으로 변환합니다.
    -   나머지 필드는 직접 매핑합니다.
-   **기타**: 에디터 전용 프리뷰 기능 포함.

### 4. `PilotSO.cs`

-   **역할**: 파일럿 데이터를 저장하는 ScriptableObject입니다.
-   **주요 필드**: `PilotID`, `PilotName`, 스탯 값들, `Specialization` (Enum), `InitialLevel`, `InitialSkills` (List<string>), `Notes`.
-   **`Apply(PilotData data)` 메서드**: `PilotData` 객체로부터 데이터를 받아 `PilotSO`의 필드를 채웁니다.
    -   `Specialization` 문자열을 `AF.Models.SpecializationType` Enum으로 파싱합니다 (실패 시 경고 로그 및 기본값 `StandardCombat` 사용).
    -   `InitialSkills` 문자열을 쉼표(`,`) 기준으로 분리하여 `List<string>`으로 변환합니다.
    -   나머지 필드는 직접 매핑합니다.
-   **기타**: 에디터 전용 프리뷰 기능 포함.

### 5. `AssemblySO.cs`

-   **역할**: 완성된 Armored Frame의 조립 구성(어떤 프레임, 파츠, 파일럿, 무기를 사용하는지) 정보를 저장하는 ScriptableObject입니다. 이 SO 자체는 런타임 `ArmoredFrame` 객체를 직접 생성하지 않고, 각 컴포넌트의 ID만을 참조 형태로 가지고 있습니다.
-   **주요 필드**: `AssemblyID`, `AFName`, `TeamID`, `FrameID`, `PilotID`, 각 파츠 슬롯별 `PartID` (`HeadPartID`, `BodyPartID` 등), 무기 슬롯별 `WeaponID` (`Weapon1ID`, `Weapon2ID`), `Notes`.
-   **`Apply(AssemblyData data)` 메서드**: `AssemblyData` 객체로부터 데이터를 받아 `AssemblySO`의 필드를 채웁니다. 모든 필드가 문자열 또는 정수형 ID이므로 별도의 파싱 로직 없이 직접 매핑됩니다.
-   **기타**: 에디터 전용 프리뷰 기능이 풍부하게 구현되어 있습니다. 각 컴포넌트 ID에 해당하는 프레임, 파일럿, 파츠, 무기 스프라이트 이미지를 로드하여 인스펙터에 보여줍니다.

### 6. `FlavorTextSO.cs`

-   **역할**: 전투 로그에 사용될 Flavor Text 템플릿을 저장하는 ScriptableObject입니다.
-   **주요 필드**: `templateKey` (관련 템플릿 그룹화 키), `templateText` (실제 텍스트 템플릿).
-   **`Apply(FlavorTextData data)` 메서드**: `FlavorTextData` 객체로부터 데이터를 받아 `FlavorTextSO`의 필드를 채웁니다. (`id`는 에셋 파일명으로 사용됨)

## 데이터 변환 및 런타임 사용 흐름

1.  **Excel → DataModel**: `ExcelParser`가 `AF_Data.xlsx`를 읽어 각 시트의 행 데이터를 `FrameData`, `PartData` 등의 `DataModels` 객체 리스트로 변환합니다. 이 단계에서는 데이터가 대부분 원시 형태(주로 문자열)로 저장됩니다.
2.  **DataModel → ScriptableObject**: `ScriptableObjectGenerator`가 `DataModels` 객체 리스트를 순회하며 각 객체에 대해 대응하는 `ScriptableObject`(`FrameSO`, `PartSO` 등) 인스턴스를 생성합니다. 그 후, 각 SO의 **`Apply` 메서드를 호출**하여 `DataModel` 객체의 데이터를 SO 필드로 변환하고 저장합니다. 이 `Apply` 메서드 내에서 문자열 Enum 파싱, 문자열 리스트 변환 등의 **실질적인 데이터 변환**이 이루어집니다.
3.  **ScriptableObject → 런타임 모델 (게임 실행 시)**: 게임이 실행될 때, `AssemblySO`에 정의된 컴포넌트 ID들을 사용하여 `Resources.Load` 등으로 필요한 `FrameSO`, `PartSO`, `WeaponSO`, `PilotSO` 에셋들을 로드합니다. 로드된 각 SO의 데이터를 기반으로 실제 게임 로직에서 사용될 `AF.Models` 네임스페이스의 런타임 객체(예: `Frame`, `Part`, `Weapon`, `Pilot`)를 **생성**합니다. (이 생성 로직은 현재 분석된 SO 스크립트 내에는 직접적으로 보이지 않으며, 별도의 팩토리 클래스나 `ArmoredFrame` 생성 로직 내에 존재할 것으로 예상됩니다. 예를 들어, 각 SO에 `CreateInstance()` 같은 메서드를 추가하여 구현할 수 있습니다.)

## 결론

`Assets/AF/Data/ScriptableObjects` 디렉토리의 스크립트들은 Excel 데이터와 실제 게임 로직 사이의 중요한 다리 역할을 합니다. 이들은 Excel에서 가져온 원시 데이터를 Unity 에디터에서 관리하기 쉽고 런타임에 효율적으로 로드할 수 있는 ScriptableObject 형태로 변환하고 저장합니다. 각 SO의 `Apply` 메서드는 데이터 변환의 핵심 로직을 담당하며, 특히 문자열 데이터를 게임 내에서 사용하는 Enum이나 List 등의 타입으로 정확하게 파싱하는 역할을 수행합니다. 최종적으로 이 SO들은 게임 시작 시나 필요할 때 로드되어 런타임에 사용될 `AF.Models` 객체를 생성하는 데 필요한 데이터를 제공합니다. 