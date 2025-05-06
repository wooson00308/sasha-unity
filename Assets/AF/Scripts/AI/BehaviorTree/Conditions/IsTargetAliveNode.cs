using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 현재 목표 대상이 유효하고 생존 상태인지 검사하는 조건 노드입니다.
    /// </summary>
    public class IsTargetAliveNode : ConditionNode
    {
        public IsTargetAliveNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (blackboard.CurrentTarget != null && !blackboard.CurrentTarget.IsDestroyed)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Target {blackboard.CurrentTarget.name} is alive. Success.");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Target on blackboard is null or destroyed. Failure.");
                return NodeStatus.Failure;
            }
        }
    }
} 