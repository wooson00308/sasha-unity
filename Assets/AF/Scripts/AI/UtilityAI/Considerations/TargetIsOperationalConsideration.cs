using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the target unit is currently operational.
    /// Returns 1 if operational, 0 otherwise.
    /// </summary>
    public class TargetIsOperationalConsideration : IConsideration
    {
        public string Name => "Target Is Operational";

        private ArmoredFrame _targetUnit;

        public TargetIsOperationalConsideration(ArmoredFrame targetUnit)
        {
            _targetUnit = targetUnit;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // No need for actor context here, only target
            if (_targetUnit == null)
            {
                // Debug.LogWarning($"{Name}: Target Unit is null.");
                return 0f; // Cannot score a null target
            }

            return _targetUnit.IsOperational ? 1f : 0f;
        }
    }
} 