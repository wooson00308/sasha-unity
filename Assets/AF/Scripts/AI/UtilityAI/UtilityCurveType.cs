namespace AF.AI.UtilityAI
{
    /// <summary>
    /// 유틸리티 점수 계산에 사용될 반응 곡선의 유형을 정의합니다.
    /// </summary>
    public enum UtilityCurveType
    {
        /// <summary>
        /// 선형: 입력값이 증가함에 따라 점수가 일정 비율로 증가하거나 감소합니다.
        /// </summary>
        Linear,
        /// <summary>
        /// 다항식 (예: 제곱, 세제곱): 입력값의 거듭제곱에 따라 점수가 변합니다. 가속/감속 효과를 줄 수 있습니다.
        /// </summary>
        Polynomial,
        /// <summary>
        /// 로지스틱 (S-커브): 특정 구간에서 급격하게 변하고 양 끝에서는 완만해지는 S자 형태의 곡선입니다.
        /// 임계값 주변에서 민감하게 반응하는 경우에 유용합니다.
        /// </summary>
        Logistic,
        /// <summary>
        /// 로그: 입력값이 클수록 변화량이 점차 줄어드는 곡선입니다.
        /// 초기 입력값 변화에 민감하고 이후에는 둔감해지는 경우에 사용됩니다.
        /// </summary>
        Logarithmic,
        /// <summary>
        /// 역로그 (Exponential): 입력값이 클수록 변화량이 급격히 증가하는 곡선입니다.
        /// </summary>
        Exponential,
        /// <summary>
        /// 역 로지스틱: 로지스틱 곡선을 수평으로 뒤집은 형태. 특정 값 이상에서 점수가 급격히 떨어지는 경우 등에 사용.
        /// </summary>
        InverseLogistic,
        /// <summary>
        /// 계단 함수: 특정 임계값을 기준으로 점수가 0 또는 1로 결정됩니다.
        /// </summary>
        Step,
        /// <summary>
        /// 가우시안 (종 모양): 특정 중심값(평균)에서 가장 높은 점수를 갖고 양쪽으로 갈수록 점수가 감소하는 형태.
        /// 최적의 값 범위가 있을 때 유용합니다 (예: 최적 사거리).
        /// </summary>
        Gaussian
    }
} 