using System;

namespace AF.Models
{
    /// <summary>
    /// 무기의 기본 타입을 정의합니다.
    /// </summary>
    public enum WeaponType
    {
        /// <summary>
        /// 근접형 무기: 가까운 거리에서 높은 데미지를 주는 무기입니다.
        /// </summary>
        Melee,

        /// <summary>
        /// 중거리형 무기: 중간 거리에서 효과적인 무기입니다.
        /// </summary>
        MidRange,

        /// <summary>
        /// 원거리형 무기: 멀리 있는 적을 타격할 수 있는 무기입니다.
        /// </summary>
        LongRange
    }
} 