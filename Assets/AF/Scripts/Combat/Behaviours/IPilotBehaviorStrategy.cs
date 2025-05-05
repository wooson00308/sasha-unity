using AF.Models;
using AF.AI.UtilityAI;
using UnityEngine;

namespace AF.Combat.Behaviors
{
    /// <summary>
    /// 파일럿의 행동 결정 로직 인터페이스
    /// </summary>
    public interface IPilotBehaviorStrategy
    {
        /// <summary>
        /// 현재 상황에 맞는 최적의 행동을 결정합니다.
        /// </summary>
        /// <param name="activeUnit">행동 주체 유닛</param>
        /// <param name="context">전투 시뮬레이터 서비스</param>
        /// <returns>선택된 IUtilityAction 객체 (없으면 null)</returns>
        IUtilityAction DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService context);
    }
}
