using System;

namespace AF.Models
{
    /// <summary>
    /// 상태 효과 정보를 나타내는 클래스
    /// 이제 스탯 변경 및 틱 효과 정보를 포함합니다.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public string EffectName { get; private set; }
        public string Description { get; private set; }
        public int DurationTurns { get; set; } // 남은 턴 수, -1은 영구 지속

        // 스탯 변경 정보
        public StatType StatToModify { get; private set; }
        public ModificationType ModificationType { get; private set; }
        public float ModificationValue { get; private set; }

        // 틱 효과 정보
        public TickEffectType TickEffectType { get; private set; }
        public float TickValue { get; private set; }

        /// <summary>
        /// 기본 생성자 (효과 없음)
        /// </summary>
        private StatusEffect(string effectName, string description, int durationTurns)
        {
            EffectName = effectName;
            Description = description;
            DurationTurns = durationTurns;
            StatToModify = StatType.None;
            ModificationType = ModificationType.None;
            ModificationValue = 0f;
            TickEffectType = TickEffectType.None;
            TickValue = 0f;
        }

        /// <summary>
        /// 스탯 변경 효과 생성자
        /// </summary>
        public StatusEffect(string effectName, string description, int durationTurns, StatType statToModify, ModificationType modType, float modValue)
            : this(effectName, description, durationTurns)
        {
            StatToModify = statToModify;
            ModificationType = modType;
            ModificationValue = modValue;
        }

        /// <summary>
        /// 틱 효과 생성자
        /// </summary>
        public StatusEffect(string effectName, string description, int durationTurns, TickEffectType tickType, float tickValue)
            : this(effectName, description, durationTurns)
        {
            TickEffectType = tickType;
            TickValue = tickValue;
        }
        
        /// <summary>
        /// 스탯 변경 + 틱 효과 동시 적용 생성자 (필요시)
        /// </summary>
        public StatusEffect(string effectName, string description, int durationTurns, StatType statToModify, ModificationType modType, float modValue, TickEffectType tickType, float tickValue)
            : this(effectName, description, durationTurns, statToModify, modType, modValue)
        {
            TickEffectType = tickType;
            TickValue = tickValue;
        }

        // TODO: 효과 적용/제거 시 필요한 로직 (예: 초기 스탯 저장 등) 추가 가능
    }
}
