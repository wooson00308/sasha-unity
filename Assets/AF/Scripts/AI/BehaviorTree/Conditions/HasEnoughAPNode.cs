using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 용

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 유닛이 지정된 양의 AP를 가지고 있는지 검사하는 조건 노드입니다.
    /// </summary>
    public class HasEnoughAPNode : ConditionNode
    {
        private readonly float requiredAP;

        /// <summary>
        /// HasEnoughAPNode를 생성합니다.
        /// </summary>
        /// <param name="requiredAP">필요한 AP 양입니다.</param>
        public HasEnoughAPNode(float requiredAP)
        {
            // AP 비용은 0 이상이어야 함 (음수 AP 소모는 말이 안 됨)
            this.requiredAP = Mathf.Max(0f, requiredAP);
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // ArmoredFrame에 CurrentAP 프로퍼티와 HasEnoughAP 메서드가 있다고 가정
            if (agent.HasEnoughAP(requiredAP))
            {
                // Debug.Log($"[{this.GetType().Name}] {agent.name}: Has enough AP ({agent.CurrentAP} >= {requiredAP}). Success.");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[{this.GetType().Name}] {agent.name}: Not enough AP ({agent.CurrentAP} < {requiredAP}). Failure.");
                return NodeStatus.Failure;
            }
        }
    }
} 