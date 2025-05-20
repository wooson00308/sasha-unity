using AF.Combat;
using AF.Models;
using System.Linq;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BD_001_SelfRepair" : 매 턴 Body 파츠 내구도를 5 회복.
    /// 현재 구현은 즉시 Body 파츠를 5 회복(전투 컨텍스트 외부 호출 가정).
    /// </summary>
    public class SelfRepairAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BD_001_SelfRepair";
        private const float REPAIR_AMOUNT = 5f;

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || !user.IsOperational) return false;

            // Body 슬롯 찾기 (프레임 정의에 따라 Body slot 식별)
            string bodySlot = user.Parts.FirstOrDefault(kvp => kvp.Value.Type == PartType.Body).Key;
            if (string.IsNullOrEmpty(bodySlot)) return false;

            float repaired = user.ApplyRepair(bodySlot, REPAIR_AMOUNT);
            if (repaired <= 0f) return false;

            // 이벤트 발행 (선택)
            ctx?.Bus?.Publish(new CombatActionEvents.RepairAppliedEvent(
                actor: user,
                target: user,
                actionType: CombatActionEvents.ActionType.RepairSelf,
                targetSlotIdentifier: bodySlot,
                amountRepaired: repaired,
                turnNumber: ctx?.CurrentTurn ?? 0));

            return true;
        }
    }
} 