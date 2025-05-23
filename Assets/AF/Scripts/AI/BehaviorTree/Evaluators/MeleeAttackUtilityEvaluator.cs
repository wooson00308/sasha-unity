using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 근접전 공격 유틸리티 평가자 - 적극적인 근접 공격을 권장
    /// </summary>
    public class MeleeAttackUtilityEvaluator : IUtilityEvaluator
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

            // 기본 유틸리티 - 근접전은 적극적
            float utility = 0.6f; // 높은 기본값

            // 근접 무기일 때 더 적극적
            if (weapon.Type == WeaponType.Melee)
            {
                utility += 0.2f; // 근접 무기 보너스
            }

            // 자신의 체력 상태에 따른 공격성 조절 (근접전은 위험을 감수)
            if (agentHealthRatio >= 0.7f)
            {
                utility += 0.2f; // 체력 충분하면 매우 적극적
            }
            else if (agentHealthRatio >= 0.4f)
            {
                utility += 0.1f; // 중간 체력에서도 공격
            }
            else if (agentHealthRatio <= 0.3f)
            {
                utility -= 0.1f; // 매우 위험할 때만 약간 자제
            }

            // 타겟 체력에 따른 우선순위
            if (targetHealthRatio <= 0.4f)
            {
                utility += 0.3f; // 약한 적 마무리
            }
            else if (targetHealthRatio <= 0.7f)
            {
                utility += 0.1f; // 중간 체력 적
            }

            // 거리에 따른 보정 (근접전은 가까울수록 좋음)
            if (distanceToTarget <= weapon.MaxRange * 0.5f)
            {
                utility += 0.2f; // 최적 근접 거리
            }
            else if (distanceToTarget <= weapon.MaxRange)
            {
                utility += 0.1f; // 공격 가능 거리
            }

            // AP 고려 (근접전은 연속 공격 중요)
            if (apRatio >= 0.6f)
            {
                utility += 0.1f; // 연속 공격 가능
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