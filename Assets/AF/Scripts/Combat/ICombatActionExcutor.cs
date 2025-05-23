using UnityEngine;
using AF.Models;

namespace AF.Combat
{
    /// <summary>행동(공격‧이동‧방어‧수리 등)을 실제 수행하고 이벤트를 발행하는 서비스</summary>
    public interface ICombatActionExecutor
    {
        bool Execute(
            CombatContext ctx,
            ArmoredFrame actor,
            CombatActionEvents.ActionType actionType,
            ArmoredFrame targetFrame,
            Vector3? targetPosition,
            Weapon weapon,
            bool isCounter = false,
            bool freeCounter = false);
    }
}
