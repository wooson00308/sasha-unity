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
        /// <param name="curveType">사용할 곡선 유형.</param>
        /// <param name="input">평가할 원시 입력값 (예: 거리, 체력).</param>
        /// <param name="min">입력값의 최소 범위.</param>
        /// <param name="max">입력값의 최대 범위.</param>
        /// <param name="steepness">곡선의 가파른 정도 (Polynomial, Logistic 등에서 사용).</param>
        /// <param name="exponent">지수 파라미터 (Polynomial 등에서 사용).</param>
        /// <param name="threshold">임계값 (Step 등에서 사용).</param>
        /// <param name="invert">결과 점수를 반전시킬지 여부 (1 - score).</param>
        /// <param name="midpoint">중간점 파라미터 (Logistic 등에서 사용).</param>
        /// <returns>0과 1 사이의 유틸리티 점수.</returns>
        public static float Evaluate(UtilityCurveType curveType, float input, float min = 0f, float max = 1f, float steepness = 1f, float exponent = 2f, float threshold = 0.5f, bool invert = false, float midpoint = 0.5f)
        {
            // 입력값 정규화 (0과 1 사이로)
            float normalizedInput = Mathf.InverseLerp(min, max, input);

            float score;
            switch (curveType)
            {
                case UtilityCurveType.Linear:
                    score = normalizedInput;
                    break;
                case UtilityCurveType.Polynomial:
                    // steepness를 지수로 사용 (예: 2이면 제곱, 0.5이면 제곱근)
                    score = EvaluatePolynomial(normalizedInput, steepness);
                    break;
                case UtilityCurveType.Logistic:
                    score = EvaluateLogistic(normalizedInput, steepness, midpoint);
                    break;
                case UtilityCurveType.Logarithmic:
                    score = EvaluateLogarithmic(normalizedInput, steepness);
                    break;
                case UtilityCurveType.InverseLogistic:
                    score = 1f - EvaluateLogistic(normalizedInput, steepness, midpoint);
                    break;
                case UtilityCurveType.Step:
                    score = normalizedInput >= threshold ? 1f : 0f;
                    break;
                case UtilityCurveType.Gaussian:
                    float mean = midpoint;
                    float stdDev = Mathf.Max(0.01f, steepness);
                    float exponentValue = -Mathf.Pow(normalizedInput - mean, 2f) / (2f * Mathf.Pow(stdDev, 2f));
                    score = Mathf.Exp(exponentValue);
                    break;
                default:
                    score = normalizedInput;
                    break;
            }

            if (invert)
            {
                score = 1f - score;
            }

            return Mathf.Clamp01(score);
        }

        private static float EvaluatePolynomial(float normalizedInput, float exponent)
        {
            if (exponent <= 0) return normalizedInput;
            return Mathf.Pow(normalizedInput, exponent);
        }

        private static float EvaluateLogistic(float normalizedInput, float steepness, float midpoint)
        {
            return 1f / (1f + Mathf.Exp(-steepness * (normalizedInput - midpoint)));
        }

        private static float EvaluateLogarithmic(float normalizedInput, float logBase)
        {
            if (logBase <= 1f) logBase = 2f;
            float valueToLog = Mathf.Clamp01(normalizedInput) * (logBase - 1f) + 1f;
            return Mathf.Log(valueToLog, logBase);
        }
    }
} 