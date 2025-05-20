using System;

namespace AF.Models
{
    /// <summary>
    /// 어빌리티가 발생시키는 주요 효과의 종류를 정의합니다.
    /// </summary>
    public enum AbilityEffectType
    {
        /// <summary>
        /// 효과 없음 또는 정의되지 않음
        /// </summary>
        None,

        /// <summary>
        /// 대상의 스탯을 직접적으로 수정합니다. (일시적 또는 영구적)
        /// </summary>
        StatModifier,

        /// <summary>
        /// 대상에게 상태 효과(버프/디버프)를 적용합니다.
        /// </summary>
        ApplyStatusEffect,

        /// <summary>
        /// 대상에게 직접적인 데미지를 입힙니다.
        /// </summary>
        DirectDamage,

        /// <summary>
        /// 대상의 내구도를 직접적으로 회복시킵니다.
        /// </summary>
        DirectHeal,

        /// <summary>
        /// 특정 오브젝트(예: 터렛, 방어막)를 소환하거나 생성합니다.
        /// </summary>
        SpawnObject,

        /// <summary>
        /// 특별한 행동을 수행합니다. (예: 위치 이동, 강제 상태 변경 등)
        /// </summary>
        SpecialAction,

        /// <summary>
        /// 여러 효과를 복합적으로 발생시킵니다.
        /// EffectParameters에 여러 효과 정의가 포함될 수 있습니다.
        /// </summary>
        Composite,

        /// <summary>
        /// 어빌리티 자체의 사용을 막거나 허용하는 등의 컨트롤을 합니다.
        /// </summary>
        ControlAbilityUsage
    }
} 