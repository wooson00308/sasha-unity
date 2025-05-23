using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// AP 기반 효용 평가자 - AP 효율성을 고려한 평가
    /// </summary>
    public class APEfficiencyEvaluator : IUtilityEvaluator
    {
        private readonly CombatActionEvents.ActionType actionType;
        private readonly float baseUtility;
        private readonly float apThreshold;

        public APEfficiencyEvaluator(CombatActionEvents.ActionType actionType, float baseUtility = 0.5f, float apThreshold = 3f)
        {
            this.actionType = actionType;
            this.baseUtility = baseUtility;
            this.apThreshold = apThreshold;
        }

        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            float apCost = GetActionAPCost(agent, actionType);
            float remainingAP = agent.CurrentAP;
            
            // AP가 부족하면 효용값 0
            if (remainingAP < apCost)
                return 0f;
            
            // 남은 AP가 임계값보다 많을수록 효용값 증가
            float apEfficiency = (remainingAP - apCost) / apThreshold;
            return Mathf.Clamp01(baseUtility * (1f + apEfficiency));
        }

        private float GetActionAPCost(ArmoredFrame agent, CombatActionEvents.ActionType actionType)
        {
            // 실제 액션별 AP 비용 계산
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    var weapon = agent.GetPrimaryWeapon();
                    return weapon?.BaseAPCost ?? 3f;
                case CombatActionEvents.ActionType.Move: 
                    return 4f; // 기본 이동 비용
                case CombatActionEvents.ActionType.UseAbility: 
                    return 2f; // 기본값, 실제로는 어빌리티별로 다름
                case CombatActionEvents.ActionType.Reload: 
                    return agent.GetPrimaryWeapon()?.ReloadAPCost ?? 2f;
                case CombatActionEvents.ActionType.Defend: 
                    return 2f;
                default: 
                    return 1f;
            }
        }
    }
} 