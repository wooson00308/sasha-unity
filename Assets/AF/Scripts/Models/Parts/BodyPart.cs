using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 바디 파츠 구현
    /// </summary>
    [System.Serializable]
    public class BodyPart : Part
    {
        public BodyPart(string name, Stats stats, float durability)
            : base(name, PartType.Body, stats, durability)
        {
        }

        public BodyPart(string name, Stats stats, float durability, float weight)
            : base(name, PartType.Body, stats, durability, weight)
        {
        }

        public override void OnDestroyed(ArmoredFrame parentAF)
        {
            // Body 파괴 시 ArmoredFrame 작동 불능 처리 (ArmoredFrame 클래스 내 CheckOperationalStatus에서 처리 필요)
            // parentAF.IsOperational = false; // 직접 설정보다는 CheckOperationalStatus 로직에서 Body 상태를 확인하도록 하는 것이 좋음
            // 현재 ArmoredFrame.CheckOperationalStatus()에 Body 파츠 확인 로직이 이미 있음.
        }
    }
} 