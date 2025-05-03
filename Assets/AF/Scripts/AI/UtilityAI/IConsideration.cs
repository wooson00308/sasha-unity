using AF.Combat; // Assuming CombatContext might be needed
using AF.Models; // <<< Add Models namespace for ArmoredFrame

namespace AF.AI.UtilityAI
{
    /// <summary>
    /// Evaluates a specific aspect of the game state to produce a score (typically 0-1).
    /// This score contributes to the overall utility of an action.
    /// </summary>
    public interface IConsideration
    {
        string Name { get; } // Name for debugging/identification

        /// <summary>
        /// Calculates the score based on the current context.
        /// </summary>
        /// <param name="actor">The ArmoredFrame performing the action.</param>
        /// <param name="context">The current combat context providing necessary information.</param>
        /// <returns>A score typically between 0 and 1.</returns>
        float CalculateScore(ArmoredFrame actor, CombatContext context);
    }
} 