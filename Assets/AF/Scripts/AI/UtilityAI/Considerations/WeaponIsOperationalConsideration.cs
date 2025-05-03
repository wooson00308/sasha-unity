using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the specified weapon is operational and not currently reloading.
    /// Returns 1 if usable, 0 otherwise.
    /// </summary>
    public class WeaponIsOperationalConsideration : IConsideration
    {
        public string Name => "Weapon Is Operational";

        private Weapon _weapon;

        public WeaponIsOperationalConsideration(Weapon weapon)
        {
            _weapon = weapon;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            if (_weapon == null)
            {
                // Debug.LogWarning($"{Name}: Weapon is null.");
                return 0f; // Cannot score a null weapon
            }

            // Check both operational status and reloading status
            bool isUsable = _weapon.IsOperational && !_weapon.IsReloading;

            return isUsable ? 1f : 0f;
        }
    }
} 