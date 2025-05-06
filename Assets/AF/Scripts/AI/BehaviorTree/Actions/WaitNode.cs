using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// 유닛이 현재 턴에 대기하는 액션 노드입니다. (아무것도 하지 않음)
    /// 항상 Success를 반환합니다.
    /// </summary>
    public class WaitNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // Debug.Log($"[{GetType().Name}] {agent.name}: Waiting. Success.");
            // 특별히 Blackboard에 기록할 내용 없음. 대기는 행동 없음을 의미.
            // 필요하다면 blackboard.DecidedActionType = null; 또는 특정 대기 상태를 명시할 수 있음.
            return NodeStatus.Success;
        }
    }
} 