# 행동 트리(Behavior Tree) 시스템 분석

## 1. 개요 및 도입 배경

기존 `IPilotBehaviorStrategy` 기반의 AI 행동 결정 로직을 행동 트리(Behavior Tree, BT) 시스템으로 전환함으로써, 다음과 같은 개선을 목표로 하였다:

-   AI 행동 패턴의 유연성 및 확장성 증대
-   파일럿 숙련도 등 다양한 변수에 따른 AI 행동 양상 차별화
-   AI 로직의 가독성 및 유지보수성 향상

본 문서는 `Assets/AF/Scripts/AI/BehaviorTree/` 경로에 구현된 행동 트리 시스템의 주요 구성 요소, 데이터 흐름, 그리고 타 시스템과의 통합 방식을 분석한다.

## 2. 핵심 구성 요소: 기본 노드 타입 (`AF.AI.BehaviorTree` 네임스페이스)

행동 트리는 계층적인 노드 구조를 통해 복잡한 AI 행동 로직을 모듈화한다. 기본 노드 타입은 다음과 같다.

### 2.1. `BTNode.cs` (기본 노드)

모든 행동 트리 노드의 추상 기반 클래스이다.

-   **`NodeStatus` 열거형**: 노드의 실행 결과 상태(`Running`, `Success`, `Failure`)를 정의한다.
-   **`Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)` 추상 메서드**: 각 노드의 핵심 실행 로직을 정의한다. 에이전트(`agent`), 데이터 공유 저장소(`blackboard`), 전투 컨텍스트(`context`)를 파라미터로 받아 실행에 필요한 정보를 활용한다.

### 2.2. `SelectorNode.cs` (선택 노드 - OR 로직)

자식 노드들을 순서대로 `Tick`하며, 자식 중 하나라도 `Success` 또는 `Running`을 반환하면 즉시 해당 상태를 반환한다. 모든 자식이 `Failure`를 반환해야만 `Failure`로 처리된다.

-   `List<BTNode> childNodes`를 통해 자식 노드들을 관리한다.

### 2.3. `SequenceNode.cs` (순차 노드 - AND 로직)

자식 노드들을 순서대로 `Tick`하며, 자식 중 하나라도 `Failure`를 반환하면 즉시 `Failure`로 처리된다. 모든 자식이 `Success`를 반환해야 `Success`로 간주되며, 자식 중 하나라도 `Running`이면 `Running` 상태를 반환한다.

-   `List<BTNode> childNodes`를 통해 자식 노드들을 관리한다.

## 3. 핵심 구성 요소: 잎새 노드 (Leaf Nodes) - 조건 및 액션 (`AF.AI.BehaviorTree` 네임스페이스)

잎새 노드는 행동 트리의 가장 말단에서 실제 조건 검사 또는 행동 결정을 수행한다. 이 노드들은 일반적으로 상태를 가지지 않으며(stateless), `Tick` 메서드의 파라미터를 통해 실행에 필요한 정보를 얻는다.

### 3.1. `ConditionNode` (조건 노드 기반 클래스)

특정 조건을 검사하여 그 결과에 따라 `Success` 또는 `Failure`를 반환하는 역할을 한다.

**주요 구현 조건 노드:**

-   **`IsTargetInRangeNode`**: 현재 대상(`blackboard.CurrentTarget`)이 선택된 무기(`blackboard.SelectedWeapon`)의 유효 사거리 내에 있는지 검사한다.
-   **`HasEnoughAPNode`**: 지정된 행동 타입(`actionType`)에 필요한 AP를 `CombatActionExecutor`를 통해 동적으로 계산하여 에이전트(`agent`)가 충분히 보유하고 있는지 확인한다. (참고: `CombatSimulatorService`는 `Reload` 액션의 AP 계산 시 `blackboard.WeaponToReload`를 참조한다.)
-   **`IsTargetAliveNode` / `HasValidTargetNode`**: 현재 대상(`blackboard.CurrentTarget`)이 유효하며 파괴되지 않았는지 검사한다.
-   **`NeedsReloadNode`**: 에이전트가 장착한 무기 중 재장전이 가장 시급한 무기(탄약 부족 또는 소진 기준)를 찾아 `blackboard.WeaponToReload`에 설정하고, 현재 선택된 무기(`blackboard.SelectedWeapon`)는 `null`로 초기화한다. 적합한 무기가 없으면 `Failure`를 반환한다.
-   **`IsHealthLowNode`**: 에이전트의 현재 체력 비율이 설정된 임계값(`healthThresholdPercentage`) 이하인지 검사한다.
-   **`IsTargetTooCloseNode`**: 현재 대상과의 거리가 너무 가까운지(카이팅 필요 여부) 판단한다. 명시적 카이팅 거리 또는 무기의 최소 사거리와 안전 마진을 기준으로 검사한다.
-   **`CanMoveThisActivationNode`**: 현재 유닛 활성화 주기 동안 이미 이동 행동을 수행했는지 `CombatContext.MovedThisActivation` 플래그를 통해 확인한다.

### 3.2. `ActionNode` (액션 노드 기반 클래스)

특정 행동을 "결정"하거나 "의사 표시"하는 역할을 하며, 그 결과로 `Success` 또는 `Failure`를 반환한다. 일부 액션은 진행 중 상태(`Running`)를 가질 수 있으나, 현재 구현된 노드들은 대부분 즉시 결과를 반환한다.

-   **중요**: 대부분의 액션 노드는 실제 게임 월드 액션을 직접 실행하지 않고, 결정된 행동의 종류와 관련 정보를 `Blackboard`에 기록하는 역할을 수행한다. 실제 액션 실행은 `CombatSimulatorService`에 의해 중앙에서 관리된다. 단, `ReloadWeaponNode`와 같이 일부 노드는 직접 `CombatActionExecutor`를 호출하여 행동을 시작하기도 한다.

**주요 구현 액션 노드:**

-   **`AttackTargetNode`**: 현재 대상과 선택된 무기가 유효하고 공격 가능한 상태이면, `blackboard.DecidedActionType = Attack`으로 설정하고 `Success`를 반환한다. 공격 후 즉시 재장전이 필요하다고 판단되면 `blackboard.ImmediateReloadWeapon = true`로 설정하여 후속 재장전을 요청할 수 있다.
-   **`MoveToTargetNode`**: 현재 대상이 선택된 무기의 최적 교전 거리에 있지 않고 이동 AP가 충분하다면, 적절한 교전 위치를 `blackboard.IntendedMovePosition`에 설정하고 `blackboard.DecidedActionType = Move`로 지정한 후 `Success`를 반환한다.
-   **`ReloadWeaponNode`**: `blackboard.WeaponToReload`에 지정된 무기가 있을 경우, `CombatActionExecutor`를 통해 해당 무기의 재장전을 "시작"시키고 `NodeStatus.Success`를 반환한다. 이후 `blackboard.WeaponToReload`는 `null`로 초기화된다. 이 노드는 재장전 "시작"의 성공 여부만 판단하므로, 실제 재장전이 완료되기까지 여러 턴이 소요되더라도 다른 행동을 탐색할 수 있게 한다.
-   **`DefendNode`**: 방어에 필요한 AP가 충분하다면, `blackboard.DecidedActionType = Defend`로 설정하고 `Success`를 반환한다.
-   **`SelectTargetNode`**: 유효한 적 중 가장 적합한 대상(예: 가장 가까운 적)을 찾아 `blackboard.CurrentTarget`에 설정하고, 해당 대상에 사용할 무기가 있다면 `blackboard.SelectedWeapon`도 설정한 후 `Success`를 반환한다. 적합한 대상이나 무기가 없으면 `Failure`를 반환한다.
-   **`WaitNode`**: 특별한 행동 없이 항상 `Success`를 반환한다.
-   **`MoveAwayFromTargetNode`**: 현재 대상으로부터 일정 거리(무기 최소 사거리 + 버퍼 또는 지정된 거리)만큼 멀어지는 위치를 `blackboard.IntendedMovePosition`에 설정하고, `blackboard.DecidedActionType = Move`로 지정한 후 `Success`를 반환한다.

## 4. 핵심 구성 요소: `Blackboard.cs` (데이터 공유 저장소) (`AF.AI.BehaviorTree` 네임스페이스)

행동 트리 내의 여러 노드 간에 데이터를 공유하고, AI가 최종적으로 결정한 행동과 관련된 정보를 저장하는 중앙 데이터 저장소 역할을 한다.

-   **주요 데이터 필드**: `CurrentTarget` (현재 목표 대상), `IntendedMovePosition` (목표 이동 위치), `DecidedActionType` (결정된 행동 종류), `SelectedWeapon` (선택된 무기), `WeaponToReload` (재장전할 무기), `ImmediateReloadWeapon` (즉시 재장전 필요 여부 및 대상 무기) 등을 포함한다. `ImmediateReloadWeapon`은 주로 공격 직후 재장전이 필요할 때 설정된다.
-   **데이터 접근**: 제네릭 `SetData<T>`, `GetData<T>` 메서드를 통해 다양한 타입의 데이터를 유연하게 저장하고 검색할 수 있다.
-   **소유 및 초기화**: 각 `ArmoredFrame` 인스턴스는 `AICtxBlackboard`라는 이름으로 자신만의 `Blackboard` 인스턴스를 소유하며, 이 블랙보드는 전투 시작 시 또는 각 유닛의 활성화 시점에 적절히 초기화된다.

## 5. 시스템 통합 및 실행 흐름 분석

행동 트리 시스템은 기존 게임 로직, 특히 `ArmoredFrame` 모델 및 `CombatSimulatorService`와 긴밀하게 통합되어 실행된다.

1.  **`ArmoredFrame` 모델 연동**:
    *   `ArmoredFrame` 클래스에는 `public BTNode BehaviorTreeRoot { get; set; }` 속성이 추가되어, 각 유닛이 실행할 행동 트리의 루트 노드를 참조한다.
    *   또한, `public Blackboard AICtxBlackboard { get; private set; }` 속성을 통해 각 유닛 전용 블랙보드 인스턴스를 관리하며, 이는 생성자에서 초기화된다.

2.  **`Blackboard` 클래스 활용**: 위 4번 항목에서 설명된 `Blackboard` 클래스는 행동 트리 실행에 필수적인 데이터를 저장하고 공유하는 역할을 수행한다 (예: `ImmediateReloadWeapon` 프로퍼티를 통한 즉시 재장전 관리).

3.  **BT 노드 시그니처 표준화**:
    *   모든 행동 트리 노드의 핵심 실행 메서드는 `Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)` 시그니처를 사용하여 일관성을 유지하며, 실행에 필요한 모든 정보를 이 파라미터를 통해 전달받는다.

4.  **`CombatSimulatorService`와의 통합 및 실행 상세**:
    *   기존 `IPilotBehaviorStrategy` 관련 로직은 제거되고, 행동 트리 기반으로 대체되었다.
    *   **전투 시작 시 (`StartCombat`)**: `CombatSimulatorService`는 전투에 참여하는 각 `ArmoredFrame`의 `BehaviorTreeRoot`에 해당 유닛의 파일럿 전문화 타입(`SpecializationType`)에 따라 적절한 행동 트리(예: `RangedCombatBT.Create(unit)`, `MeleeCombatBT.Create(unit)`, `BasicAttackBT.Create()`)를 동적으로 생성하여 할당하고, `AICtxBlackboard`를 초기화한다.
    *   **턴 처리 시 (`ProcessNextTurn`) 유닛 활성화 및 행동 결정**:
        *   현재 활성화된 유닛(`_currentActiveUnit`)의 블랙보드는 해당 유닛의 턴이 시작될 때 초기화된다.
        *   유닛은 AP가 소진되거나 최대 행동 횟수에 도달할 때까지 `while` 루프 내에서 여러 번 행동을 시도할 수 있다.
        *   **각 행동 시도 루프**: 
            1.  블랙보드의 `DecidedActionType` 등 이전 행동 결정 관련 데이터를 초기화한다.
            2.  `_currentActiveUnit.BehaviorTreeRoot.Tick(...)`을 호출하여 행동 트리를 실행하고 행동을 결정한다.
            3.  `Tick()` 실행 후, `_currentActiveUnit.AICtxBlackboard`를 확인하여 `DecidedActionType`, 관련 데이터(대상, 무기 등), 그리고 `ImmediateReloadWeapon` 설정 여부를 읽어온다.
            4.  결정된 행동(`DecidedActionType`)을 `_actionExecutor.Execute(...)`를 통해 실행한다.
            5.  만약 `ImmediateReloadWeapon`이 블랙보드에 설정되어 있다면(즉, 특정 무기를 즉시 재장전해야 한다면), 주 행동(예: 공격) 실행 후 이어서 해당 무기에 대한 재장전 액션을 `_actionExecutor.Execute(...)`를 통해 실행한다. 이 경우, `Reload` 액션의 AP 비용은 `AICtxBlackboard.WeaponToReload` (또는 `ImmediateReloadWeapon`이 가리키는 무기)를 기준으로 계산된다.
            6.  즉시 재장전 실행 후에는 `AICtxBlackboard`의 `ImmediateReloadWeapon` 관련 상태를 초기화할 수 있다.