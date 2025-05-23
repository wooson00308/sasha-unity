using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 카이팅 유틸리티 평가자 - 적이 너무 가까울 때 거리를 벌려야 하는 상황을 평가
    /// </summary>
    public class KitingUtilityEvaluator : IUtilityEvaluator
    {
        private readonly float kitingDistance;

        public KitingUtilityEvaluator(float kitingDistance)
        {
            this.kitingDistance = kitingDistance;
        }

        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return 0f;

            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null)
            {
                target = FindNearestEnemy(agent, context);
                if (target == null) return 0f;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float targetHealthRatio = target.CurrentDurabilityRatio;

            // 기본 유틸리티
            float utility = 0f;

            // 거리가 가까울수록 카이팅 필요성 증가
            if (distanceToTarget <= kitingDistance * 0.5f)
            {
                utility += 0.8f; // 매우 가까우면 즉시 후퇴
            }
            else if (distanceToTarget <= kitingDistance * 0.8f)
            {
                utility += 0.6f; // 가까우면 후퇴 고려
            }
            else if (distanceToTarget <= kitingDistance)
            {
                utility += 0.3f; // 임계 거리에서 약간 후퇴
            }
            else
            {
                return 0f; // 충분히 멀면 카이팅 불필요
            }

            // 자신의 체력이 낮을수록 카이팅 필요성 증가
            if (agentHealthRatio <= 0.3f)
            {
                utility += 0.3f; // 위험하면 적극적 후퇴
            }
            else if (agentHealthRatio <= 0.6f)
            {
                utility += 0.1f; // 중간 위험에서 조심스럽게
            }

            // 적의 체력이 높을수록 카이팅 필요성 증가
            if (targetHealthRatio >= 0.7f)
            {
                utility += 0.1f; // 강한 적은 거리 유지
            }

            return Mathf.Clamp01(utility);
        }

        private ArmoredFrame FindNearestEnemy(ArmoredFrame agent, CombatContext context)
        {
            if (context?.Participants == null || context?.TeamAssignments == null) return null;
            if (!context.TeamAssignments.TryGetValue(agent, out int agentTeamId)) return null;

            ArmoredFrame nearestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var unit in context.Participants)
            {
                if (unit != agent && unit.IsOperational)
                {
                    if (context.TeamAssignments.TryGetValue(unit, out int unitTeamId) && unitTeamId != agentTeamId)
                    {
                        float distance = Vector3.Distance(agent.Position, unit.Position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestEnemy = unit;
                        }
                    }
                }
            }

            return nearestEnemy;
        }
    }
} 