using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 용
using AF.Services;

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
            bool hasEnough = agent.HasEnoughAP(requiredAP);

            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger; // AF.Services 네임스페이스 필요
            logger?.Log(
                $"[{GetType().Name}] {agent.Name}: RequiredAP={requiredAP:F1}, CurrentAP={agent.CurrentAP:F1}, HasEnough={hasEnough}. " +
                $"Result: {(hasEnough ? NodeStatus.Success : NodeStatus.Failure)}",
                LogLevel.Debug
            );

            return hasEnough ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
} 