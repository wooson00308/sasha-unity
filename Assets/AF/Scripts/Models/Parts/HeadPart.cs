using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 헤드 파츠 구현
    /// </summary>
    [System.Serializable]
    public class HeadPart : Part
    {
        public HeadPart(string name, Stats stats, float durability) 
            : base(name, PartType.Head, stats, durability)
        {
        }

        public HeadPart(string name, Stats stats, float durability, float weight)
            : base(name, PartType.Head, stats, durability, weight)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            // TODO: 명중률 감소 등 실제 패널티 로직 추가
        }
    }
} 