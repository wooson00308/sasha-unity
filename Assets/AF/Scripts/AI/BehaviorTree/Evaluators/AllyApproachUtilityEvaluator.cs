using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 아군 접근 유틸리티 평가자 - 아군에게 다가가야 하는 상황을 평가
    /// </summary>
    public class AllyApproachUtilityEvaluator : IUtilityEvaluator
    {
        private readonly float repairRange;
        private readonly float healthThreshold;

        public AllyApproachUtilityEvaluator(float repairRange, float healthThreshold)
        {
            this.repairRange = repairRange;
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

            float distance = Vector3.Distance(agent.Position, weakestAlly.Position);
            
            // 이미 범위 안에 있으면 접근 불필요
            if (distance <= repairRange) return 0f;

            float allyHealthRatio = weakestAlly.CurrentDurabilityRatio;
            float agentHealthRatio = agent.CurrentDurabilityRatio;

            // 기본 접근 유틸리티
            float utility = 0.3f;

            // 아군이 위험할수록 접근 필요성 증가
            if (allyHealthRatio <= 0.3f)
            {
                utility += 0.7f; // 매우 위험한 아군에게 접근
            }
            else if (allyHealthRatio <= 0.5f)
            {
                utility += 0.5f; // 위험한 아군에게 접근
            }
            else if (allyHealthRatio <= healthThreshold)
            {
                utility += 0.2f; // 약간 위험한 아군에게 접근
            }

            // 거리에 따른 보정
            if (distance > repairRange * 3f)
            {
                utility -= 0.2f; // 너무 멀면 접근 어려움
            }
            else if (distance > repairRange * 2f)
            {
                utility -= 0.1f; // 멀면 약간의 패널티
            }

            // 자신의 안전도 고려
            if (agentHealthRatio <= 0.3f)
            {
                utility -= 0.3f; // 자신이 위험하면 접근 자제
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