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
        /// 데미지를 적용합니다.
        /// </summary>
        /// <param name="damageAmount">적용할 데미지 양</param>
        /// <returns>파츠가 파괴되었는지 여부</returns>
        public virtual bool ApplyDamage(float damageAmount)
        {
            _currentDurability -= damageAmount;
            
            // 내구도가 0 이하로 떨어지면 파츠는 작동 불능 상태가 됩니다.
            if (_currentDurability <= 0)
            {
                _currentDurability = 0;
                _isOperational = false;
                return true; // 파츠 파괴됨
            }
            return false; // 파츠 파괴되지 않음
        }

        /// <summary>
        /// 내구도를 회복합니다.
        /// </summary>
        /// <param name="repairAmount">회복할 내구도 양</param>
        public virtual void Repair(float repairAmount)
        {
            _currentDurability = Mathf.Min(_currentDurability + repairAmount, _maxDurability);
            if (_currentDurability > 0)
            {
                _isOperational = true;
            }
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
    }
} 