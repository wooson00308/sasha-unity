using System;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame, Part, Pilot 등의 기본 스탯을 정의하는 클래스입니다.
    /// </summary>
    [Serializable]
    public class Stats
    {
        /// <summary>
        /// 공격력: 무기의 기본 데미지에 곱해지는 계수입니다.
        /// </summary>
        [SerializeField] private float _attackPower = 1.0f;

        /// <summary>
        /// 방어력: 받는 데미지를 감소시키는 수치입니다.
        /// </summary>
        [SerializeField] private float _defense = 1.0f;

        /// <summary>
        /// 속도: 이동 속도와 행동 주기에 영향을 줍니다.
        /// </summary>
        [SerializeField] private float _speed = 1.0f;

        /// <summary>
        /// 정확도: 공격의 명중률에 영향을 줍니다.
        /// </summary>
        [SerializeField] private float _accuracy = 1.0f;

        /// <summary>
        /// 회피율: 적의 공격을 회피할 확률에 영향을 줍니다.
        /// </summary>
        [SerializeField] private float _evasion = 1.0f;

        /// <summary>
        /// 내구도: 최대 체력에 영향을 줍니다.
        /// </summary>
        [SerializeField] private float _durability = 100.0f;

        /// <summary>
        /// 에너지 효율: 에너지 사용량과 충전 속도에 영향을 줍니다.
        /// </summary>
        [SerializeField] private float _energyEfficiency = 1.0f;

        // 공개 프로퍼티
        public float AttackPower => _attackPower;
        public float Defense => _defense;
        public float Speed => _speed;
        public float Accuracy => _accuracy;
        public float Evasion => _evasion;
        public float Durability => _durability;
        public float EnergyEfficiency => _energyEfficiency;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Stats() { }

        /// <summary>
        /// 모든 스탯을 지정하는 생성자
        /// </summary>
        public Stats(float attackPower, float defense, float speed, float accuracy, float evasion, float durability, float energyEfficiency)
        {
            _attackPower = attackPower;
            _defense = defense;
            _speed = speed;
            _accuracy = accuracy;
            _evasion = evasion;
            _durability = durability;
            _energyEfficiency = energyEfficiency;
        }

        /// <summary>
        /// 두 Stats 객체를 더합니다.
        /// </summary>
        public static Stats operator +(Stats a, Stats b)
        {
            return new Stats(
                a._attackPower + b._attackPower,
                a._defense + b._defense,
                a._speed + b._speed,
                a._accuracy + b._accuracy,
                a._evasion + b._evasion,
                a._durability + b._durability,
                a._energyEfficiency + b._energyEfficiency
            );
        }

        /// <summary>
        /// Stats에 계수를 곱합니다.
        /// </summary>
        public static Stats operator *(Stats stats, float multiplier)
        {
            return new Stats(
                stats._attackPower * multiplier,
                stats._defense * multiplier,
                stats._speed * multiplier,
                stats._accuracy * multiplier,
                stats._evasion * multiplier,
                stats._durability * multiplier,
                stats._energyEfficiency * multiplier
            );
        }
    }
} 