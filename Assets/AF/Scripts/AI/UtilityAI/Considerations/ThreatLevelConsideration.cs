using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.AI.UtilityAI;
using System.Linq; // For Count()
using AF.Services; // <<< 추가

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the perceived threat level around the actor based on nearby enemies.
    /// Currently uses the number of enemies within a specified radius, evaluated by a response curve.
    /// </summary>
    public class ThreatLevelConsideration : IConsideration
    {
        public string Name => "Threat Level";
        public float LastScore { get; set; }

        private float _threatRadius; // 위협 감지 반경
        private int _maxThreatCount; // 점수 계산 시 최대 위협 수 (이 수를 넘으면 점수는 0 또는 1로 고정)

        // Curve parameters
        private UtilityCurveType _curveType;
        private float _steepness = 1f;
        private float _offsetX = 0.5f;
        private float _offsetY = 0f;
        private bool _invertScore = false; // Typically true for threat (more enemies = lower utility for non-defensive actions)

        public ThreatLevelConsideration(
            float threatRadius = 10f, // Default radius
            int maxThreatCount = 5,  // Default max count for scaling
            UtilityCurveType curveType = UtilityCurveType.Linear,
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = true) // Default to inverted score
        {
            _threatRadius = Mathf.Max(0.1f, threatRadius);
            _maxThreatCount = Mathf.Max(1, maxThreatCount); // 최소 1 이상
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // <<< 로거 가져오기 추가 >>>
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            if (actor == null)
            {
                logger?.TextLogger?.Log($"{Name}: Actor is null.", LogLevel.Warning); // <<< TextLogger 사용 및 레벨 Warning으로 변경
                this.LastScore = 0f;
                return 0f;
            }

            // Get enemies from context
            var enemies = context.GetEnemies(actor);
            if (enemies == null)
            {
                this.LastScore = _invertScore ? 1f : 0f;
                return _invertScore ? 1f : 0f; // No enemies, threat is minimal (or maximal if not inverted)
            }

            // Count enemies within the radius
            int enemiesNearby = enemies.Count(enemy =>
                enemy != null &&
                enemy.IsOperational &&
                Vector3.Distance(actor.Position, enemy.Position) <= _threatRadius
            );

            // Evaluate using the curve based on the count
            // Input range is 0 to _maxThreatCount
            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,
                input: enemiesNearby,
                min: 0f,
                max: _maxThreatCount,
                steepness: _steepness,
                invert: _invertScore,
                midpoint: _offsetX
            );

            logger?.TextLogger?.Log($"[ThreatLevelConsideration] Actor={actor.Name}, Radius={_threatRadius:F1}, NearbyThreats={enemiesNearby}, MaxThreat={_maxThreatCount}, Score={score:F3}", LogLevel.Debug); // <<< TextLogger로 변경 및 활성화

            if (enemiesNearby == 0) // <<< 0점 반환 로직은 유지
            {
                this.LastScore = 0f;
                return 0f; 
            }

            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 