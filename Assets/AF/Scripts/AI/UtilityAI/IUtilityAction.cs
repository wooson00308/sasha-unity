using System.Collections.Generic;
using AF.Combat; // Assuming CombatContext might be needed
using AF.Models; // <<< Add Models namespace for ArmoredFrame
using UnityEngine; // For Vector3?

namespace AF.AI.UtilityAI
{
    /// <summary>
    /// Represents a potential action an AI agent can take.
    /// Contains considerations to evaluate the action's utility.
    /// </summary>
    public interface IUtilityAction
    {
        string Name { get; } // Name for debugging/identification
        List<IConsideration> Considerations { get; }

        // +++ 추가된 속성 +++
        CombatActionEvents.ActionType AssociatedActionType { get; } // 이 유틸리티 액션에 해당하는 실제 전투 액션 타입
        ArmoredFrame Target { get; } // 대상 유닛 (공격, 수리 등 대상이 필요한 액션)
        Weapon AssociatedWeapon { get; } // 사용 무기 (공격, 재장전 등 무기가 필요한 액션)
        Vector3? TargetPosition { get; } // 목표 위치 (이동 액션)
        // +++ 속성 추가 끝 +++

        // +++ 추가: 마지막으로 계산된 유틸리티 점수 (로그 및 디버깅용) +++
        float LastCalculatedUtility { get; set; }
        // +++ 속성 추가 끝 +++

        /// <summary>
        /// Calculates the overall utility score for this action based on its considerations.
        /// </summary>
        /// <param name="actor">The ArmoredFrame performing the action.</param>
        /// <param name="context">The current combat context providing necessary information.</param>
        /// <returns>A utility score (typically 0-1, but can be higher depending on scoring method).</returns>
        float CalculateUtility(ArmoredFrame actor, CombatContext context);

        /// <summary>
        /// Executes the action (or generates the necessary parameters for execution).
        /// </summary>
        /// <param name="actor">The ArmoredFrame performing the action.</param>
        /// <param name="context">The current combat context.</param>
        void Execute(ArmoredFrame actor, CombatContext context);
        // Or potentially return an ActionParameter object for CombatActionExecutor
        // object GetExecutionParameters(ArmoredFrame actor, CombatContext context);
    }
} 