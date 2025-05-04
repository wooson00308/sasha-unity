using AF.Combat;
using AF.Models;
using UnityEngine;
using System.Linq;
using AF.AI.UtilityAI;

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 주변의 위협 수준을 평가하는 Consideration.
    /// 가까운 적의 수와 거리를 고려하여 점수를 계산한다 (위협이 높을수록 점수 높음).
    /// </summary>
    public class IncomingThreatConsideration : IConsideration
    {
        public string Name => "Incoming Threat";
        public float LastScore { get; set; }

        private float _threatRadius;
        private int _maxThreatCount;
        private UtilityCurveType _curveType;
        private float _steepness;
        private float _offsetX;
        private float _offsetY;
        private bool _invertScore;

        public IncomingThreatConsideration(
            float threatRadius = 10f,
            int maxThreatCount = 5,
            UtilityCurveType curveType = UtilityCurveType.Linear,
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = true)
        {
            _threatRadius = Mathf.Max(0.1f, threatRadius);
            _maxThreatCount = Mathf.Max(1, maxThreatCount);
            _curveType = curveType;
            _steepness = steepness;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            var enemies = context.GetEnemies(actor);
            if (enemies == null || !enemies.Any())
            {
                this.LastScore = 0f;
                return 0f; // 적이 없으면 위협 없음
            }

            int enemiesNearby = enemies.Count(enemy =>
                enemy != null &&
                enemy.IsOperational &&
                Vector3.Distance(actor.Position, enemy.Position) <= _threatRadius
            );

            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,
                input: enemiesNearby,
                min: 0f,
                max: _maxThreatCount,
                steepness: _steepness,
                invert: _invertScore,
                midpoint: _offsetX
            );

            if (enemiesNearby == 0)
            {
                this.LastScore = 0f;
                return 0f; // 주변에 적 없음
            }

            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }

        // 예시: 적의 위협 계수 계산 함수 (나중에 구현)
        // private float CalculateEnemyThreatFactor(ArmoredFrame enemy)
        // {
        //     // 예: 적의 공격력, 남은 탄약 등을 기반으로 1 이상의 값 반환
        //     return 1.0f; 
        // }
    }
} 