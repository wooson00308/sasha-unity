using UnityEngine;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 데미지 처리와 관련된 이벤트
    /// </summary>
    public class DamageEvents
    {
        /// <summary>
        /// 데미지 발생 이벤트 - 데미지가 적용되기 전에 발생
        /// </summary>
        public class DamageCalculatedEvent : ICombatEvent
        {
            public ArmoredFrame Source { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public Weapon Weapon { get; private set; }
            public float RawDamage { get; private set; }
            public float CalculatedDamage { get; private set; }
            public DamageType DamageType { get; private set; }
            public PartType TargetPart { get; private set; }

            public DamageCalculatedEvent(ArmoredFrame source, ArmoredFrame target, Weapon weapon,
                                         float rawDamage, float calculatedDamage,
                                         DamageType damageType, PartType targetPart)
            {
                Source = source;
                Target = target;
                Weapon = weapon;
                RawDamage = rawDamage;
                CalculatedDamage = calculatedDamage;
                DamageType = damageType;
                TargetPart = targetPart;
            }
        }

        /// <summary>
        /// 데미지 회피 이벤트
        /// </summary>
        public class DamageAvoidedEvent : ICombatEvent
        {
            public enum AvoidanceType { Dodge, Deflect, Intercept, Shield }

            public ArmoredFrame Source { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public float DamageAvoided { get; private set; }
            public AvoidanceType Type { get; private set; }
            public string Description { get; private set; }
            public bool IsCounterAttack { get; private set; }

            public DamageAvoidedEvent(ArmoredFrame source, ArmoredFrame target,
                                     float damageAvoided, AvoidanceType type, string description,
                                     bool isCounterAttack)
            {
                Source = source;
                Target = target;
                DamageAvoided = damageAvoided;
                Type = type;
                Description = description;
                IsCounterAttack = isCounterAttack;
            }
        }
    }
} 