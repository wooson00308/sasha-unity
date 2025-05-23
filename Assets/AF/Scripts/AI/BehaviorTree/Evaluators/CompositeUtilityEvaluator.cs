using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;

namespace AF.AI.BehaviorTree.Evaluators
{
    /// <summary>
    /// 여러 평가자를 조합하는 복합 평가자
    /// </summary>
    public class CompositeUtilityEvaluator : IUtilityEvaluator
    {
        private readonly IUtilityEvaluator[] evaluators;
        private readonly float[] weights;

        public CompositeUtilityEvaluator(IUtilityEvaluator[] evaluators, float[] weights = null)
        {
            this.evaluators = evaluators;
            this.weights = weights ?? new float[evaluators.Length];
            
            // 가중치가 제공되지 않으면 균등 분배
            if (weights == null)
            {
                for (int i = 0; i < this.weights.Length; i++)
                {
                    this.weights[i] = 1.0f / evaluators.Length;
                }
            }
        }

        public float Evaluate(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            
            float totalUtility = 0f;
            for (int i = 0; i < evaluators.Length; i++)
            {
                float utility = evaluators[i].Evaluate(agent, blackboard, context);
                
                // 디버깅 로그 추가
                if (textLogger != null)
                {
                    string evaluatorName = evaluators[i].GetType().Name;
                    textLogger.Log($"[CompositeUtility] {agent.Name}: {evaluatorName}[{i}] = {utility:F3}", LogLevel.Debug);
                }
                
                // 첫 번째 evaluator가 0이면 전체 결과를 0으로 (핵심 조건 실패)
                if (i == 0 && utility == 0f)
                {
                    if (textLogger != null)
                    {
                        textLogger.Log($"[CompositeUtility] {agent.Name}: First evaluator returned 0, aborting composite evaluation", LogLevel.Debug);
                    }
                    return 0f;
                }
                
                totalUtility += utility * weights[i];
            }
            
            float finalUtility = Mathf.Clamp01(totalUtility);
            if (textLogger != null)
            {
                textLogger.Log($"[CompositeUtility] {agent.Name}: Final composite utility = {finalUtility:F3}", LogLevel.Debug);
            }
            
            return finalUtility;
        }
    }
} 