using System.Collections.Generic;
using AF.Combat; // CombatContext 때문에 추가
using AF.Models; // ArmoredFrame 때문에 추가

namespace AF.AI.BehaviorTree
{
    public class SelectorNode : BTNode
    {
        protected List<BTNode> childNodes = new List<BTNode>();

        public SelectorNode(List<BTNode> childNodes)
        {
            this.childNodes = childNodes;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            foreach (BTNode node in childNodes)
            {
                switch (node.Tick(agent, blackboard, context)) // 자식 노드 호출 시에도 Tick 사용
                {
                    case NodeStatus.Running:
                        return NodeStatus.Running;
                    case NodeStatus.Success:
                        return NodeStatus.Success;
                    case NodeStatus.Failure:
                        continue;
                }
            }
            return NodeStatus.Failure;
        }
    }
} 