using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 다리 파츠 구현
    /// </summary>
    [System.Serializable]
    public class LegsPart : Part
    {
        public LegsPart(string name, Stats stats, float durability)
            : base(name, PartType.Legs, stats, durability)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            Debug.Log($"{parentAF.Name}의 다리 파츠({_name})가 파괴되었습니다! 이동 및 회피 관련 패널티 적용 가능.");
            // TODO: 이동 속도 감소, 회피율 감소 등 실제 패널티 로직 추가
        }
    }
} 