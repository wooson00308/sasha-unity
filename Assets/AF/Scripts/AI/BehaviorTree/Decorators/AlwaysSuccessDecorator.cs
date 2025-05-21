using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Decorators
{
    public class AlwaysSuccessDecorator : BTNode
    {
        private BTNode child;

        public AlwaysSuccessDecorator(BTNode child)
        {
            this.child = child;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (child == null)
            {
                // 혹시 모를 상황 대비
                return NodeStatus.Failure;
            }
            child.Tick(agent, blackboard, context); 
            return NodeStatus.Success;              
        }
    }
} 