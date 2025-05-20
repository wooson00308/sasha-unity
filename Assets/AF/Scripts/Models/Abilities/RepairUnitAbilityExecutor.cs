using System.Linq;
using AF.Combat;
using AF.Models;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BP_001_RepairUnit" : 선택한 아군 파츠 25 회복.
    /// </summary>
    public class RepairUnitAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BP_001_RepairUnit";
        private const float REPAIR_AMOUNT = 25f;

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || target == null) return false;
            if (!target.IsOperational) return false;

            string mostDamaged = target.Parts
                .Where(kvp => kvp.Value.IsOperational && kvp.Value.CurrentDurability < kvp.Value.MaxDurability)
                .OrderBy(kvp => kvp.Value.CurrentDurability / kvp.Value.MaxDurability)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(mostDamaged)) return false;

            float repaired = target.ApplyRepair(mostDamaged, REPAIR_AMOUNT);
            if (repaired <= 0f) return false;

            ctx?.Bus?.Publish(new CombatActionEvents.RepairAppliedEvent(user, target, CombatActionEvents.ActionType.RepairAlly, mostDamaged, repaired, ctx?.CurrentTurn ?? 0));
            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || target == null || data == null) return false;
            if (!user.IsOperational || !target.IsOperational) return false;

            // 대상 유닛에 수리 가능한 파츠가 있는지 확인
            bool hasDamagedPart = target.Parts.Values.Any(p => p.IsOperational && p.CurrentDurability < p.MaxDurability);
            if (!hasDamagedPart) return false;

            // AP 비용 체크
            if (!user.HasEnoughAP(data.APCost)) return false;

            return true;
        }
    }
} 