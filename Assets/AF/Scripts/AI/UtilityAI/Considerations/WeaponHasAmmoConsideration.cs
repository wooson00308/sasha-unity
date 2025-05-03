using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Checks if the specified weapon has ammunition.
    /// Returns 1 if it has ammo (or is infinite), 0 otherwise.
    /// </summary>
    public class WeaponHasAmmoConsideration : IConsideration
    {
        public string Name => "Weapon Has Ammo";

        private Weapon _weapon;

        public WeaponHasAmmoConsideration(Weapon weapon)
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

            return _weapon.HasAmmo() ? 1f : 0f;
        }
    }
} 