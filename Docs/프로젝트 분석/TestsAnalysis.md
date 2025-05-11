# Tests 분석 (`Assets/AF/Scripts/Tests`)

이 문서는 `Assets/AF/Scripts/Tests` 디렉토리에 포함된 테스트 관련 스크립트, 특히 `CombatTestRunner.cs`의 역할과 구조를 분석합니다. 이 스크립트는 전투 시스템의 로직을 검증하고 다양한 시나리오를 시뮬레이션하기 위한 Unity 에디터 내 테스트 환경을 제공합니다.

## `CombatTestRunner.cs` 분석

-   **역할**: `IService`를 구현하는 MonoBehaviour 싱글톤(`Instance`)으로, Unity 에디터 내에서 `CombatSimulatorService`를 사용하여 전투 시나리오를 설정, 실행하고 그 과정을 로깅하는 테스트 관리자 역할을 수행합니다. Odin Inspector를 광범위하게 활용하여 사용자 친화적인 에디터 인터페이스를 제공합니다.
-   **주요 기능**:
    -   **테스트 설정 (AFSetup 클래스)**: 중첩 클래스 `AFSetup`을 통해 각 전투 참가 유닛을 상세하게 설정합니다.
        -   **모드**: `useCustomAssembly` (커스텀 조립 여부)에 따라 `AssemblySO`를 사용하거나, 개별 `FrameSO`, `PartSO` (Head, Body, Arms, Legs), `WeaponSO` (R1, L1), `PilotSO`를 직접 선택하여 유닛을 구성합니다.
        -   **기본 정보**: 유닛의 `teamId`, `startPosition`, 커스텀 시 `customAFName`을 설정합니다.
        -   **콜사인**: 플레이어 스쿼드 유닛의 경우 상태 유지를 위한 고유 `callsign`을 문자열로 설정합니다.
        -   **Odin Inspector UI**: 파츠/무기/파일럿 선택 시 `ValueDropdown`으로 SO 목록을 제공하고, `PreviewField`로 선택된 SO의 스프라이트 미리보기를 표시합니다.
        -   **참가자 관리**: `playerSquadSetups` (플레이어 스쿼드, 상태 유지)와 `scenarioParticipantSetups` (일회성 유닛) 두 리스트로 참가자를 관리하며, 인스펙터 버튼으로 추가/제거합니다.
    -   **팀 컬러 시스템 관리**:
        -   정적 `teamColorPalette` 리스트의 색상을 사용하여, `UpdateTeamColors()`가 각 `teamId`에 색상을 할당하고 `_currentTeamColors` 딕셔너리에 저장합니다.
        -   `AFSetup` 내 `teamColorPreview` 필드로 인스펙터에서 팀 색상을 시각적으로 확인합니다.
        -   외부에서 `TryGetTeamColor(int teamId, out Color color)`로 팀 색상을 조회할 수 있습니다.
    -   **플레이어 스쿼드 정보 및 상태 유지**:
        -   `GetPlayerSquadUnitNames()`: `playerSquadSetups`에 등록된 플레이어 유닛의 `callsign` 목록을 `HashSet<string>`으로 제공합니다.
        -   `_persistentPlayerFrames` (Dictionary<string, ArmoredFrame>): 플레이어 스쿼드 유닛의 `ArmoredFrame` 인스턴스를 `callsign` 기준으로 전투 간에 상태를 유지합니다. 전투 시작 시 이 정보를 바탕으로 유닛을 재구성하거나 새로 생성합니다.
        -   `ResetPlayerSquadState()`: 저장된 플레이어 스쿼드 상태를 초기화합니다.
    -   **데이터베이스 로딩**: `LoadAllScriptableObjects()`를 통해 각종 SO 에셋(`FrameSO`, `PartSO`, `WeaponSO`, `PilotSO`, `AssemblySO`)을 프로젝트 폴더에서 로드하여 내부 딕셔너리(`_framesDB` 등)에 저장하고, `AFSetup`의 `ValueDropdown`에 사용합니다.
    -   **전투 시뮬레이션 실행 (`StartCombatTestAsync`)**:
        -   설정된 `AFSetup` 리스트를 바탕으로 `ArmoredFrame` 인스턴스들을 생성합니다 (`CreateTestArmoredFrame` 또는 `CreateCustomArmoredFrame`).
        -   플레이어 유닛은 `_persistentPlayerFrames`를 참조하여 상태를 이어받거나 신규 생성합니다.
        -   `CombatSimulatorService`의 `SetupCombat()`과 `StartCombatAsync()`를 호출하여 비동기적으로 전투를 시작합니다.
        -   `EndCombatTest()`로 현재 진행 중인 전투를 강제 종료할 수 있습니다.
        -   Unity UI 버튼(`combatStartButton`)을 통해서도 전투 시작이 가능합니다 (`TriggerCombatTestFromUIButton`).
    -   **AI 행동 트리 테스트**: `AFSetup`에서 `PilotSO`를 설정하면, 해당 파일럿의 `BehaviorTreeAsset`이 유닛의 `BehaviorTreeRunner`에 할당되어 전투 중 AI 행동 패턴을 테스트하고 관찰할 수 있습니다. **특히 `SupportBT`와 같이 새롭게 추가되거나 복잡한 로직을 가진 행동 트리의 경우, `CombatTestRunner`를 통해 다양한 시나리오(특정 아군/적 유닛 배치, 초기 HP 상태 조절 등)를 설정하고 실행함으로써 예상치 못한 행동이나 버그를 조기에 발견하는 데 매우 유용합니다. 예를 들어, 수리 로직이 특정 조건에서만 실패하거나, 이동 후 방어 행동이 의도한 대로 작동하는지 등을 반복 테스트하며 디버깅할 수 있습니다.**
    -   **디버깅 및 로깅**:
        -   `TextLoggerService`를 활용하여 전투 로그를 기록합니다.
        -   **로그 필터링**: 인스펙터의 `logLevelsToRecord` (`LogLevelFlags`) 옵션을 `TextLoggerService`에 전달하여, 기록 시점부터 특정 레벨의 로그를 제외합니다.
        -   **로그 파일 저장**: `SaveFilteredLogToFile` 메서드를 통해, 전투 종료 후 필터링된 로그를 파일로 저장합니다. 파일명은 동적으로 생성되며(`BattleLog_{battleId}_{timestamp}.txt`), 저장 시 로그 포맷이 정리됩니다 (색상 태그 제거, 스프라이트 태그 변환). **이 기능은 특히 `InverterNode`와 같이 특정 노드의 오작동으로 인해 발생하는 미묘한 버그를 추적하거나, 여러 턴에 걸친 AI의 판단 과정을 상세히 분석해야 할 때 매우 유용하게 활용되었습니다.**
    -   **결과 검증**: 현재 코드에서는 Unity Test Framework와의 직접적인 연동(Assertion 등)은 명확히 보이지 않습니다. 테스트 결과는 주로 로깅된 내용을 수동으로 확인하거나 외부 도구와 연동하여 검증할 수 있습니다.
-   **구현 방식**: `IService`를 구현한 MonoBehaviour 싱글톤으로, Unity 에디터 내 특정 씬이나 UI를 통해 실행됩니다. Unity Test Framework의 자동화된 테스트 케이스보다는, 시나리오 기반의 수동/반자동 테스트 및 데이터 생성에 초점을 맞춘 것으로 보입니다.
-   **의존성**: `Sirenix.OdinInspector`, `AF.Models` (ArmoredFrame, Part, Weapon 등), `AF.Combat` (`CombatSimulatorService`), `AF.Services` (`ServiceLocator`, `EventBusService`, `TextLoggerService`), `AF.Data` (각종 `*SO` 에셋), `Cysharp.Threading.Tasks`, `AF.AI.BehaviorTree` (`BehaviorTreeRunner`, `BehaviorTreeAsset`), `AF.AI.BehaviorTree.PilotBTs`, `System.IO`, `UnityEngine.UI`.

## 테스트의 중요성

-   **버그 조기 발견**: 복잡한 전투 시스템의 로직 오류나 예외 케이스를 다양한 설정으로 시뮬레이션하여 버그를 조기에 발견하고 수정할 수 있습니다.
-   **밸런스 검증**: 다양한 유닛 조합과 AI 전략에 대한 전투 시뮬레이션을 반복 실행하여 게임 밸런스를 테스트하고 조정하는 데 활용될 수 있습니다.
-   **리팩토링 안전성 확보**: 코드 구조를 변경하거나 최적화할 때, 기존 기능이 의도대로 작동하는지 다양한 시나리오 테스트를 통해 빠르게 검증하여 리팩토링에 대한 안정성을 높입니다.
-   **AI 행동 관찰**: 특정 상황에서 AI가 어떤 행동을 하는지 직접 관찰하고 디버깅하는 데 유용합니다. **`SupportBT` 개발 과정에서 `CombatTestRunner`는 `사샤`의 행동 패턴 변화를 단계별로 확인하고, `InverterNode` 문제 해결에 결정적인 역할을 했습니다.**

## 결론

`CombatTestRunner.cs`는 `sasha-unity` 프로젝트의 핵심 기능인 전투 시스템의 안정성과 정확성을 보장하기 위한 중요한 에디터 도구입니다. Odin Inspector를 활용한 상세한 시나리오 설정, 플레이어 상태 유지, AI 행동 트리 연동, 그리고 자동 로그 파일 저장 기능을 통해 개발자는 전투 관련 코드 변경 시 발생할 수 있는 잠재적 문제를 효율적으로 테스트하고, 게임 밸런스를 검증하며, 코드 품질을 유지할 수 있습니다. 복잡한 상호작용과 다양한 변수가 존재하는 전투 시스템에서 이러한 테스트 및 관찰 환경 구축은 필수적입니다. **최근 `SupportBT`의 개발 및 디버깅 과정에서 그 유용성이 다시 한번 입증되었습니다.** 