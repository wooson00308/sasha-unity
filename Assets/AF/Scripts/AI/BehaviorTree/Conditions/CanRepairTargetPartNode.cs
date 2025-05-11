using AF.Combat;
using AF.Models;
using System.Linq; // Required for Linq operations like Any()
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
            if (blackboard == null || blackboard.CurrentTarget == null || !blackboard.CurrentTarget.IsOperational || context == null)
            {
                return NodeStatus.Failure;
            }

            ArmoredFrame targetToRepair = blackboard.CurrentTarget;
            var textLogger = context.Logger?.TextLogger;

            bool canRepair = false;
            if (targetToRepair.Parts != null)
            {
                foreach (var partEntry in targetToRepair.Parts)
                {
                    Part part = partEntry.Value;
                    textLogger?.Log($"  [BT PART_CHECK] Agent: {agent?.Name}, Target: {targetToRepair.Name}, Part: {partEntry.Key}, Op: {part.IsOperational}, CurHP: {part.CurrentDurability}, MaxHP: {part.MaxDurability}", LogLevel.Debug);
                    if (part.IsOperational && part.CurrentDurability < part.MaxDurability)
                    {
                        canRepair = true;
                        break; 
                    }
                }
            }

            if (canRepair)
            {
                textLogger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Target {targetToRepair.Name} HAS repairable parts. RESULT: SUCCESS", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                textLogger?.Log($"[BT] {agent?.Name} - CanRepairTargetPartNode: Target {targetToRepair.Name} has NO repairable parts. RESULT: FAILURE", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 