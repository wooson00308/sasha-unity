# BehaviorTree 스크립트 (AF/Scripts/AI/BehaviorTree)

> SASHA-Unity 프로젝트의 AI 시스템에서 사용되는 Behavior Tree 관련 C# 스크립트(.cs 파일) 문서입니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/AI/BehaviorTree`
  - Behavior Tree 노드의 기본 클래스 및 핵심 로직 파일들이 있습니다.
  - `BTNode.cs`: Behavior Tree 노드의 기본 추상 클래스. 모든 노드는 이 클래스를 상속받아 `Tick` 메서드를 구현하며, AI 에이전트의 상태와 전투 컨텍스트를 기반으로 행동을 결정하고 `NodeStatus` (Running, Success, Failure) 결과를 반환합니다.
  - `ActionNode.cs`: 특정 행동을 수행하는 **잎새 노드**의 기본 클래스. 이 클래스를 상속받는 노드들은 실제 게임 내 액션을 실행하며, 그 결과에 따라 Success, Failure, 또는 Running 상태를 반환합니다.
  - `ConditionNode.cs`: 특정 조건을 검사하는 **잎새 노드**의 기본 클래스. 이 클래스를 상속받는 노드들은 주어진 조건이 충족되는지 판단하고, 충족되면 Success, 아니면 Failure를 반환합니다.
  - `SequenceNode.cs`: **컴포지트 노드**. 자식 노드들을 **순서대로** 실행합니다. 자식 노드 중 하나라도 `Failure`를 반환하면 즉시 실행을 멈추고 자신도 `Failure`를 반환합니다. 모든 자식 노드가 `Success`를 반환해야 자신도 `Success`를 반환합니다. 도중에 `Running`인 자식이 있으면 자신도 `Running`을 반환합니다.
  - `SelectorNode.cs`: **컴포지트 노드**. 자식 노드들을 **순서대로** 실행합니다. 자식 노드 중 하나라도 `Success`를 반환하면 즉시 실행을 멈추고 자신도 `Success`를 반환합니다. 모든 자식 노드가 `Failure`를 반환해야 자신도 `Failure`를 반환합니다. 도중에 `Running`인 자식이 있으면 자신도 `Running`을 반환합니다.
  - `UtilitySelectorNode.cs`: **유틸리티 기반 선택 노드**. 각 자식 액션의 효용값을 계산하여 가장 높은 효용값을 가진 액션을 실행합니다. 기존의 단순한 우선순위 기반 선택 대신 상황에 따른 동적 우선순위를 제공하며, 디버깅을 위한 로깅 기능과 효용값 추적 기능을 포함합니다.
  - `Blackboard.cs`: Behavior Tree 실행 중 AI 에이전트가 공유하는 데이터를 저장하는 공간입니다. 현재 대상 (`CurrentTarget`), 결정된 행동 타입 (`DecidedActionType`), 선택된 무기 (`SelectedWeapon`) 등 AI의 판단에 필요한 정보를 노드들이 읽고 쓰며 상태를 공유합니다. 각 에이전트는 자신만의 Blackboard 인스턴스를 가집니다.

  - `/Actions`
    - 특정 행동을 수행하는 액션 노드 스크립트들이 있습니다.
    - **유틸리티 시스템 관련:**
      - `IUtilityAction.cs`: 유틸리티 기반 행동을 정의하는 인터페이스. `CalculateUtility()` 메서드로 현재 상황에서의 효용값(0.0~1.0)을 계산하고, `Execute()` 메서드로 행동을 실행합니다.
      - `BTNodeUtilityAction.cs`: 기존 BTNode를 유틸리티 액션으로 래핑하는 어댑터 클래스. 기존 노드들을 유틸리티 시스템에서 사용할 수 있게 해줍니다.
    - **기본 행동 노드들:**
      - `AttackTargetNode.cs`: 블랙보드에 설정된 목표 대상에게 블랙보드에 설정된 무기로 공격할 것을 결정하고 `Blackboard.DecidedActionType`을 `Attack`으로 설정하는 노드입니다.
      - `ConfirmAbilityUsageNode.cs`: 블랙보드에 선택된 어빌리티(`Blackboard.SelectedAbility`) 사용에 필요한 AP가 충분한지 확인하고, 사용 가능하면 `Blackboard.DecidedActionType`을 `UseAbility`로 설정하는 노드입니다.
      - `DefendNode.cs`: 유닛이 방어 행동(`CombatActionEvents.ActionType.Defend`)을 할 것을 결정하고 `Blackboard.DecidedActionType`에 기록하는 노드입니다.
      - `MoveToTargetNode.cs`: 블랙보드에 설정된 목표 대상에게 이동할 위치(`Blackboard.IntendedMovePosition`)를 결정하고 `Blackboard.DecidedActionType`을 `Move`로 설정하는 노드입니다.
      - `ReloadWeaponNode.cs`: 블랙보드에 지정된 무기(`Blackboard.WeaponToReload`) 또는 주무기를 재장전할 것을 결정하고 `Blackboard.DecidedActionType`을 `Reload`로 설정하는 노드입니다.
      - `WaitNode.cs`: AI가 현재 턴에 아무 행동도 하지 않고 대기할 것을 결정하는 노드입니다. 항상 `NodeStatus.Success`를 반환합니다.
    - **타겟팅 관련:**
      - `SelectTargetNode.cs`: 자신과 다른 팀의 파괴되지 않은 유닛 중 가장 가까운 적을 찾아 `Blackboard.CurrentTarget`으로 설정하는 노드입니다.
      - `SelectNearestAllyNode.cs`: 가장 가까운 아군 유닛을 찾아 `Blackboard.CurrentTarget`에 기록하는 노드입니다.
      - `SelectLowestHealthAllyNode.cs`: 체력이 가장 낮은 아군 유닛을 찾아 `Blackboard.CurrentTarget`에 기록하는 노드입니다.
    - **무기 및 어빌리티 관련:**
      - `SelectAlternativeWeaponNode.cs`: 현재 선택된 무기가 사용 불가능할 때 대체 무기를 선택하는 노드입니다.
      - `SelectSelfActiveAbilityNode.cs`: 자신에게 사용할 수 있는 액티브 어빌리티를 찾아 `Blackboard.SelectedAbility`에 기록하는 노드입니다.
    - **수리 시스템 관련:**
      - `RepairSelfNode.cs`: 스스로를 수리하는 행동을 결정하고 `Blackboard.DecidedActionType`에 기록하는 노드입니다.
      - `SetRepairAllyActionNode.cs`: 아군 수리 행동을 설정하는 노드입니다.
      - `SetPreservedRepairTargetNode.cs`: 블랙보드에 수리 대상 데이터를 보존하는 노드입니다.
      - `RestorePreservedRepairTargetNode.cs`: 블랙보드에 보존된 수리 대상 데이터를 복원하는 노드입니다.
      - `ClearPreservedRepairDataNode.cs`: 블랙보드에 저장된 보존된 수리 대상 데이터를 삭제하는 노드입니다.

  - `/Conditions`
    - 특정 조건을 판단하는 조건 노드 스크립트들이 있습니다.
    - **행동 가능성 검사:**
      - `CanMoveThisActivationNode.cs`: 현재 활성화 주기 내에서 이동이 가능한지 판단하는 노드입니다.
      - `CanDefendThisActivationNode.cs`: 현재 활성화 주기 내에서 방어가 가능한지 판단하는 노드입니다.
      - `CanRepairTargetPartNode.cs`: 대상 유닛의 특정 파츠를 수리할 수 있는지 판단하는 노드입니다.
      - `HasEnoughAPNode.cs`: 특정 행동(`ActionType`)을 수행하기에 충분한 AP(행동력)가 있는지 판단하는 노드입니다.
      - `HasRepairUsesNode.cs`: 수리 기능에 남은 사용 횟수가 있는지 판단하는 노드입니다.
    - **타겟 및 무기 상태 검사:**
      - `HasSelectedWeaponNode.cs`: 블랙보드에 `SelectedWeapon`이 유효하게 설정되어 있는지 판단하는 노드입니다.
      - `HasValidTargetNode.cs`: 블랙보드에 `CurrentTarget`이 유효하게 설정되어 있는지 판단하는 노드입니다.
      - `IsSelectedWeaponUsableForAttackNode.cs`: 선택된 무기가 공격에 사용 가능한 상태인지 판단하는 노드입니다.
      - `IsTargetAliveNode.cs`: 블랙보드의 `CurrentTarget`이 파괴되지 않고 작동 가능한 상태인지 판단하는 노드입니다.
    - **거리 및 사거리 검사:**
      - `IsTargetInRangeNode.cs`: 블랙보드의 `CurrentTarget`이 특정 거리에 있는지 판단하는 노드입니다.
      - `IsTargetInAttackRangeNode.cs`: 블랙보드의 `CurrentTarget`이 `Blackboard.SelectedWeapon`의 공격 사거리 내에 있는지 판단하는 노드입니다.
      - `IsTargetInRepairRangeNode.cs`: 블랙보드의 `CurrentTarget`이 수리 가능한 범위 내에 있는지 판단하는 노드입니다.
      - `IsTargetTooCloseNode.cs`: 블랙보드의 `CurrentTarget`이 특정 최소 거리보다 가까운지 판단하는 노드입니다.
      - `MoveAwayFromTargetNode.cs`: 대상으로부터 멀어지는 것이 필요한 상황인지 판단하는 로직을 가진 노드입니다.
    - **전투 상황 검사:**
      - `IsEnemyTargetingSelfNode.cs`: 현재 유닛이 적의 공격 대상으로 지정되었는지 판단하는 노드입니다.
      - `IsHealthLowNode.cs`: 현재 유닛의 체력이 낮은지 판단하는 노드입니다.
      - `IsAnyWeaponReloadingNode.cs`: 장착한 무기 중 재장전 중인 무기가 하나라도 있는지 판단하는 노드입니다.
      - `NeedsReloadNode.cs`: 블랙보드의 `SelectedWeapon` 또는 주무기가 재장전이 필요한 상태인지 판단하는 노드입니다.

  - `/Evaluators`
    - 유틸리티 시스템에서 사용되는 평가자 스크립트들이 있습니다.
    - **기본 인터페이스:**
      - `IUtilityEvaluator.cs`: 유틸리티 평가 인터페이스. `Evaluate()` 메서드를 통해 특정 상황의 효용값을 계산합니다.
      - `CompositeUtilityEvaluator.cs`: 여러 평가자를 조합하여 복합적인 효용값을 계산하는 평가자입니다.
      - `APEfficiencyEvaluator.cs`: AP 효율성을 기반으로 효용값을 계산하는 평가자입니다.
    - **공격 관련 평가자:**
      - `AttackUtilityEvaluator.cs`: 공격 행동의 효용값을 계산하는 평가자. 대미지 기댓값, 명중률, AP 효율성 등을 종합적으로 고려합니다.
      - `MeleeAttackUtilityEvaluator.cs`: 근접 공격에 특화된 효용값 평가자입니다.
      - `RangedAttackUtilityEvaluator.cs`: 원거리 공격에 특화된 효용값 평가자입니다.
      - `DefensiveAttackUtilityEvaluator.cs`: 방어적 공격 상황에서의 효용값을 평가하는 평가자입니다.
      - `SupportDefensiveAttackUtilityEvaluator.cs`: 지원형 유닛의 방어적 공격을 평가하는 평가자입니다.
    - **이동 및 포지셔닝 평가자:**
      - `MeleeApproachUtilityEvaluator.cs`: 근접 전투를 위한 접근 이동의 효용값을 평가하는 평가자입니다.
      - `RangedPositioningUtilityEvaluator.cs`: 원거리 전투를 위한 포지셔닝의 효용값을 평가하는 평가자입니다.
      - `KitingUtilityEvaluator.cs`: 카이팅(거리 유지하며 공격) 전술의 효용값을 평가하는 평가자입니다.
    - **지원 관련 평가자:**
      - `AllyApproachUtilityEvaluator.cs`: 아군에게 접근하는 행동의 효용값을 평가하는 평가자입니다.
      - `AllyRepairUtilityEvaluator.cs`: 아군 수리 행동의 효용값을 평가하는 평가자입니다.
    - **어빌리티 및 방어 평가자:**
      - `AbilityUtilityEvaluator.cs`: 어빌리티 사용의 효용값을 계산하는 평가자입니다. 어빌리티 타입, 효과, 상황 등을 종합적으로 고려합니다.
      - `DefensiveUtilityEvaluator.cs`: 방어 행동의 효용값을 평가하는 평가자입니다.

  - `/Decorators`
    - 노드의 결과에 영향을 주는 데코레이터 노드 스크립트들이 있습니다.
    - `AlwaysSuccessDecorator.cs`: 자식 노드의 실행 결과와 상관없이 항상 `NodeStatus.Success`를 반환하는 데코레이터 노드입니다.
    - `InverterNode.cs`: 자식 노드의 실행 결과를 반전시키는 데코레이터 노드입니다. 자식이 Success면 Failure, Failure면 Success를 반환하며, Running 상태는 그대로 전달합니다.

  - `/PilotBTs`
    - 실제 파일럿(AI)들이 사용하는 Behavior Tree 스크립트들이 있습니다. 각 클래스는 정적 `Create()` 메서드를 통해 해당 타입의 행동 트리를 생성하여 반환합니다.
    - `BasicAttackBT.cs`: 기본적인 공격 성향의 파일럿이 사용하는 행동 트리를 정의합니다. 유틸리티 기반 시스템과 전통적인 시퀀스 기반 시스템을 혼합하여 사용하며, 어빌리티 사용, 공격, 이동, 재장전 등의 행동을 상황에 따라 선택합니다.
    - `DefenderBT.cs`: 방어 성향의 파일럿이 사용하는 행동 트리를 정의합니다. 생존과 방어에 높은 우선순위를 두며, 재장전이나 조건부 방어 같은 생존 관련 행동을 우선시합니다.
    - `MeleeCombatBT.cs`: 근접 전투 성향의 파일럿이 사용하는 행동 트리를 정의합니다. 근접 전투에 특화된 우선순위를 가지며, 접근 후 공격하는 패턴을 주로 사용합니다.
    - `RangedCombatBT.cs`: 원거리 전투 성향의 파일럿이 사용하는 행동 트리를 정의합니다. 원거리 무기 사거리를 활용하기 위한 카이팅과 재배치 로직이 핵심이며, 유틸리티 시스템을 적극 활용합니다.
    - `SupportBT.cs`: 지원 성향의 파일럿이 사용하는 행동 트리를 정의합니다. 아군 지원(수리, 버프 등)에 최우선 순위를 두며, 복잡한 아군 지원 로직과 생존 관련 행동을 포함합니다. 공격은 다른 모든 행동이 불가능할 때만 시도합니다. 