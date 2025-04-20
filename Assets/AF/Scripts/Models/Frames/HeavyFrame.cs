using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 중량 프레임 구현 클래스입니다.
    /// Frame 클래스를 상속받아 Heavy 타입 프레임의 슬롯 구성을 정의합니다.
    /// </summary>
    [System.Serializable] 
    public class HeavyFrame : Frame
    {
        // Heavy 프레임의 슬롯 정의 (Standard와 동일하게 시작, 필요시 수정)
        private static readonly Dictionary<string, PartSlotDefinition> _slots = new Dictionary<string, PartSlotDefinition>
        {
            { "Head", new PartSlotDefinition { SlotIdentifier = "Head", RequiredPartType = PartType.Head } },
            { "Body", new PartSlotDefinition { SlotIdentifier = "Body", RequiredPartType = PartType.Body } },
            { "Arm_Left", new PartSlotDefinition { SlotIdentifier = "Arm_Left", RequiredPartType = PartType.Arm } },
            { "Arm_Right", new PartSlotDefinition { SlotIdentifier = "Arm_Right", RequiredPartType = PartType.Arm } },
            { "Legs", new PartSlotDefinition { SlotIdentifier = "Legs", RequiredPartType = PartType.Legs } },
            // 중량 프레임은 백팩 슬롯이 필수일 수 있음 (예시)
            { "Backpack", new PartSlotDefinition { SlotIdentifier = "Backpack", RequiredPartType = PartType.Backpack } }, 
        };

        /// <summary>
        /// HeavyFrame 생성자입니다.
        /// </summary>
        public HeavyFrame(string name, Stats baseStats)
            : base(name, FrameType.Heavy, baseStats) // 부모 생성자 호출 (FrameType.Heavy)
        {
            // HeavyFrame 고유의 초기화 로직 (예: 호환성 조정 등)
        }

        /// <summary>
        /// HeavyFrame 생성자 (무게 포함)
        /// </summary>
        public HeavyFrame(string name, Stats baseStats, float weight)
            : base(name, FrameType.Heavy, baseStats, weight) // 부모 생성자 호출 (FrameType.Heavy)
        {
        }

        /// <summary>
        /// Frame 클래스의 추상 메서드를 구현하여 Heavy 프레임의 슬롯 정의를 반환합니다.
        /// </summary>
        public override IReadOnlyDictionary<string, PartSlotDefinition> GetPartSlots()
        {
            return _slots;
        }

        // 필요하다면 CanEquipPart 또는 GetCompatibilityFactor 메서드를 재정의하여
        // Heavy 프레임만의 특별한 호환성 규칙을 적용할 수 있습니다.
    }
} 