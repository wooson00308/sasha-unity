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
                // Sequence 2: Ally Support (Repairing Allies)
                new SequenceNode(new List<BTNode>
                {
                    new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, "Body"),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    new HasRepairUsesNode(), // <<< Added check for repair uses before attempting ally repair
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairAlly),

                    new SelectorNode(new List<BTNode>
                    {
                        new IsTargetInRepairRangeNode(defaultRepairRange),
                        new SequenceNode(new List<BTNode>
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode(defaultRepairRange)
                        })
                    }),
                    
                    new CanRepairTargetPartNode(),
                    new SetRepairAllyActionNode()
                }),

                // Sequence NEW: Potential Ally Support (Move to nearest ally and Defend)
                new SequenceNode(new List<BTNode>
                {
                    new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, "Body"),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
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