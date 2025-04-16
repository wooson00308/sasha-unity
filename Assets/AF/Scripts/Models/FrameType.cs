using System;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame의 기본 프레임 타입을 정의합니다.
    /// </summary>
    public enum FrameType
    {
        /// <summary>
        /// 경량 프레임: 속도와 회피에 특화되었지만, 무거운 장비 장착에 제한이 있습니다.
        /// </summary>
        Light,

        /// <summary>
        /// 범용 프레임: 균형 잡힌 성능으로 다양한 파츠와의 호환성이 좋습니다.
        /// </summary>
        Standard,

        /// <summary>
        /// 중장갑 프레임: 내구성이 높고 강력한 장비 장착이 가능하지만, 이동성이 낮습니다.
        /// </summary>
        Heavy
    }
} 