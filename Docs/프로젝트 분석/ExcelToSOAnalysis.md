# ExcelToSO 디렉토리 분석 (`Assets/ExcelToSO/Scripts`)

이 문서는 `Assets/ExcelToSO/Scripts` 디렉토리 및 그 하위 디렉토리(`DataModels`, `Editor`)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 Excel 파일(`Assets/AF/Data/AF_Data.xlsx`)의 데이터를 읽어 Unity의 ScriptableObject 에셋(`Assets/AF/Data/Resources/` 하위)으로 변환하는 에디터 기능을 제공합니다.

## 주요 컴포넌트

### 1. 인터페이스 (`IExcelRow.cs`)

-   **`IExcelRow`**: `DataModels` 내의 모든 데이터 클래스가 구현해야 하는 인터페이스입니다. `FromExcelRow(IRow row)` 메서드를 정의하여 NPOI 라이브러리의 `IRow` 객체로부터 데이터를 파싱하여 클래스 필드를 채우는 표준 방식을 제공합니다.

### 2. Excel 파싱 (`ExcelParser.cs`)

-   **역할**: NPOI 라이브러리를 사용하여 지정된 Excel 파일 및 시트에서 데이터를 읽고, 각 행을 지정된 `TModel` 타입(`IExcelRow` 구현체)의 객체로 변환하여 리스트로 반환합니다.
-   **주요 기능 (`Parse<TModel>`)**: 특정 시트 이름을 받아 해당 시트의 데이터를 파싱합니다. 헤더 행(0번 인덱스)을 건너뛰고 데이터 행(1번 인덱스부터)을 순회하며 각 행에 대해 `TModel` 인스턴스를 생성하고 `FromExcelRow` 메서드를 호출하여 데이터를 채웁니다. 간단한 오류 처리(빈 행 건너뛰기, 파싱 오류 로깅)를 포함합니다.
-   **의존성**: NPOI 라이브러리 (`NPOI.XSSF.UserModel`, `NPOI.SS.UserModel`).

### 3. 데이터 모델 (`DataModels/`)

-   **역할**: `AF_Data.xlsx` Excel 파일의 각 시트 구조에 대응하는 C# 클래스들을 정의합니다. 모든 클래스는 `IExcelRow` 인터페이스를 구현하여 `ExcelParser`가 행 데이터를 객체로 변환할 수 있도록 합니다.
-   **구현된 클래스 및 매핑되는 Excel 시트/컬럼**:

    -   **`FrameData.cs` (→ `Frames` 시트)**:
        -   `FrameID` (A), `FrameName` (B), `FrameType` (C, *문자열*), 스탯들(D-L), `FrameWeight` (M), 슬롯 정보(N-U, *문자열*), `Notes` (V).
        -   `id` 속성은 `FrameID`을 반환합니다.
    -   **`PartData.cs` (→ `Parts` 시트)**:
        -   `PartID` (A), `PartName` (B), `PartType` (C, *문자열*), 스탯들(D-L), `MaxDurability` (M), `PartWeight` (N), `Abilities` (O, *문자열*), `Notes` (P).
        -   `id` 속성은 `PartID`를 반환합니다.
    -   **`WeaponData.cs` (→ `Weapons` 시트)**:
        -   `WeaponID` (A), `WeaponName` (B), `WeaponType` (C, *문자열*), `DamageType` (D, *문자열*), 스탯/정보(E-M), `SpecialEffects` (N, *문자열*), `AttackFlavorKey` (O), `ReloadFlavorKey` (P), `Notes` (Q).
        -   `id` 속성은 `WeaponID`를 반환합니다.
    -   **`PilotData.cs` (→ `Pilots` 시트)**:
        -   `PilotID` (A), `PilotName` (B), 스탯들(C-K), `Specialization` (L, *문자열*), `InitialLevel` (M), `InitialSkills` (N, *문자열*), `Notes` (O).
        -   `id` 속성은 `PilotID`를 반환합니다.
    -   **`AssemblyData.cs` (→ `AF_Assemblies` 시트)**:
        -   `AssemblyID` (A), `AFName` (B), `TeamID` (C), `FrameID` (D), `PilotID` (E), 파츠 ID들(F-K), 무기 ID들(L-M), `Notes` (N).
        -   `id` 속성은 `AssemblyID`를 반환합니다.
    -   **`FlavorTextData.cs` (→ `FlavorTexts` 시트)**:
        -   `id` (A), `templateKey` (B), `templateText` (C).

-   **`FromExcelRow` 구현**: 각 클래스의 `FromExcelRow` 메서드는 Excel 시트의 컬럼 순서에 맞춰 `row.GetCell(index)`를 호출하여 데이터를 읽고 속성에 할당합니다. 간단한 타입 변환 헬퍼(`GetFloatValue`, `GetIntValue`)를 포함합니다.
-   **주의사항**: 현재 `DataModels`는 Excel의 Enum 관련 컬럼(`FrameType`, `PartType`, `WeaponType`, `DamageType`, `Specialization`)과 복수 값 컬럼(`Abilities`, `InitialSkills`)을 **단순 문자열**로 읽어옵니다. 이 문자열들은 이후 `ScriptableObjectGenerator`가 호출하는 각 ScriptableObject의 `Apply` 메서드 내에서 실제 Enum 타입으로 파싱되거나 리스트로 분리되어야 합니다.

### 4. ScriptableObject 생성 (`ScriptableObjectGenerator.cs`)

-   **역할**: `ExcelParser`를 통해 파싱된 `DataModels` 객체 리스트를 기반으로 실제 ScriptableObject 에셋(`FrameSO`, `PartSO`, `WeaponSO`, `PilotSO`, `AssemblySO`, `FlavorTextSO`)을 생성하고 `Assets/AF/Data/Resources/<Type>/` 경로에 저장합니다.
-   **주요 기능 (`Generate<TModel, TSO>`)**: 제네릭 메서드로, Excel 데이터 모델 타입(`TModel`)과 생성할 ScriptableObject 타입(`TSO`)을 받습니다.
    1.  `ExcelParser.Parse<TModel>`를 호출하여 지정된 Excel 파일과 시트에서 데이터 리스트를 가져옵니다.
    2.  데이터 리스트의 각 `TModel` 객체에 대해:
        a.  `ScriptableObject.CreateInstance<TSO>()`를 사용하여 SO 인스턴스를 생성합니다.
        b.  리플렉션을 사용하여 `TSO` 타입의 **`Apply(TModel data)`** 메서드를 찾아 호출합니다. **이 `Apply` 메서드는 각 SO 클래스 (`FrameSO`, `PartSO` 등) 내부에 반드시 구현되어 있어야 하며**, `TModel` 객체의 데이터를 SO 필드에 매핑하는 핵심 로직을 담당합니다 (예: `FrameData.FrameType` 문자열을 `AF.Models.FrameType` Enum으로 파싱, `Stats` 객체 생성, `Abilities` 문자열을 `List<string>`으로 변환 등).
        c.  `TModel` 객체의 `id` 속성 값을 사용하여 에셋 파일 이름(예: `FRM_STD_01.asset`)을 결정합니다.
        d.  `AssetDatabase.CreateAsset`를 호출하여 지정된 `Resources` 하위 경로에 SO 에셋을 생성합니다.
    3.  모든 데이터 처리가 끝나면 `AssetDatabase.SaveAssets()` 및 `AssetDatabase.Refresh()`를 호출합니다.
-   **의존성**: `ExcelParser`, `IExcelRow`, `UnityEditor` API, 각 `TSO` 타입의 `Apply` 메서드 구현.

### 5. 에디터 메뉴 (`Editor/DataGeneratorMenu.cs`)

-   **역할**: Unity 에디터 상단 메뉴에 "Tools/Generate AF Data from Excel" 항목을 추가하고, 이 메뉴를 클릭했을 때 데이터 생성 프로세스를 실행하는 역할을 합니다.
-   **주요 기능 (`GenerateData` 메서드)**:
    -   Excel 파일 경로(`Assets/AF/Data/AF_Data.xlsx`)와 ScriptableObject 에셋 저장 경로(`Assets/AF/Data/Resources/`)를 정의합니다.
    -   **[주의!]** 여기에 명시된 Excel 파일 경로 (`Assets/AF/Data/AF_Data.xlsx`)는 스크립트 내에서 사용되는 **상대 경로**입니다. 만약 **외부 도구(예: AI 어시스턴트의 Excel 관련 도구)**를 사용하여 이 파일에 접근해야 할 경우, 해당 도구는 **절대 윈도우 경로**(예: `C:\Users\사용자명\...\sasha-unity\Assets\AF\Data\AF_Data.xlsx`)를 요구할 수 있습니다. 상대 경로 또는 다른 형식의 경로(예: `/c%/...`)는 인식되지 않을 수 있으니 유의해야 합니다.
    -   **[추가 주의!]** 또한, 외부 도구 중 특정 셀에 값을 쓰는 기능(예: `mcp_excel-mcp-server_excel_write_to_sheet`)을 사용할 경우, 단일 셀을 지정하더라도 `range` 매개변수는 `"A1"`과 같은 형식이 아니라 `"A1:A1"`과 같이 콜론으로 시작과 끝 셀을 명시하는 형식을 요구할 수 있습니다. 도구의 정확한 매개변수 형식을 확인하고 사용해야 합니다.
    -   Excel 파일 존재 여부를 확인합니다.
    -   각 데이터 타입(Frame, Part, Weapon, Pilot, Assembly, FlavorText)에 대해 대응하는 `DataModel`과 `ScriptableObject` 타입을 지정하고, 정확한 Excel 시트 이름과 출력 하위 폴더 이름을 명시하여 `GenerateSO<TModel, TSO>` 헬퍼 메서드를 호출합니다.
-   **`GenerateSO<TModel, TSO>` 헬퍼 메서드**: `ScriptableObjectGenerator.Generate` 메서드를 호출하기 전에 출력 폴더 존재 여부를 확인하고 없으면 생성합니다.
-   **특징**: 개발자가 에디터 메뉴 클릭 한 번으로 Excel 데이터로부터 모든 관련 ScriptableObject 에셋을 자동으로 생성(또는 업데이트)할 수 있도록 편리한 인터페이스를 제공합니다.

## 시스템 흐름 요약

1.  개발자가 Unity 에디터 메뉴에서 "Tools/Generate AF Data from Excel"을 클릭합니다.
2.  `DataGeneratorMenu.GenerateData`가 실행되어 각 데이터 타입별로 `GenerateSO`를 호출합니다.
3.  `GenerateSO`는 `ScriptableObjectGenerator.Generate`를 호출합니다.
4.  `ScriptableObjectGenerator`는 `ExcelParser.Parse`를 사용하여 `AF_Data.xlsx`의 지정된 시트에서 데이터를 읽어 해당 `DataModels` 클래스의 리스트로 변환합니다.
5.  파싱된 각 데이터 모델 객체(`FrameData`, `PartData` 등)에 대해:
    a.  대응하는 ScriptableObject(`FrameSO`, `PartSO` 등) 인스턴스를 생성합니다.
    b.  생성된 SO의 **`Apply` 메서드**를 리플렉션으로 호출하여 데이터 모델의 정보를 SO 필드로 **변환 및 복사**합니다 (이 단계에서 문자열 Enum 파싱 등이 수행됨).
    c.  데이터 모델의 `id`를 파일명으로 사용하여 `.asset` 파일을 `Assets/AF/Data/Resources/<Type>/` 폴더에 저장합니다.
6.  모든 생성이 완료되면 에셋 데이터베이스를 저장하고 새로 고칩니다.

## 결론

`Assets/ExcelToSO` 시스템은 `AF_Data.xlsx` Excel 파일을 사용하여 게임 데이터를 중앙에서 관리하고, Unity 에디터 메뉴를 통해 이를 `ScriptableObject` 에셋으로 자동 변환하는 강력하고 효율적인 데이터 파이프라인을 구축합니다. `DataModels`는 Excel 시트 구조를 C#으로 표현하고, `ExcelParser`는 데이터 로딩을, `ScriptableObjectGenerator`는 SO 생성 및 `Apply` 메서드 호출을, `DataGeneratorMenu`는 사용자 인터페이스를 제공합니다. 이 시스템은 데이터 기반 개발을 용이하게 하며, 기획/디자인 변경 사항을 코드 수정 없이 게임에 반영할 수 있게 해줍니다.

Excel 데이터로부터 생성된 ScriptableObject 자체의 정의와 데이터 변환 과정에 대한 자세한 분석은 [`ScriptableObjectsAnalysis.md`](./ScriptableObjectsAnalysis.md) 문서를 참조하십시오. 