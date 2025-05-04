using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the ammo percentage of a specific weapon using a configurable response curve.
    /// </summary>
    public class AmmoLevelConsideration : IConsideration
    {
        public string Name => "Ammo Level";
        public float LastScore { get; set; }

        private Weapon _weapon;

        // Curve parameters
        private UtilityCurveType _curveType;
        private float _steepness = 1f;
        private float _offsetX = 0.5f;
        private float _offsetY = 0f;
        private bool _invertScore = false; // Typically false (more ammo = higher score for firing)

        public AmmoLevelConsideration(
            Weapon weapon,
            UtilityCurveType curveType = UtilityCurveType.Linear, // Default to Linear
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = false) // Default to non-inverted score for ammo
        {
            _weapon = weapon;
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            if (_weapon == null || _weapon.MaxAmmo <= 0) // 무한 탄창은 어떻게 처리할지? MaxAmmo가 0이거나 음수일 수 있음.
            {
                // 무한 탄창이거나 유효하지 않은 무기 -> 점수 1 또는 0? 여기선 1로 처리 (항상 발사 가능)
                // Debug.LogWarning($"{Name}: Weapon is null, has infinite ammo, or invalid MaxAmmo.");
                this.LastScore = 1f;
                 return 1f; 
            }

            // Calculate ammo percentage (0 to 1)
            float ammoPercentage = Mathf.Clamp01((float)_weapon.CurrentAmmo / _weapon.MaxAmmo);

            // Evaluate using the curve
            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,
                input: ammoPercentage,
                min: 0f,
                max: 1f,
                steepness: _steepness,
                invert: _invertScore,
                midpoint: _offsetX
            );

            // Debug.Log($"{Name}: Ammo%={ammoPercentage:P1}, Score={score}, Inverted={_invertScore}");
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 