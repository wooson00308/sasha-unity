using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 수리 대상이 실제로 손상되었는지 (현재 체력이 최대 체력 미만인지) 평가하는 Consideration.
    /// 손상되지 않았으면 0점을 반환하여 액션 선택을 막는다 (Blocking).
    /// </summary>
    public class TargetDamagedConsideration : IConsideration
    {
        public string Name => "Target Is Damaged";
        public float LastScore { get; set; }

        private ArmoredFrame _target;

        public TargetDamagedConsideration(ArmoredFrame target)
        {
            _target = target;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (_target == null)
            {
                logger?.TextLogger?.Log("TargetDamagedConsideration: Target is null.", LogLevel.Error);
                this.LastScore = 0f;
                return 0f;
            }

            bool isDamaged = _target.TotalCurrentDurability < _target.TotalMaxDurability;
            float score = isDamaged ? 1f : 0f; // 손상되었으면 1점, 아니면 0점

            logger?.TextLogger?.Log($"[TargetDamagedConsideration] Target={_target.Name}, CurHP={_target.TotalCurrentDurability:F0}, MaxHP={_target.TotalMaxDurability:F0}, IsDamaged={isDamaged}, Score={score:F1}", LogLevel.Debug);
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 