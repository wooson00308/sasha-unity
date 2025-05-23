using AF.Combat;
using AF.Models;
using AF.Models.Abilities;
using AF.Data;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 어빌리티 사용에 대한 유틸리티를 평가합니다.
    /// 실제 어빌리티 실행 가능성과 상황적 필요성을 모두 고려합니다.
    /// </summary>
    public class AbilityUtilityEvaluator : IUtilityEvaluator
    {
        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // 기본적으로 어빌리티를 사용할 수 없는 상황이면 0
            if (agent == null || blackboard == null) return 0f;

            // 타겟이 없으면 어빌리티 우선순위 낮음
            ArmoredFrame target = blackboard.CurrentTarget ?? agent.CurrentTarget;
            if (target == null) return 0.1f;

            // 실제로 사용 가능한 어빌리티가 있는지 확인
            float bestAbilityUtility = 0f;
            bool hasUsableAbility = false;

            // 현재 상황 정보 수집 (블랙보드 활용)
            float agentHealthRatio = agent.CurrentDurabilityRatio;
            float targetHealthRatio = target.CurrentDurabilityRatio;
            float distanceToTarget = Vector3.Distance(agent.Position, target.Position);
            float currentAP = agent.CurrentAP;
            float maxAP = agent.CombinedStats.MaxAP;
            float apRatio = currentAP / maxAP;

            // 에이전트의 모든 파츠에서 어빌리티 탐색
            foreach (var partEntry in agent.Parts)
            {
                var part = partEntry.Value;
                if (part?.Abilities == null || !part.IsOperational) continue;

                foreach (string abilityId in part.Abilities)
                {
                    // AbilityDatabase에서 어빌리티 정보 가져오기
                    if (!AbilityDatabase.TryGetAbility(abilityId, out AbilitySO abilitySO)) continue;
                    if (abilitySO?.TargetType != Models.AbilityTargetType.Self) continue;

                    // 실제 실행 가능한지 확인
                    if (!AbilityEffectRegistry.TryGetExecutor(abilityId, out IAbilityEffectExecutor executor)) continue;
                    if (!executor.CanExecute(context, agent, target, abilitySO)) continue;

                    hasUsableAbility = true;

                    // 어빌리티별 상황적 유틸리티 계산 (동적)
                    float abilityUtility = CalculateDynamicAbilityUtility(agent, target, abilitySO, 
                        agentHealthRatio, targetHealthRatio, distanceToTarget, apRatio, blackboard);
                    bestAbilityUtility = Mathf.Max(bestAbilityUtility, abilityUtility);
                }
            }

            // 사용 가능한 어빌리티가 없으면 0
            if (!hasUsableAbility) return 0f;

            return bestAbilityUtility;
        }

        private float CalculateDynamicAbilityUtility(ArmoredFrame agent, ArmoredFrame target, AbilitySO abilitySO,
            float agentHealthRatio, float targetHealthRatio, float distanceToTarget, float apRatio, Blackboard blackboard)
        {
            float baseUtility = 0f;
            var weapon = agent.GetPrimaryWeapon();

            // 어빌리티 종류별 동적 상황 판단
            switch (abilitySO.AbilityName)
            {
                case "정밀 조준":
                    if (weapon != null)
                    {
                        float weaponRange = weapon.MaxRange;
                        float distanceRatio = distanceToTarget / weaponRange;
                        
                        // 거리별 동적 유틸리티
                        if (distanceRatio >= 0.7f && distanceRatio <= 0.9f)
                        {
                            baseUtility = 0.9f; // 최적 사거리
                        }
                        else if (distanceRatio > 0.5f && distanceRatio < 1.0f)
                        {
                            baseUtility = 0.6f; // 준최적 사거리
                        }
                        else
                        {
                            baseUtility = 0.3f; // 비최적 사거리
                        }

                        // AP 여유도에 따른 보정
                        if (apRatio >= 0.8f) baseUtility += 0.2f; // AP 충분
                        else if (apRatio <= 0.4f) baseUtility -= 0.3f; // AP 부족

                        // 타겟 체력이 높을 때 더 유용 (킬 확률 증가)
                        if (targetHealthRatio >= 0.7f) baseUtility += 0.1f;
                    }
                    break;

                case "에너지 실드":
                    // 체력 비율에 따른 동적 유틸리티
                    if (agentHealthRatio <= 0.3f)
                    {
                        baseUtility = 1.0f; // 매우 위험한 상황
                    }
                    else if (agentHealthRatio <= 0.6f)
                    {
                        baseUtility = 0.8f; // 위험한 상황
                    }
                    else if (agentHealthRatio <= 0.8f)
                    {
                        baseUtility = 0.5f; // 약간 위험
                    }
                    else
                    {
                        baseUtility = 0.2f; // 안전한 상황
                    }

                    // 거리에 따른 보정 (가까이 있을수록 더 위험)
                    if (distanceToTarget <= 15f) baseUtility += 0.3f;
                    else if (distanceToTarget <= 25f) baseUtility += 0.1f;

                    // 타겟 체력이 높고 자신이 위험할 때 더 유용
                    if (targetHealthRatio >= 0.7f && agentHealthRatio <= 0.5f) baseUtility += 0.2f;
                    break;

                case "응급 수리 키트":
                    // 체력이 매우 낮을 때만 유용
                    if (agentHealthRatio <= 0.3f)
                    {
                        baseUtility = 1.0f - agentHealthRatio; // 체력이 낮을수록 더 높은 유틸리티
                    }
                    else if (agentHealthRatio <= 0.5f)
                    {
                        baseUtility = 0.4f;
                    }
                    else
                    {
                        baseUtility = 0f; // 체력이 충분하면 불필요
                    }

                    // 안전한 거리에서 더 유용
                    if (distanceToTarget >= 30f) baseUtility += 0.2f;
                    else if (distanceToTarget <= 15f) baseUtility -= 0.3f; // 위험한 거리에서는 덜 유용
                    break;

                default:
                    // 기본적인 상황별 유틸리티
                    if (agentHealthRatio <= 0.5f)
                    {
                        baseUtility = 0.6f;
                    }
                    else if (agentHealthRatio <= 0.8f)
                    {
                        baseUtility = 0.4f;
                    }
                    else
                    {
                        baseUtility = 0.2f;
                    }
                    break;
            }

            // AP 비용 대비 효율성 고려
            float apCostRatio = abilitySO.APCost / agent.CombinedStats.MaxAP;
            if (apCostRatio > 0.4f) baseUtility -= 0.2f; // 비싼 어빌리티는 패널티

            return Mathf.Clamp01(baseUtility);
        }
    }
} 