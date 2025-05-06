using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// 유닛이 방어 자세를 취할 것을 결정하는 액션 노드입니다.
    /// 실제 방어 효과 적용 및 AP 소모는 외부 시스템에서 처리한다고 가정합니다.
    /// </summary>
    public class DefendNode : ActionNode
    {
        // AP 비용은 CombatActionExecutor의 정의를 따르는 것이 이상적이나,
        // 직접 참조가 어려울 경우 CombatContext를 통해 전달받거나, BT 생성 시 설정할 수 있음.
        // 여기서는 임시로 노드 내 상수를 사용.
        private const float DEFEND_AP_COST = 1f; // CombatActionExecutor.DEFEND_AP_COST 와 일치 필요

        public DefendNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // AP 체크는 agent의 현재 AP를 사용
            if (agent.CurrentAP < DEFEND_AP_COST)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Not enough AP ({agent.CurrentAP}) to defend (needs {DEFEND_AP_COST} AP).");
                return NodeStatus.Failure; // AP 부족
            }

            // 방어 의사를 블랙보드에 기록
            blackboard.DecidedActionType = CombatActionEvents.ActionType.Defend;
            // Debug.Log($"[{GetType().Name}] {agent.name}: Decided to defend. Success.");
            return NodeStatus.Success;
        }
    }
} 