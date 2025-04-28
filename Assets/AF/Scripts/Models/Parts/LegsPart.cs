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

        public LegsPart(string name, Stats stats, float durability, float weight)
            : base(name, PartType.Legs, stats, durability, weight)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            // TODO: 이동 속도 감소, 회피율 감소 등 실제 패널티 로직 추가
        }
    }
} 