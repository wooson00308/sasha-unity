using System.Collections.Generic;
using AF.Combat; // Assuming CombatContext might be needed
using AF.Models; // <<< Add Models namespace

namespace AF.AI.UtilityAI
{
    /// <summary>
    /// Selects the best action to perform based on calculated utility scores.
    /// </summary>
    public interface IActionSelector
    {
        /// <summary>
        /// Selects the highest scoring action from a list of potential actions.
        /// </summary>
        /// <param name="actor">The ArmoredFrame performing the action.</param>
        /// <param name="availableActions">A list of actions the agent can potentially perform.</param>
        /// <param name="context">The current combat context.</param>
        /// <returns>The selected action, or null if no suitable action is found.</returns>
        IUtilityAction SelectAction(ArmoredFrame actor, List<IUtilityAction> availableActions, CombatContext context);
    }
} 