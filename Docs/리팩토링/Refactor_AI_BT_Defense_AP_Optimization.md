# AI 행동 트리(BT) 개선: 불필요한 방어 및 AP 낭비 방지

## 1. 현재 문제점 요약

### 1.1. "이동이 더 이득인데 방어로 AP 낭비" 현상
현재 일부 행동 트리(특히 `BasicAttackBT`, `MeleeCombatBT`)에서, 이동이나 공격 같은 주요 행동이 특정 조건(예: 사거리 만족, AP 부족, 이미 행동 완료 등)으로 인해 실행되지 못했을 경우, 후순위로 설정된 방어 행동이 AP가 남아있다는 이유만으로 실행되는 경우가 발생합니다. 이는 다음과 같은 비효율을 야기합니다:

-   **AP 낭비**: 이동이 전략적으로 더 유리하거나, 다음 턴을 위해 AP를 보존해야 하는 상황임에도 불구하고 불필요하게 방어 행동으로 AP를 소모합니다.
-   **전술적 유연성 저하**: 유닛이 상황에 맞는 최적의 행동 대신, 단순히 "남는 AP를 소모하기 위한" 방어 행동을 선택하게 되어 전술적 유연성이 떨어집니다. 예를 들어, 적에게 접근해야 하는 근접 유닛이 이동 AP가 부족하거나 이미 이동했다고 해서 제자리 방어만 반복하는 경우가 발생할 수 있습니다.

### 1.2. `DefendNode` 자체의 조건 부족
`DefendNode.cs`는 현재 AP 소모량만 체크하고 방어 의사를 결정합니다. "이번 활성화에 이미 방어했는지" 등의 복합적인 조건은 BT 내의 `CanDefendThisActivationNode` 같은 별도 조건 노드에 의존하고 있습니다. 이는 설계상 문제는 아니나, BT 구성 시 해당 조건 노드를 누락하면 의도치 않은 중복 방어가 발생할 수 있습니다. (현재 제공된 BT들은 대부분 잘 사용하고 있는 것으로 보입니다.)

## 2. 개선 목표

-   **불필요한 방어 행동 최소화**: AP가 남아있더라도, 전략적으로 방어가 최선이 아닌 경우 방어 행동을 자제하도록 합니다.
-   **이동 우선순위 보장**: 이동이 가능하고 전략적으로 필요할 경우, 다른 행동(특히 불필요한 방어)보다 우선적으로 이동을 선택하도록 보장합니다.
-   **AP 활용 효율성 증대**: 유닛이 현재 AP를 보다 상황에 맞게 효과적으로 사용하도록 개선합니다.
-   **AI 행동 예측 가능성 및 합리성 향상**: AI가 더 논리적이고 이해하기 쉬운 판단을 내리도록 합니다.

## 3. 핵심 개선 전략: "조건부 방어" 로직 도입

핵심 아이디어는 방어 행동을 결정하기 전에 **"정말로 다른 유효한 행동(특히 이동)이 불가능했는가?"** 를 더 명확하게 판단하는 것입니다.

### 3.1. 판단 근거
-   유닛이 이번 활성화 주기에 **이미 이동을 수행했는가?**
-   이동에 **충분한 AP를 보유하고 있는가?**
-   이동 외에 다른 공격/스킬 사용 등의 **우선순위 높은 행동이 가능한가?**

### 3.2. BT 수정 원칙
-   기존에 [이동/공격 Sequence] -> [방어 Sequence] 순서로 되어 있는 BT 구조에서, 두 Sequence 사이에 "조건부 방어"를 위한 논리를 추가합니다.
-   주로 `InverterNode`, `CanMoveThisActivationNode`, `HasEnoughAPNode(CombatActionEvents.ActionType.Move)` 등의 조합을 활용하여 "이동할 여지가 있었음에도 안 한 것"과 "이동할 수단이 없어서 못한 것"을 구분합니다.

## 4. BT 유형별 수정 가이드라인

### 4.1. `BasicAttackBT` 및 `MeleeCombatBT` (주요 변경 대상)

이 두 BT는 "교전 실패 시 방어" 패턴이 명확하여 개선 효과가 클 것으로 예상됩니다.

-   **기존 구조의 문제점**: 교전 Sequence (공격 또는 이동 시도)가 실패하면, 그 이유(예: 이동 AP 부족, 이미 이동 완료, 공격 대상 없음 등)와 무관하게 후순위의 방어 Sequence가 AP만 있다면 실행될 수 있습니다.
-   **변경 방향**: 교전 Sequence 실행 후, 방어 Sequence 실행 전에 "이동이 정말 불가능했거나 이미 수행했을 경우"에만 방어를 고려하도록 조건을 추가합니다.

-   **`BasicAttackBT.cs` 수정 제안 예시**:

    ```csharp
    // BasicAttackBT.cs
    public static BTNode Create()
    {
        return new SelectorNode(new List<BTNode>
        {
            // ... (기존 상위 우선순위: 재장전 중 방어, 거리 벌리기, 자가 수리 등은 유지) ...

            // 2. 타겟팅 및 교전 시퀀스 (이동 또는 공격)
            new SequenceNode(new List<BTNode>
            {
                new SelectTargetNode(),
                new HasValidTargetNode(), 
                new SelectorNode(new List<BTNode>
                {
                    // 2a. 즉시 공격 (사거리 내이고 AP 충분)
                    new SequenceNode(new List<BTNode>
                    {
                        new IsTargetInRangeNode(),
                        new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                        new AttackTargetNode()
                    }),
                    // 2b. 이동 (아직 이동 안했고 AP 충분하면)
                    new SequenceNode(new List<BTNode>
                    {
                        new CanMoveThisActivationNode(), // ★ 중요: 이동 조건
                        new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                        new MoveToTargetNode()
                    })
                })
            }),

            // ... (기존 필수 재장전 등은 유지) ...

            // 4. 조건부 방어 시퀀스 (★ 변경된 핵심 로직)
            //    : "이동할 여지가 없었거나 이미 이동을 완료했고", "방어는 가능한" 경우
            new SequenceNode(new List<BTNode>
            {
                // "이번 활성화에 이동할 여지가 없었거나 이미 이동을 했다면" Success
                new InverterNode( 
                    new SequenceNode(new List<BTNode> // "이동할 여지가 있었는가?" 체크
                    {
                        new CanMoveThisActivationNode(), // 아직 안 움직였고
                        new HasEnoughAPNode(CombatActionEvents.ActionType.Move) // 이동할 AP도 있다면
                        // 실제 MoveToTargetNode는 다른 조건(예: IsTargetInRange)으로 실행 안될 수 있지만,
                        // "이동을 시도할 수 있는 기본적인 조건은 만족했다"는 의미.
                    })
                ),
                new CanDefendThisActivationNode(), // 그리고 이번 활성화에 방어 안했고
                new HasEnoughAPNode(CombatActionEvents.ActionType.Defend), // 방어 AP는 있고
                new DefendNode()
            }),

            // ... (기존 낮은 탄약 재장전, 대기 노드 등은 유지) ...
        });
    }
    ```

    -   **`InverterNode` 로직 설명**:
        -   내부 `SequenceNode { CanMoveThisActivationNode, HasEnoughAPNode(Move) }`는 "이번 활성화에 이동할 수 있는 기본적인 조건(아직 안 움직임, 이동 AP 충분)을 갖추었는가?"를 평가합니다.
        -   만약 이 조건이 참(Success)이라면, 유닛은 이동할 여지가 있었던 것이므로 `InverterNode`는 거짓(Failure)을 반환하여 이 방어 Sequence를 실행하지 않습니다. (즉, "움직일 수 있었는데 다른 이유로 안 움직인 것뿐이니, 굳이 방어 안 함")
        -   만약 이 조건이 거짓(Failure)이라면 (예: 이미 이동했거나, 이동 AP 부족), 유닛은 이동할 여지가 없었던 것이므로 `InverterNode`는 참(Success)을 반환하여 (다른 방어 조건 만족 시) 방어를 고려하게 됩니다.

### 4.2. `RangedCombatBT`

-   현재도 `Main Combat Logic` 내 `SelectorNode`의 마지막 옵션으로 방어가 위치하여, 다른 이동/공격 옵션이 모두 실패했을 때 방어를 고려하는 구조입니다. 이는 비교적 합리적입니다.
-   **개선 고려 사항**: 다만, "이동 AP 부족으로 이동 실패 시"에도 방어로 넘어갈 수 있는 부분은 위의 `BasicAttackBT`와 유사한 `InverterNode` 기반의 조건부 방어 로직을 추가하여 더 정교하게 만들 수 있습니다. (선택적 개선)

### 4.3. `DefenderBT` 및 `SupportBT`

-   이 BT들은 주요 역할이 방어 또는 지원이므로, 현재 로직에서 대대적인 수정 필요성은 적을 수 있습니다.
-   `DefenderBT`는 이미 다양한 상황에서 방어를 우선시하도록 설계되어 있습니다.
-   **개선 고려 사항**: 그럼에도 불구하고, 각 BT의 세부 흐름을 검토하여 명백한 AP 낭비나 비효율적인 방어 선택이 발생하는 부분이 있다면 미세 조정합니다. 예를 들어, Support 유닛이 아군 지원이 불가능하고 공격/이동도 마땅치 않을 때 최후의 수단으로 방어하는 것은 합리적일 수 있습니다.

## 5. 관련 BT 노드 검토

-   **`DefendNode.cs`**: 자체 로직 변경은 필요 없습니다. BT 내에서 어떻게 호출되느냐가 중요합니다.
-   **`CanDefendThisActivationNode.cs`**: 이번 활성화 주기 내 방어 행동 가능 여부를 체크하는 중요한 조건 노드입니다. BT 설계 시 방어 행동 결정 전에 적절히 사용되어야 합니다. (현재 잘 사용 중)
-   **`CanMoveThisActivationNode.cs`**: 이번 활성화 주기 내 이동 행동 가능 여부를 체크합니다. 조건부 방어 로직의 핵심 구성요소로 활용됩니다.
-   **`HasEnoughAPNode.cs`**: 모든 행동 전에 AP 충분 여부를 검사하는 기본적인 조건 노드입니다. 방어 행동의 AP 비용(`CombatActionExecutor.DEFEND_AP_COST`)에 맞춰 정확한 값을 사용해야 합니다.

## 6. 기대 효과

-   AI 유닛의 AP 활용 효율 증대로 불필요한 소모 감소.
-   특히 이동이 중요한 유닛(예: 근접 유닛)이 보다 적극적으로 위치를 잡거나, 원거리 유닛이 적절한 거리를 유지하는 등 상황에 맞는 더 지능적인 행동 선택.
-   전투 로그 분석 시, 이전보다 합리적인 이유로 방어 행동을 선택하는 패턴 관찰 가능.
-   전반적인 전투 양상의 자연스러움 및 전략성 향상.

## 7. 다음 단계

1.  본 문서에 제안된 내용을 바탕으로 각 BT 스크립트 (`BasicAttackBT.cs`, `MeleeCombatBT.cs` 등)에 실제 코드 변경을 적용합니다.
2.  수정된 BT를 사용하는 유닛들로 전투 시뮬레이션을 다양하게 실행하고, 전투 로그 및 실제 행동을 관찰하여 개선 효과를 검증합니다.
3.  특히 "이동이 더 유리한 상황에서 불필요하게 방어하는지", "AP가 부족하지 않은데도 이동해야 할 때 방어하는지" 등의 특정 시나리오를 집중적으로 테스트합니다.
4.  테스트 결과에 따라 조건부 방어 로직의 세부 조건(예: `InverterNode` 내부의 판단 기준)을 미세 조정하거나, 다른 BT 노드와의 상호작용을 추가적으로 개선합니다. 