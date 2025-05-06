# Combat 디렉토리 분석 (`Assets/AF/Scripts/Combat`)

이 문서는 `Assets/AF/Scripts/Combat` 디렉토리 및 그 하위 디렉토리(`Behaviours`)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 턴 기반 전투 시스템의 핵심 로직, 로그 기록, AI 행동 전략 등을 포함합니다.

## 핵심 서비스 및 컴포넌트

### 1. 전투 시뮬레이션 (`ICombatSimulatorService.cs`, `CombatSimulatorService.cs`)

-   **역할**: 전체 전투의 흐름을 관리하고 조정하는 핵심 서비스입니다. `IService`를 구현하여 `ServiceLocator`를 통해 접근합니다.
-   **주요 기능**:
    -   **전투 시작/종료 (`StartCombat`, `EndCombat`)**: 전투 세션을 초기화하고 참가자 정보를 설정하며, 전투 종료 시 결과를 판정하고 관련 이벤트를 발행합니다.
    -   **턴 및 사이클 관리 (`ProcessNextTurn`, `CurrentTurn`, `CurrentCycle`)**: 전투는 **턴(Round)**으로 구성되며, 각 턴은 모든 유닛이 한 번씩 활성화되는 **사이클(Cycle)**로 나뉩니다. `ProcessNextTurn`은 다음 유닛을 활성화하고, 모든 유닛이 활성화되면 다음 턴으로 넘어가는 로직을 관리합니다.
    -   **유닛 활성화**: 턴마다 정해진 순서(현재는 참여 순서, 향후 속도 기반 등 변경 가능)에 따라 유닛을 활성화(`_currentActiveUnit`)하고 관련 이벤트(`UnitActivationStart/EndEvent`)를 발행합니다.
    -   **행동 결정 및 위임**: 활성화된 유닛이 AI 유닛일 경우, 파일럿의 전문화 타입(`SpecializationType`)에 맞는 `IPilotBehaviorStrategy`를 사용하여 행동을 결정합니다. 결정된 행동(공격, 이동, 방어 등)은 `ICombatActionExecutor`에게 실행을 위임합니다.
    -   **상태 관리**: 전투 참가자 목록, 팀 정보, 유닛 상태(방어 여부, 행동 완료 여부 등)를 추적합니다.
    -   **유틸리티**: 참가자, 아군, 적군 목록 조회, 유닛 격파 여부 확인 등의 기능을 제공합니다.
-   **의존성**: `EventBus`, `TextLoggerService`, `ICombatActionExecutor`, `IStatusEffectProcessor`, `IBattleResultEvaluator`, `IPilotBehaviorStrategy` 구현체들.
-   **특징**: 전투의 전체적인 오케스트레이션을 담당하며, 실제 액션 실행, 상태 효과 처리, 결과 판정 등은 하위 컴포넌트/서비스에 위임합니다.

### 2. 전투 액션 실행 (`ICombatActionExecutor.cs`, `CombatActionExecutor.cs`)

-   **역할**: `CombatSimulatorService`로부터 위임받아 실제 전투 액션(공격, 이동, 방어, 재장전, 수리 등)을 수행하고 결과를 반환하며, 관련 이벤트를 발행합니다.
-   **주요 기능 (`Execute` 메서드)**:
    -   **AP 비용 계산 및 확인**: 행동 타입에 따라 필요한 AP를 계산하고, 행동 주체의 현재 AP가 충분한지 확인합니다.
    -   **액션별 로직 수행**:
        -   **공격**: 사거리, 탄약, 재장전 상태 확인 -> 명중 판정(정확도, 회피율, 거리 보정) -> 데미지 계산(기본 데미지, 공격력, 방어력, 치명타) -> `DamageAppliedEvent` 또는 `DamageAvoidedEvent` 발행 -> 피격 파츠 결정 -> 파츠 데미지 적용 (`Part.ApplyDamage`) -> 파츠 파괴 시 `PartDestroyedEvent` 발행 -> 반격 시도(`TryCounterAttack`).
        -   **이동**: 이동 가능 거리 내에서 목표 위치로 이동(`actor.Position` 업데이트) -> `ActionCompletedEvent` 발행 (이동 거리, 최종 위치 포함).
        -   **방어**: 방어 상태 플래그 설정(`ctx.DefendedThisTurn`) 및 관련 상태 효과 적용 -> `ActionCompletedEvent` 발행.
        -   **재장전**: 무기의 재장전 시작(`weapon.StartReload`) -> `ActionCompletedEvent` 발행 (재장전 시작 명시).
        -   **수리 (아군/자가)**: 가장 손상된 파츠 탐색 -> 수리량 적용(`target.ApplyRepair`) -> `RepairAppliedEvent` 발행 -> `ActionCompletedEvent` 발행.
    -   **이벤트 발행**: 행동 시작(`ActionStartEvent`), 행동 완료(`ActionCompletedEvent`), 무기 발사(`WeaponFiredEvent`), 데미지/회피/파츠 파괴/수리 관련 이벤트 등을 적절한 시점에 발행합니다.
    -   **AP 소모**: 행동 성공 시 계산된 AP 비용만큼 소모(`actor.ConsumeAP`).
-   **[추가] 행동 트리 연동 고려사항**: 행동 트리 기반 시스템에서는 `CombatActionExecutor`가 행동 노드(`AttackTargetNode`, `MoveToTargetNode` 등)의 실행 결과(`Success`, `Failure`)와 상태 변경(`ArmoredFrame`의 `IntendedMovePosition` 설정 등)을 해석하여 실제 게임 액션(공격 애니메이션, 경로 탐색 및 이동 실행, AP 소모)으로 변환하는 역할이 추가로 필요합니다. 이 부분은 향후 구현될 예정입니다.
-   **특징**: 구체적인 전투 액션의 성공/실패 판정, 상태 변화 적용, 결과 이벤트 발행을 담당하는 핵심 로직입니다.

### 3. 텍스트 로깅 (`ITextLogger.cs`, `TextLogger.cs`, `TextLoggerService.cs`)

-   **역할**: 전투 중 발생하는 모든 주요 이벤트와 상태 변화를 상세한 텍스트 로그로 기록하고 관리합니다.
-   **`TextLogger` 주요 기능**:
    -   **로그 기록 (`Log`, `LogEvent`)**: 메시지, 로그 레벨(`LogLevel`), 이벤트 타입(`LogEventType`), 턴/사이클 정보, 관련 유닛, 스냅샷/델타 정보 등을 포함하는 `LogEntry` 객체를 생성하여 내부 리스트(`_logs`)에 저장합니다.
    -   **`LogEntry` 구조**: 각 로그 항목은 기본 정보 외에, 특정 `LogEventType`에 따라 관련된 상세 데이터(델타 정보)를 저장합니다 (예: `DamageAppliedEvent`의 경우 공격자, 피격자, 데미지량, 파츠 슬롯 등).
    -   **스냅샷 저장**: `RoundStart`, `UnitActivationStart` 등 특정 시점에는 모든 유닛의 전체 상태 스냅샷(`Dictionary<string, ArmoredFrameSnapshot>`)을 함께 기록하여, 로그 재생 시 특정 시점의 상태를 복원할 수 있도록 합니다.
    -   **포맷팅 및 조회**: 로그 레벨, 턴 번호, 들여쓰기 등을 적용하여 로그를 가독성 있게 포맷팅하고(`FormatLogEntry`), 다양한 조건(로그 레벨, 검색어)으로 로그를 필터링하여 조회하는 기능을 제공합니다.
    -   **파일 저장 (`SaveToFile`)**: 기록된 로그를 파일로 저장합니다.
    -   **전투 요약 (`GenerateBattleSummary`)**: 전투 결과, 턴 수, 피해량 등을 요약하여 제공합니다.
-   **`TextLoggerService` 주요 기능**:
    -   `TextLogger` 인스턴스를 생성하고 `IService`로 래핑하여 `ServiceLocator`를 통해 관리합니다.
    -   **이벤트 자동 구독**: `EventBus`를 구독하여 전투 관련 이벤트(세션 시작/종료, 턴/활성화 시작/종료, 액션 완료, 데미지 발생 등)가 발생하면 자동으로 `TextLogger.LogEvent`를 호출하여 해당 이벤트를 로그로 기록합니다.
    -   **Flavor Text 관리**: `FlavorTextSO` 에셋을 로드하여 이벤트 로그에 무작위 Flavor Text를 삽입하는 기능을 제공합니다 (`GetRandomFlavorText`, `FormatFlavorText`).
    -   **포맷팅 제어**: `TextLogger`의 로그 포맷팅 옵션(로그 레벨 표시, 턴 접두사 표시 등)을 외부에서 제어할 수 있는 메서드를 제공합니다.
-   **특징**: 이벤트 기반 자동 로깅과 상세한 델타/스냅샷 정보를 통해 전투 과정을 정밀하게 기록하고 분석/재생할 수 있는 기반을 제공합니다.

### 4. 전투 컨텍스트 (`CombatContext.cs`)

-   **역할**: 전투 관련 서비스 및 상태 정보를 하나로 묶어 액션 실행기(`CombatActionExecutor`), AI 전략(`IPilotBehaviorStrategy`) 등에 전달하는 읽기 전용 구조체입니다.
-   **포함 정보**: `EventBus`, `TextLoggerService`, 전투 ID, 현재 턴/사이클 번호, 방어 유닛 목록, 전체 참가자 목록, 팀 정보, 이번 활성화에 이동한 유닛 목록.
-   **목적**: 메서드 호출 시 필요한 다양한 정보들을 개별적으로 전달하는 대신, 하나의 컨텍스트 객체로 묶어 전달하여 코드 가독성과 유지보수성을 높입니다.

### 5. 상태 효과 처리 (`StatusEffectProcessor.cs`, `IStatusEffectProcessor`)

-   **역할**: 유닛에게 적용된 상태 효과의 턴 기반 처리(틱 효과 적용, 지속 시간 감소, 만료 처리)를 담당합니다.
-   **주요 기능 (`Tick` 메서드)**:
    -   유닛의 `ActiveStatusEffects` 목록을 순회합니다.
    -   지속 시간이 있는 효과의 `DurationTurns`를 감소시키고, 0 이하가 되면 효과를 제거(`unit.RemoveStatusEffect`)하고 관련 이벤트(`StatusEffectExpiredEvent`)를 발행합니다.
    -   틱 효과(DoT, HoT 등)가 있는 경우, 해당 효과를 적용(예: `DamageAppliedEvent` 발행)하고 관련 이벤트(`StatusEffectTickEvent`)를 발행합니다.
-   **사용**: `CombatSimulatorService`의 턴 처리 로직 내에서 각 유닛에 대해 호출될 것으로 예상됩니다.

### 6. 전투 결과 판정 (`BattleResultEvaluator.cs`, `IBattleResultEvaluator`)

-   **역할**: 전투 종료 시 참가자들의 최종 상태와 팀 정보를 바탕으로 전투 결과(승리, 패배, 무승부, 중단)를 판정합니다.
-   **주요 기능 (`Evaluate` 메서드)**:
    -   작동 가능한 유닛들을 팀별로 그룹화합니다.
    -   살아남은 팀의 수에 따라 결과를 결정합니다 (0팀: 무승부, 1팀: 승리/패배 판정, 2팀 이상: 무승부).
-   **사용**: `CombatSimulatorService.EndCombat` 메서드 내에서 호출됩니다.

## 파일럿 행동 전략 (`Behaviours/`)

-   **`IPilotBehaviorStrategy.cs`**: 모든 파일럿 AI 행동 전략 클래스가 구현해야 하는 인터페이스입니다. `DetermineAction` 메서드를 정의하여 현재 상황(`CombatContext`)에서 수행할 최적의 행동(액션 타입, 대상, 목표 위치, 사용할 무기)을 결정하도록 요구합니다.
-   **`PilotBehaviorStrategyBase.cs`**: 모든 전략 클래스의 기본 클래스 역할을 할 수 있는 추상 클래스입니다. 공통 상수(AP 비용, 거리 계수 등)와 유틸리티 메서드(AP 비용 계산, 체력 확인 등)를 제공합니다.
-   **구체적인 전략 클래스들** (`MeleeCombatBehaviorStrategy`, `RangedCombatBehaviorStrategy`, `DefenseCombatBehaviorStrategy`, `SupportCombatBehaviorStrategy`, `StandardCombatBehaviorStrategy`):
    -   각각 파일럿의 특정 `SpecializationType`에 맞는 행동 로직을 구현합니다.
    -   일반적으로 특정 우선순위에 따라 행동을 결정합니다 (예: 수리 -> 재장전 -> 공격 -> 이동 -> 방어 -> 대기).

**[수정] 현재 리팩토링 진행 중:**
-   기존 `IPilotBehaviorStrategy` 기반 시스템은 현재 **행동 트리(Behavior Tree)** 기반 시스템으로 점진적으로 리팩토링되고 있습니다 (`Assets/AF/Scripts/AI/BehaviorTree/`).
-   새로운 시스템에서는 `BTNode`를 상속하는 다양한 조건 노드(`IsTargetInRangeNode`, `HasEnoughAPNode` 등)와 액션 노드(`AttackTargetNode`, `MoveToTargetNode`, `SelectTargetNode` 등)를 조합하여 파일럿의 행동 로직을 구성합니다.
-   각 파일럿 타입이나 숙련도에 따라 다른 구조의 행동 트리를 할당하여 더 유연하고 복잡한 AI 행동 패턴을 구현하는 것을 목표로 합니다.
-   이 리팩토링이 완료되면, `DetermineAction` 메서드는 해당 파일럿의 행동 트리를 실행(`Execute`)하는 방식으로 변경될 가능성이 높습니다.

## 이벤트 (`*Events.cs`, `ICombatEvent.cs`)

-   **역할**: 전투 시스템 내의 다양한 상태 변화와 액션 결과를 알리기 위한 구체적인 이벤트 데이터 구조를 정의합니다.
-   **`ICombatEvent`**: 모든 전투 관련 이벤트가 상속받는 마커 인터페이스입니다 (`IEvent`도 상속).
-   **주요 이벤트 카테고리**:
    -   `CombatSessionEvents`: 전투 시작/종료, 턴/라운드 시작/종료, 유닛 활성화 시작/종료.
    -   `CombatActionEvents`: 액션 시작/완료, 무기 발사, 수리 적용.
    -   `DamageEvents`: 데미지 적용, 데미지 회피.
    -   `PartEvents`: 파츠 파괴, 시스템 치명적 실패(Body 파괴 등).
    -   `StatusEffectEvents`: 상태 효과 적용/만료/틱.
-   **특징**: 각 이벤트는 발생한 상황에 대한 구체적인 데이터(관련 유닛, 액션 타입, 결과 값, 턴 정보 등)를 포함하며, `EventBus`를 통해 발행되어 `TextLoggerService`나 다른 관심 있는 시스템(UI 등)에서 구독하여 처리합니다.

## 열거형 (`LogLevel.cs`, `LogEventType.cs`)

-   `LogLevel`: 텍스트 로그의 심각도/종류를 정의합니다 (Debug, Info, Success, Warning, Error, Danger, Critical, System).
-   `LogEventType`: `TextLogger`에서 로그 항목의 종류를 구분하고, 델타/스냅샷 저장 로직을 결정하는 데 사용됩니다.

## 시스템 흐름 요약

1.  `CombatSimulatorService.StartCombat` 호출로 전투가 시작됩니다.
2.  `CombatSimulatorService.ProcessNextTurn`이 호출될 때마다 다음 유닛이 활성화됩니다.
3.  활성화된 유닛이 AI인 경우, `SpecializationType`에 맞는 `IPilotBehaviorStrategy`가 `DetermineAction`을 통해 행동을 결정합니다.
4.  결정된 행동은 `CombatSimulatorService`를 통해 `CombatActionExecutor.Execute`로 전달됩니다.
5.  `CombatActionExecutor`는 AP 확인, 액션별 로직 수행(명중/데미지 계산, 이동 처리, 상태 변경 등), 결과 반환 및 관련 이벤트(ActionCompleted, DamageApplied 등)를 `EventBus`에 발행합니다.
6.  `TextLoggerService`는 `EventBus`를 구독하고 있다가 관련 전투 이벤트가 발생하면 자동으로 `TextLogger`를 통해 로그를 기록합니다. 이 때 스냅샷 또는 델타 정보가 함께 저장될 수 있습니다.
7.  `CombatSimulatorService`는 활성화된 유닛의 행동이 끝나면 다시 `ProcessNextTurn`을 호출하여 다음 유닛을 활성화하거나, 모든 유닛이 행동했으면 다음 턴으로 넘어갑니다.
8.  턴 시작 시 `StatusEffectProcessor.Tick`이 각 유닛에 대해 호출되어 상태 효과를 처리합니다.
9.  `CombatSimulatorService`는 매 사이클마다 전투 종료 조건(`CheckBattleEndCondition`)을 확인하고, 조건 만족 시 `EndCombat`을 호출하여 결과를 판정하고 전투를 종료합니다.

## 결론

`Assets/AF/Scripts/Combat` 디렉토리는 복잡한 턴 기반 전투 시스템을 체계적으로 구현하고 있습니다. `CombatSimulatorService`가 전체 흐름을 조율하고, `CombatActionExecutor`가 실제 행동을 처리하며, `TextLogger` 시스템은 상세한 로그 기록을 담당합니다. 파일럿 AI는 `IPilotBehaviorStrategy` 패턴을 통해 모듈식으로 구현되어 있으며, `EventBus`를 이용한 이벤트 기반 통신으로 각 컴포넌트 간의 결합도를 낮추고 있습니다. `CombatContext`는 필요한 정보를 효율적으로 전달하는 역할을 합니다. 이 구조는 전투 로직의 확장과 유지보수를 용이하게 만들 것으로 보입니다. 