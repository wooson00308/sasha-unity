using UnityEngine;
using AF.Models; // ArmoredFrame, Weapon 모델 사용
using AF.Models.Abilities;

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
            Weapon weapon, // Weapon 모델 사용을 위해 AF.Models 네임스페이스 필요
            bool isCounter = false,
            bool freeCounter = false,
            AbilityEffect abilityEffect = null);

        /// <summary>
        /// 지정된 행동에 필요한 AP 비용을 계산하여 반환합니다.
        /// </summary>
        /// <param name="actionType">확인할 행동의 타입입니다.</param>
        /// <param name="actor">행동을 수행할 유닛입니다.</param>
        /// <param name="weapon">공격 행동일 경우 사용할 무기입니다. 다른 행동 타입에는 null일 수 있습니다.</param>
        /// <returns>계산된 AP 비용. 유효하지 않은 경우 float.MaxValue를 반환할 수 있습니다.</returns>
        float GetActionAPCost(
            CombatActionEvents.ActionType actionType,
            ArmoredFrame actor,
            Weapon weapon = null,
            AbilityEffect abilityEffect = null
        );
    }
} 