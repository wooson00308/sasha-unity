using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 팔 파츠 구현
    /// </summary>
    [System.Serializable]
    public class ArmsPart : Part
    {
        public ArmsPart(string name, Stats stats, float durability)
            : base(name, PartType.Arm, stats, durability)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            Debug.Log($"{parentAF.Name}의 팔 파츠({_name})가 파괴되었습니다! 무기 사용 등에 패널티 적용 가능.");
            // TODO: 무기 명중률 감소 또는 특정 무기 사용 불가 등 패널티 로직 추가
        }
    }
} 