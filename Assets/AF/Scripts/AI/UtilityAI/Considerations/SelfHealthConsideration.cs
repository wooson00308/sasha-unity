using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 자신의 현재 체력 상태를 평가하는 Consideration.
    /// 체력이 낮을수록 높은 점수를 반환한다.
    /// </summary>
    public class SelfHealthConsideration : IConsideration
    {
        public string Name => "Self Health";
        public float LastScore { get; set; }

        public SelfHealthConsideration()
        {
            // 생성자에서 필요한 초기화 수행 (현재는 없음)
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null)
            {
                logger?.TextLogger?.Log("SelfHealthConsideration: Actor is null.", LogLevel.Error);
                this.LastScore = 0f;
                return 0f;
            }

            // 체력 비율 계산 (0 ~ 1)
            float healthPercentage = (actor.TotalMaxDurability > 0) 
                ? actor.TotalCurrentDurability / actor.TotalMaxDurability 
                : 0f;

            // UtilityCurveEvaluator 사용: 체력이 낮을수록 높은 점수 (invert=true)
            float score = UtilityCurveEvaluator.Evaluate(
                input: healthPercentage,
                min: 0f,
                max: 1f,
                curveType: UtilityCurveType.Polynomial, // 예: 선형 또는 역제곱 등 선택 가능
                steepness: 2f, // 역제곱 (체력이 낮을수록 급격히 점수 증가)
                invert: true
            );

            logger?.TextLogger?.Log($"[SelfHealthConsideration] Actor={actor.Name}, Health%={healthPercentage:P1}, Score={score:F3}", LogLevel.Debug);
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 