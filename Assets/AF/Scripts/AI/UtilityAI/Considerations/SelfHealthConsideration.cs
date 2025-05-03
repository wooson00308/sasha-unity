using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the actor's own current health percentage using a configurable response curve.
    /// </summary>
    public class SelfHealthConsideration : IConsideration
    {
        public string Name => "Self Health";

        // Curve parameters
        private UtilityCurveType _curveType;
        private float _steepness = 1f;
        private float _offsetX = 0.5f;
        private float _offsetY = 0f;
        private bool _invertScore = false; // Typically true (lower health = higher score for defensive actions)

        public SelfHealthConsideration(
            UtilityCurveType curveType = UtilityCurveType.Linear,
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = true) // Default to inverted score for self health
        {
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            if (actor == null || actor.TotalMaxDurability <= 0)
            {
                // Debug.LogWarning($"{Name}: Actor is null or has invalid Max Durability.");
                return 0f; // Cannot score invalid actor
            }

            // Calculate self health percentage (0 to 1)
            float healthPercentage = Mathf.Clamp01(actor.TotalCurrentDurability / actor.TotalMaxDurability);

            // Evaluate using the curve
            return UtilityCurveEvaluator.Evaluate(
                healthPercentage,
                0f, // minInput
                1f, // maxInput
                _curveType,
                _steepness,
                _offsetX,
                _offsetY,
                _invertScore
            );
        }
    }
} 