# 프로젝트 분석 및 복기 가이드

이 문서는 Sasha-Unity 프로젝트의 전체 구조와 각 모듈의 역할을 효율적으로 파악하고 복기하기 위한 가이드입니다.
아래 나열된 분석 문서들을 순서대로 혹은 필요에 따라 참고하여 프로젝트에 대한 이해도를 높일 수 있습니다.

## 1. 시스템 아키텍처 및 핵심 로직 파악

프로젝트의 가장 핵심적인 시스템들의 설계와 작동 방식을 이해하기 위해 다음 문서들을 순차적으로 살펴보는 것을 권장합니다.

*   **`Docs/프로젝트 분석/EventBusAnalysis.md`**: 게임 내 시스템 간의 유연한 통신을 담당하는 이벤트 버스 시스템의 구조와 작동 방식을 설명합니다. 시스템 간의 상호작용을 이해하는 첫걸음입니다.
*   **`Docs/프로젝트 분석/ServicesAnalysis.md`**: 서비스 로케이터 패턴을 기반으로 게임의 핵심 기능들을 모듈화하고 관리하는 서비스 계층(`ServiceLocator`, `ServiceManager` 등)에 대해 설명합니다. 각 서비스가 어떻게 등록되고 관리되는지 파악할 수 있습니다.
*   **`Docs/프로젝트 분석/ModelsAnalysis.md`**: 게임의 핵심 데이터 구조, 특히 `ArmoredFrame` 및 관련 컴포넌트(파츠, 프레임, 무기, 파일럿)와 행동 트리 AI 모델의 구조를 상세히 설명합니다. 게임 유닛이 어떻게 구성되는지 이해할 수 있습니다.
*   **`Docs/프로젝트 분석/BehaviorTreeAnlaysis.md`**: AI의 행동 로직을 담당하는 행동 트리(Behavior Tree) 시스템의 구성 요소, 데이터 흐름, 타 시스템과의 통합 방식을 분석합니다. AI가 어떻게 의사결정을 내리는지 알 수 있습니다.
*   **`Docs/프로젝트 분석/CombatAnalysis.md`**: 턴 기반 전투 시스템의 핵심 로직, 특히 `CombatSimulatorService`와 `CombatActionExecutor`의 역할, 행동 트리와의 연동, 로그 기록 시스템에 대해 설명합니다. 전투가 어떻게 진행되는지 파악할 수 있습니다.

## 2. 데이터 관리 및 에디터 확장 이해

게임 데이터가 어떻게 관리되고 Unity 에디터와 연동되는지 파악하려면 다음 문서들을 참고합니다.

*   **`Docs/프로젝트 분석/ExcelToSOAnalysis.md`**: Excel 파일(`AF_Data.xlsx`)의 데이터를 읽어 Unity의 ScriptableObject 에셋으로 변환하는 에디터 기능의 구조와 데이터 파이프라인을 설명합니다. 게임 데이터가 어디서 오는지 알 수 있습니다.
    *   **참고: `AF_Data.xlsx` 파일 직접 확인 시**:
        *   Excel 파일의 시트 목록을 확인하기 위해 `mcp_excel-mcp-server_excel_describe_sheets` 도구를 사용합니다.
        *   각 시트의 내용을 읽기 위해 `mcp_excel-mcp-server_excel_read_sheet` 도구를 사용합니다.
        *   **특정 셀에 값을 쓰기 위해 `mcp_excel-mcp-server_excel_write_to_sheet` 도구를 사용할 경우, `range` 인자에는 단일 셀(예: "A1")이 아닌 범위 형태(예: "A1:A1")로 지정해야 합니다.**
        *   이 도구들은 파일 경로를 인자로 받으며, **절대 경로**를 사용해야 합니다. 상대 경로 사용 시 "Path is not absolute" 오류가 발생할 수 있습니다.
        *   올바른 경로 예시 (Windows 환경): `C:\\Users\\사용자명\\프로젝트경로\\Assets\\AF\\Data\\AF_Data.xlsx` (백슬래시 `\\` 사용에 유의).
*   **`Docs/프로젝트 분석/ScriptableObjectsAnalysis.md`**: `ExcelToSO` 시스템에 의해 생성된 ScriptableObject 에셋들의 데이터 구조 정의와, Excel 데이터가 실제 게임 데이터 형식으로 변환되는 과정을 설명합니다. 변환된 데이터가 어떻게 저장되는지 파악할 수 있습니다.

## 3. 사용자 인터페이스 및 부가 시스템 확인

플레이어에게 보여지는 부분과 게임의 몰입도를 높이는 시스템에 대한 이해는 다음 문서들을 통해 얻을 수 있습니다.

*   **`Docs/프로젝트 분석/UIAnalysis.md`**: 전투 화면 및 전투 준비 화면의 사용자 인터페이스(`CombatTextUIService`, `CombatRadarUIService`, `CombatSetupUIService` 등) 관리 스크립트들의 역할과 구조를 분석합니다. 플레이어가 게임과 어떻게 상호작용하는지 알 수 있습니다.
*   **`Docs/프로젝트 분석/SoundAnalysis.md`**: 전반적인 사운드 재생을 담당하는 `SoundService`와 전투 중 특정 이벤트에 맞춰 사운드를 재생하는 `CombatSoundService`의 구조와 작동 방식을 설명합니다. 게임의 청각적 경험이 어떻게 구성되는지 파악할 수 있습니다.

## 4. 테스트 및 검증 환경

구현된 시스템들의 안정성을 확보하고 다양한 시나리오를 검증하는 방법에 대해서는 다음 문서를 참고합니다.

*   **`Docs/프로젝트 분석/TestsAnalysis.md`**: 전투 시스템의 로직 검증 및 시나리오 시뮬레이션을 위한 Unity 에디터 내 테스트 환경(`CombatTestRunner.cs`)의 역할과 구조를 분석합니다.

---

위 가이드에 따라 각 분석 문서를 체계적으로 검토하면 프로젝트의 전체적인 흐름과 세부 구현 내용을 효과적으로 복기하고 이해하는 데 도움이 될 것입니다. 