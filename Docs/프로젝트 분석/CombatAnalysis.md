# Combat 디렉토리 분석 (`Assets/AF/Scripts/Combat`)

이 문서는 `Assets/AF/Scripts/Combat` 디렉토리 및 그 하위 디렉토리(`Behaviours` - 현재는 사용되지 않음)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 턴 기반 전투 시스템의 핵심 로직, 로그 기록 등을 포함합니다.

## 핵심 서비스 및 컴포넌트

### 1. 전투 시뮬레이션 (`ICombatSimulatorService.cs`, `CombatSimulatorService.cs`)

-   **역할**: 전체 전투의 흐름을 관리하고 조정하는 핵심 서비스입니다. `IService`를 구현하여 `ServiceLocator`를 통해 접근합니다.
-   **주요 기능**:
    -   **전투 시작/종료 (`StartCombat`, `EndCombat`)**: 전투 세션(`_currentBattleId`, `_battleName`)을 초기화하고 참가자(`_participants`) 정보를 설정하며, 전투 종료 시 결과를 판정하고 관련 이벤트를 발행합니다. `StartCombat` 시 각 AI 유닛의 파일럿 전문화 타입(`unit.Pilot.Specialization`)에 따라 `RangedCombatBT.Create(unit)`, `MeleeCombatBT.Create(unit)`, `DefenderBT.Create(unit)`, `SupportBT.Create(unit)` (신규 추가), `BasicAttackBT.Create()` 등을 사용하여 적절한 행동 트리(`BehaviorTreeRoot`)를 할당하고 `AICtxBlackboard.ClearAllData()`를 호출하여 블랙보드를 초기화합니다. `EndCombat` 호출 시 또는 `CheckBattleEndCondition()`에서 `_resultEvaluator.EvaluateBattleResult()`의 결과가 `null`이 아닐 때 (명확한 승/패/전멸Draw) 전투를 종료하며, 이때 최종 유닛 상태 스냅샷(`finalSnapshot`)을 생성하여 `CombatEndEvent`에 포함시켜 발행합니다.
    -   **턴 및 사이클 관리 (`ProcessNextTurn`, `CurrentTurn`, `CurrentCycle`)**: 전투는 전체 라운드를 의미하는 **턴(`_currentTurn`)**과, 각 턴 내에서 유닛이 순차적으로 활성화되는 단계를 의미하는 **사이클(`_currentCycle`)**로 구성됩니다. `ProcessNextTurn`은 다음 유닛을 활성화(`GetNextActiveUnit`)하고, 모든 유닛이 현재 턴에서 행동을 마쳤는지(`_actedThisCycle` 확인) 여부에 따라 다음 턴으로 넘어가는 로직을 관리합니다. 새 턴이 시작되면 `RoundStartEvent`가, 턴이 종료되면 `RoundEndEvent`가 발행되며, 이후 `CheckBattleEndCondition()`을 호출합니다. `Reload` 액션의 AP 비용 계산 시에는 `CombatActionExecutor` 내부에서 유닛의 `AICtxBlackboard.WeaponToReload`를 참조할 가능성이 있습니다 (또는 `CombatActionExecutor`가 직접 계산).
    -   **유닛 활성화**: `_currentActiveUnit`으로 현재 활성화된 유닛을 관리하며, 활성화 시작/종료 시 `UnitActivationStartEvent`/`UnitActivationEndEvent`를 발행합니다.
    -   **행동 결정 및 위임 (행동 트리 기반)**: 활성화된 AI 유닛의 `ArmoredFrame.BehaviorTreeRoot.Tick()`을 호출하여 행동을 결정합니다. 유닛은 AP 및 행동 횟수 제한(코드 내 루프 및 조건문으로 관리, `ActionType.None`의 AP 비용이 `0f`로 수정되어 AP 부족으로 인한 의도치 않은 루프 종료 방지) 내에서 여러 행동을 시도할 수 있습니다. 각 행동 시도 후, 유닛의 `ArmoredFrame.AICtxBlackboard`에 기록된 `DecidedActionType` 및 관련 정보(대상, 무기, 목표 위치 등)를 바탕으로 `PerformAction()` (내부적으로 `_actionExecutor.ExecuteAction()`)을 호출하여 실행을 위임합니다. 공격 행동 직후 `AICtxBlackboard.ImmediateReloadWeapon`이 `true`로 설정되어 있다면, `PerformAction()`을 통해 즉시 재장전 액션을 시도합니다. `SupportBT`의 경우, 수리(`RepairAlly`, `RepairSelf`) 또는 방어(`Defend`) 등의 특화 행동이 이 과정을 통해 결정되고 실행될 수 있습니다.
    -   **상태 관리**: 전투 참가자 목록(`_participants`), 팀 정보(`_teamAssignments`), 그리고 유닛의 상태를 추적하기 위해 `_defendedThisTurn` (턴 당 방어 여부), `_actedThisCycle` (현재 턴/사이클에 행동한 유닛), `_movedThisActivation` (현재 활성화 주기 중 이동 여부), `_defendedThisActivation` (현재 활성화 주기 중 방어 여부) 등의 `HashSet` 컬렉션들을 사용합니다.
    -   **유틸리티**: `GetParticipants()`, `GetAllies(ArmoredFrame unit)`, `GetEnemies(ArmoredFrame unit)`, `HasUnitDefendedThisTurn(ArmoredFrame unit)`, `IsUnitDefeated(ArmoredFrame unit)` 등의 조회 기능을 제공합니다.
-   **의존성**: `EventBus`, `TextLoggerService`, `ICombatActionExecutor`, `IStatusEffectProcessor`, `IBattleResultEvaluator`, `BehaviorTree` 관련 클래스들, `UnityEngine` (Vector3, Time 등).
-   **특징**: 전투의 전체적인 오케스트레이션을 담당하며, 실제 액션 실행, 상태 효과 처리, 결과 판정 등은 하위 컴포넌트/서비스에 위임합니다. 세분화된 상태 추적 Set들을 활용합니다.

### 2. 전투 액션 실행 (`ICombatActionExecutor.cs`, `CombatActionExecutor.cs`)

-   **역할**: `CombatSimulatorService`로부터 위임받아 실제 전투 액션(공격, 이동, 방어, 재장전, 수리 등)을 수행하고 결과를 반환하며, 관련 이벤트를 발행합니다. `CombatContext`를 통해 전투 상황 정보를 전달받습니다.
-   **주요 기능 (`Execute` 메서드)**: (메소드 시그니처: `Execute(CombatContext ctx, ArmoredFrame actor, ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon, bool isCounter = false, bool freeCounter = false)`)
    -   **AP 비용 계산 및 확인**: 행동 타입에 따라 필요한 AP를 계산합니다. `ActionType.None`의 AP 비용은 `0f`로 명시적으로 처리됩니다. `DEFEND_AP_COST`, `REPAIR_ALLY_AP_COST`, `REPAIR_SELF_AP_COST` 등은 `public const` 상수로 정의되어 있으며, 공격(`CalculateAttackAPCost`) 및 이동(`CalculateMoveAPCost`) AP는 별도 메소드로 계산됩니다. `GetActionAPCost` 인터페이스 메서드도 제공됩니다. 행동 주체의 현재 AP가 충분한지 확인하며, 부족 시 실패 처리 및 `ActionCompletedEvent`를 발행합니다.
    -   **이동 횟수 제한**: 이동 액션(`ActionType.Move`)의 경우, `ctx.MovedThisActivation` Set을 확인하여 이미 해당 활성화 주기에 이동했으면 실패 처리합니다.
    -   **액션별 로직 수행**:
        -   **공격 (`ExecuteAttack`)**: 사거리, 탄약, 재장전 상태 확인 -> 명중 판정 -> 데미지 계산 -> 이벤트 발행 (`PartDestroyedEvent` 발생 시 `FrameWasActuallyDestroyed` 플래그 설정 포함) -> 파츠 데미지 적용 -> 반격 시도 (`TryCounterAttack`).
        -   **이동 (`PerformMoveToPosition`)**: 이동 가능 거리 내에서 목표 위치로 이동 -> 이벤트 발행. 성공 시 `ctx.MovedThisActivation.Add(actor)`로 이동 상태 기록.
        -   **방어 (`ApplyDefendStatus`)**: 방어 상태 플래그 설정 (`ctx.DefendedThisTurn` 및 `ctx.DefendedThisActivation` Set에 추가) 및 상태 효과 적용 -> 이벤트 발행.
        -   **재장전 (`ExecuteReload`)**: 무기의 재장전 시작 -> 이벤트 발행.
        -   **수리 (아군/자가)**: `GetMostDamagedPartSlot(actualTarget, ctx)` 메서드로 가장 손상된 파츠를 찾아 (`CombatContext` 전달) `BASE_REPAIR_AMOUNT` 만큼 수리 (`actualTarget.ApplyRepair(targetSlot, BASE_REPAIR_AMOUNT)`). 성공 시 `RepairAppliedEvent` 발행. `GetMostDamagedPartSlot`은 내구도 비율이 가장 낮은 작동 가능한 파츠를 반환하며, 수리할 파츠가 없는 경우 `null`을 반환하여 `Execute` 메서드가 `false`를 반환하도록 유도합니다.
    -   **이벤트 발행**: 행동 시작 시 `ActionStartEvent`, 종료 시 `ActionCompletedEvent` (성공 여부, 결과 설명, 최종 위치, 이동 거리 등 포함)를 발행합니다.
    -   **AP 소모**: 행동 성공 및 AP 비용이 있는 경우, 행동 주체의 AP를 소모합니다.
-   **행동 트리 연동 방식**: `CombatSimulatorService`가 행동 트리를 실행한 후, 결정된 액션 정보를 `CombatContext`와 함께 `CombatActionExecutor.Execute()`에 전달하여 실제 행동을 수행합니다. 이 과정에서 `CombatActionExecutor`는 `CombatContext`를 통해 현재 턴, 팀 정보, 다른 유닛 상태 등 전투 전반의 상황을 참조합니다. 예를 들어 `SupportBT`의 경우, `CanRepairTargetPartNode`와 같은 조건 노드가 `Blackboard`의 상태를 변경하고, 이후 `SetRepairAllyActionNode`가 `DecidedActionType`을 `RepairAlly`로 설정하면, `CombatSimulatorService`가 이를 감지하여 `CombatActionExecutor.Execute()`를 호출하게 됩니다.
-   **특징**: 구체적인 전투 액션의 성공/실패 판정, 상태 변화 적용, 결과 이벤트 발행을 담당하는 핵심 로직입니다. AP 비용 상수화, 이동 제한 로직, 구체화된 수리 로직 등이 포함됩니다.

### 3. 텍스트 로깅 (`ITextLogger.cs`, `TextLogger.cs`, `TextLoggerService.cs`)

-   **역할**: `TextLogger`는 전투 중 발생하는 모든 주요 이벤트와 상태 변화를 `LogEntry` 객체로 생성하여 상세한 텍스트 로그로 기록하고 관리합니다. `TextLoggerService`는 `TextLogger` 인스턴스를 관리하고, `EventBus`를 통해 이벤트를 자동 구독하여 로깅을 수행하며, Flavor Text 삽입, 팀 컬러 적용, 로그 포맷팅 제어 등의 추가 기능을 제공합니다.

-   **`LogEntry` 구조 (핵심 변경 사항)**: `public class LogEntry`로 변경되었으며, 다음과 같은 상세한 델타 정보 필드들이 대거 추가되어 로그의 정보량이 크게 증가했습니다.
    -   기본 정보: `Message`, `LogLevel`, `Timestamp`, `TurnNumber`, `CycleNumber`, `EventType`, `ContextUnit`, `ShouldUpdateTargetView`.
    -   스냅샷: `TurnStartStateSnapshot` (`Dictionary<string, ArmoredFrameSnapshot>`) - 턴 시작 시점의 전체 유닛 상태.
    -   이벤트별 델타 정보:
        -   `DamageApplied`: `Damage_SourceUnitName`, `Damage_TargetUnitName`, `Damage_DamagedPartSlot`, `Damage_AmountDealt`, `Damage_NewDurability`, `Damage_IsCritical`, `Damage_PartWasDestroyed`.
        -   `ActionCompleted`: `Action_ActorName`, `Action_Type`, `Action_IsSuccess`, `Action_ResultDescription`, `Action_TargetName`, `Action_DistanceMoved`, `Action_NewPosition`, `Action_IsCounterAttack`.
        -   `DamageAvoided`: `Avoid_SourceName`, `Avoid_TargetName`, `Avoid_Type`, `Avoid_IsCounterAttack`.
        -   `PartDestroyed`: `PartDestroyed_OwnerName`, `PartDestroyed_PartType`, `PartDestroyed_DestroyerName`, `PartDestroyed_SlotId`, `PartDestroyed_FrameWasActuallyDestroyed`.
        -   `WeaponFired`: `Weapon_AttackerName`, `Weapon_TargetName`, `Weapon_WeaponName`, `Weapon_IsHit`, `Weapon_IsCounterAttack`, `Weapon_CurrentAmmo`, `Weapon_MaxAmmo`.
        -   `StatusEffectApplied`: `StatusApplied_TargetName`, `StatusApplied_SourceName`, `StatusApplied_EffectType`, `StatusApplied_Duration`, `StatusApplied_Magnitude`.
        -   `StatusEffectExpired`: `StatusExpired_TargetName`, `StatusExpired_EffectType`, `StatusExpired_WasDispelled`.
        -   `StatusEffectTick`: `StatusTick_TargetName`, `StatusTick_EffectName`, `StatusTick_Value`, `StatusTick_TickType`.
        -   `RepairApplied`: `Repair_ActorName`, `Repair_TargetName`, `Repair_PartSlot`, `Repair_Amount`, `Repair_ActionType`.
        -   `CounterAttackAnnounced`: `Counter_DefenderName`, `Counter_AttackerName`.

-   **`TextLogger` 주요 기능**:
    -   **로그 기록 (`Log`, `LogEvent`)**: `LogEntry` 객체를 생성하여 내부 리스트(`_logs`)에 저장합니다. `LogEvent`는 `ICombatEvent`를 받아 내부적으로 특정 이벤트 핸들러(예: `LogDamageApplied`)를 호출하여 적절한 델타 정보가 포함된 `LogEntry`를 생성합니다.
    -   **기록 시점 필터링**: `AllowedLogLevels` (`LogLevelFlags`) 프로퍼티와 `ConvertLogLevelToFlag` 메서드를 사용하여, 설정된 레벨의 로그만 내부 리스트에 기록합니다.
    -   **포맷팅 (`FormatLogEntry`, `FormatLogEntryForFile`)**: `ShowLogLevel`, `ShowTurnPrefix`, `UseIndentation` 프로퍼티와 `TextLoggerService`의 `_logActionSummaries`, `_useSpriteIcons` 플래그, 그리고 `GetTeamColoredName` (팀 컬러 적용) 등을 활용하여 로그를 가독성 있게 포맷팅합니다.
    -   **파일 저장 (`SaveToFile`)**: `GetFormattedLogsForFileSaving`을 통해 포맷팅된 로그를 파일로 저장합니다. 파일 저장 시에는 `RemoveRichTextTags`로 색상 태그를 제거하고, `ConvertSpriteTagToTextMarker`로 스프라이트 태그를 텍스트 마커(예: `[HIT]`)로 변환합니다.
    -   **조회 및 기타**: `GetLogs`, `SearchLogs`, `Clear`, `GenerateBattleSummary`, `LogUnitDetails` 등의 유틸리티 메서드를 제공합니다.

-   **`TextLoggerService` 주요 기능**:
    -   `TextLogger` 인스턴스를 생성(`_textLogger`)하고 `IService`로 래핑하여 `ServiceLocator`를 통해 관리합니다.
    -   **이벤트 자동 구독**: `EventBus`를 구독하여 전투 관련 이벤트 발생 시 자동으로 `TextLogger.LogEvent` (또는 내부 핸들러)를 호출하여 해당 이벤트를 로그로 기록합니다.
    -   **Flavor Text 관리**: `FlavorTextSO` 에셋들을 `Resources/FlavorTexts` 폴더에서 로드(`LoadFlavorTextTemplates`)하여 `_flavorTextTemplates` (Dictionary<string, List<string>>)에 저장하고, `GetRandomFlavorText`와 `FormatFlavorText`를 통해 이벤트 로그에 무작위 Flavor Text를 삽입합니다. 일부 시스템 메시지(예: "명시적 대기")도 개선된 스타일로 제공합니다.
    -   **팀 컬러 적용 지원**: `CombatTestRunner` 서비스(`_combatTestRunnerCache`)를 참조하여 `GetTeamColoredName` 메서드를 통해 유닛 이름에 팀 색상을 적용한 문자열을 반환합니다. 이는 `TextLogger`의 포맷팅 과정에서 사용될 수 있습니다.
    -   **포맷팅 제어**: `TextLogger`의 `ShowLogLevel`, `ShowTurnPrefix`, `UseIndentation` 프로퍼티 및 자체 `_logActionSummaries`, `_useSpriteIcons` 플래그를 설정하는 public 메서드(`SetShowLogLevel`, `SetLogActionSummaries` 등)를 제공하여 외부에서 로그 포맷팅 방식을 제어할 수 있게 합니다.

-   **특징**: `LogEntry`의 대폭적인 정보 확장으로 매우 상세한 전투 과정 기록이 가능해졌습니다. 이벤트 기반 자동 로깅, 기록 시점 로그 레벨 필터링, 개선된 Flavor Text 시스템, 팀 컬러 연동, 다양한 로그 포맷팅 옵션 제어 기능을 통해 정밀한 분석 및 디버깅, 사용자 친화적인 로그 표시가 가능합니다.

### 4. 전투 컨텍스트 (`CombatContext.cs`)

-   **역할**: 전투 관련 서비스 및 상태 정보를 하나로 묶어 전달하는 클래스입니다. `ICombatActionExecutor`, 행동 트리 노드 등에 전달됩니다.
-   **포함 정보**: `EventBus`, `TextLoggerService`, `ICombatActionExecutor`, `BattleId` (전투 ID), `CurrentTurn` (현재 턴), `CurrentCycle` (현재 사이클), `DefendedThisTurn` (이번 턴에 방어한 유닛 목록), `Participants` (전체 참가자 목록), `TeamAssignments` (팀 정보), `MovedThisActivation` (이번 활성화에 이동한 유닛 목록), `DefendedThisActivation` (이번 활성화에 방어한 유닛 목록).
-   **목적**: 메서드 호출 시 필요한 다양한 정보들을 개별적으로 전달하는 대신, 하나의 컨텍스트 객체로 묶어 전달하여 코드 가독성과 유지보수성을 높입니다. `DefendedThisActivation`의 추가로 유닛의 현재 활성화 주기 내 방어 행동 여부를 정확히 추적할 수 있게 되었습니다.

### 5. 상태 효과 처리 (`StatusEffectProcessor.cs`, `IStatusEffectProcessor`)

-   **역할**: 유닛에게 적용된 상태 효과의 턴 기반 처리(틱 효과 적용, 지속 시간 감소, 만료 처리)를 담당합니다.
-   **주요 기능 (`Tick` 메서드)**: (메소드 시그니처: `Tick(CombatContext ctx, ArmoredFrame unit)`)
    -   유닛의 `ActiveStatusEffects` 목록을 순회합니다.
    -   지속 시간이 있는 효과의 `DurationTurns`를 감소시키고, 0 이하가 되면 효과를 제거(`unit.RemoveStatusEffect`)하고 관련 이벤트(`StatusEffectExpiredEvent`, `ctx.Bus`를 통해 발행)를 발행합니다.
    -   틱 효과(DoT, HoT 등)가 있는 경우, 해당 효과를 적용(예: DoT의 경우 `unit.ApplyDamage` 호출)하고 관련 이벤트(`StatusEffectTickEvent`, `ctx.Bus`를 통해 발행)를 발행합니다.
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

1.  `CombatSimulatorService.StartCombat` 호출로 전투가 시작되며, 각 AI 유닛의 전문화 타입에 따라 적절한 행동 트리(예: `RangedCombatBT`, `MeleeCombatBT`, `DefenderBT`, `SupportBT`, `BasicAttackBT`)가 할당되고 블랙보드가 초기화됩니다.
2.  `CombatSimulatorService.ProcessNextTurn`이 호출될 때마다 다음 유닛이 활성화됩니다. `Reload` 액션의 AP 비용 계산 시에는 유닛의 `AICtxBlackboard.WeaponToReload`를 참조하거나 `CombatActionExecutor`가 직접 계산합니다.
3.  활성화된 AI 유닛은 AP와 최대 행동 횟수 제한 내에서 `BehaviorTreeRoot.Tick()`을 반복적으로 호출하여 블랙보드에 행동 결정 사항을 기록하고, `CombatSimulatorService`는 이 결정에 따라 `CombatActionExecutor.Execute()`를 호출하여 실제 행동을 수행합니다. `ActionType.None`의 AP 비용이 `0f`로 처리되면서, AP가 부족하더라도 유닛이 최소한의 행동(대기 등)을 결정할 수 있는 기회를 가질 수 있게 되었습니다.