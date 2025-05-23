using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 방어적 공격 유틸리티 평가자 - 안전한 상황에서만 공격을 권장
    /// </summary>
    public class DefensiveAttackUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return 0f;

            // 타겟이 없으면 스스로 가장 가까운 적을 찾기
            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null)
            {
                target = FindNearestEnemy(agent, context);
                if (target == null) return 0f;
            }

            var weapon = agent.GetPrimaryWeapon();
            if (weapon == null || !weapon.IsOperational) return 0f;
            if (!agent.HasEnoughAP(weapon.BaseAPCost)) return 0f;

            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float targetHealthRatio = target.CurrentDurabilityRatio;
            float apRatio = agent.CurrentAP / agent.CombinedStats.MaxAP;

            // 기본 유틸리티 - 방어자는 매우 보수적
            float utility = 0.3f; // 낮은 기본값

            // 자신의 체력이 충분할 때만 적극적 (방어자 특성)
            if (agentHealthRatio >= 0.8f)
            {
                utility += 0.4f; // 안전하면 공격
            }
            else if (agentHealthRatio >= 0.6f)
            {
                utility += 0.2f; // 적당히 안전하면 소극적 공격
            }
            else
            {
                utility -= 0.3f; // 위험하면 공격 회피
            }

            // 타겟이 약할 때 기회 포착
            if (targetHealthRatio <= 0.3f)
            {
                utility += 0.3f; // 마무리 기회
            }
            else if (targetHealthRatio <= 0.5f)
            {
                utility += 0.1f; // 약간의 기회
            }

            // AP 여유도 고려 (방어를 위한 AP 확보 중요)
            if (apRatio >= 0.7f)
            {
                utility += 0.2f; // AP 충분하면 공격 가능
            }
            else if (apRatio <= 0.4f)
            {
                utility -= 0.4f; // AP 부족하면 공격 자제
            }

            // 거리 안전성 고려
            if (distanceToTarget >= weapon.MaxRange * 0.7f)
            {
                utility += 0.1f; // 안전한 거리에서 공격
            }
            else if (distanceToTarget <= weapon.MinRange * 1.2f)
            {
                utility -= 0.2f; // 너무 가까우면 위험
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