using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using System.Linq;
using UnityEngine; // Added for Debug logs
using System; // Added for Random

namespace AF.AI.UtilityAI.Selectors
{
    /// <summary>
    /// Selects the action with the highest utility score, applying a minimum threshold and random tie-breaking.
    /// </summary>
    public class HighestScoreSelector : IActionSelector
    {
        private const float MinUtilityThreshold = 0.1f; // 액션 선택을 위한 최소 유틸리티 점수
        private static readonly System.Random _random = new System.Random(); // System.Random 명시

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

            List<IUtilityAction> bestActions = new List<IUtilityAction>(); // 최고 점수 액션 목록
            float highestScore = float.MinValue;

            foreach (var action in availableActions)
            {
                if (action == null) continue;

                float currentScore = action.CalculateUtility(actor, context);
                // Debug.Log($"Action: {action.Name}, Calculated Score: {currentScore}");

                if (currentScore > highestScore)
                {
                    highestScore = currentScore;
                    bestActions.Clear(); // 새로운 최고 점수, 목록 초기화
                    bestActions.Add(action);
                }
                else if (currentScore == highestScore)
                {
                    bestActions.Add(action); // 동점 액션 추가
                }
            }

            // 최소 점수 임계값 확인
            if (highestScore < MinUtilityThreshold || bestActions.Count == 0)
            {
                // Debug.Log($"No action met the minimum threshold ({MinUtilityThreshold}) or no actions found. Highest score: {highestScore}.");
                return null; // 적절한 액션 없음
            }

            // 동점 처리 (랜덤 선택)
            int selectedIndex = (bestActions.Count > 1) ? _random.Next(bestActions.Count) : 0;
            IUtilityAction selectedAction = bestActions[selectedIndex];

            // Debug.Log($"Selected Action: {selectedAction?.Name ?? "None"} with score {highestScore} (Tie-break count: {bestActions.Count})");
            return selectedAction;
        }
    }
} 