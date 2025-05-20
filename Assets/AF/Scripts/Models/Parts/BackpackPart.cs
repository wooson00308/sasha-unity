using UnityEngine;
using System.Collections.Generic;

namespace AF.Models
{
    public class BackpackPart : Part
    {
        public BackpackPart(string name, Stats stats, float durability, float weight, List<string> initialAbilities = null)
            : base(name, PartType.Backpack, stats, durability, weight, initialAbilities)
        {
            // Backpack-specific initialization, if any
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            // Logic for when the backpack is destroyed
            // Example: Remove special abilities, cause an explosion, etc.
            Debug.Log($"{parentAF.Name}의 백팩 {Name}이(가) 파괴되었습니다!");
        }

        // Add any backpack-specific methods or properties here
        // For example, methods to activate special abilities
    }
} 