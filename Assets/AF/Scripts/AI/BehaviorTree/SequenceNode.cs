using System.Collections.Generic;
using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree
{
    public class SequenceNode : BTNode
    {
        protected List<BTNode> childNodes = new List<BTNode>();

        public SequenceNode(List<BTNode> childNodes)
        {
            this.childNodes = childNodes;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            bool anyChildRunning = false;
            foreach (BTNode node in childNodes)
            {
                switch (node.Tick(agent, blackboard, context)) // 자식 노드 호출 시에도 Tick 사용
                {
                    case NodeStatus.Running:
                        anyChildRunning = true; // Running인 자식이 있으면 일단 기억
                        break;
                    case NodeStatus.Success:
                        continue; // 이 자식은 성공했으니 다음 자식으로
                    case NodeStatus.Failure:
                        return NodeStatus.Failure; // 자식 중 하나라도 Failure면 Sequence도 Failure
                }
            }
            // 모든 자식이 Failure가 아니었고 (즉, 다 Success거나 일부가 Running)
            // Running인 자식이 하나라도 있었으면 Sequence는 Running
            // 모든 자식이 Success였으면 Sequence는 Success
            return anyChildRunning ? NodeStatus.Running : NodeStatus.Success;
        }
    }
} 