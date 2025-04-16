using System;

namespace AF.Models
{
    /// <summary>
    /// 무기의 데미지 타입을 정의합니다.
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// 물리 데미지: 표준적인 물리적 충격에 의한 데미지입니다.
        /// </summary>
        Physical,

        /// <summary>
        /// 에너지 데미지: 레이저나 플라즈마 등의 에너지 기반 데미지입니다.
        /// </summary>
        Energy,

        /// <summary>
        /// 폭발 데미지: 폭발에 의한 광역 데미지입니다.
        /// </summary>
        Explosive,

        /// <summary>
        /// 관통 데미지: 방어력을 일부 무시하는 관통 데미지입니다.
        /// </summary>
        Piercing,

        /// <summary>
        /// 전기 데미지: 전기 충격에 의한 데미지로, 전자 장비에 추가 효과를 줄 수 있습니다.
        /// </summary>
        Electric
    }
} 