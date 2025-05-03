using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the estimated hit chance for attacking a target with a specific weapon.
    /// Uses the same calculation logic as CombatActionExecutor.
    /// </summary>
    public class HitChanceConsideration : IConsideration
    {
        public string Name => "Hit Chance";

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
            // Basic validation
            if (actor == null || _targetUnit == null || _weapon == null || !_targetUnit.IsOperational)
            {
                return 0f; // Cannot hit an invalid or non-operational target
            }

            // Replicate hit chance calculation from CombatActionExecutor
            float distance = Vector3.Distance(actor.Position, _targetUnit.Position);

            // Check range first (although AttackAction might already filter this)
            if (distance > _weapon.Range)
            {
                return 0f; // Out of range
            }

            float atkAcc = actor.CombinedStats.Accuracy;
            float tgtEva = _targetUnit.CombinedStats.Evasion;
            float baseHit = _weapon.Accuracy
                          + (atkAcc - 1f) * 1f // Assuming 1f is the base accuracy multiplier effect
                          - tgtEva * 0.5f;      // Assuming 0.5f is the evasion effectiveness factor

            // Apply distance penalty if outside optimal range (80% of max range)
            float optRange = _weapon.Range * 0.8f;
            if (distance > optRange && _weapon.Range > optRange) // Avoid division by zero if range == optRange
            {
                float penaltyRatio = (distance - optRange) / (_weapon.Range - optRange);
                // Assuming penalty scales hit chance down by up to 50%, clamped at 50% minimum effectiveness
                baseHit *= Mathf.Clamp(1f - penaltyRatio * 0.5f, 0.5f, 1f); 
            }

            // Clamp final hit chance between min/max (e.g., 1% to 95%)
            const float MIN_HIT = 0.01f, MAX_HIT = 0.95f;
            float finalHitChance = Mathf.Clamp(baseHit, MIN_HIT, MAX_HIT);

            // The calculated hit chance is the score
            return finalHitChance;
        }
    }
} 