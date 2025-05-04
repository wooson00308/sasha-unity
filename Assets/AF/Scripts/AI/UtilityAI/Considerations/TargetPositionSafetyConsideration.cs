using AF.Combat;
using AF.Models;
using UnityEngine;
using System.Linq; // For LINQ
using AF.Services; // <<< 추가

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// 목표 이동 지점 주변의 안전성을 평가하는 Consideration.
    /// 주변의 적 유닛 수를 기반으로 점수를 계산한다 (적이 적을수록 점수 높음).
    /// </summary>
    public class TargetPositionSafetyConsideration : IConsideration
    {
        public string Name => "Target Position Safety";
        public float LastScore { get; set; }

        private Vector3 _targetPosition;
        private float _safetyRadius = 5f; // 안전 반경 (임시값, 조정 필요)
        private AnimationCurve _responseCurve; // 점수 계산을 위한 커스텀 커브 (또는 UtilityCurveEvaluator 사용)

        public TargetPositionSafetyConsideration(Vector3 targetPosition, float safetyRadius = 5f)
        {
            _targetPosition = targetPosition;
            _safetyRadius = Mathf.Max(0.1f, safetyRadius); // 최소 반경 보장

            // 예시: 적이 0명일 때 1점, 1명일 때 0.3점, 2명 이상일 때 0.1점 이하로 점수 급감
            _responseCurve = new AnimationCurve(
                new Keyframe(0, 1f),  // 적 0명 = 1점
                new Keyframe(1, 0.3f), // 적 1명 = 0.3점 (안전도 크게 감소)
                new Keyframe(2, 0.1f), // 적 2명 = 0.1점
                new Keyframe(3, 0.01f) // 적 3명 = 0.01점 (거의 0점)
            );
            _responseCurve.preWrapMode = WrapMode.ClampForever;
            _responseCurve.postWrapMode = WrapMode.ClampForever;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // <<< 로거 가져오기 추가 >>>
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            var enemies = context.GetEnemies(actor);
            if (enemies == null)
            {
                this.LastScore = 1f;
                return 1f; // 적이 없으면 안전함
            }

            // 목표 지점 반경 내 적 유닛 수 계산
            int enemyCountNearby = enemies.Count(enemy => 
                enemy != null && enemy.IsOperational && 
                Vector3.Distance(enemy.Position, _targetPosition) <= _safetyRadius
            );

            // 적 수에 따라 커브에서 점수 평가
            float score = _responseCurve.Evaluate(enemyCountNearby);
            
            logger?.TextLogger?.Log($"[TargetPositionSafetyConsideration] Pos={_targetPosition}, Radius={_safetyRadius:F1}, NearbyEnemies={enemyCountNearby}, Score={score:F3}", LogLevel.Debug); // <<< TextLogger로 변경 및 활성화
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f);
            return this.LastScore;
        }
    }
} 