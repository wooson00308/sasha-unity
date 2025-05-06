using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 현재 유닛이 유효한 공격 목표를 가지고 있는지 검사하는 조건 노드입니다.
    /// </summary>
    public class HasValidTargetNode : ConditionNode
    {
        public HasValidTargetNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (blackboard.CurrentTarget != null && !blackboard.CurrentTarget.IsDestroyed)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Has a valid target on blackboard: {blackboard.CurrentTarget.name}. Success.");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: No valid target on blackboard. Failure.");
                return NodeStatus.Failure;
            }
        }
    }
} 