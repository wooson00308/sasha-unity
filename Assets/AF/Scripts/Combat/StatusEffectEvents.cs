using UnityEngine;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 전투 중 발생하는 상태 이상 효과와 관련된 이벤트
    /// </summary>
    public class StatusEffectEvents
    {
        /// <summary>
        /// 상태 효과의 기본 타입 열거형
        /// </summary>
        public enum StatusEffectType
        {
            // 긍정적 효과
            Buff_AttackBoost,
            Buff_DefenseBoost,
            Buff_RepairOverTime,
            Buff_AccuracyBoost,
            Buff_SpeedBoost,
            Buff_RepairField,
            Buff_ShieldGenerator,
            Buff_MaxAPBoost,         // MaxAP 증가
            Buff_APRecoveryBoost,    // AP 회복량 증가
            Buff_EvasionBoost,       // 회피율 증가
            Buff_Utility,            // 기타 유틸리티/마커용 (선택적)
            
            // 부정적 효과
            Debuff_Stunned,
            Debuff_Slowed,
            Debuff_AccuracyReduced,
            Debuff_DefenseReduced,
            Debuff_Immobilized,
            Debuff_DamageOverTime,
            Debuff_SystemCorruption,
            Debuff_WeaponJammed,
            Debuff_Burning,
            Debuff_EMPed,
            
            // 환경 효과
            Environmental_Smoke,
            Environmental_Water,
            Environmental_HighGround,
            Environmental_LowVisibility,
            Environmental_Radiation,
            Environmental_ExtremeHeat,
            Environmental_ExtremeHold
        }
        
        /// <summary>
        /// 상태 효과 적용 이벤트
        /// </summary>
        public class StatusEffectAppliedEvent : ICombatEvent
        {
            public ArmoredFrame Target { get; private set; }
            public ArmoredFrame Source { get; private set; } // null일 수 있음 (환경 효과의 경우)
            public StatusEffectType EffectType { get; private set; }
            public int Duration { get; private set; } // 턴 단위, -1은 영구 효과
            public float Magnitude { get; private set; } // 효과의 강도
            public string EffectId { get; private set; } // 효과 추적을 위한 고유 ID
            
            public StatusEffectAppliedEvent(ArmoredFrame target, ArmoredFrame source, 
                                           StatusEffectType effectType, int duration, 
                                           float magnitude, string effectId)
            {
                Target = target;
                Source = source;
                EffectType = effectType;
                Duration = duration;
                Magnitude = magnitude;
                EffectId = effectId;
            }
        }
        
        /// <summary>
        /// 상태 효과 종료 이벤트
        /// </summary>
        public class StatusEffectExpiredEvent : ICombatEvent
        {
            public ArmoredFrame Target { get; private set; }
            public StatusEffectType EffectType { get; private set; }
            public string EffectId { get; private set; }
            public bool WasDispelled { get; private set; } // 강제로 제거되었는지 여부
            
            public StatusEffectExpiredEvent(ArmoredFrame target, StatusEffectType effectType, 
                                           string effectId, bool wasDispelled = false)
            {
                Target = target;
                EffectType = effectType;
                EffectId = effectId;
                WasDispelled = wasDispelled;
            }
        }
        
        /// <summary>
        /// 상태 효과 틱(주기적 적용) 이벤트
        /// </summary>
        public class StatusEffectTickEvent : ICombatEvent
        {
            public ArmoredFrame Target { get; private set; }
            public StatusEffect Effect { get; private set; } // 효과 정보를 통째로 전달
            
            public StatusEffectTickEvent(ArmoredFrame target, StatusEffect effect)
            {
                Target = target;
                Effect = effect; 
            }
        }
        
        /// <summary>
        /// 환경 효과 시작 이벤트
        /// </summary>
        public class EnvironmentalEffectStartEvent : ICombatEvent
        {
            public StatusEffectType EffectType { get; private set; }
            public Vector3 Position { get; private set; }
            public float Radius { get; private set; }
            public int Duration { get; private set; } // 턴 단위, -1은 영구 효과
            public string Description { get; private set; }
            public string EffectId { get; private set; }
            
            public EnvironmentalEffectStartEvent(StatusEffectType effectType, Vector3 position, 
                                                float radius, int duration, string description, 
                                                string effectId)
            {
                EffectType = effectType;
                Position = position;
                Radius = radius;
                Duration = duration;
                Description = description;
                EffectId = effectId;
            }
        }
        
        /// <summary>
        /// 환경 효과 종료 이벤트
        /// </summary>
        public class EnvironmentalEffectEndEvent : ICombatEvent
        {
            public StatusEffectType EffectType { get; private set; }
            public Vector3 Position { get; private set; }
            public string EffectId { get; private set; }
            public bool WasDispelled { get; private set; } // 강제로 제거되었는지 여부
            
            public EnvironmentalEffectEndEvent(StatusEffectType effectType, Vector3 position, 
                                             string effectId, bool wasDispelled = false)
            {
                EffectType = effectType;
                Position = position;
                EffectId = effectId;
                WasDispelled = wasDispelled;
            }
        }
        
        /// <summary>
        /// 상태 효과 저항 이벤트 - 대상이 상태 효과에 저항했을 때
        /// </summary>
        public class StatusEffectResistEvent : ICombatEvent
        {
            public ArmoredFrame Target { get; private set; }
            public ArmoredFrame Source { get; private set; } // null일 수 있음 (환경 효과의 경우)
            public StatusEffectType EffectType { get; private set; }
            public float ResistChance { get; private set; } // 저항 확률
            public float ResistRoll { get; private set; } // 저항 판정 값
            
            public StatusEffectResistEvent(ArmoredFrame target, ArmoredFrame source, 
                                          StatusEffectType effectType, float resistChance, 
                                          float resistRoll)
            {
                Target = target;
                Source = source;
                EffectType = effectType;
                ResistChance = resistChance;
                ResistRoll = resistRoll;
            }
        }
    }
} 