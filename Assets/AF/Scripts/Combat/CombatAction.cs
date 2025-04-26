using UnityEngine; // Sprite 사용
using AF.Combat; // ActionType Enum 사용 (CombatActionEvents 네임스페이스)

namespace AF.Combat // 네임스페이스는 Combat으로 지정
{
    /// <summary>
    /// 유닛이 수행할 수 있는 개별 행동(액션) 정보를 담는 구조체입니다.
    /// </summary>
    public struct CombatAction
    {
        /// <summary>
        /// 행동의 이름 (UI 표시용)
        /// </summary>
        public string Name;

        /// <summary>
        /// 행동에 소모되는 AP
        /// </summary>
        public float ApCost;

        /// <summary>
        /// 행동의 종류
        /// </summary>
        public CombatActionEvents.ActionType Type; // 기존 ActionType Enum 사용

        /// <summary>
        /// 행동의 출처 객체 (Weapon, Skill, AbilityData 등 참조용)
        /// </summary>
        public object Source;

        /// <summary>
        /// UI에 표시될 아이콘 (선택적)
        /// </summary>
        public Sprite IconSprite;

        // 필요한 추가 정보 (예: 사거리, 대상 타입, 쿨다운 등)
        // public float Range;
        // public TargetType RequiredTarget; // enum TargetType { Self, Ally, Enemy, Position } 등
        // public bool IsReady; // 쿨다운 등 준비 상태

        /// <summary>
        /// CombatAction 생성자
        /// </summary>
        public CombatAction(string name, float apCost, CombatActionEvents.ActionType type, object source = null, Sprite icon = null)
        {
            Name = name;
            ApCost = apCost;
            Type = type;
            Source = source;
            IconSprite = icon;
            // Range = 0f;
            // RequiredTarget = TargetType.Enemy;
            // IsReady = true;
        }

        // 추가 편의 속성 또는 메서드 정의 가능
        // public bool RequiresTarget => Type == CombatActionEvents.ActionType.Attack || Type == CombatActionEvents.ActionType.RepairAlly;
    }
} 