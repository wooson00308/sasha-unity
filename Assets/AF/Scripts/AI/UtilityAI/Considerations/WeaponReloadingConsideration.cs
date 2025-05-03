using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates whether a specific weapon is currently reloading.
    /// Returns 1 if not reloading, 0 if reloading.
    /// </summary>
    public class WeaponReloadingConsideration : IConsideration
    {
        public string Name => "Weapon Reloading Status";

        private Weapon _weapon;

        public WeaponReloadingConsideration(Weapon weapon)
        {
            _weapon = weapon;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            if (_weapon == null)
            {
                // Debug.LogWarning($"{Name}: Weapon is null.");
                return 0f; // Cannot evaluate null weapon
            }

            // If the weapon is currently reloading, return 0 (cannot be used).
            // Otherwise, return 1 (can be used).
            return _weapon.IsReloading ? 0f : 1f;
        }
    }
} 