using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates whether a specific weapon is currently reloading.
    /// Returns 1 if not reloading, 0 if reloading.
    /// </summary>
    public class WeaponReloadingConsideration : IConsideration
    {
        public string Name => "Weapon Reloading Status";
        public float LastScore { get; set; }

        private Weapon _weapon;

        public WeaponReloadingConsideration(Weapon weapon)
        {
            _weapon = weapon;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (_weapon == null)
            {
                logger?.TextLogger?.Log($"{Name}: Weapon is null.", LogLevel.Warning);
                this.LastScore = 0f;
                return 0f; // Cannot evaluate null weapon
            }

            // If the weapon is currently reloading, return 0 (cannot be used).
            // Otherwise, return 1 (can be used).
            float score = _weapon.IsReloading ? 0f : 1f;
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 