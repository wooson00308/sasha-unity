using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 근접전 접근 유틸리티 평가자 - 적에게 다가가야 하는 상황을 평가
    /// </summary>
    public class MeleeApproachUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return 0f;

            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null)
            {
                target = FindNearestEnemy(agent, context);
                if (target == null) return 0f;
            }

            var weapon = agent.GetPrimaryWeapon();
            if (weapon == null) return 0f;

            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float apRatio = agent.CurrentAP / agent.CombinedStats.MaxAP;

            // 기본 접근 유틸리티
            float utility = 0.3f;

            // 사거리 밖이면 접근 필요성 높음
            if (distanceToTarget > weapon.MaxRange)
            {
                utility += 0.5f; // 사거리 밖이면 접근 필수
            }
            else if (distanceToTarget > weapon.MaxRange * 0.7f)
            {
                utility += 0.3f; // 최적 거리 아니면 접근 고려
            }
            else if (distanceToTarget <= weapon.MaxRange * 0.3f)
            {
                utility -= 0.2f; // 너무 가까우면 접근 불필요
            }

            // 체력이 충분하면 적극적 접근
            if (agentHealthRatio >= 0.7f)
            {
                utility += 0.2f; // 건강하면 적극적
            }
            else if (agentHealthRatio <= 0.3f)
            {
                utility -= 0.1f; // 위험하면 신중하게
            }

            // AP 여유도 고려
            if (apRatio >= 0.7f)
            {
                utility += 0.1f; // AP 충분하면 접근 후 공격 가능
            }
            else if (apRatio <= 0.3f)
            {
                utility -= 0.2f; // AP 부족하면 접근 자제
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