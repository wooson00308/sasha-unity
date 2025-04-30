using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame을 구성하는 모든 파츠의 기본 추상 클래스입니다.
    /// </summary>
    [Serializable]
    public abstract class Part
    {
        /// <summary>
        /// 파츠의 이름
        /// </summary>
        [SerializeField] protected string _name;

        /// <summary>
        /// 파츠의 타입
        /// </summary>
        [SerializeField] protected PartType _type;

        /// <summary>
        /// 파츠의 스탯
        /// </summary>
        [SerializeField] protected Stats _stats;

        /// <summary>
        /// 파츠의 무게
        /// </summary>
        [SerializeField] protected float _weight = 10f; // 기본 무게 10으로 설정

        /// <summary>
        /// 현재 내구도
        /// </summary>
        [SerializeField] protected float _currentDurability;

        /// <summary>
        /// 최대 내구도
        /// </summary>
        [SerializeField] protected float _maxDurability;

        /// <summary>
        /// 파츠가 현재 작동 가능한지 여부
        /// </summary>
        protected bool _isOperational;

        /// <summary>
        /// 특수 능력 목록
        /// </summary>
        protected List<string> _abilities;

        // 공개 프로퍼티
        public string Name => _name;
        public PartType Type => _type;
        public Stats PartStats => _stats;
        public float Weight => _weight;
        public float CurrentDurability => _currentDurability;
        public float MaxDurability => _maxDurability;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<string> Abilities => _abilities;

        /// <summary>
        /// 생성자
        /// </summary>
        protected Part(string name, PartType type, Stats stats, float durability)
        {
            _name = name;
            _type = type;
            _stats = stats;
            _maxDurability = durability;
            _currentDurability = durability;
            _isOperational = true;
            _abilities = new List<string>();
        }

        /// <summary>
        /// 파츠의 현재 내구도를 설정하고 작동 상태를 갱신합니다.
        /// </summary>
        /// <param name="value">설정할 내구도 값</param>
        /// <returns>이 호출로 인해 파츠가 작동 가능(true) 또는 불가능(false) 상태로 변경되었는지 여부. 상태 변경 없으면 null 반환.</returns>
        public virtual bool? SetDurability(float value)
        {
            bool previousOperationalState = _isOperational;
            _currentDurability = Mathf.Clamp(value, 0, _maxDurability); // 0 ~ MaxDurability 범위 유지

            bool newOperationalState = _currentDurability > 0;

            if (previousOperationalState != newOperationalState)
            {
                _isOperational = newOperationalState;
                return _isOperational; // 상태 변경 발생 (true: 작동 가능해짐, false: 작동 불가능해짐)
            }

            return null; // 상태 변경 없음
        }

        /// <summary>
        /// 데미지를 적용합니다.
        /// </summary>
        /// <param name="damageAmount">적용할 데미지 양</param>
        /// <returns>파츠가 파괴되었는지 여부 (이 공격으로 인해 작동 불능 상태가 되었는지)</returns>
        public virtual bool ApplyDamage(float damageAmount)
        {
            bool? statusChanged = SetDurability(_currentDurability - damageAmount);

            // 상태가 변경되었고, 그 상태가 '작동 불가능(false)'이면 파괴된 것으로 간주
            return statusChanged.HasValue && !statusChanged.Value;
        }

        /// <summary>
        /// 특수 능력을 추가합니다.
        /// </summary>
        public void AddAbility(string ability)
        {
            if (!_abilities.Contains(ability))
            {
                _abilities.Add(ability);
            }
        }

        /// <summary>
        /// 파츠 파괴 시 적용되는 효과를 구현합니다.
        /// </summary>
        public abstract void OnDestroyed(ArmoredFrame parentAF);

        /// <summary>
        /// 무게를 받는 생성자 오버로드
        /// </summary>
        protected Part(string name, PartType type, Stats stats, float durability, float weight)
        {
            _name = name;
            _type = type;
            _stats = stats;
            _maxDurability = durability;
            _currentDurability = durability;
            _isOperational = true;
            _abilities = new List<string>();
            _weight = weight; // 전달받은 무게 설정
        }
    }
} 