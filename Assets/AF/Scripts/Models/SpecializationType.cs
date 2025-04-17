namespace AF.Models
{
    /// <summary>
    /// 파일럿 전문화 타입
    /// </summary>
    public enum SpecializationType
    {
        /// <summary>
        /// 공격 전문화: 데미지와 명중률 향상
        /// </summary>
        Combat,

        /// <summary>
        /// 방어 전문화: 내구도와 회피율 향상
        /// </summary>
        Defense,

        /// <summary>
        /// 지원 전문화: 특수 능력 효율 향상
        /// </summary>
        Support,

        /// <summary>
        /// 기계 전문화: 프레임-파츠 호환성 향상
        /// </summary>
        Engineering
    }
} 