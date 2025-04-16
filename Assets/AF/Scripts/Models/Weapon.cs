using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame에 장착 가능한 무기 클래스입니다.
    /// </summary>
    [Serializable]
    public class Weapon
    {
        /// <summary>
        /// 무기의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 무기의 타입 (근접, 중거리, 원거리)
        /// </summary>
        [SerializeField] private WeaponType _type;

        /// <summary>
        /// 데미지 타입 (물리, 에너지, 폭발, 관통, 전기)
        /// </summary>
        [SerializeField] private DamageType _damageType;

        /// <summary>
        /// 기본 데미지
        /// </summary>
        [SerializeField] private float _damage;

        /// <summary>
        /// 정확도 (0.0 ~ 1.0)
        /// </summary>
        [SerializeField] private float _accuracy;

        /// <summary>
        /// 사거리 (유닛 단위)
        /// </summary>
        [SerializeField] private float _range;

        /// <summary>
        /// 공격 속도 (초당 공격 횟수)
        /// </summary>
        [SerializeField] private float _attackSpeed;

        /// <summary>
        /// 연사 시 과열도
        /// </summary>
        [SerializeField] private float _overheatPerShot;

        /// <summary>
        /// 현재 과열도
        /// </summary>
        private float _currentHeat;

        /// <summary>
        /// 특수 효과 목록
        /// </summary>
        private List<string> _specialEffects;

        /// <summary>
        /// 무기가 현재 작동 가능한지 여부
        /// </summary>
        private bool _isOperational;

        // 공개 프로퍼티
        public string Name => _name;
        public WeaponType Type => _type;
        public DamageType DamageType => _damageType;
        public float Damage => _damage;
        public float Accuracy => _accuracy;
        public float Range => _range;
        public float AttackSpeed => _attackSpeed;
        public float CurrentHeat => _currentHeat;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<string> SpecialEffects => _specialEffects;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Weapon()
        {
            _name = "Default Weapon";
            _type = WeaponType.MidRange;
            _damageType = DamageType.Physical;
            _damage = 10.0f;
            _accuracy = 0.7f;
            _range = 5.0f;
            _attackSpeed = 1.0f;
            _overheatPerShot = 0.1f;
            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 상세 정보를 지정하는 생성자
        /// </summary>
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, float range, float attackSpeed, float overheatPerShot)
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _range = Mathf.Max(1.0f, range);
            _attackSpeed = Mathf.Max(0.1f, attackSpeed);
            _overheatPerShot = Mathf.Max(0.0f, overheatPerShot);
            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 무기를 발사합니다.
        /// </summary>
        /// <returns>과열로 인한 발사 실패시 false, 성공시 true</returns>
        public bool Fire()
        {
            if (!_isOperational)
                return false;

            // 과열 체크
            if (_currentHeat >= 1.0f)
                return false;

            // 과열도 증가
            _currentHeat += _overheatPerShot;
            if (_currentHeat > 1.0f)
                _currentHeat = 1.0f;

            return true;
        }

        /// <summary>
        /// 무기의 과열도를 냉각합니다.
        /// </summary>
        /// <param name="cooldownAmount">냉각량</param>
        public void Cooldown(float cooldownAmount)
        {
            _currentHeat = Mathf.Max(0.0f, _currentHeat - cooldownAmount);
        }

        /// <summary>
        /// 특수 효과를 추가합니다.
        /// </summary>
        public void AddSpecialEffect(string effect)
        {
            if (!_specialEffects.Contains(effect))
            {
                _specialEffects.Add(effect);
            }
        }

        /// <summary>
        /// 무기를 수리합니다.
        /// </summary>
        public void Repair()
        {
            _isOperational = true;
            _currentHeat = 0.0f;
        }

        /// <summary>
        /// 무기를 손상시킵니다.
        /// </summary>
        public void DamageWeapon()
        {
            _isOperational = false;
        }

        /// <summary>
        /// 실제 데미지를 계산합니다. (명중률 고려)
        /// </summary>
        /// <returns>명중 시 데미지 값, 빗나갈 경우 0</returns>
        public float CalculateDamage(float attackerAccuracyMod, float targetEvasionMod)
        {
            // 실제 명중률 계산 (무기 정확도 + 공격자 정확도 보정 - 타겟 회피 보정)
            float finalAccuracy = Mathf.Clamp01(_accuracy + attackerAccuracyMod - targetEvasionMod);
            
            // 명중 여부 판정
            if (UnityEngine.Random.value <= finalAccuracy)
            {
                return _damage;
            }
            return 0.0f; // 빗나감
        }
    }
} 