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
            Debug.Log($"{parentAF.Name}의 헤드 파츠({_name})가 파괴되었습니다! 명중률 관련 패널티 적용 가능.");
            // TODO: 명중률 감소 등 실제 패널티 로직 추가
        }
    }
} 