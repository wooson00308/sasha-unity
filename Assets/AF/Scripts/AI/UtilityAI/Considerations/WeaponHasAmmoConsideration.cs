using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the specified weapon has ammunition.
    /// Returns 1 if it has ammo (or is infinite), 0 otherwise.
    /// </summary>
    public class WeaponHasAmmoConsideration : IConsideration
    {
        public string Name => "Weapon Has Ammo";
        public float LastScore { get; set; }

        private Weapon _weapon;

        public WeaponHasAmmoConsideration(Weapon weapon)
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

            float score = _weapon.HasAmmo() ? 1f : 0f;
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 