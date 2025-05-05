using AF.Combat;
using AF.Models;
using UnityEngine;
using System.Linq; // For LINQ
using AF.Services; // <<< 추가

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 목표 이동 지점과 가장 가까운 적 유닛과의 거리를 평가하는 Consideration.
    /// 가까울수록 높은 점수를 반환한다 (UtilityCurveEvaluator 사용).
    /// </summary>
    public class DistanceToEnemyConsideration : IConsideration
    {
        public string Name => "Distance to Closest Enemy";
        public float LastScore { get; set; }

        private Vector3 _targetPosition;
        private float _maxDistance; // 점수 계산 시 최대 거리 (예: 맵 크기 또는 센서 범위)

        // 생성자에서 목표 위치와 최대 거리를 받음
        public DistanceToEnemyConsideration(Vector3 targetPosition, float maxDistance = 50f) // maxDistance 임시값
        {
            _targetPosition = targetPosition;
            _maxDistance = Mathf.Max(1f, maxDistance); // 최소 1 이상 보장
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // <<< 로거 가져오기 추가 >>>
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            var enemies = context.GetEnemies(actor);
            if (enemies == null || !enemies.Any())
            {
                this.LastScore = 0f;
                return 0f; // 적이 없으면 거리 점수는 의미 없음 (또는 1점? 상황 따라 결정)
            }

            // 가장 가까운 적 찾기
            float minDistance = float.MaxValue;
            bool enemyFound = false;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsOperational)
                {
                    float distance = Vector3.Distance(enemy.Position, _targetPosition);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        enemyFound = true;
                    }
                }
            }

            if (!enemyFound)
            {
                this.LastScore = 0f;
                return 0f; // 작동 중인 적을 찾지 못함
            }

            // 원시 거리값(minDistance)과 범위(0, _maxDistance)를 전달하고, invert 설정 변경
            float score = UtilityCurveEvaluator.Evaluate(
                input: minDistance,
                min: 0f,
                max: _maxDistance,
                curveType: UtilityCurveType.Polynomial, 
                steepness: 2f, // 가까울수록 제곱으로 점수 증가
                invert: true  // 복구: 결과를 반전시켜 가까울수록 높은 점수
            );
            
            logger?.TextLogger?.Log($"[DistanceToEnemyConsideration] Pos={_targetPosition}, ClosestEnemyDist={minDistance:F1}, MaxDist={_maxDistance:F1}, Score={score:F3}", LogLevel.Debug); // <<< TextLogger로 변경 및 활성화

            // <<< 추가: 근접 거리 페널티 적용 >>>
            const float CLOSE_DISTANCE_THRESHOLD = 5.0f;
            const float CLOSE_DISTANCE_PENALTY_FACTOR = 0.7f; // 점수를 70%로 감소

            if (minDistance <= CLOSE_DISTANCE_THRESHOLD)
            {
                float originalScore = score;
                score *= CLOSE_DISTANCE_PENALTY_FACTOR;
                logger?.TextLogger?.Log($"[DistanceToEnemyConsideration] Close distance penalty applied. Dist ({minDistance:F1}) <= Threshold ({CLOSE_DISTANCE_THRESHOLD:F1}). Score reduced: {originalScore:F3} -> {score:F3}", LogLevel.Debug);
            }
            // <<< 페널티 적용 끝 >>>

            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 