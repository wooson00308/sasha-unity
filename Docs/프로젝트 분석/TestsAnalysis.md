# Tests 분석 (`Assets/AF/Scripts/Tests`)

이 문서는 `Assets/AF/Scripts/Tests` 디렉토리에 포함된 테스트 관련 스크립트, 특히 `CombatTestRunner.cs`의 역할과 구조를 분석합니다. 이 스크립트는 전투 시스템의 로직을 검증하고 다양한 시나리오를 시뮬레이션하기 위한 자동화된 테스트 환경을 제공하는 것으로 보입니다.

## `CombatTestRunner.cs` 분석

-   **역할**: Unity 에디터 또는 별도의 테스트 환경에서 `CombatSimulatorService`를 사용하여 전투 시나리오를 실행하고 결과를 검증하는 테스트 러너(Test Runner) 또는 테스트 관리자 역할을 수행합니다.
    -   `public static CombatTestRunner Instance { get; private set; }`로 싱글톤 인스턴스를 제공하여 외부에서 쉽게 접근할 수 있습니다.
-   **주요 기능**:
    -   **테스트 설정 로딩**: 특정 테스트 시나리오(참여 유닛 구성, 초기 배치, 사용될 AI 전략 등)를 정의하는 설정 파일(ScriptableObject, JSON 등)을 로드하거나 코드 내에서 직접 정의할 수 있습니다.
        -   `AFSetup` 클래스를 통해 각 유닛의 어셈블리 또는 커스텀 파츠, 팀 ID, 시작 위치, 그리고 상태 유지를 위한 고유 `callsign`을 설정합니다.
        -   인스펙터의 "전투 옵션"에서 `maxTurns` 필드를 통해 최대 진행 턴 수를 설정하여 무한 루프를 방지합니다.
    -   **팀 컬러 시스템 관리**:
        -   정적 `teamColorPalette` 리스트에 미리 정의된 색상들을 사용하여, 전투 시작 시 `UpdateTeamColors()` 메서드가 각 팀 ID에 색상을 할당하고 `_currentTeamColors` 딕셔너리에 저장합니다.
        -   `AFSetup` 내 `teamColorPreview` 필드를 통해 인스펙터에서 팀 ID 변경 시 할당될 색상을 시각적으로 확인할 수 있습니다.
        -   외부 서비스(예: `CombatRadarUIService`)에서 `TryGetTeamColor(int teamId, out Color color)` 메서드를 통해 특정 팀의 색상을 조회할 수 있는 기능을 제공합니다.
    -   **플레이어 스쿼드 정보 제공**:
        -   `GetPlayerSquadUnitNames()` 메서드를 통해 현재 `playerSquadSetups`에 등록된 플레이어 유닛들의 `callsign` 목록을 `HashSet<string>` 형태로 제공하여, 다른 시스템에서 플레이어 소속 유닛을 쉽게 식별할 수 있도록 합니다.
    -   **전투 시뮬레이션 실행**: 로드된 설정을 바탕으로 `CombatSimulatorService`를 초기화하고 전투 시뮬레이션을 시작합니다. 아마도 `CombatSimulatorService.StartCombat(CombatSetupInfo)`와 같은 메서드를 호출할 것입니다.
    -   **이벤트 구독 및 결과 기록**: 전투 진행 중 발생하는 주요 이벤트(`CombatActionEvent`, `DamageDealtEvent`, `PartDestroyedEvent`, `CombatEndEvent` 등)를 `EventBus`를 통해 구독하여 전투 결과를 기록하거나 특정 조건(예: 특정 유닛 파괴, 특정 턴 도달)을 감지합니다.
    -   **결과 검증 (Assertion)**: 전투 종료 후(`CombatEndEvent` 수신 시) 또는 테스트 시나리오에서 정의된 특정 시점에서 예상된 결과(예: 특정 유닛 생존/파괴 여부, 특정 파츠 내구도, 전투 소요 턴)와 실제 결과를 비교하여 테스트 통과/실패를 판정합니다. Unity Test Framework의 Assertion (`Assert.AreEqual`, `Assert.IsTrue` 등)을 사용할 가능성이 높습니다.
    -   **다중 테스트 실행**: 여러 개의 다른 테스트 시나리오를 순차적 또는 병렬적으로 실행하고 각 테스트의 결과를 요약하여 보고하는 기능을 포함할 수 있습니다.
    -   **Headless 모드 지원**: UI 없이 백그라운드에서 테스트를 실행할 수 있도록 지원할 수 있습니다. (예: CI/CD 환경에서의 자동 테스트)
    -   **디버깅 및 로깅**:
        -   `TextLoggerService`를 활용하여 전투 중 발생하는 이벤트를 기록합니다.
        -   **로그 필터링**: 인스펙터에서 `logLevelsToRecord` 옵션을 통해 특정 로그 레벨(`LogLevelFlags` 기반)을 **기록 시점에서부터 제외**하도록 설정할 수 있습니다. 이 설정은 `TextLoggerService`로 전달되어, 실제 로그가 `TextLogger` 내부에 저장될 때 적용됩니다.
        -   **로그 파일 저장**: 전투 종료 후, `SaveFilteredLogToFile` 메서드를 통해 현재까지 기록된 로그(설정된 필터링이 적용된)를 파일로 자동 저장합니다. 파일명은 일반적으로 전투 ID와 타임스탬프를 포함하는 동적인 형식(예: `BattleLog_{battleId}_{timestamp}.txt`)으로 생성됩니다. 이때, 파일로 저장되는 로그는 Unity 콘솔과 달리 **색상 관련 태그가 제거**되고 **스프라이트 태그가 텍스트 마커로 변환**되어 가독성을 높입니다. 이 포맷팅은 `CombatTestRunner`가 `TextLogger`의 `GetFormattedLogsForFileSaving` 메서드를 호출하여 처리합니다.
    -   **AI 행동 검증**: 현재 진행 중인 AI 행동 로직 리팩토링(행동 트리 도입)에 따라, 향후에는 이 테스트 러너가 다양한 행동 트리의 실행 결과와 특정 상황에서의 AI 행동 패턴을 검증하는 역할을 수행하도록 업데이트될 수 있습니다.
-   **구현 방식 추정**:
    -   Unity Test Framework (`[Test]`, `[UnityTest]` 어트리뷰트 사용) 기반으로 작성되었을 가능성이 높습니다. 이 경우, Unity 에디터의 Test Runner 창을 통해 테스트를 실행하고 결과를 확인할 수 있습니다.
    -   또는, 일반적인 MonoBehaviour 스크립트로 작성되어 특정 씬에서 실행되거나 에디터 스크립트를 통해 수동으로 트리거될 수도 있습니다.
    -   전투 설정 및 결과 검증 로직이 복잡하다면, 별도의 헬퍼 클래스나 데이터 구조를 사용할 수 있습니다.
-   **의존성**: `CombatSimulatorService`, `CombatActionExecutor`, `EventBusService`, `ServiceLocator`, `ArmoredFrame` 및 관련 모델 클래스, `*SO` 에셋 (테스트 데이터 로딩용), Unity Test Framework (가능성 높음).

## 테스트의 중요성

-   **버그 조기 발견**: 복잡한 전투 시스템의 로직 오류나 예외 케이스를 코드 변경 시 자동으로 감지하여 버그를 조기에 발견하고 수정할 수 있습니다.
-   **밸런스 검증**: 다양한 유닛 조합과 AI 전략에 대한 전투 시뮬레이션을 반복 실행하여 게임 밸런스를 테스트하고 조정하는 데 활용될 수 있습니다.
-   **리팩토링 안전성 확보**: 코드 구조를 변경하거나 최적화할 때, 기존 기능이 의도대로 작동하는지 테스트를 통해 빠르게 검증하여 리팩토링에 대한 안정성을 높입니다.
-   **회귀 테스트**: 이전에 수정된 버그가 다시 발생하지 않는지 확인하는 회귀 테스트를 자동화합니다.

## 결론

`CombatTestRunner.cs`는 `sasha-unity` 프로젝트의 핵심 기능인 전투 시스템의 안정성과 정확성을 보장하기 위한 중요한 도구입니다. 자동화된 테스트를 통해 개발자는 전투 관련 코드 변경 시 발생할 수 있는 잠재적 문제를 효율적으로 감지하고, 게임 밸런스를 검증하며, 코드 품질을 유지할 수 있습니다. 특히 복잡한 상호작용과 다양한 변수가 존재하는 전투 시스템에서는 이러한 테스트 환경 구축이 필수적입니다. 