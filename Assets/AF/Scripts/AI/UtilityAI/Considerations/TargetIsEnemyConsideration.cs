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

        private ArmoredFrame _targetUnit;

        public TargetIsEnemyConsideration(ArmoredFrame targetUnit)
        {
            _targetUnit = targetUnit;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            if (actor == null || _targetUnit == null)
            {
                return 0f; // Cannot determine relationship without both units
            }

            // Option 1: Use CombatContext if it has TeamAssignments
            if (context.TeamAssignments != null &&
                context.TeamAssignments.TryGetValue(actor, out int actorTeam) &&
                context.TeamAssignments.TryGetValue(_targetUnit, out int targetTeam))
            {
                return (actorTeam != targetTeam) ? 1f : 0f;
            }

            // Option 2: Fallback using CombatSimulatorService (less ideal from Consideration directly)
            try
            {
                var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                if (simulator != null)
                {
                    // Check if the target is in the list of enemies for the actor
                    var enemies = simulator.GetEnemies(actor);
                    return enemies != null && enemies.Contains(_targetUnit) ? 1f : 0f;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{Name}: Error accessing CombatSimulatorService: {ex.Message}");
            }

            // Default to 0 if relationship cannot be determined
            Debug.LogWarning($"{Name}: Could not determine team relationship between {actor.Name} and {_targetUnit.Name}.");
            return 0f;
        }
    }
} 