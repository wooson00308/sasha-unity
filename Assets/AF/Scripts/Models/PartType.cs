using System;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame을 구성하는 파츠의 타입을 정의합니다.
    /// </summary>
    public enum PartType
    {
        /// <summary>
        /// 프레임: AF의 기본 골격으로, 전체적인 성능과 파츠 호환성을 결정합니다.
        /// </summary>
        Frame,

        /// <summary>
        /// 바디: 프레임 위에 장착되는 중심부로, 전체 내구도와 파츠 장착 제한에 영향을 줍니다.
        /// </summary>
        Body,

        /// <summary>
        /// 헤드: 센서 범위, 타겟팅 기능, 정확도, 가시성을 담당합니다.
        /// </summary>
        Head,

        /// <summary>
        /// 팔: 무기 장착 및 근접 전투 능력을 결정합니다.
        /// </summary>
        Arm,

        /// <summary>
        /// 다리: 이동 능력과 안정성을 결정합니다.
        /// </summary>
        Legs,

        /// <summary>
        /// 백팩: 특수 능력과 에너지 저장소를 제공합니다.
        /// </summary>
        Backpack
    }
} 