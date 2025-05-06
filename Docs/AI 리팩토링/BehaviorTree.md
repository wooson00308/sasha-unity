# AI 행동 트리 (Behavior Tree) 구현

## 1. 목표

기존 `IPilotBehaviorStrategy` 기반의 AI 행동 결정 로직을 행동 트리 기반으로 리팩토링하여 다음과 같은 이점을 얻고자 한다:

-   보다 유연하고 확장 가능한 AI 행동 패턴 구현
-   파일럿 숙련도에 따른 다양한 AI 행동 양상 적용
-   AI 로직의 가독성 및 유지보수성 향상

## 2. 기본 노드 타입 정의 (`AF.AI.BehaviorTree` 네임스페이스)

행동 트리를 구성하는 기본 노드 타입을 `Assets/AF/Scripts/AI/BehaviorTree/` 경로에 다음과 같이 정의한다.

### 2.1. `BTNode.cs` (기본 노드)

모든 행동 트리 노드의 기반이 되는 추상 클래스.

-   `NodeStatus` 열거형 (`Running`, `Success`, `Failure`) 정의
-   `Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)` 추상 메서드 정의: 모든 노드는 이 메서드를 구현하여 실행 로직을 정의한다. 실행에 필요한 정보는 파라미터로 주입받는다.

### 2.2. `SelectorNode.cs` (선택 노드 - OR)

자식 노드들을 순서대로 `Tick`하며, 하나라도 `Success` 또는 `Running`을 반환하면 즉시 해당 상태를 반환. 모든 자식이 `Failure`여야만 `Failure`를 반환.

-   `List<BTNode> childNodes`를 가짐.
-   `Tick` 메서드 로직 구현.

### 2.3. `SequenceNode.cs` (순차 노드 - AND)

자식 노드들을 순서대로 `Tick`하며, 하나라도 `Failure`를 반환하면 즉시 `Failure`를 반환. 모든 자식이 `Success`여야 `Success`를 반환. 자식 중 하나라도 `Running`이면 `Running`을 반환.

-   `List<BTNode> childNodes`를 가짐.
-   `Tick` 메서드 로직 구현.

## 3. 잎새 노드 (Leaf Nodes) 정의 (`AF.AI.BehaviorTree` 네임스페이스)

실제 조건 검사 및 행동 결정을 담당하는 잎새 노드들은 `ConditionNode`와 `ActionNode`를 상속받아 구현한다. 이 노드들은 일반적으로 **상태를 가지지 않으며(stateless)**, 실행 시 필요한 정보는 `Tick` 메서드의 파라미터(`agent`, `blackboard`, `context`)를 통해 얻는다.

### 3.1. `ConditionNode` (조건 노드 기반 클래스)

-   `Tick` 추상 메서드를 상속받음.
-   조건 검사 후 `Success` 또는 `Failure`를 반환.

**구현된 조건 노드 예시:**

-   **`IsTargetInRangeNode`**: `blackboard.CurrentTarget`이 `blackboard.SelectedWeapon` (또는 주무기)의 유효 사거리 내에 있는지 검사.
-   **`HasEnoughAPNode`**: 생성 시 지정된 `requiredAP`를 `agent`가 가지고 있는지 검사.
-   **`IsTargetAliveNode`**: `blackboard.CurrentTarget`이 유효하고 파괴되지 않았는지 검사.
-   **`NeedsReloadNode`**: `blackboard.SelectedWeapon` (또는 주무기)가 재장전을 필요로 하는지 검사.
-   **`IsHealthLowNode`**: `agent`의 현재 체력 비율이 생성 시 지정된 `healthThresholdPercentage` 이하인지 검사.
-   **`HasValidTargetNode`**: `blackboard.CurrentTarget`이 유효하고 파괴되지 않았는지 검사 (`IsTargetAliveNode`와 동일 로직).

### 3.2. `ActionNode` (액션 노드 기반 클래스)

-   `Tick` 추상 메서드를 상속받음.
-   행동 "결정" 또는 "의사 표시" 후 `Success` 또는 `Failure`를 반환. 일부 액션은 진행 중 상태(`Running`)를 가질 수 있음 (현재 구현된 노드에는 없음).
-   **주의**: 대부분의 액션 노드는 실제 행동을 실행하지 않고, 결정된 내용을 `Blackboard`에 기록하는 역할을 한다.

**구현된 액션 노드 예시:**

-   **`AttackTargetNode`**: `blackboard.CurrentTarget`과 `blackboard.SelectedWeapon`이 유효하고 공격 가능한 상태이면, `blackboard.DecidedActionType = Attack` 설정 후 `Success` 반환.
-   **`MoveToTargetNode`**: `blackboard.CurrentTarget`이 `blackboard.SelectedWeapon`(또는 주무기)의 사거리 밖에 있고 `agent`가 최소 이동 AP를 가졌다면, `blackboard.IntendedMovePosition`에 타겟 위치 설정 후 `Success` 반환.
-   **`ReloadWeaponNode`**: `blackboard.SelectedWeapon`(또는 주무기)가 재장전 필요하고 `agent`가 충분한 AP를 가졌다면, `blackboard.DecidedActionType = Reload`, `blackboard.SelectedWeapon` 설정 후 `Success` 반환.
-   **`DefendNode`**: `agent`가 방어에 필요한 AP를 가졌다면, `blackboard.DecidedActionType = Defend` 설정 후 `Success` 반환.
-   **`SelectTargetNode`**: 유효한 적 중 가장 가까운 대상을 찾아 `blackboard.CurrentTarget`에 설정 후 `Success` 반환. 적합한 대상이 없으면 `Failure` 반환.
-   **`WaitNode`**: 항상 `Success` 반환 (특별한 행동 없음).

## 4. `Blackboard.cs` 정의 (`AF.AI.BehaviorTree` 네임스페이스)

행동 트리 노드 간 데이터 공유 및 최종 행동 결정을 저장하기 위한 클래스.

-   주요 데이터 필드 (`CurrentTarget`, `IntendedMovePosition`, `DecidedActionType`, `SelectedWeapon` 등) 제공.
-   제네릭 `SetData<T>`, `GetData<T>` 메서드로 유연한 데이터 저장/검색 지원.
-   각 `ArmoredFrame` 인스턴스는 자신만의 `Blackboard` 인스턴스(`AICtxBlackboard` 속성)를 소유하며, 전투 시작 또는 유닛 활성화 시점에 초기화될 수 있다.

## 5. 행동 트리 통합 및 실행 과정 (구현 완료)

기존 시스템에 행동 트리를 통합하고 실행하는 과정은 다음과 같이 구현되었다:

1.  **`ArmoredFrame` 모델 수정 완료**:
    *   `public BTNode BehaviorTreeRoot { get; set; }` 속성 추가.
    *   `public Blackboard AICtxBlackboard { get; private set; }` 속성 추가 및 생성자에서 초기화.

2.  **`Blackboard` 클래스 도입 완료** (상기 4번 항목 참조).

3.  **BT 노드 시그니처 변경 완료**:
    *   모든 BT 관련 노드의 핵심 실행 메서드는 `Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)` 시그니처를 사용하도록 수정됨.

4.  **`CombatSimulatorService` 수정 완료**:
    *   기존 `IPilotBehaviorStrategy` 관련 필드 및 초기화 로직 제거.
    *   `StartCombat` 메서드: 참가하는 각 `ArmoredFrame`의 `BehaviorTreeRoot`에 `BasicAttackBT.Create()`로 생성된 트리를 할당하고 `AICtxBlackboard`를 초기화.
    *   `ProcessNextTurn` 메서드:
        *   현재 활성화된 유닛(`_currentActiveUnit`)의 블랙보드를 초기화.
        *   `_currentActiveUnit.BehaviorTreeRoot.Tick(...)` 호출.
        *   `Tick` 후, `_currentActiveUnit.AICtxBlackboard`를 확인하여 `DecidedActionType` 및 관련 데이터(타겟, 무기 등)를 읽음.
        *   읽어온 정보를 바탕으로 `_actionExecutor.Execute(...)` 호출.
        *   `DetermineActionForUnit` 메서드 제거.

5.  **`CombatTestRunner` 수정 완료**:
    *   `CreateTestArmoredFrame`, `CreateCustomArmoredFrame` 메서드: `ArmoredFrame` 생성 시 `BehaviorTreeRoot` 할당 및 `AICtxBlackboard` 초기화 로직 추가.
    *   `StartCombatTestAsync` 메서드: 재사용되는 플레이어 스쿼드 유닛(`existingAf`)의 `AICtxBlackboard` 초기화 로직 추가.

---
(향후 개선 사항: 파일럿 특성별 BT 할당, 더 다양한 노드 추가, BT 실행 로직 개선 등) 