using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Conditions;
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;
using System.Collections.Generic;

namespace AF.AI.BehaviorTree.PilotBTs
{
    public class SupportBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            float defaultRepairRange = 3f;
            float allyHealthThresholdForRepair = 0.85f;
            float desiredProximityToAlly = 3f;

            return new SelectorNode(new List<BTNode>
            {
                // NEW: Self Active Ability Usage (Highest Priority)
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility)
                }),
                // Sequence 2: Ally Support (Repairing Allies)
                new SequenceNode(new List<BTNode>
                {
                    new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, "Body"),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    new HasRepairUsesNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairAlly),
                    // 사거리 내에 있을 때만 수리 시도
                    new IsTargetInRepairRangeNode(defaultRepairRange),
                    new CanRepairTargetPartNode(),
                    new SetRepairAllyActionNode()
                }),

                // Sequence NEW: Move to nearest ally if no one needs repair (proximity support)
                new SequenceNode(new List<BTNode>
                {
                    // 1. 수리 대상이 없음을 명확히 (SelectLowestHealthAllyNode 실패 시)
                    new InverterNode(
                        new SequenceNode(new List<BTNode>
                        {
                            new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, "Body"),
                            new HasValidTargetNode(),
                            new IsTargetAliveNode()
                        })
                    ),
                    // 2. 가장 가까운 아군을 타겟팅
                    new SelectNearestAllyNode(),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    // 3. 이미 충분히 가까우면 이동하지 않음 (2m 기준)
                    new InverterNode(
                        new IsTargetInRangeNode(desiredProximityToAlly)
                    ),
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    new MoveToTargetNode(defaultRepairRange)
                }),

                // Sequence 1: Self-preservation (Self-repair first, then defend)
                new SequenceNode(new List<BTNode>
                {
                    new IsHealthLowNode(0.5f), // Standardized health threshold for self-repair attempt
                    new SelectorNode(new List<BTNode> // Attempt repair or defend
                    {
                        new SequenceNode(new List<BTNode> // 1a. Self-repair
                        {
                            new HasRepairUsesNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                            new RepairSelfNode()
                        }),
                        new SequenceNode(new List<BTNode> // 1b. Defend if repair not possible
                        {
                            new CanDefendThisActivationNode(), // Ensure defend is still a valid action this turn if repair fails
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode()
                        })
                    })
                }),

                // Sequence 3: Basic Attack (If no support actions are needed/possible)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),
                    new HasSelectedWeaponNode(),
                    new IsTargetInAttackRangeNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                    new AttackTargetNode()
                }),

                // Default Action: Wait
                new SequenceNode(new List<BTNode>
                {
                    new HasEnoughAPNode(CombatActionEvents.ActionType.None),
                    new WaitNode()
                })
            });
        }
    }
} 