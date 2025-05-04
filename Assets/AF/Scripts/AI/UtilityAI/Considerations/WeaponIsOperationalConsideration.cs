using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the specified weapon is operational and not currently reloading.
    /// Returns 1 if usable, 0 otherwise.
    /// </summary>
    public class WeaponIsOperationalConsideration : IConsideration
    {
        public string Name => "Weapon Is Operational";
        public float LastScore { get; set; }

        private Weapon _weapon;

        public WeaponIsOperationalConsideration(Weapon weapon)
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
                return 0f; // Cannot score a null weapon
            }

            // Check both operational status and reloading status
            bool isUsable = _weapon.IsOperational && !_weapon.IsReloading;

            this.LastScore = isUsable ? 1f : 0f;
            return this.LastScore;
        }
    }
} 