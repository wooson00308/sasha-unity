using UnityEngine;

namespace AF.AI.UtilityAI
{
    /// <summary>
    /// 유틸리티 곡선을 평가하는 정적 헬퍼 클래스입니다.
    /// 입력값을 0과 1 사이의 정규화된 값으로 변환하고, 선택된 곡선 유형에 따라 최종 유틸리티 점수를 계산합니다.
    /// </summary>
    public static class UtilityCurveEvaluator
    {
        /// <summary>
        /// 주어진 입력값과 곡선 파라미터를 사용하여 유틸리티 점수를 계산합니다.
        /// </summary>
        /// <param name="inputValue">평가할 원시 입력값 (예: 거리, 체력).</param>
        /// <param name="minInput">입력값의 최소 범위.</param>
        /// <param name="maxInput">입력값의 최대 범위.</param>
        /// <param name="curveType">사용할 곡선 유형.</param>
        /// <param name="steepness">곡선의 가파른 정도 (Polynomial, Logistic 등에서 사용).</param>
        /// <param name="offsetX">곡선의 수평 이동 (Logistic 등에서 사용).</param>
        /// <param name="offsetY">곡선의 수직 이동.</param>
        /// <param name="invert">결과 점수를 반전시킬지 여부 (1 - score).</param>
        /// <returns>0과 1 사이의 유틸리티 점수.</returns>
        public static float Evaluate(
            float inputValue,
            float minInput,
            float maxInput,
            UtilityCurveType curveType,
            float steepness = 1f, // Polynomial의 지수 또는 Logistic의 기울기
            float offsetX = 0.5f, // Logistic의 중간점 등
            float offsetY = 0f, // 최종 점수 수직 이동
            bool invert = false)
        {
            // 1. 입력값 정규화 (0 ~ 1 범위로)
            float normalizedInput = Mathf.Clamp01((inputValue - minInput) / (maxInput - minInput));

            float score = 0f;

            // 2. 선택된 곡선 유형에 따라 점수 계산
            switch (curveType)
            {
                case UtilityCurveType.Linear:
                    score = EvaluateLinear(normalizedInput);
                    break;
                case UtilityCurveType.Polynomial:
                    // steepness를 지수로 사용 (예: 2이면 제곱, 0.5이면 제곱근)
                    score = EvaluatePolynomial(normalizedInput, steepness);
                    break;
                case UtilityCurveType.Logistic:
                    // TODO: Implement Logistic curve calculation
                    // score = EvaluateLogistic(normalizedInput, steepness, offsetX);
                    score = normalizedInput; // Placeholder
                    break;
                case UtilityCurveType.Logarithmic:
                    // TODO: Implement Logarithmic curve calculation
                    // score = EvaluateLogarithmic(normalizedInput, steepness);
                    score = normalizedInput; // Placeholder
                    break;
                case UtilityCurveType.Exponential:
                    // TODO: Implement Exponential curve calculation
                    // score = EvaluateExponential(normalizedInput, steepness);
                     score = normalizedInput; // Placeholder
                    break;
                default:
                    score = normalizedInput; // 기본값은 선형처럼 처리
                    break;
            }

            // 3. 수직 이동 적용
            score += offsetY;

            // 4. 결과 반전 (필요시)
            if (invert)
            {
                score = 1f - score;
            }

            // 5. 최종 점수를 0과 1 사이로 제한
            return Mathf.Clamp01(score);
        }

        private static float EvaluateLinear(float normalizedInput)
        {
            return normalizedInput;
        }

        private static float EvaluatePolynomial(float normalizedInput, float exponent)
        {
            // 지수가 0보다 작거나 같으면 예외 처리 또는 기본값 반환
            if (exponent <= 0) return normalizedInput; 
            return Mathf.Pow(normalizedInput, exponent);
        }

        // --- TODO: 다른 곡선 평가 함수 구현 --- 
        // private static float EvaluateLogistic(float normalizedInput, float steepness, float midpoint) { ... }
        // private static float EvaluateLogarithmic(float normalizedInput, float baseValue) { ... }
        // private static float EvaluateExponential(float normalizedInput, float exponent) { ... }
    }
} 