using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// Checks if the agent can still move in the current activation cycle.
    /// It reads from the CombatContext.MovedThisActivation set.
    /// </summary>
    public class CanMoveThisActivationNode : ConditionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || context == null)
            {
                return NodeStatus.Failure; // Essential references missing
            }

            if (context.MovedThisActivation.Contains(agent))
            {
                // Agent has already moved in this activation cycle
                return NodeStatus.Failure; 
            }
            else
            {
                // Agent has not yet moved in this activation cycle
                return NodeStatus.Success;
            }
        }
    }
} 