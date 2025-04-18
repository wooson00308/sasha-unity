using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 파츠 슬롯의 정의를 나타냅니다. 어떤 타입의 파츠를 끼울 수 있는지 등의 정보를 포함합니다.
    /// </summary>
    [Serializable]
    public class PartSlotDefinition
    {
        /// <summary>
        /// 슬롯 식별자 (예: "Head", "Arm_Left", "Legs")
        /// </summary>
        public string SlotIdentifier;

        /// <summary>
        /// 이 슬롯에 장착 가능한 파츠의 타입입니다.
        /// </summary>
        public PartType RequiredPartType;

        // 예: 나중에 시각화나 추가 규칙을 위해 필드 추가 가능
        // public Vector3 AttachmentPoint;
        // public float CompatibilityModifier = 1.0f;
        // public float WeightCapacity;
    }


    /// <summary>
    /// ArmoredFrame의 기본 골격인 프레임의 추상 기본 클래스입니다.
    /// </summary>
    [Serializable]
    public abstract class Frame // 추상 클래스로 변경
    {
        /// <summary>
        /// 프레임의 이름
        /// </summary>
        [SerializeField] protected string _name;

        /// <summary>
        /// 프레임의 타입 (경량, 범용, 중장갑)
        /// </summary>
        [SerializeField] protected FrameType _type;

        /// <summary>
        /// 프레임의 기본 스탯
        /// </summary>
        [SerializeField] protected Stats _baseStats;

        /// <summary>
        /// 프레임 자체의 무게
        /// </summary>
        [SerializeField] protected float _weight = 50f; // 기본 프레임 무게 50으로 설정

        // 기존 파츠 호환성 시스템: 파츠 타입별 호환성 계수를 저장하는 딕셔너리
        [SerializeField] protected Dictionary<PartType, float> _partCompatibility = new Dictionary<PartType, float>(); 

        // 공개 프로퍼티
        public string Name => _name;
        public FrameType Type => _type;
        public Stats BaseStats => _baseStats;
        public float Weight => _weight;
        // 호환성 딕셔너리 접근자 (읽기 전용으로 제공할지 결정 필요)
        // public IReadOnlyDictionary<PartType, float> PartCompatibility => _partCompatibility; 

        /// <summary>
        /// 기본 생성자
        /// </summary>
        protected Frame(string name, FrameType type, Stats baseStats)
        {
            _name = name;
            _type = type;
            _baseStats = baseStats;
            InitializeDefaultCompatibility(); // 호환성 딕셔너리 초기화 호출
            AdjustCompatibilityByType(); // 프레임 타입별 호환성 조정
        }

        /// <summary>
        /// 무게를 받는 생성자 오버로드 추가 (필요시 사용)
        /// </summary>
        protected Frame(string name, FrameType type, Stats baseStats, float weight)
        {
            _name = name;
            _type = type;
            _baseStats = baseStats;
            _weight = weight; // 전달받은 무게 설정
            InitializeDefaultCompatibility();
            AdjustCompatibilityByType();
        }

        /// <summary>
        /// 기본 호환성 딕셔너리를 초기화합니다. (모든 파츠 1.0)
        /// </summary>
        private void InitializeDefaultCompatibility()
        {
            _partCompatibility.Clear();
            foreach (PartType partType in Enum.GetValues(typeof(PartType)))
            {
                if (partType != PartType.Frame) // 프레임 자신은 제외 (필요하다면 이 로직도 검토)
                {
                    _partCompatibility[partType] = 1.0f;
                }
            }
        }

        /// <summary>
        /// 프레임 타입에 따라 기본 호환성 값을 조정합니다.
        /// </summary>
        private void AdjustCompatibilityByType()
        {
            // TODO: 이 로직을 ScriptableObject나 설정 파일에서 로드하도록 개선 가능
            switch (_type)
            {
                case FrameType.Light:
                    _partCompatibility[PartType.Body] = 0.8f; // 예시 값
                    _partCompatibility[PartType.Arm] = 0.9f; // 예시 값
                    break;
                case FrameType.Heavy:
                    _partCompatibility[PartType.Legs] = 0.8f; // 예시 값
                    _partCompatibility[PartType.Head] = 0.9f; // 예시 값
                    break;
                // Standard는 기본값(1.0) 유지
            }
        }

        /// <summary>
        /// 이 프레임이 제공하는 파츠 슬롯 정의 목록을 반환합니다.
        /// 자식 클래스에서 반드시 구현해야 합니다.
        /// </summary>
        /// <returns>슬롯 식별자를 키로 하는 읽기 전용 슬롯 정의 딕셔너리</returns>
        public abstract IReadOnlyDictionary<string, PartSlotDefinition> GetPartSlots();

        /// <summary>
        /// 특정 슬롯에 주어진 파츠를 장착할 수 있는지 확인합니다.
        /// 기본적으로 슬롯 정의의 RequiredPartType과 파츠의 Type을 비교합니다.
        /// 필요시 자식 클래스에서 재정의하여 추가적인 호환성 규칙을 적용할 수 있습니다.
        /// </summary>
        /// <param name="part">장착하려는 파츠</param>
        /// <param name="slotIdentifier">장착하려는 슬롯의 식별자</param>
        /// <returns>장착 가능하면 true, 그렇지 않으면 false</returns>
        public virtual bool CanEquipPart(Part part, string slotIdentifier)
        {
            if (part == null) return false;

            if (GetPartSlots().TryGetValue(slotIdentifier, out var slotDef))
            {
                if (part.Type == slotDef.RequiredPartType)
                {
                    // 기존 호환성 시스템 체크 (선택 사항)
                    if (_partCompatibility.TryGetValue(part.Type, out float factor) && factor < 0.9f) // 예: 0.9 미만이면 경고 또는 불가
                    {
                        Debug.LogWarning($"프레임({Name})과 파츠({part.Name}) 타입({part.Type}) 간 호환성 낮음 ({factor:F1})");
                        // return false; // 호환성 낮으면 아예 장착 불가로 할 수도 있음
                    }
                    return true; 
                }
                else
                {
                    Debug.LogWarning($"슬롯({slotIdentifier})은 {slotDef.RequiredPartType} 타입만 받지만, {part.Type} 타입 파츠({part.Name}) 시도됨");
                }
            }
            else
            {
                Debug.LogWarning($"프레임({Name})에 존재하지 않는 슬롯({slotIdentifier})에 파츠 장착 시도됨");
            }
            return false;
        }

        /// <summary>
        /// 특정 파츠 타입에 대한 프레임의 호환성 계수를 반환합니다.
        /// </summary>
        public virtual float GetCompatibilityFactor(PartType partType)
        {
            // 딕셔너리에서 직접 값을 찾아 반환, 없으면 기본값 1.0
            return _partCompatibility.TryGetValue(partType, out float factor) ? factor : 1.0f;
        }
    }
} 