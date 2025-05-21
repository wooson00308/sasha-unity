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
                blackboard.RepairTargetPartSlotId = null; // 대상이 없으므로 클리어
                return NodeStatus.Failure; // No valid ally target to repair
            }

            if (string.IsNullOrEmpty(blackboard.TargetPartSlot))
            {
                // Optionally log: Debug.LogWarning($"[BT] {agent?.Name} tried to set RepairAlly for {blackboard.CurrentTarget.Name} but TargetPartSlot is not set.");
                blackboard.RepairTargetPartSlotId = null; // 수리할 파츠 정보가 없으므로 클리어
                return NodeStatus.Failure; // 수리할 파츠 정보가 없음
            }

            blackboard.DecidedActionType = CombatActionEvents.ActionType.RepairAlly;
            blackboard.RepairTargetPartSlotId = blackboard.TargetPartSlot; // 수리할 파츠 슬롯 ID 설정

            // Optionally log: Debug.Log($"[BT] {agent?.Name} decided to RepairAlly target: {blackboard.CurrentTarget.Name}, PartSlot: {blackboard.RepairTargetPartSlotId}");
            return NodeStatus.Success;
        }
    }
} 