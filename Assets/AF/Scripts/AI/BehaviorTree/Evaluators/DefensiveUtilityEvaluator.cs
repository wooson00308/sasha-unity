using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 방어 유틸리티 평가자 - 방어가 필요한 상황을 평가
    /// </summary>
    public class DefensiveUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null) return 0f;

            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float apRatio = agent.CurrentAP / agent.CombinedStats.MaxAP;

            // 기본 방어 유틸리티
            float utility = 0.2f;

            // 체력이 낮을수록 방어 필요성 증가
            if (agentHealthRatio <= 0.3f)
            {
                utility += 0.6f; // 매우 위험
            }
            else if (agentHealthRatio <= 0.5f)
            {
                utility += 0.4f; // 위험
            }
            else if (agentHealthRatio <= 0.7f)
            {
                utility += 0.2f; // 약간 위험
            }

            // AP가 충분하면 방어 고려
            if (apRatio >= 0.5f)
            {
                utility += 0.1f;
            }

            // 적이 근처에 있으면 방어 필요성 증가
            ArmoredFrame target = blackboard?.CurrentTarget ?? agent.CurrentTarget;
            if (target != null)
            {
                float distance = Vector3.Distance(agent.Position, target.Position);
                if (distance <= 15f)
                {
                    utility += 0.2f; // 가까운 적
                }
                else if (distance <= 25f)
                {
                    utility += 0.1f; // 적당한 거리의 적
                }
            }

            return Mathf.Clamp01(utility);
        }
    }
} 