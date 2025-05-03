using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using System.Linq;
using UnityEngine; // Added for Debug logs

namespace AF.AI.UtilityAI.Selectors
{
    /// <summary>
    /// A simple action selector that chooses the action with the highest calculated utility score.
    /// </summary>
    public class HighestScoreSelector : IActionSelector
    {
        public IUtilityAction SelectAction(ArmoredFrame actor, List<IUtilityAction> availableActions, CombatContext context)
        {
            if (availableActions == null || availableActions.Count == 0)
            {
                return null; // No actions to select from
            }

            if (actor == null)
            {
                Debug.LogError("HighestScoreSelector received a null actor!");
                return null;
            }

            IUtilityAction bestAction = null;
            float highestScore = float.MinValue;

            foreach (var action in availableActions)
            {
                if (action == null) continue;

                float currentScore = action.CalculateUtility(actor, context);
                // Debug.Log($"Action: {action.Name}, Calculated Score: {currentScore}"); // Uncomment for detailed debugging

                if (currentScore > highestScore)
                {
                    highestScore = currentScore;
                    bestAction = action;
                }
            }

            // Debug.Log($"Selected Action: {bestAction?.Name ?? "None"} with score {highestScore}"); // Uncomment for summary debugging
            return bestAction;
        }
    }
} 