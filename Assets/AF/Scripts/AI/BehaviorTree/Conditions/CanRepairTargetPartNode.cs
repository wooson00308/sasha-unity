using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Conditions
{
    public class CanRepairTargetPartNode : ConditionNode
    {
        public CanRepairTargetPartNode()
        {
            // Constructor can be empty
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var logger = context?.Logger?.TextLogger;

            if (blackboard == null || blackboard.CurrentTarget == null || !blackboard.CurrentTarget.IsOperational || string.IsNullOrEmpty(blackboard.TargetPartSlot))
            {
                // logger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Pre-conditions not met (Null BB, Target, or TargetPartSlot). RESULT: FAILURE", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            ArmoredFrame targetAlly = blackboard.CurrentTarget;
            string targetPartSlotId = blackboard.TargetPartSlot;

            if (targetAlly.Parts.TryGetValue(targetPartSlotId, out Part partToRepair))
            {
                // logger?.Log($"[BT DEBUG] {agent?.Name} checking part {targetPartSlotId} on {targetAlly.Name}. IsOperational: {partToRepair.IsOperational}, CurrentHP: {partToRepair.CurrentDurability}, MaxHP: {partToRepair.MaxDurability}", LogLevel.Debug);
                if (partToRepair.IsOperational && partToRepair.CurrentDurability < partToRepair.MaxDurability)
                {
                    // logger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Target {targetAlly.Name}'s part '{targetPartSlotId}' IS repairable. RESULT: SUCCESS", LogLevel.Debug);
                    return NodeStatus.Success;
                }
                else
                {
                    // logger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Target {targetAlly.Name}'s part '{targetPartSlotId}' is NOT repairable (fully repaired or destroyed). RESULT: FAILURE", LogLevel.Debug);
                    return NodeStatus.Failure;
                }
            }
            else
            {
                // logger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Target {targetAlly.Name} does not have part slot '{targetPartSlotId}'. RESULT: FAILURE", LogLevel.Debug);
                return NodeStatus.Failure; // Target part slot not found on the ally
            }
        }
    }
} 