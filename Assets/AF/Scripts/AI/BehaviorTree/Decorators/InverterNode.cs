using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Decorators
{
    /// <summary>
    /// Decorator node that inverts the result of its child node.
    /// Success becomes Failure, and Failure becomes Success.
    /// Running status is returned as is.
    /// </summary>
    public class InverterNode : BTNode
    {
        private BTNode _child;

        public InverterNode(BTNode child)
        {
            _child = child;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (_child == null)
            {
                Debug.LogError("InverterNode: Child node is null.");
                return NodeStatus.Failure;
            }

            NodeStatus childStatus = _child.Tick(agent, blackboard, context);

            switch (childStatus)
            {
                case NodeStatus.Success:
                    return NodeStatus.Failure;
                case NodeStatus.Failure:
                    return NodeStatus.Success;
                default: // Running or Error
                    return childStatus;
            }
        }
    }
} 