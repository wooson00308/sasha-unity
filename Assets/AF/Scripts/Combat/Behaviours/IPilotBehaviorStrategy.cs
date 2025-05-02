using AF.Models;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AF.Combat.Behaviors
{
    /// <summary>
    /// 파일럿 전문화별 행동 결정 전략 인터페이스
    /// </summary>
    public interface IPilotBehaviorStrategy
    {
        /// <summary>
        /// 행동을 비동기적으로 결정해 반환한다.
        /// </summary>
        /// <param name="activeUnit">행동 주체</param>
        /// <param name="context">전투 컨텍스트</param>
        /// <param name="cancellationToken">작업 취소 토큰</param>
        UniTask<(CombatActionEvents.ActionType actionType,
                 ArmoredFrame targetFrame,
                 Vector3? targetPosition,
                 Weapon weapon)>
        DetermineActionAsync(ArmoredFrame activeUnit, CombatContext context, CancellationToken cancellationToken = default);
    }
}
