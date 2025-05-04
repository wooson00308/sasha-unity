using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 수리 대상이 액터 자신 또는 아군인지 평가하는 Consideration.
    /// 적군이면 0점을 반환하여 액션 선택을 막는다 (Blocking).
    /// </summary>
    public class IsAllyOrSelfConsideration : IConsideration
    {
        public string Name => "Target is Ally or Self";
        public float LastScore { get; set; }

        private ArmoredFrame _target;

        public IsAllyOrSelfConsideration(ArmoredFrame target)
        {
            _target = target;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null || _target == null)
            {
                logger?.TextLogger?.Log("IsAllyOrSelfConsideration: Actor or Target is null.", LogLevel.Error);
                this.LastScore = 0f;
                return 0f;
            }

            bool isAllyOrSelf = (actor == _target) || (actor.TeamId == _target.TeamId);
            float score = isAllyOrSelf ? 1f : 0f; // 아군 또는 자신이면 1점, 적군이면 0점

            logger?.TextLogger?.Log($"[IsAllyOrSelfConsideration] Actor={actor.Name}(Team:{actor.TeamId}), Target={_target.Name}(Team:{_target.TeamId}), IsAllyOrSelf={isAllyOrSelf}, Score={score:F1}", LogLevel.Debug);
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 