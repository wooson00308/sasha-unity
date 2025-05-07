using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree // 네임스페이스를 다른 Condition 노드들과 동일하게 AF.AI.BehaviorTree로 설정
{
    /// <summary>
    /// Checks if the agent can still defend in the current activation cycle.
    /// It reads from the CombatContext.DefendedThisActivation.
    /// </summary>
    public class CanDefendThisActivationNode : ConditionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || context == null)
            {
                return NodeStatus.Failure; // Essential references missing
            }

            if (context.DefendedThisActivation.Contains(agent))
            {
                return NodeStatus.Failure; // Already defended this activation
            }

            return NodeStatus.Success; // Can still defend
        }
    }
} 