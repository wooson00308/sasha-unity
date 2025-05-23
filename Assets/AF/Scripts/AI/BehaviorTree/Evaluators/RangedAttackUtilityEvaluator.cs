using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.BehaviorTree.Conditions;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 원거리 공격 유틸리티 평가자 - 타겟을 공격할 가치가 있는 상황을 평가
    /// </summary>
    public class RangedAttackUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return 0f;

            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null || !target.IsOperational) return 0f; // 타겟 없거나 비활성화

            Weapon selectedWeapon = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
            if (selectedWeapon == null || !selectedWeapon.IsOperational) return 0f; // 공격 가능한 무기 없음

            // 거리에 따른 공격 유틸리티 기본 값 (최적 사거리에서 높음)
            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float utility = GetUtilityByDistance(distanceToTarget, selectedWeapon.MinRange, selectedWeapon.MaxRange);

            // 타겟 체력에 따른 가중치 (체력 낮을수록 공격 가중치 증가)
            float targetHealthRatio = target.CurrentDurabilityRatio;
            utility *= (1f + (1f - targetHealthRatio) * 0.5f); // 체력 0%면 1.5배, 100%면 1배

            // 자신의 체력에 따른 가중치 (자신의 체력이 높을수록 공격적)
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            utility *= (1f + agentHealthRatio * 0.2f); // 체력 100%면 1.2배, 0%면 1배

            // 탄약 상황에 따른 가중치 (탄약 충분할수록 공격적)
            if (selectedWeapon.MaxAmmo > 0 && selectedWeapon.CurrentAmmo <= selectedWeapon.MaxAmmo * 0.2f && !selectedWeapon.IsReloading)
            {
                utility *= 0.5f; // 탄약 부족하고 재장전 중 아니면 공격 우선순위 낮춤
            }

            // 재장전 중이면 공격 불가능
            if (selectedWeapon.IsReloading) return 0f;

            return Mathf.Clamp01(utility);
        }

        private float GetUtilityByDistance(float distance, float minRange, float maxRange)
        {
            // 사거리 내에 없으면 0
            if (distance < minRange || distance > maxRange)
            {
                return 0f;
            }

            // 최적 사거리 (중앙) 계산
            float optimalRange = (minRange + maxRange) / 2f;

            // 거리가 최적 사거리에 가까울수록 유틸리티 증가
            float range = maxRange - minRange;
            if (range <= 0) return 1f; // 사거리가 없으면 항상 1 (접근전용 무기 등)

            float distanceRatio = Mathf.Abs(distance - optimalRange) / (range / 2f);

            // 거리가 최적 사거리에서 멀어질수록 유틸리티 감소 (선형)
            float utility = 1f - distanceRatio;

            return Mathf.Clamp01(utility);
        }
    }
} 