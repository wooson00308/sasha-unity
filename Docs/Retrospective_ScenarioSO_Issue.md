# 회고록: ScenarioSO 생성 기능 구현 이슈 (YYYY-MM-DD)

## 1. 목표

기존 Excel-to-ScriptableObject 시스템에 새로운 데이터 타입인 `ScenarioSO`를 추가한다. `ScenarioSO`는 엑셀 "Scenarios" 시트의 여러 행 데이터를 그룹화하여, 각 시나리오 ID당 하나의 SO 에셋을 생성하고 내부에 유닛 배치 정보 리스트를 포함하도록 한다.

## 2. 발생 문제 및 해결 과정

### 문제 1: `ApplyData` 메서드 이름 불일치
*   **현상:** 제네릭 `Generate` 메서드에서 `ApplyData` 호출 시 `FrameSO`, `PartSO` 등에서 해당 메서드를 찾지 못하는 오류 발생.
*   **원인:** 기존 SO들은 `Apply` 메서드를 사용하고 있었으나, 신규 `ScenarioSO`에 `ApplyData`를 사용하고 이를 다른 SO 생성 로직에도 적용될 것이라고 잘못 가정함.
*   **해결:** 모든 관련 SO 스크립트(`FrameSO`, `PartSO`, `WeaponSO`, `PilotSO`, `AssemblySO`, `FlavorTextSO`, `ScenarioSO`)의 데이터 적용 메서드 이름을 `ApplyData`로 통일함. (SASHA의 제안 중 방법 2 채택)

### 문제 2: 에셋 파일 이름 생성 오류
*   **현상:** `AssetDatabase.CreateAsset` 호출 시 에셋 파일 이름이 비어있다는 오류 (`'...' is not a valid asset file name.`) 발생.
*   **원인:** 제네릭 `Generate` 메서드가 SO 인스턴스의 `name` 속성을 사용하여 파일 이름을 만드는데, 각 SO의 `ApplyData` 메서드 내에서 `this.name`을 설정하는 로직이 누락되어 있었음.
*   **해결:** 모든 관련 SO 스크립트의 `ApplyData` 메서드 내부에 `this.name = data.ID;` (또는 적절한 ID 필드) 코드를 추가하여 SO 인스턴스의 이름을 설정하도록 함.

### 문제 3: `ScenarioSO`만 생성되지 않는 현상
*   **현상:** 다른 SO는 정상 생성되는데 `ScenarioSO` 에셋만 생성되지 않음 (간혹 폴더만 생성됨).
*   **초기 진단 및 시도 (잘못된 접근 포함):**
    *   `ApplyData` 메서드 이름 불일치, 에셋 파일 이름 생성 오류 해결 후에도 문제 지속.
    *   데이터 파싱 오류 의심 -> 엑셀 데이터 확인 -> 데이터 자체 이상 없음 확인.
    *   `GenerateScenarioScriptableObjects` 메서드 로직 오류 의심 -> 상세 로그 추가, `try-catch` 강화 -> 특정 ID 오류 로그 미발견.
    *   `ScenarioSO` 클래스 및 직렬화 오류 의심 -> SO 클래스 단순화 및 `ApplyData` 호출 제거 테스트 -> 빈 에셋 생성 테스트도 실패.
    *   에셋 경로 생성 시점 및 `so.name` 사용 일치 여부 확인 및 수정.
    *   메뉴 경로 착각 문제 발견 및 수정 (이것도 문제였지만, 근본 원인은 아니었음).
*   **진짜 원인 (최종 발견):** `ScenarioRowData.cs` 파일의 `IExcelRow` 인터페이스 구현에서 **`FromExcelRow(IRow row)` 메서드 내부 로직이 누락**되어 있었음. `ExcelParser.Parse<ScenarioRowData>`는 `FromExcelRow`를 호출만 할 뿐, 내부에서 데이터를 채워주는 로직이 없었기 때문에 항상 비어있는 `ScenarioRowData` 리스트가 반환됨. 이로 인해 `GenerateScenarioScriptableObjects` 메서드가 그룹화할 데이터가 없어 SO 생성 로직을 실행하지 않고 `return` 문으로 조기 종료됨.
*   **해결:**
    1.  `ScenarioRowData.FromExcelRow` 메서드 내부에 NPOI의 `IRow` 객체로부터 셀 인덱스 기반으로 데이터를 읽어와 각 프로퍼티에 할당하는 로직을 구현함.
    2.  디버깅 과정에서 수정했던 테스트 코드(`ScriptableObjectGenerator.cs`)와 단순화 코드(`ScenarioSO.cs`)를 원래 로직으로 복구함.

### 부가적 실수: `RenameAsset` 타이밍 오류 (수정 과정에서 발견)
*   **현상:** 초기 `ScenarioSO.ApplyData` 구현 시 `#if UNITY_EDITOR` 블록 안에서 `AssetDatabase.RenameAsset`을 호출하려 했음.
*   **원인:** `ApplyData`가 호출되는 시점은 `AssetDatabase.CreateAsset` 이전이므로, 해당 SO는 아직 에셋 경로를 가지지 않아 `GetAssetPath`가 실패하고 `RenameAsset`도 실패함.
*   **해결:** 해당 로직 제거. 에셋 이름은 `ScriptableObjectGenerator`에서 `CreateAsset` 호출 시 지정하는 것으로 충분함.

## 3. 근본 원인 분석

*   **인터페이스 구현 미비:** `IExcelRow` 인터페이스를 사용하는 `ExcelParser`의 동작 방식을 오해하고, `ScenarioRowData`에서 핵심 메서드인 `FromExcelRow`의 구현을 누락함. 다른 데이터 모델과의 차이점을 간과함.
*   **기존 코드 분석 부족:** `ExcelParser`가 정확히 어떻게 동작하는지, 다른 데이터 모델(`FrameData` 등)은 `FromExcelRow`를 어떻게 구현했는지 확인하지 않음.
*   **잘못된 가정:** `ExcelParser`가 리플렉션 등으로 데이터를 자동 매핑해 줄 것이라고 안일하게 가정함.
*   **비효율적인 디버깅:** 실제 데이터 파싱 단계의 문제를 놓치고, SO 생성/저장 단계나 메뉴 경로 등 부차적인 부분에서 원인을 찾으려 함. 로그만으로는 파싱 결과(빈 리스트)의 원인을 특정하기 어려웠음.
*   **일관성 부족 및 소통 미흡:** 메뉴 구조 변경 등 초기 실수가 문제 해결 과정을 더 복잡하게 만듦.

## 4. 교훈 및 개선점

*   **인터페이스/추상 클래스 구현 철저 확인:** 인터페이스나 추상 클래스를 구현할 때는 모든 필수 메서드의 실제 동작 로직이 제대로 작성되었는지 반드시 확인한다.
*   **라이브러리/모듈 동작 방식 명확히 이해:** 사용하는 라이브러리나 내부 모듈(ex: `ExcelParser`)이 정확히 어떤 방식으로 동작하는지 코드를 직접 보거나 문서를 통해 명확히 이해하고 사용한다.
*   **코드 수정 전 반드시 기존 코드 분석:** (기존 교훈 강화)
*   **구조적 차이 인지 및 영향 예측:** (기존 교훈 강화)
*   **체계적인 디버깅:** 데이터 흐름의 가장 첫 단계부터 순서대로 확인한다. (ex: 파싱 -> 그룹화 -> 객체 생성 -> 데이터 적용 -> 에셋 저장). 각 단계의 입력과 출력이 예상대로 나오는지 검증한다.
*   **일관성 유지 및 명확한 소통:** (기존 교훈 강화)
*   **가정 배제:** (기존 교훈 강화) 