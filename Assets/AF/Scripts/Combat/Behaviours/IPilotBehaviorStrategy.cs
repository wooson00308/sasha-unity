using AF.Models;
using UnityEngine;

namespace AF.Combat.Behaviors
{
    /// <summary>
    /// 파일럿 전문화별 행동 결정 전략 인터페이스
    /// </summary>
    public interface IPilotBehaviorStrategy
    {
        /// <summary>
        /// 행동을 결정해 반환한다.
        /// </summary>
        (CombatActionEvents.ActionType actionType,
         ArmoredFrame targetFrame,
         Vector3? targetPosition,
         Weapon weapon)
        DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService context);
    }
}
