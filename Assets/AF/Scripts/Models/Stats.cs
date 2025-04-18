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

        /// <summary>
        /// 최대 행동력 (AP)
        /// </summary>
        [SerializeField] private float _maxAP = 10.0f; // 기본값 10으로 설정

        /// <summary>
        /// 턴당 행동력 회복량
        /// </summary>
        [SerializeField] private float _apRecovery = 3.0f; // 기본값 3으로 설정

        // 공개 프로퍼티
        public float AttackPower => _attackPower;
        public float Defense => _defense;
        public float Speed => _speed;
        public float Accuracy => _accuracy;
        public float Evasion => _evasion;
        public float Durability => _durability;
        public float EnergyEfficiency => _energyEfficiency;
        public float MaxAP => _maxAP;
        public float APRecovery => _apRecovery;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Stats() 
        {
            // 필드 초기화 값 대신 명시적으로 0 또는 기본값으로 설정 (기획 의도 반영)
            _attackPower = 0f;
            _defense = 0f;
            _speed = 0f;
            _accuracy = 0f;
            _evasion = 0f;
            _durability = 0f;
            _energyEfficiency = 1f; // 에너지 효율은 기본 1이 적절할 수 있음
            _maxAP = 0f;
            _apRecovery = 0f;
        }

        /// <summary>
        /// 모든 스탯을 지정하는 생성자 (AP 관련 스탯 포함)
        /// </summary>
        public Stats(float attackPower = 0f, float defense = 0f, float speed = 0f, 
                     float accuracy = 0f, float evasion = 0f, float durability = 0f, 
                     float energyEfficiency = 1f, // 에너지 효율은 기본 1 유지?
                     float maxAP = 0f, float apRecovery = 0f)
        {
            _attackPower = attackPower;
            _defense = defense;
            _speed = speed;
            _accuracy = accuracy;
            _evasion = evasion;
            _durability = durability;
            _energyEfficiency = energyEfficiency;
            _maxAP = maxAP;
            _apRecovery = apRecovery;
        }

        /// <summary>
        /// 두 Stats 객체를 더합니다. (AP 관련 스탯 포함)
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
                a._energyEfficiency + b._energyEfficiency,
                a._maxAP + b._maxAP,
                a._apRecovery + b._apRecovery
            );
        }

        /// <summary>
        /// Stats에 계수를 곱합니다. (AP 관련 스탯 포함 - 곱하는게 맞는지 확인 필요, 일단 제외)
        /// TODO: AP 스탯에 곱셈이 필요한지 결정해야 함 (예: 버프/디버프). 지금은 곱하지 않음.
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
                stats._energyEfficiency * multiplier,
                stats._maxAP, 
                stats._apRecovery
            );
        }

        /// <summary>
        /// 특정 스탯 값을 주어진 방식(덧셈 또는 곱셈)으로 수정합니다. (AP 관련 스탯 처리 추가)
        /// </summary>
        public void ApplyModifier(StatType statToModify, ModificationType modType, float value)
        {
            // 0 또는 None 값은 무시
            if (value == 0f || modType == ModificationType.None || statToModify == StatType.None) return;

            switch (statToModify)
            {
                case StatType.AttackPower:
                    if (modType == ModificationType.Additive) _attackPower += value;
                    else if (modType == ModificationType.Multiplicative) _attackPower *= value;
                    break;
                case StatType.Defense:
                    if (modType == ModificationType.Additive) _defense += value;
                    else if (modType == ModificationType.Multiplicative) _defense *= value;
                    break;
                case StatType.Speed:
                    if (modType == ModificationType.Additive) _speed += value;
                    else if (modType == ModificationType.Multiplicative) _speed *= value;
                    break;
                case StatType.Accuracy:
                    if (modType == ModificationType.Additive) _accuracy += value;
                    else if (modType == ModificationType.Multiplicative) _accuracy *= value;
                    break;
                case StatType.Evasion:
                    if (modType == ModificationType.Additive) _evasion += value;
                    else if (modType == ModificationType.Multiplicative) _evasion *= value;
                    break;
                case StatType.Durability:
                    if (modType == ModificationType.Additive) _durability += value;
                    else if (modType == ModificationType.Multiplicative) _durability *= value;
                    break;
                case StatType.EnergyEfficiency:
                    if (modType == ModificationType.Additive) _energyEfficiency += value;
                    else if (modType == ModificationType.Multiplicative) _energyEfficiency *= value;
                    break;
                case StatType.MaxAP:
                    if (modType == ModificationType.Additive) _maxAP += value;
                    else if (modType == ModificationType.Multiplicative) _maxAP *= value;
                    break;
                case StatType.APRecovery:
                    if (modType == ModificationType.Additive) _apRecovery += value;
                    else if (modType == ModificationType.Multiplicative) _apRecovery *= value;
                    break;
                default:
                    Debug.LogWarning($"ApplyModifier: 처리되지 않은 스탯 타입 - {statToModify}");
                    break;
            }
            
            // 음수 스탯 방지 (AP 관련 스탯 포함)
            _attackPower = Mathf.Max(0, _attackPower);
            _defense = Mathf.Max(0, _defense); 
            _speed = Mathf.Max(0, _speed);
            _accuracy = Mathf.Max(0, _accuracy);
            _evasion = Mathf.Max(0, _evasion);
            _durability = Mathf.Max(0, _durability);
            _energyEfficiency = Mathf.Max(0, _energyEfficiency);
            _maxAP = Mathf.Max(1, _maxAP); // 최대 AP는 최소 1 이상
            _apRecovery = Mathf.Max(0, _apRecovery); // AP 회복량은 0 이상
        }
        
        /// <summary>
        /// Stats 객체를 보기 좋은 문자열로 변환합니다.
        /// </summary>
        /// <returns>모든 스탯 값을 포함하는 문자열</returns>
        public override string ToString()
        {
            return $"[Stats: Atk:{_attackPower:F1} Def:{_defense:F1} Spd:{_speed:F1} Acc:{_accuracy:F1} Eva:{_evasion:F1} Dur:{_durability:F1} EE:{_energyEfficiency:F1} MaxAP:{_maxAP:F1} APR:{_apRecovery:F1}]";
        }
    }
} 