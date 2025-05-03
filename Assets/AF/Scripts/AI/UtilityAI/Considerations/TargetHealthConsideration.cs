using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI; // For UtilityCurveType etc.

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the target's current health percentage using a configurable response curve.
    /// </summary>
    public class TargetHealthConsideration : IConsideration
    {
        public string Name => "Target Health";

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
            if (_targetUnit == null || _targetUnit.TotalMaxDurability <= 0)
            {
                // Debug.LogWarning($"{Name}: Target Unit is null or has invalid Max Durability.");
                return 0f; // Cannot score invalid target
            }

            // Calculate health percentage (0 to 1) using new properties
            float healthPercentage = Mathf.Clamp01(_targetUnit.TotalCurrentDurability / _targetUnit.TotalMaxDurability);

            // Evaluate using the curve
            // Input range is 0 to 1 for health percentage
            return UtilityCurveEvaluator.Evaluate(
                healthPercentage,
                0f, // minInput for percentage
                1f, // maxInput for percentage
                _curveType,
                _steepness,
                _offsetX,
                _offsetY,
                _invertScore
            );
        }
    }
} 