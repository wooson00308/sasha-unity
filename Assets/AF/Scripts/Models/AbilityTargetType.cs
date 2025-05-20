using System;

namespace AF.Models
{
    /// <summary>
    /// 어빌리티가 영향을 미치는 대상의 유형을 정의합니다.
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary>
        /// 대상 없음 (예: 광역 버프/디버프, 맵 전체 효과)
        /// </summary>
        None,

        /// <summary>
        /// 시전자 자신
        /// </summary>
        Self,

        /// <summary>
        /// 단일 적 유닛 (ArmoredFrame)
        /// </summary>
        EnemyUnit,

        /// <summary>
        /// 단일 아군 유닛 (ArmoredFrame)
        /// </summary>
        AllyUnit,

        /// <summary>
        /// 단일 적 파츠
        /// </summary>
        EnemyPart,

        /// <summary>
        /// 단일 아군 파츠
        /// </summary>
        AllyPart,

        /// <summary>
        /// 특정 위치 (지점 범위 효과의 중심)
        /// </summary>
        Position,

        /// <summary>
        /// 범위 내 모든 적 유닛
        /// </summary>
        AoE_EnemyUnits,

        /// <summary>
        /// 범위 내 모든 아군 유닛
        /// </summary>
        AoE_AllyUnits,

        /// <summary>
        /// 범위 내 모든 유닛 (적아 구분 없음)
        /// </summary>
        AoE_AllUnits
    }
} 