using System;
using System.Collections.Generic;
using System.Linq;
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
        protected List<AbilityData> _abilities;

        // 공개 프로퍼티
        public string Name => _name;
        public PartType Type => _type;
        public Stats PartStats => _stats;
        public float Weight => _weight;
        public float CurrentDurability => _currentDurability;
        public float MaxDurability => _maxDurability;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<AbilityData> Abilities => _abilities;

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
            _abilities = new List<AbilityData>();
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
        /// 특수 능력을 추가합니다. 동일한 이름의 능력이 이미 있다면 추가하지 않습니다.
        /// </summary>
        public void AddAbility(AbilityData ability)
        {
            if (ability != null && !_abilities.Any(a => a.Name == ability.Name))
            {
                _abilities.Add(ability);
                Debug.Log($"Part ({Name}): 어빌리티 '{ability.Name}' 추가됨.");
            }
            else if (ability != null)
            {
                Debug.LogWarning($"Part ({Name}): 어빌리티 '{ability.Name}'은(는) 이미 보유하고 있습니다.");
            }
        }

        /// <summary>
        /// 턴 시작 시 모든 어빌리티의 쿨다운을 감소시킵니다.
        /// </summary>
        public void TickAbilityCooldowns()
        {
            if (_abilities == null) return;
            foreach(var ability in _abilities)
            {
                ability.TickCooldown();
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
            _abilities = new List<AbilityData>();
            _weight = weight;
        }
    }
} 