using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 유닛의 현재 체력이 지정된 비율 이하인지 검사하는 조건 노드입니다.
    /// </summary>
    public class IsHealthLowNode : ConditionNode
    {
        private readonly float healthThresholdPercentage; // 0.0f ~ 1.0f 사이의 값

        /// <summary>
        /// IsHealthLowNode를 생성합니다.
        /// </summary>
        /// <param name="healthThresholdPercentage">체력 임계값 비율 (예: 0.3f는 30% 이하)</param>
        public IsHealthLowNode(float healthThresholdPercentage = 0.3f)
        {
            this.healthThresholdPercentage = Mathf.Clamp01(healthThresholdPercentage);
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // healthThresholdPercentage는 생성자에서 0.0f ~ 1.0f 사이로 Clamp됨

            float currentActualHP = agent.GetCurrentAggregatedHP(); // agent 사용
            float maxTotalHP = agent.CombinedStats.Durability;      // agent 사용

            if (maxTotalHP <= 0) // 최대 체력이 0 이하면 비교 무의미, 또는 이미 파괴된 상태로 간주
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Max HP is 0 or less ({maxTotalHP}). Failure.");
                return NodeStatus.Failure;
            }

            float currentHpPercentage = currentActualHP / maxTotalHP;

            if (currentHpPercentage <= healthThresholdPercentage)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Health is low ({currentHpPercentage * 100:F1}% <= {healthThresholdPercentage * 100:F1}%). Success.");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Health is not low ({currentHpPercentage * 100:F1}% > {healthThresholdPercentage * 100:F1}%). Failure.");
                return NodeStatus.Failure;
            }
        }
    }
} 