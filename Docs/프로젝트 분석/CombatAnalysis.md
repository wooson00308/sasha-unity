# Combat 디렉토리 분석 (`Assets/AF/Scripts/Combat`)

이 문서는 `Assets/AF/Scripts/Combat` 디렉토리 및 그 하위 디렉토리(`Behaviours` - 현재는 사용되지 않음)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 턴 기반 전투 시스템의 핵심 로직, 로그 기록 등을 포함합니다.

## 핵심 서비스 및 컴포넌트

### 1. 전투 시뮬레이션 (`ICombatSimulatorService.cs`, `CombatSimulatorService.cs`)

-   **역할**: 전체 전투의 흐름을 관리하고 조정하는 핵심 서비스입니다. `IService`를 구현하여 `ServiceLocator`를 통해 접근합니다.
-   **주요 기능**:
    -   **전투 시작/종료 (`StartCombat`, `EndCombat`)**: 전투 세션을 초기화하고 참가자 정보를 설정하며, 전투 종료 시 결과를 판정하고 관련 이벤트를 발행합니다. 각 AI 유닛의 파일럿 전문화 타입(`SpecializationType`)에 따라 적절한 행동 트리(예: `RangedCombatBT`, `MeleeCombatBT`, `BasicAttackBT`)를 할당하고 블랙보드를 초기화합니다. `BattleResultEvaluator.Evaluate()`의 결과가 `null`이 아닐 때 (명확한 승/패/전멸Draw) `EndCombat`을 호출하여 전투를 종료합니다.
    -   **턴 및 사이클 관리 (`ProcessNextTurn`, `CurrentTurn`, `CurrentCycle`)**: 전투는 **턴(Round)**으로 구성되며, 각 턴은 모든 유닛이 한 번씩 활성화되는 **사이클(Cycle)**로 나뉩니다. `ProcessNextTurn`은 다음 유닛을 활성화하고, 모든 유닛이 활성화되면 다음 턴으로 넘어가는 로직을 관리합니다. 특히, 한 턴에 모든 유닛의 행동이 끝나거나 더 이상 행동할 유닛이 없을 경우, 다음 턴으로 넘어가기 전에 `CheckBattleEndCondition()`을 통해 전투 종료 여부를 우선 확인합니다. 만약 전투가 아직 끝나지 않았다면 (`_isInCombat`이 `true`이고 `BattleResultEvaluator`가 `null`을 반환), `ProcessNextTurn`은 `true`를 반환하여 다음 턴이 정상적으로 시작되도록 보장합니다 (이는 특정 상황에서 전투가 "Aborted"로 조기 종료되던 문제를 해결합니다). `Reload` 액션의 AP 비용 계산 시에는 유닛의 `AICtxBlackboard.WeaponToReload`를 참조합니다.
    -   **유닛 활성화**: 턴마다 정해진 순서에 따라 유닛을 활성화(`_currentActiveUnit`)하고 관련 이벤트(`UnitActivationStart/EndEvent`)를 발행합니다.
    -   **행동 결정 및 위임 (행동 트리 기반)**: 활성화된 AI 유닛의 `ArmoredFrame.BehaviorTreeRoot`에 할당된 행동 트리를 `Tick()`합니다. **유닛은 AP가 허용하고 최대 행동 횟수 제한 내에서 여러 행동을 시도할 수 있습니다.** 각 행동 시도 후 유닛의 `ArmoredFrame.AICtxBlackboard`에 기록된 `DecidedActionType` 및 관련 정보(대상, 무기, 목표 위치 등)를 바탕으로 `ICombatActionExecutor`에게 실행을 위임합니다. 공격 행동 직후 `AICtxBlackboard.ImmediateReloadWeapon`이 설정되어 있다면, 즉시 재장전 액션을 시도합니다.
    -   **상태 관리**: 전투 참가자 목록, 팀 정보, 유닛 상태(방어 여부, 행동 완료 여부 등)를 추적합니다.
    -   **유틸리티**: 참가자, 아군, 적군 목록 조회, 유닛 격파 여부 확인 등의 기능을 제공합니다.
-   **의존성**: `EventBus`, `TextLoggerService`, `ICombatActionExecutor`, `IStatusEffectProcessor`, `IBattleResultEvaluator`, `BehaviorTree` 관련 클래스들.
-   **특징**: 전투의 전체적인 오케스트레이션을 담당하며, 실제 액션 실행, 상태 효과 처리, 결과 판정 등은 하위 컴포넌트/서비스에 위임합니다.

### 2. 전투 액션 실행 (`ICombatActionExecutor.cs`, `CombatActionExecutor.cs`)

-   **역할**: `CombatSimulatorService`로부터 위임받아 실제 전투 액션(공격, 이동, 방어, 재장전, 수리 등)을 수행하고 결과를 반환하며, 관련 이벤트를 발행합니다.
-   **주요 기능 (`Execute` 메서드)**:
    -   **AP 비용 계산 및 확인**: 행동 타입에 따라 필요한 AP를 계산하고 (`GetActionAPCost` 인터페이스 메서드 활용), 행동 주체의 현재 AP가 충분한지 확인합니다.
    -   **액션별 로직 수행**:
        -   **공격**: 사거리, 탄약, 재장전 상태 확인 -> 명중 판정 -> 데미지 계산 -> 이벤트 발행 (`PartDestroyedEvent` 발생 시 `FrameWasActuallyDestroyed` 플래그 설정 포함) -> 파츠 데미지 적용 -> 반격 시도.
        -   **이동**: 이동 가능 거리 내에서 목표 위치로 이동 -> 이벤트 발행.
        -   **방어**: 방어 상태 플래그 설정 및 상태 효과 적용 -> 이벤트 발행.
        -   **재장전**: 무기의 재장전 시작 -> 이벤트 발행.
        -   **수리 (아군/자가)**: 가장 손상된 파츠 탐색 -> 수리량 적용 -> 이벤트 발행.
    -   **이벤트 발행**: 행동 시작/완료, 무기 발사, 데미지/회피/파츠 파괴/수리 관련 이벤트 등을 적절한 시점에 발행합니다. `PartDestroyedEvent`의 경우, 해당 파츠 파괴로 인해 프레임 전체가 파괴되었는지 여부를 나타내는 `FrameWasActuallyDestroyed` 플래그를 포함하여 발행합니다.
    -   **AP 소모**: 행동 성공 시 계산된 AP 비용만큼 소모.
-   **행동 트리 연동 방식**: `CombatSimulatorService`는 각 유닛의 행동 트리(`BehaviorTreeRoot.Tick()`)를 실행한 후, 유닛의 `AICtxBlackboard`에 기록된 `DecidedActionType`과 관련 정보를 읽어옵니다. 이 정보를 바탕으로 `CombatActionExecutor.Execute()`를 호출하여 실제 게임 액션을 수행하고 AP를 소모하며, 관련된 전투 이벤트를 발행합니다. 대부분의 행동 트리 액션 노드(예: `AttackTargetNode`, `MoveToTargetNode`)는 행동을 '결정'하고 블랙보드에 필요한 정보를 기록하는 역할을 하며, 실제 실행은 `CombatSimulatorService`에 의해 중앙에서 관리됩니다. 단, `ReloadWeaponNode`와 같이 일부 액션 노드는 `Execute` 메서드 내에서 직접 `CombatActionExecutor`를 호출하여 행동을 실행하기도 합니다.
-   **특징**: 구체적인 전투 액션의 성공/실패 판정, 상태 변화 적용, 결과 이벤트 발행을 담당하는 핵심 로직입니다.

### 3. 텍스트 로깅 (`ITextLogger.cs`, `TextLogger.cs`, `TextLoggerService.cs`)

-   **역할**: 전투 중 발생하는 모든 주요 이벤트와 상태 변화를 상세한 텍스트 로그로 기록하고 관리합니다. 일부 시스템 메시지 (예: "명시적 대기")는 좀 더 SF 메카물에 어울리는 스타일로 개선되었습니다.
-   **`TextLogger` 주요 기능**:
    -   **로그 기록 (`Log`, `LogEvent`)**: 메시지, 로그 레벨(`LogLevel`), 이벤트 타입(`LogEventType`), 턴/사이클 정보, 관련 유닛, 스냅샷/델타 정보 등을 포함하는 `LogEntry` 객체를 생성하여 내부 리스트(`_logs`)에 저장합니다. **기록 시점 필터링**: `AllowedLogLevels` 프로퍼티에 설정된 플래그에 따라, 허용된 레벨의 로그만 내부 리스트에 기록합니다.
    -   **`LogEntry` 구조**: 각 로그 항목은 기본 정보 외에, 특정 `LogEventType`에 따라 관련된 상세 데이터(델타 정보)를 저장합니다 (예: `DamageAppliedEvent`의 경우 공격자, 피격자, 데미지량, 파츠 슬롯 등). `PartDestroyedEvent` 로그의 경우 `PartDestroyed_SlotId` 필드를 통해 파괴된 파츠의 구체적인 슬롯 ID를 기록합니다.
    -   **스냅샷 저장**: `RoundStart`, `UnitActivationStart` 등 특정 시점에는 모든 유닛의 전체 상태 스냅샷(`Dictionary<string, ArmoredFrameSnapshot>`)을 함께 기록하여, 로그 재생 시 특정 시점의 상태를 복원할 수 있도록 합니다.
    -   **포맷팅 및 조회**: 로그 레벨, 턴 번호, 들여쓰기 등을 적용하여 로그를 가독성 있게 포맷팅하고(`FormatLogEntry`), 다양한 조건(로그 레벨, 검색어)으로 로그를 필터링하여 조회하는 기능을 제공합니다.
    -   **파일 저장 (`SaveToFile`, `GetFormattedLogsForFileSaving`)**: 기록된 로그를 파일로 저장합니다. 파일 저장 시에는 Unity 에디터 콘솔과 달리 색상 태그(`<color=...>`)는 제거되고, 스프라이트 태그(`<sprite index=...>`)는 텍스트 마커(`[HIT]`, `[MISS]` 등)로 변환되어 가독성을 높입니다.
    -   **전투 요약 (`GenerateBattleSummary`)**: 전투 결과, 턴 수, 피해량 등을 요약하여 제공합니다.
-   **`TextLoggerService` 주요 기능**:
    -   `TextLogger` 인스턴스를 생성하고 `IService`로 래핑하여 `ServiceLocator`를 통해 관리합니다.
    -   **이벤트 자동 구독**: `EventBus`를 구독하여 전투 관련 이벤트(세션 시작/종료, 턴/활성화 시작/종료, 액션 완료, 데미지 발생 등)가 발생하면 자동으로 `TextLogger.LogEvent`를 호출하여 해당 이벤트를 로그로 기록합니다.
    -   **Flavor Text 관리**: `FlavorTextSO` 에셋을 로드하여 이벤트 로그에 무작위 Flavor Text를 삽입하는 기능을 제공합니다 (`GetRandomFlavorText`, `FormatFlavorText`). 일부 시스템 메시지(예: "명시적 대기", "AI 모듈 연결 실패")도 보다 몰입감 있는 SF 스타일로 개선되었습니다.
    -   **포맷팅 제어**: `TextLogger`의 로그 포맷팅 옵션(로그 레벨 표시, 턴 접두사 표시 등)을 외부에서 제어할 수 있는 메서드를 제공합니다. (필요시, `TextLogger`의 `AllowedLogLevels`를 설정하여 기록될 로그 레벨을 제어하는 기능도 포함 가능)
-   **특징**: 이벤트 기반 자동 로깅과 상세한 델타/스냅샷 정보를 통해 전투 과정을 정밀하게 기록하고 분석/재생할 수 있는 기반을 제공합니다.

### 4. 전투 컨텍스트 (`CombatContext.cs`)

-   **역할**: 전투 관련 서비스 및 상태 정보를 하나로 묶어 전달하는 **클래스**입니다 (기존 `readonly struct`에서 변경됨). `ICombatActionExecutor`, 행동 트리 노드 등에 전달됩니다.
-   **포함 정보**: `EventBus`, `TextLoggerService`, `ICombatActionExecutor`, 전투 ID, 현재 턴/사이클 번호, 방어 유닛 목록, 전체 참가자 목록, 팀 정보, 이번 활성화에 이동한 유닛 목록.
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
    -   살아남은 팀의 수에 따라 결과를 결정합니다 (0팀: 무승부, 1팀: 승리/패배 판정). **2팀 이상 생존 시에는 `null`을 반환하여 전투가 지속되도록 합니다.** (이전에는 Draw 처리)
-   **사용**: `CombatSimulatorService.CheckBattleEndCondition()` 메서드 내에서 호출되어, 그 결과가 `null`이 아닐 때만 전투 종료 로직이 진행됩니다.

## 파일럿 행동 결정 방식의 변화 (행동 트리 시스템)

-   **기존 시스템 (레거시)**: 과거에는 `IPilotBehaviorStrategy` 인터페이스와 그 구현체들(`MeleeCombatBehaviorStrategy` 등)을 사용하여 파일럿의 전문화 타입에 따라 행동 로직을 분리했습니다. 각 전략은 `DetermineAction` 메서드를 통해 행동을 결정했습니다.
-   **현재 시스템 (행동 트리)**: 현재는 `IPilotBehaviorStrategy` 시스템 대신 **행동 트리(Behavior Tree)** 기반 시스템으로 완전히 전환되었습니다. 이 시스템은 `Assets/AF/Scripts/AI/BehaviorTree/` 경로에 구현되어 있습니다.
    -   **핵심 구성요소**: `BTNode` (기본), `SelectorNode` (OR), `SequenceNode` (AND) 같은 복합 노드와, 실제 조건 검사 및 행동 결정을 담당하는 다양한 잎새 노드(`ConditionNode`, `ActionNode`의 파생 클래스들)로 구성됩니다.
    -   **주요 노드**: `IsTargetInRangeNode`, `HasEnoughAPNode` (동적 AP 계산), `NeedsReloadNode` (`WeaponToReload` 설정), `AttackTargetNode` (공격 결정), `MoveToTargetNode` (이동 결정), `ReloadWeaponNode` (재장전 실행), `SelectTargetNode` 등 다양한 노드가 구현되어 사용됩니다. 특히 `ReloadWeaponNode`는 재장전 시작 시 `Success`를 반환하여, 재장전 대기 턴 중 다른 행동 탐색이 가능하게 합니다.
    -   **데이터 공유**: `Blackboard` 클래스 인스턴스(`ArmoredFrame.AICtxBlackboard`)를 통해 노드 간 데이터 공유 및 최종 행동 결정 사항(예: `DecidedActionType`, `CurrentTarget`, `WeaponToReload`, `

## 시스템 흐름 요약

1.  `CombatSimulatorService.StartCombat` 호출로 전투가 시작되며, 각 AI 유닛의 전문화 타입에 따라 적절한 행동 트리(예: `RangedCombatBT`, `MeleeCombatBT`, `BasicAttackBT`)가 할당되고 블랙보드가 초기화됩니다.
2.  `CombatSimulatorService.ProcessNextTurn`이 호출될 때마다 다음 유닛이 활성화됩니다. `Reload` 액션의 AP 비용 계산 시에는 유닛의 `AICtxBlackboard.WeaponToReload`를 참조합니다.
3.  활성화된 AI 유닛은 AP와 최대 행동 횟수 제한 내에서 `BehaviorTreeRoot.Tick()`을 반복적으로 호출하여 블랙보드에 행동 결정 사항을 기록하고, `CombatSimulatorService`는 이 정보를 바탕으로 `CombatActionExecutor.Execute`를 호출하여 실제 행동을 실행합니다. 각 행동 실행 후에는 컨텍스트(예: `MovedThisActivation`)가 업데이트됩니다. 만약 공격 액션 후 `AICtxBlackboard.ImmediateReloadWeapon`이 설정되어 있다면, `CombatSimulatorService`는 이어서 재장전 액션을 시도합니다. `ReloadWeaponNode`는 재장전 시작 시 `Success`를 반환하여, 재장전 대기 턴 중 다른 행동 탐색이 가능하게 합니다.
4.  행동 실행 시 AP가 부족하거나, 유닛이 비활성화되거나, 최대 행동 횟수에 도달하면 해당 유닛의 활성화가 종료됩니다.
5.  `CombatActionExecutor`는 AP 확인, 액션별 로직 수행, 결과 반환 및 관련 이벤트를 `EventBus`에 발행합니다. (`PartDestroyedEvent`에는 `FrameWasActuallyDestroyed` 플래그 포함)
6.  `TextLoggerService`는 `EventBus`를 구독하여 전투 이벤트를 자동으로 로그로 기록합니다. (`LogEntry`에는 `PartDestroyed_SlotId` 포함, 일부 시스템 메시지 스타일 변경)
7.  `CombatSimulatorService`는 현재 유닛의 활성화가 종료되거나, 혹은 한 턴에 모든 유닛이 행동을 마쳤을 경우 다시 `ProcessNextTurn`을 호출하여 다음 유닛을 활성화하거나 다음 턴을 준비합니다.
8.  턴 시작 시 `StatusEffectProcessor.Tick`이 각 유닛에 대해 호출되어 상태 효과를 처리합니다.
9.  `CombatSimulatorService`는 매 유닛 활성화 종료 시 또는 새 턴 시작 전에 `CheckBattleEndCondition()` (`BattleResultEvaluator.Evaluate()` 호출)을 통해 전투 종료 조건을 확인합니다. `BattleResultEvaluator`가 `Victory`, `Defeat`, `Draw` 중 하나를 반환하면 `EndCombat`을 호출하여 전투를 종료합니다. 만약 `BattleResultEvaluator`가 `null`을 반환하고 (즉, 두 팀 이상 생존), 현재 턴에서 더 이상 행동할 유닛이 없다면 (`_currentActiveUnit == null`), `ProcessNextTurn`은 `true`를 반환하여 다음 턴으로 정상적으로 진행되도록 보장합니다.

## 결론