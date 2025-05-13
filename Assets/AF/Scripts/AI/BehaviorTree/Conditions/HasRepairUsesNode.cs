using AF.AI.BehaviorTree;
using AF.Models;
using AF.Combat;

namespace AF.AI.BehaviorTree.Conditions
{
    /// <summary>
    /// Checks if the agent has remaining repair uses.
    /// </summary>
    public class HasRepairUsesNode : ConditionNode
    {
        public HasRepairUsesNode()
        {
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var logger = context?.Logger?.TextLogger; // Get logger instance

            // Check if ArmoredFrame reference exists
            if (agent == null)
            {
                // Log an error or handle the missing agent case
                logger?.Log($"[{this.GetType().Name}] Agent is null. Failure.", LogLevel.Error);
                return NodeStatus.Failure;
            }

            int repairUses = agent.GetCurrentRepairUses();
            // Check if the agent has repair uses left
            if (repairUses > 0)
            {
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Has {repairUses} repair uses remaining. Success.", LogLevel.Debug);
                return NodeStatus.Success; // Repair uses available
            }
            else
            {
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Has no repair uses left (Current: {repairUses}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure; // No repair uses left
            }
        }
    }
    
} 