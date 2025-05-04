using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services; // <<< 추가
// using AF.AI.UtilityAI; // <<< 네임스페이스 제거 시도 유지

namespace AF.AI.UtilityAI.Considerations
{
    /// <summary>
    /// Evaluates the distance between the actor and a target using a configurable response curve.
    /// </summary>
    public class TargetDistanceConsideration : IConsideration
    {
        public string Name => "Target Distance";
        public float LastScore { get; set; }

        private ArmoredFrame _targetUnit;
        private Vector3? _targetPosition;
        // private float _optimalRange; // 이제 사용 안 함
        // private float _maxRange;     // 이제 사용 안 함

        // +++ 곡선 파라미터 추가 +++
        private UtilityCurveType _curveType;
        private float _minDistance; // 곡선 계산 시작 거리 (예: 0)
        private float _maxDistance; // 곡선 계산 종료 거리 (예: 무기 최대 사거리)
        private float _curveSteepness = 1f; // 곡선 가파름/지수
        private float _curveOffsetX = 0.5f; // 곡선 수평 이동
        private float _curveOffsetY = 0f;   // 곡선 수직 이동
        private bool _invertScore = false; // 점수 반전 여부 (가까울수록 높은 점수를 원할 경우 true)
        // +++ 파라미터 추가 끝 +++

        // 생성자 수정: 곡선 타입과 파라미터를 받도록 변경
        public TargetDistanceConsideration(
            ArmoredFrame targetUnit,
            UtilityCurveType curveType,
            float minDistance,
            float maxDistance,
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = false)
        {
            _targetUnit = targetUnit;
            _curveType = curveType;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _curveSteepness = steepness;
            _curveOffsetX = offsetX;
            _curveOffsetY = offsetY;
            _invertScore = invert;
        }

        // Vector3를 받는 생성자도 유사하게 수정 (필요시 사용)
        public TargetDistanceConsideration(
            Vector3 targetPosition,
            UtilityCurveType curveType,
            float minDistance,
            float maxDistance,
            float steepness = 1f,
            float offsetX = 0.5f,
            float offsetY = 0f,
            bool invert = false)
        {
            _targetPosition = targetPosition;
            _curveType = curveType;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _curveSteepness = steepness;
            _curveOffsetX = offsetX;
            _curveOffsetY = offsetY;
            _invertScore = invert;
        }

        public float CalculateScore(ArmoredFrame actor, CombatContext context)
        {
            // <<< 로거 가져오기 추가 >>>
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>();

            Vector3? currentTargetPos = _targetPosition ?? _targetUnit?.Position;

            if (actor == null || currentTargetPos == null)
            {
                logger?.TextLogger?.Log($"{Name}: Actor or Target Position is null.", LogLevel.Warning); // <<< TextLogger 사용 및 레벨 Warning으로 변경
                this.LastScore = 0f;
                return 0f;
            }

            float distance = Vector3.Distance(actor.Position, currentTargetPos.Value);

            // UtilityCurveEvaluator를 사용하여 점수 계산
            float score = UtilityCurveEvaluator.Evaluate(
                curveType: _curveType,
                input: distance,
                min: _minDistance,
                max: _maxDistance,
                steepness: _curveSteepness,
                invert: _invertScore,
                midpoint: _curveOffsetX
            );

            logger?.TextLogger?.Log($"{Name}: Dist={distance:F1}, Score={score:F3}", LogLevel.Debug); // <<< TextLogger로 변경 및 활성화
            this.LastScore = Mathf.Clamp(score, 0f, 1.0f); 
            return this.LastScore;
        }
    }
} 