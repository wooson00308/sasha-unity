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
            float defaultRepairRange = 10f;
            float allyHealthThresholdForRepair = 0.85f;
            float desiredProximityToAlly = 10f;

            return new SelectorNode(new List<BTNode>
            {
                // Sequence 0: Defend while reloading
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),

                // Sequence 1: Self-preservation
                new SequenceNode(new List<BTNode>
                {
                    new IsHealthLowNode(0.3f),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),

                // Sequence 2: Ally Support (Repairing Allies)
                new SequenceNode(new List<BTNode>
                {
                    new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, "Body"),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairAlly),

                    new SelectorNode(new List<BTNode>
                    {
                        new IsTargetInRepairRangeNode(defaultRepairRange),
                        new SequenceNode(new List<BTNode>
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode()
                        })
                    }),
                    
                    new CanRepairTargetPartNode(),
                    new SetRepairAllyActionNode()
                }),

                // Sequence NEW: Potential Ally Support (Move to nearest ally and Defend)
                new SequenceNode(new List<BTNode>
                {
                    new SelectNearestAllyNode(),
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    new InverterNode(
                       new IsTargetInRangeNode(desiredProximityToAlly)
                    ),
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    new MoveToTargetNode(desiredProximityToAlly),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
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