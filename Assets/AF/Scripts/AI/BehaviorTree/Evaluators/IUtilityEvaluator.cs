using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 유틸리티 평가 인터페이스
    /// </summary>
    public interface IUtilityEvaluator
    {
        float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
    }
} 