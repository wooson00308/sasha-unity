using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the target unit is currently operational.
    /// Returns 1 if operational, 0 otherwise.
    /// </summary>
    public class TargetIsOperationalConsideration : IConsideration
    {
        public string Name => "Target Is Operational";
        public float LastScore { get; set; }

        private ArmoredFrame _targetUnit;

        public TargetIsOperationalConsideration(ArmoredFrame targetUnit)
        {
            _targetUnit = targetUnit;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            // No need for actor context here, only target
            if (_targetUnit == null)
            {
                logger?.TextLogger?.Log($"{Name}: Target Unit is null.", LogLevel.Warning);
                this.LastScore = 0f;
                return 0f; // Cannot score a null target
            }

            float score = _targetUnit.IsOperational ? 1f : 0f;
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 