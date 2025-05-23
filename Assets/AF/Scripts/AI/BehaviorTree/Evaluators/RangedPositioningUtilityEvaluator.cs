using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 원거리 포지셔닝 유틸리티 평가자 - 최적 사격 위치로 이동해야 하는 상황을 평가
    /// </summary>
    public class RangedPositioningUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return 0f;

            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null || !target.IsOperational) return 0f; // 타겟 없거나 비활성화

            Weapon selectedWeapon = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
            if (selectedWeapon == null) return 0f; // 사용할 무기 없음

            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float minRange = selectedWeapon.MinRange;
            float maxRange = selectedWeapon.MaxRange;

            // 사거리 내에 있는지 확인
            bool isInRange = distanceToTarget >= minRange && distanceToTarget <= maxRange;

            // 이미 사거리 내에 있고, 카이팅 거리에 너무 가깝지도 않으면 이동 필요성 낮음
            // (RangedAttackUtilityEvaluator가 공격을 선택할 가능성이 높음)
            float kitingDistance = maxRange * 0.5f; // 임시로 최대 사거리의 절반을 카이팅 거리 기준으로 사용
            bool isTooCloseForKiting = distanceToTarget <= kitingDistance;

            if (isInRange && !isTooCloseForKiting)
            {
                return 0.1f; // 사거리 내, 카이팅 필요 없으면 이동 필요성 낮음
            }

            // 사거리 밖이면 이동 필요성 증가
            float utility = 0f;
            if (distanceToTarget > maxRange)
            {
                utility += 0.7f; // 너무 멀면 접근 필요
            }
            else if (distanceToTarget < minRange)
            {
                utility += 0.6f; // 너무 가까우면 (근접전 상태) 거리 벌릴 필요
            }

            // 자신의 체력이 낮으면 포지셔닝 통해 위험 회피 고려
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            if (agentHealthRatio <= 0.4f)
            {
                utility += 0.2f; // 체력 낮으면 포지셔닝 가중치 증가
            }

            // 적의 체력이 높으면 안전한 포지션 확보 고려
            float targetHealthRatio = target.CurrentDurabilityRatio;
            if (targetHealthRatio >= 0.8f)
            {
                utility += 0.1f; // 강한 적에게서 안전거리 확보
            }

            return Mathf.Clamp01(utility);
        }
    }
} 