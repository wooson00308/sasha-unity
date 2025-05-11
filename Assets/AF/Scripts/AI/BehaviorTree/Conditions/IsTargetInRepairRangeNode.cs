using AF.Models;
using UnityEngine;
using AF.Combat; // CombatContext 사용을 위해 추가

namespace AF.AI.BehaviorTree.Conditions // 네임스페이스를 Conditions로 명시
{
    public class IsTargetInRepairRangeNode : ConditionNode
    {
        private readonly float repairRange;

        public IsTargetInRepairRangeNode(float repairRange)
        {
            this.repairRange = repairRange;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null || blackboard.CurrentTarget == null || context == null) // context null 체크 추가
            {
                return NodeStatus.Failure;
            }

            if (!blackboard.CurrentTarget.IsOperational)
            {
                return NodeStatus.Failure; // Target is not operational
            }

            float distanceToTarget = Vector3.Distance(agent.Position, blackboard.CurrentTarget.Position);

            if (distanceToTarget <= repairRange)
            {
                return NodeStatus.Success;
            }
            else
            {
                return NodeStatus.Failure;
            }
        }
    }
} 