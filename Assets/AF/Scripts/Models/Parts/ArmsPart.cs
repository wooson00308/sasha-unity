using UnityEngine;
using System.Collections.Generic;

namespace AF.Models
{
    /// <summary>
    /// 팔 파츠 구현
    /// </summary>
    [System.Serializable]
    public class ArmsPart : Part
    {
        public ArmsPart(string name, Stats stats, float durability, float weight, List<string> initialAbilities = null)
            : base(name, PartType.Arm, stats, durability, weight, initialAbilities)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            // TODO: 무기 명중률 감소 또는 특정 무기 사용 불가 등 패널티 로직 추가
        }
    }
} 