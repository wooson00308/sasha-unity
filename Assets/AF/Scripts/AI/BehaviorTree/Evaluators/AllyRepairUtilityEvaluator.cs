using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 아군 수리 유틸리티 평가자 - 아군을 수리해야 하는 상황을 평가
    /// </summary>
    public class AllyRepairUtilityEvaluator : IUtilityEvaluator
    {
        private readonly float healthThreshold;

        public AllyRepairUtilityEvaluator(float healthThreshold)
        {
            this.healthThreshold = healthThreshold;
        }

        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || context?.Participants == null) return 0f;

            // 수리가 가능한지 확인
            if (agent.GetCurrentRepairUses() <= 0) return 0f;

            // 가장 체력이 낮은 아군 찾기
            ArmoredFrame weakestAlly = FindWeakestAlly(agent, context);
            if (weakestAlly == null) return 0f;

            float allyHealthRatio = weakestAlly.CurrentDurabilityRatio;
            
            // 기본 유틸리티
            float utility = 0f;

            // 아군 체력이 낮을수록 수리 필요성 증가
            if (allyHealthRatio <= 0.2f)
            {
                utility += 1.0f; // 매우 위험한 아군
            }
            else if (allyHealthRatio <= 0.4f)
            {
                utility += 0.8f; // 위험한 아군
            }
            else if (allyHealthRatio <= 0.6f)
            {
                utility += 0.6f; // 중간 위험 아군
            }
            else if (allyHealthRatio <= healthThreshold)
            {
                utility += 0.3f; // 약간 위험한 아군
            }
            else
            {
                return 0f; // 건강한 아군은 수리 불필요
            }

            // 자신의 체력이 충분할 때 더 적극적 지원
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            if (agentHealthRatio >= 0.7f)
            {
                utility += 0.2f; // 안전하면 적극적 지원
            }
            else if (agentHealthRatio <= 0.3f)
            {
                utility -= 0.3f; // 자신이 위험하면 자제
            }

            return Mathf.Clamp01(utility);
        }

        private ArmoredFrame FindWeakestAlly(ArmoredFrame agent, CombatContext context)
        {
            if (!context.TeamAssignments.TryGetValue(agent, out int agentTeamId)) return null;

            ArmoredFrame weakestAlly = null;
            float lowestHealth = 1f;

            foreach (var unit in context.Participants)
            {
                if (unit != agent && unit.IsOperational)
                {
                    if (context.TeamAssignments.TryGetValue(unit, out int unitTeamId) && unitTeamId == agentTeamId)
                    {
                        float healthRatio = unit.CurrentDurabilityRatio;
                        if (healthRatio < lowestHealth)
                        {
                            lowestHealth = healthRatio;
                            weakestAlly = unit;
                        }
                    }
                }
            }

            return weakestAlly;
        }
    }
} 