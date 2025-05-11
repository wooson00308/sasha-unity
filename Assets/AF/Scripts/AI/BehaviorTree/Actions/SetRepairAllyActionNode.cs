using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    public class SetRepairAllyActionNode : BTNode
    {
        public SetRepairAllyActionNode()
        {
            // Constructor can be empty or take parameters if needed in the future
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (blackboard == null)
            {
                Debug.LogError("SetRepairAllyActionNode: Blackboard is null.");
                return NodeStatus.Failure;
            }

            if (blackboard.CurrentTarget == null || blackboard.CurrentTarget == agent || !blackboard.CurrentTarget.IsOperational)
            {
                // Optionally log: Debug.Log($"[BT] {agent?.Name} tried to set RepairAlly but CurrentTarget is invalid or self.");
                return NodeStatus.Failure; // No valid ally target to repair
            }

            blackboard.DecidedActionType = CombatActionEvents.ActionType.RepairAlly;
            // No need to set CurrentTarget here, assuming it's already set by a preceding node like SelectLowestHealthAllyNode
            // Optionally log: Debug.Log($"[BT] {agent?.Name} decided to RepairAlly target: {blackboard.CurrentTarget.Name}");
            return NodeStatus.Success;
        }
    }
} 