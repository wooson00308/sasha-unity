using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 액션 수행에 필요한 AP(Action Points) 비용을 평가하는 Consideration.
    /// 필요한 AP가 부족하면 0점을 반환하여 액션 선택을 막는다 (Blocking).
    /// </summary>
    public class ActionPointCostConsideration : IConsideration
    {
        public string Name => "Action Point Cost";
        public float LastScore { get; set; }

        private float _actionCost;

        public ActionPointCostConsideration(float cost)
        {
            _actionCost = Mathf.Max(0f, cost); // 비용은 0 이상
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // <<< 로거 가져오기 >>>
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null)
            {
                // <<< TextLogger 사용 >>>
                //logger?.TextLogger?.Log("[ActionPointCostConsideration] Actor is null.", LogLevel.Error);
                return 0f;
            }

            bool canAfford = actor.CurrentAP >= _actionCost;
            float score = canAfford ? 1f : 0f; // AP 충분하면 1점, 아니면 0점

            // <<< TextLogger 사용 및 로그 레벨 Debug로 변경 >>>
            logger?.TextLogger?.Log($"[ActionPointCostConsideration] Actor: {actor.Name}, CurrentAP: {actor.CurrentAP:F1}, ActionCost: {_actionCost:F1}, CanAfford: {canAfford}, CalculatedScore: {score:F1}", LogLevel.Debug);

            // Debug.Log($"ActionPointCostConsideration: ActorAP={actor.CurrentAP}, Cost={_actionCost}, CanAfford={canAfford}, Score={score}"); // <<< 기존 Debug.Log 주석 처리 유지 >>>
            this.LastScore = score; // <<< 수정: 클램핑 없이 그대로 할당
            return this.LastScore;
        }
    }
} 