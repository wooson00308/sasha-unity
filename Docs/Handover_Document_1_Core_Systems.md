# 인수인계 문서 1: 핵심 시스템 개요

이 문서는 AF 프로젝트의 핵심 시스템을 구성하는 주요 C# 스크립트 네 가지의 기능과 역할을 요약합니다.

## 1. CombatSimulatorService.cs

- **경로**: `Assets/AF/Scripts/Combat/CombatSimulatorService.cs`
- **역할**: 전투 시뮬레이션의 전반적인 흐름을 관리하고 제어하는 핵심 서비스입니다.
- **주요 기능**:
    - 전투 시작 (`StartCombat`), 종료 (`EndCombat`), 턴 진행 (`ProcessNextTurn`) 관리.
    - 참가자(`ArmoredFrame`) 관리 및 팀 할당 (`AssignTeams`).
    - 각 유닛의 행동 결정 로직 (`DetermineActionForUnit`, `DetermineMeleeCombatAction` 등) 포함.
    - 공격 (`PerformAttack`), 이동 (`PerformMoveToPosition`), 방어 등 실제 행동 수행 로직 (`PerformAction`).
    - 파츠별 내구도 관리 및 전투 결과 판정 (`DetermineCombatResult`).
    - `EventBusService`를 통해 전투 관련 이벤트(시작, 종료, 턴, 행동, 피해 등)를 발행합니다.
    - `TextLoggerService`를 참조하여 전투 로그 기록에 필요한 정보를 제공합니다. (예: AI 결정 로그 토글 `_logAIDecisions`)
- **특징**:
    - 상태 효과 처리 (`ProcessStatusEffects`).
    - AP(Action Point) 계산 및 관리 (`CalculateMoveAPCost`, `CalculateAttackAPCost`, `RecoverAPOnTurnStart`).
    - AI 행동 패턴 결정 로직 포함 (근접, 원거리, 방어, 지원).

## 2. TextLogger.cs

- **경로**: `Assets/AF/Scripts/Combat/TextLogger.cs`
- **역할**: 전투 과정에서 발생하는 다양한 이벤트와 정보를 텍스트 형태로 기록하는 로거 클래스입니다.
- **주요 기능**:
    - 로그 메시지를 내부 리스트(`_logs`)에 `LogEntry` 객체로 저장합니다. (`Log`, `LogEvent`)
    - `LogEvent` 메서드를 통해 다양한 전투 이벤트(`CombatSessionEvents`, `CombatActionEvents`, `DamageEvents`, `PartEvents`, `StatusEffectEvents`)를 받아 타입에 맞는 로그 메시지를 생성합니다.
    - 기록된 로그를 조회 (`GetLogs`), 검색 (`SearchLogs`), 파일로 저장 (`SaveToFile`)하는 기능을 제공합니다.
    - 로그 메시지 포맷팅 (`FormatLogEntry`) 및 리치 텍스트 태그(`ColorizeText`, `BoldText`, `ConvertSpriteTagToTextMarker`)를 활용한 가독성 향상 기능을 포함합니다.
    - 로그 출력 형식 제어를 위한 플래그(`ShowLogLevel`, `ShowTurnPrefix`, `UseIndentation`)를 가집니다.
- **특징**:
    - `ITextLogger` 인터페이스를 구현합니다.
    - `TextLoggerService`에 의해 관리되며, 직접 이벤트를 구독/발행하지 않습니다.
    - 전투 요약 정보 생성 (`GenerateBattleSummary`) 및 특정 유닛 상세 정보 로깅 (`LogUnitDetails`) 기능을 제공합니다.

## 3. TextLoggerService.cs

- **경로**: `Assets/AF/Scripts/Combat/TextLoggerService.cs`
- **역할**: `TextLogger` 인스턴스를 관리하고, 이벤트 버스를 통해 전달되는 전투 이벤트를 받아 `TextLogger`에 로그 기록을 요청하는 서비스입니다.
- **주요 기능**:
    - `IService` 인터페이스를 구현하며 서비스 로케이터에 등록됩니다.
    - `TextLogger` 인스턴스를 생성하고 초기화/종료를 관리합니다 (`Initialize`, `Shutdown`).
    - `EventBusService`를 구독(`SubscribeToEvents`)하여 전투 관련 이벤트를 수신합니다.
    - 수신한 이벤트를 처리하는 핸들러(`HandleCombatStart`, `HandleDamageApplied` 등)를 통해 `TextLogger`의 `Log` 또는 `LogEvent` 메서드를 호출하여 로그 기록을 위임합니다.
    - `TextLogger`의 포맷팅 옵션(`SetShowLogLevel`, `SetUseIndentation`, `SetLogActionSummaries`)을 외부에서 제어할 수 있는 인터페이스를 제공합니다.
    - 전투 시작/종료 시점, 턴 시작 시점에 유닛들의 상태 요약 로그를 기록하는 로직 (`LogAllUnitDetailsOnInit`, `LogUnitDetailsOnTurnStart`)을 포함합니다.
- **특징**:
    - `TextLogger`와 `EventBus` 사이의 중재자 역할을 수행합니다.
    - 로그 기록의 세부 구현(포맷팅, 저장 방식 등)은 `TextLogger`에 위임하고, 자신은 이벤트 처리와 로깅 시점 관리에 집중합니다.

## 4. CombatTestRunner.cs

- **경로**: `Assets/AF/Scripts/Tests/CombatTestRunner.cs`
- **역할**: 유니티 에디터 환경에서 `CombatSimulatorService`를 이용한 전투 테스트 시나리오를 설정하고 실행하기 위한 클래스입니다.
- **주요 기능**:
    - Odin Inspector를 활용하여 인스펙터 창에서 테스트 참가자(`AFSetup`) 및 전투 옵션(자동 진행, 로그 레벨 등)을 쉽게 설정할 수 있도록 지원합니다.
    - `AFSetup` 클래스를 통해 테스트에 사용할 `ArmoredFrame`을 커스텀 파츠 조합 또는 미리 정의된 `AssemblySO`를 기반으로 설정할 수 있습니다.
    - 필요한 `FrameSO`, `PartSO`, `WeaponSO`, `PilotSO` 데이터를 `Resources` 폴더에서 로드하고 관리합니다.
    - `StartCombatTestAsync` 메서드를 통해 설정된 내용으로 비동기 전투 테스트를 시작하고, `EndCombatTest`로 종료합니다.
    - 테스트용 `ArmoredFrame` 인스턴스를 생성하는 로직 (`CreateTestArmoredFrame`, `CreateCustomArmoredFrame`)을 포함합니다.
- **특징**:
    - 주로 유니티 에디터 환경에서의 테스트 편의성을 위해 사용됩니다 (`#if UNITY_EDITOR`).
    - `CombatSimulatorService`와 `TextLoggerService`를 참조하여 실제 전투 시뮬레이션 및 로깅을 수행합니다.
    - 팀 ID별 색상 표시, 파츠/파일럿 SO 미리보기 등 시각적인 편의 기능을 제공합니다.

---

*이 문서는 각 스크립트의 핵심적인 역할과 기능을 요약한 것으로, 상세한 로직은 해당 스크립트 코드를 직접 참조해야 합니다.* 