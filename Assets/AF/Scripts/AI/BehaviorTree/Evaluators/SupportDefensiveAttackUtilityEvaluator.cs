using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 지원 방어적 공격 유틸리티 평가자 - 지원자의 자기 방어를 위한 공격을 평가
    /// </summary>
    public class SupportDefensiveAttackUtilityEvaluator : IUtilityEvaluator
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

            // 기본 유틸리티 - 지원자는 매우 방어적
            float utility = 0.2f; // 낮은 기본값

            // 적이 너무 가까우면 자기 방어 필요
            if (distanceToTarget <= 10f)
            {
                utility += 0.4f; // 가까운 적은 위험
            }
            else if (distanceToTarget <= 20f)
            {
                utility += 0.2f; // 중간 거리 적
            }

            // 자신의 체력이 위험하면 방어적 공격 필요
            if (agentHealthRatio <= 0.4f)
            {
                utility += 0.3f; // 위험하면 자기 방어
            }
            else if (agentHealthRatio >= 0.8f)
            {
                utility += 0.1f; // 안전하면 약간의 공격
            }

            // 약한 적은 마무리
            if (targetHealthRatio <= 0.3f)
            {
                utility += 0.2f; // 마무리 기회
            }

            // 지원 임무가 없을 때만 공격 고려
            bool hasAllyToSupport = HasAllyNeedingSupport(agent, context);
            if (hasAllyToSupport)
            {
                utility -= 0.4f; // 지원할 아군이 있으면 공격 우선순위 낮음
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

        private bool HasAllyNeedingSupport(ArmoredFrame agent, CombatContext context)
        {
            if (context?.Participants == null || context?.TeamAssignments == null) return false;
            if (!context.TeamAssignments.TryGetValue(agent, out int agentTeamId)) return false;

            foreach (var unit in context.Participants)
            {
                if (unit != agent && unit.IsOperational)
                {
                    if (context.TeamAssignments.TryGetValue(unit, out int unitTeamId) && unitTeamId == agentTeamId)
                    {
                        if (unit.CurrentDurabilityRatio <= 0.85f) // 체력 85% 미만이면 지원 필요
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
} 