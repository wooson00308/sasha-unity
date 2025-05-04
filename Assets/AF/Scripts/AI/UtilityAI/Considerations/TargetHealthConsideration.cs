using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI; // For UtilityCurveType etc.
using AF.Services; // Added for TextLoggerService

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the target's current health percentage using a configurable response curve.
    /// </summary>
    public class TargetHealthConsideration : IConsideration
    {
        public string Name => "Target Health";
        public float LastScore { get; set; }

        private ArmoredFrame _targetUnit;

        // Curve parameters
        private UtilityCurveType _curveType;
        private float _steepness = 1f;
        private float _offsetX = 0.5f;
        private float _offsetY = 0f;
        private bool _invertScore = false; // Typically true for health (lower health = higher score)

        public TargetHealthConsideration(
            ArmoredFrame targetUnit,
            UtilityCurveType curveType = UtilityCurveType.Linear, // Default to Linear
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = true) // Default to inverted score for health
        {
            _targetUnit = targetUnit;
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // Get logger
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (_targetUnit == null || _targetUnit.TotalMaxDurability <= 0)
            {
                logger?.TextLogger?.Log($"{Name}: Target Unit is null or has invalid Max Durability.", LogLevel.Warning);
                this.LastScore = 0f;
                return 0f; // Cannot score invalid target
            }

            // Calculate health percentage (0 to 1) using new properties
            float healthPercentage = Mathf.Clamp01(_targetUnit.TotalCurrentDurability / _targetUnit.TotalMaxDurability);

            // Evaluate using the curve
            // Input range is 0 to 1 for health percentage
            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,      // curveType 먼저
                input: healthPercentage,    // input
                min: 0f,                   // minInput for percentage
                max: 1f,                   // maxInput for percentage
                steepness: _steepness,    // steepness
                // exponent: 2f,           // Polynomial 사용 시 필요
                // threshold: 0.5f,        // Step 사용 시 필요
                invert: _invertScore,      // invert
                midpoint: _offsetX         // offsetX를 midpoint로 사용
                // offsetY는 Evaluate 메서드에서 제거됨
            );

            logger?.TextLogger?.Log($"[{Name}] Target={_targetUnit.Name}, HP%={healthPercentage:P1}, Score={score:F3}", LogLevel.Debug);
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 