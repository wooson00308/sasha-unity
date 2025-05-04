using AF.Combat;
using AF.Models;
using AF.Services; // For ServiceLocator
using UnityEngine;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the target unit is an enemy of the actor.
    /// Returns 1 if the target is an enemy, 0 otherwise.
    /// </summary>
    public class TargetIsEnemyConsideration : IConsideration
    {
        public string Name => "Target Is Enemy";
        public float LastScore { get; set; }

        private ArmoredFrame _targetUnit;

        public TargetIsEnemyConsideration(ArmoredFrame targetUnit)
        {
            _targetUnit = targetUnit;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null || _targetUnit == null)
            {
                return 0f; // Cannot determine relationship without both units
            }

            // Option 1: Use CombatContext if it has TeamAssignments
            if (context.TeamAssignments != null &&
                context.TeamAssignments.TryGetValue(actor, out int actorTeam) &&
                context.TeamAssignments.TryGetValue(_targetUnit, out int targetTeam))
            {
                this.LastScore = (actorTeam != targetTeam) ? 1f : 0f;
                return this.LastScore;
            }

            // Option 2: Fallback using CombatSimulatorService (less ideal from Consideration directly)
            try
            {
                var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                if (simulator != null)
                {
                    // Check if the target is in the list of enemies for the actor
                    var enemies = simulator.GetEnemies(actor);
                    float score = enemies != null && enemies.Contains(_targetUnit) ? 1f : 0f;
                    this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
                    return this.LastScore;
                }
            }
            catch (System.Exception ex)
            {
                logger?.TextLogger?.Log($"{Name}: Error accessing CombatSimulatorService: {ex.Message}", LogLevel.Error);
                this.LastScore = 0.01f;
                return 0.01f;
            }

            // Default to 0 if relationship cannot be determined
            logger?.TextLogger?.Log($"{Name}: Could not determine team relationship between {actor.Name} and {_targetUnit.Name}. Defaulting score to 0.", LogLevel.Warning);
            this.LastScore = 0f;
            return 0f;
        }
    }
} 