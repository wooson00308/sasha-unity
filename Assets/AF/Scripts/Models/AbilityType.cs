using System;

namespace AF.Models
{
    /// <summary>
    /// 어빌리티의 기본적인 발동 유형을 정의합니다.
    /// </summary>
    public enum AbilityType
    {
        /// <summary>
        /// 패시브: 특정 조건 없이 항상 적용되거나, 특정 상황에서 자동으로 발동/해제되는 효과입니다.
        /// </summary>
        Passive,

        /// <summary>
        /// 액티브: 사용자가 직접 발동시켜야 하는 효과입니다. AP 소모나 쿨타임이 있을 수 있습니다.
        /// </summary>
        Active,

        /// <summary>
        /// 트리거: 특정 게임 내 이벤트 발생 시 자동으로 발동되는 효과입니다.
        /// </summary>
        Triggered
    }
} 