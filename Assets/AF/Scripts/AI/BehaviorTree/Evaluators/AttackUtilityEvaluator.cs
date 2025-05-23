using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 공격에 대한 유틸리티를 평가합니다.
    /// 실제 무기 정보와 전술적 상황을 고려합니다.
    /// </summary>
    public class AttackUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            
            if (agent == null || blackboard == null)
            {
                if (textLogger != null)
                    textLogger.Log($"[AttackUtility] Agent or blackboard is null", LogLevel.Debug);
                return 0f;
            }

            // 타겟이 없으면 스스로 가장 가까운 적을 찾기
            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null)
            {
                // 적 팀 유닛 중 가장 가까운 타겟 찾기
                target = FindNearestEnemy(agent, context);
                if (target == null)
                {
                    if (textLogger != null)
                        textLogger.Log($"[AttackUtility] {agent.Name}: No enemy targets found", LogLevel.Debug);
                    return 0f;
                }
                else
                {
                    if (textLogger != null)
                        textLogger.Log($"[AttackUtility] {agent.Name}: Found nearest enemy target: {target.Name}", LogLevel.Debug);
                }
            }

            // 주무기 정보 가져오기
            var weapon = agent.GetPrimaryWeapon();
            if (weapon == null)
            {
                if (textLogger != null)
                    textLogger.Log($"[AttackUtility] {agent.Name}: No primary weapon", LogLevel.Debug);
                return 0f;
            }
            
            if (!weapon.IsOperational)
            {
                if (textLogger != null)
                    textLogger.Log($"[AttackUtility] {agent.Name}: Weapon {weapon.Name} is not operational", LogLevel.Debug);
                return 0f;
            }

            // 공격에 필요한 AP가 부족하면 불가
            if (!agent.HasEnoughAP(weapon.BaseAPCost))
            {
                if (textLogger != null)
                    textLogger.Log($"[AttackUtility] {agent.Name}: Not enough AP (current: {agent.CurrentAP}, required: {weapon.BaseAPCost})", LogLevel.Debug);
                return 0f;
            }

            // 현재 상황 정보 수집 (블랙보드 활용)
            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float targetHealthRatio = target.CurrentDurabilityRatio;
            float currentAP = agent.CurrentAP;
            float maxAP = agent.CombinedStats.MaxAP;
            float apRatio = currentAP / maxAP;

            // 거리 기반 기본 유틸리티 계산
            float utility = CalculateDynamicRangeUtility(distanceToTarget, weapon);
            
            if (textLogger != null)
                textLogger.Log($"[AttackUtility] {agent.Name}: Range utility = {utility:F3} (distance: {distanceToTarget:F1}m, weapon range: {weapon.MinRange}-{weapon.MaxRange})", LogLevel.Debug);

            // 상황적 보너스 계산 (동적)
            float bonus = CalculateDynamicSituationalBonus(agent, target, weapon, 
                agentHealthRatio, targetHealthRatio, apRatio, distanceToTarget, blackboard);
            
            utility += bonus;
            
            float finalUtility = Mathf.Clamp01(utility);
            if (textLogger != null)
                textLogger.Log($"[AttackUtility] {agent.Name}: Final utility = {finalUtility:F3} (base: {utility - bonus:F3}, bonus: {bonus:F3})", LogLevel.Debug);

            return finalUtility;
        }

        /// <summary>
        /// 가장 가까운 적을 찾는 헬퍼 메소드
        /// </summary>
        private ArmoredFrame FindNearestEnemy(ArmoredFrame agent, CombatContext context)
        {
            if (context?.Participants == null || context?.TeamAssignments == null) return null;

            // 현재 에이전트의 팀 ID 확인
            if (!context.TeamAssignments.TryGetValue(agent, out int agentTeamId)) return null;

            ArmoredFrame nearestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var unit in context.Participants)
            {
                // 자신이 아니고, 다른 팀이고, 작동 중인 유닛만 대상
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

        private float CalculateDynamicRangeUtility(float distance, Weapon weapon)
        {
            float minRange = weapon.MinRange;
            float maxRange = weapon.MaxRange;

            // 사거리 밖이면 공격 불가
            if (distance < minRange || distance > maxRange) return 0f;

            // 최적 사거리 계산 (무기 종류별 다르게)
            float optimalRangeStart, optimalRangeEnd;
            
            // 무기 타입별 최적 사거리 설정
            switch (weapon.Type)
            {
                case WeaponType.Melee:
                    optimalRangeStart = minRange;
                    optimalRangeEnd = minRange + (maxRange - minRange) * 0.5f;
                    break;
                case WeaponType.MidRange:
                    optimalRangeStart = minRange + (maxRange - minRange) * 0.4f;
                    optimalRangeEnd = minRange + (maxRange - minRange) * 0.8f;
                    break;
                case WeaponType.LongRange:
                    optimalRangeStart = minRange + (maxRange - minRange) * 0.6f;
                    optimalRangeEnd = maxRange;
                    break;
                default:
                    optimalRangeStart = minRange + (maxRange - minRange) * 0.6f;
                    optimalRangeEnd = minRange + (maxRange - minRange) * 0.8f;
                    break;
            }

            if (distance >= optimalRangeStart && distance <= optimalRangeEnd)
            {
                return 0.8f; // 최적 사거리
            }
            else
            {
                // 거리별 유틸리티 감소
                float distanceFromOptimal = Mathf.Min(
                    Mathf.Abs(distance - optimalRangeStart),
                    Mathf.Abs(distance - optimalRangeEnd)
                );
                float rangeSpan = maxRange - minRange;
                float penaltyRatio = distanceFromOptimal / rangeSpan;
                
                return Mathf.Max(0.2f, 0.8f - penaltyRatio);
            }
        }

        private float CalculateDynamicSituationalBonus(ArmoredFrame agent, ArmoredFrame target, Weapon weapon,
            float agentHealthRatio, float targetHealthRatio, float apRatio, float distance, Blackboard blackboard)
        {
            float bonus = 0f;

            // 타겟 체력 기반 동적 보너스
            if (targetHealthRatio <= 0.2f)
            {
                bonus += 0.5f; // 킬 확률 높음
            }
            else if (targetHealthRatio <= 0.4f)
            {
                bonus += 0.3f; // 중간 킬 확률
            }
            else if (targetHealthRatio <= 0.6f)
            {
                bonus += 0.1f; // 낮은 킬 확률
            }

            // 자신의 체력 상태에 따른 공격성 조절
            if (agentHealthRatio >= 0.8f)
            {
                bonus += 0.2f; // 체력 충분하면 적극적
            }
            else if (agentHealthRatio <= 0.3f)
            {
                bonus -= 0.2f; // 체력 부족하면 방어적
            }

            // AP 비율에 따른 보정
            if (apRatio >= 0.8f)
            {
                bonus += 0.15f; // AP 풍부
            }
            else if (apRatio <= 0.3f)
            {
                bonus -= 0.1f; // AP 부족
            }

            // 무기 효율성 동적 계산
            float damageEfficiency = weapon.Damage / weapon.BaseAPCost;
            if (damageEfficiency >= 40f)
            {
                bonus += 0.15f; // 높은 데미지 효율
            }
            else if (damageEfficiency <= 20f)
            {
                bonus -= 0.1f; // 낮은 데미지 효율
            }

            // 탄약 상황 고려
            if (weapon.MaxAmmo > 0)
            {
                float ammoRatio = (float)weapon.CurrentAmmo / weapon.MaxAmmo;
                if (ammoRatio <= 0.3f)
                {
                    bonus -= 0.2f; // 탄약 부족
                }
                else if (ammoRatio >= 0.8f)
                {
                    bonus += 0.1f; // 탄약 충분
                }
            }

            return bonus;
        }
    }
} 