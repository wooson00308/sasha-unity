using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the actor's current Action Points (AP) percentage using a configurable response curve.
    /// </summary>
    public class ActionPointConsideration : IConsideration
    {
        public string Name => "Action Points";
        public float LastScore { get; set; }

        // Curve parameters
        private UtilityCurveType _curveType;
        private float _steepness = 1f;
        private float _offsetX = 0.5f;
        private float _offsetY = 0f;
        private bool _invertScore = false; // Typically false for AP (more AP = higher score)

        public ActionPointConsideration(
            UtilityCurveType curveType = UtilityCurveType.Linear, // Default to Linear
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = false) // Default to non-inverted score for AP
        {
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null || actor.CombinedStats.MaxAP <= 0)
            {
                // logger?.TextLogger?.Log($"{Name}: Actor is null or has invalid MaxAP.", LogLevel.Warning);
                this.LastScore = 0f;
                return 0f; // Cannot score invalid actor
            }

            // Calculate AP percentage (0 to 1)
            float apPercentage = Mathf.Clamp01(actor.CurrentAP / actor.CombinedStats.MaxAP);

            // Evaluate using the curve
            // Input range is 0 to 1 for AP percentage
            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,
                input: apPercentage,
                min: 0f,
                max: 1f,
                steepness: _steepness,
                invert: _invertScore,
                midpoint: _offsetX
            );

            logger?.TextLogger?.Log($"{Name}: AP%={apPercentage:P1}, Score={score:F3}", LogLevel.Debug);
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 