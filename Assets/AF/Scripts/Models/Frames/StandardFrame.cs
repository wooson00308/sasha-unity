using System.Collections.Generic;
using UnityEngine;

namespace AF.Models // Models 하위 네임스페이스 사용 -> 수정: Models 네임스페이스 바로 아래로
{
    /// <summary>
    /// 표준형 프레임 구현 클래스입니다.
    /// Frame 클래스를 상속받아 Standard 타입 프레임의 슬롯 구성을 정의합니다.
    /// </summary>
    // [CreateAssetMenu(fileName = "StandardFrameData", menuName = "AF/Frames/Standard Frame")] // 필요하다면 ScriptableObject로 만들 수 있음
    [System.Serializable] // ArmoredFrame에서 직렬화될 수 있도록
    public class StandardFrame : Frame
    {
        // Standard 프레임의 고정된 슬롯 정의
        private static readonly Dictionary<string, PartSlotDefinition> _slots = new Dictionary<string, PartSlotDefinition>
        {
            { "Head", new PartSlotDefinition { SlotIdentifier = "HeadSlot", RequiredPartType = PartType.Head } },
            { "Body", new PartSlotDefinition { SlotIdentifier = "BodySlot", RequiredPartType = PartType.Body } },
            { "Arm_Left", new PartSlotDefinition { SlotIdentifier = "Arm_Left", RequiredPartType = PartType.Arm } },
            { "Arm_Right", new PartSlotDefinition { SlotIdentifier = "Arm_Right", RequiredPartType = PartType.Arm } },
            { "Legs", new PartSlotDefinition { SlotIdentifier = "Legs", RequiredPartType = PartType.Legs } }, // 왼다리 슬롯 추가// 오른다리 슬롯 추가
            { "Backpack", new PartSlotDefinition { SlotIdentifier = "Backpack", RequiredPartType = PartType.Backpack } }
        };

        /// <summary>
        /// StandardFrame 생성자입니다.
        /// </summary>
        /// <param name="name">프레임 이름</param>
        /// <param name="baseStats">기본 스탯</param>
        public StandardFrame(string name, Stats baseStats)
            : base(name, FrameType.Standard, baseStats) // 부모 생성자 호출
        {
            // StandardFrame 고유의 초기화 로직이 있다면 여기에 추가
        }

        /// <summary>
        /// StandardFrame 생성자 (무게 포함)
        /// </summary>
        public StandardFrame(string name, Stats baseStats, float weight)
            : base(name, FrameType.Standard, baseStats, weight) // 부모 생성자 호출
        {
            // StandardFrame 고유의 초기화 로직이 있다면 여기에 추가
        }

        /// <summary>
        /// Frame 클래스의 추상 메서드를 구현하여 Standard 프레임의 슬롯 정의를 반환합니다.
        /// </summary>
        public override IReadOnlyDictionary<string, PartSlotDefinition> GetPartSlots()
        {
            return _slots;
        }

        // 필요하다면 CanEquipPart 또는 GetCompatibilityFactor 메서드를 재정의하여
        // Standard 프레임만의 특별한 호환성 규칙을 적용할 수 있습니다.
        // 예: public override bool CanEquipPart(Part part, string slotIdentifier) { ... }
    }
} 