using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;
using AF.Services;
using AF.Combat;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the estimated hit chance for attacking a target with a specific weapon.
    /// Uses the same calculation logic as CombatActionExecutor.
    /// </summary>
    public class HitChanceConsideration : IConsideration
    {
        public string Name => "Hit Chance";
        public float LastScore { get; set; }

        private ArmoredFrame _targetUnit;
        private Weapon _weapon;

        // Note: This consideration needs the Attacker (actor) passed in CalculateScore

        public HitChanceConsideration(ArmoredFrame targetUnit, Weapon weapon)
        {
            _targetUnit = targetUnit;
            _weapon = weapon;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            // Basic validation
            if (actor == null || _targetUnit == null || _weapon == null || !_targetUnit.IsOperational)
            {
                //logger?.TextLogger?.Log($"[HitChanceConsideration] Invalid input or target not operational. Actor: {actor?.Name}, Target: {_targetUnit?.Name}, Weapon: {_weapon?.Name}, Target Operational: {_targetUnit?.IsOperational}", LogLevel.Debug);
                this.LastScore = 0f;
                return 0f; // Cannot hit an invalid or non-operational target
            }

            // Replicate hit chance calculation from CombatActionExecutor
            float distance = Vector3.Distance(actor.Position, _targetUnit.Position);

            // Check range first (using MaxRange)
            if (distance > _weapon.Range.MaxRange)
            {
                logger?.TextLogger?.Log($"[HitChanceConsideration] Out of range. Dist: {distance:F1}, MaxRange: {_weapon.Range.MaxRange:F1}", LogLevel.Debug);
                this.LastScore = 0f;
                return 0f; // Out of range
            }

            float atkAcc = actor.CombinedStats.Accuracy;
            float tgtEva = _targetUnit.CombinedStats.Evasion;
            float baseHit = _weapon.Accuracy
                          + (atkAcc - 1f) * 1f // Assuming 1f is the base accuracy multiplier effect
                          - tgtEva * 0.5f;      // Assuming 0.5f is the evasion effectiveness factor

            logger?.TextLogger?.Log($"[HitChanceConsideration] Before penalty - Dist: {distance:F1}, WeaponAcc: {_weapon.Accuracy:F3}, ActorAcc: {atkAcc:F3}, TargetEva: {tgtEva:F3}, BaseHit: {baseHit:F3}", LogLevel.Debug);

            // Apply distance penalty if outside optimal range (using MaxRange * 0.8f for now)
            // TODO: Consider using RangeData.OptimalRange if available and makes sense here
            float optRange = _weapon.Range.MaxRange * 0.8f;
            bool penaltyApplied = false;
            float hitAfterPenalty = baseHit;
            if (distance > optRange)
            {
                float rangeDifference = _weapon.Range.MaxRange - optRange;
                // 0으로 나누기 방지
                float penaltyRatio = rangeDifference > 0.01f ? (distance - optRange) / rangeDifference : 0f;
                float penaltyMultiplier = Mathf.Clamp(1f - penaltyRatio * 0.5f, 0.5f, 1f);
                hitAfterPenalty = baseHit * penaltyMultiplier;
                penaltyApplied = true;

                logger?.TextLogger?.Log($"[HitChanceConsideration] Distance penalty applied - OptRange: {optRange:F1}, RangeDiff: {rangeDifference:F1}, PenaltyRatio: {penaltyRatio:F3}, Multiplier: {penaltyMultiplier:F3}, HitAfterPenalty: {hitAfterPenalty:F3}", LogLevel.Debug);
            }

            // Clamp final hit chance between min/max (e.g., 1% to 95%)
            const float MIN_HIT = 0.01f, MAX_HIT = 0.95f;
            float finalHitChance = Mathf.Clamp(hitAfterPenalty, MIN_HIT, MAX_HIT);

            logger?.TextLogger?.Log($"[HitChanceConsideration] Final Calculation - FinalHitChance: {finalHitChance:F3}", LogLevel.Debug);

            // The calculated hit chance is the score
            this.LastScore = Mathf.Clamp(finalHitChance, 0f, MAX_HIT);
            return this.LastScore;
        }
    }
} 