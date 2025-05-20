using System.Linq;
using AF.Combat;
using AF.Models;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BP_004_RepairKit" : 자신 파츠 50 회복, 사용 제한 3회.
    /// </summary>
    public class RepairKitAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BP_004_RepairKit";
        private const float REPAIR_AMOUNT = 50f;
        private const int MAX_USES = 3;

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null) return false;

            // uses 카운트는 user.AICtxBlackboard 또는 별도 딕셔너리로 추적 (간단히 StatusEffect 스택으로 관리)
            int currentUses = 0;
            foreach (var eff in user.ActiveStatusEffects)
            {
                if (eff.EffectName == "RepairKitUsed") currentUses++;
            }
            if (currentUses >= MAX_USES) return false; // 더 이상 사용 불가

            string damaged = user.Parts
                .Where(kvp => kvp.Value.CurrentDurability < kvp.Value.MaxDurability)
                .OrderBy(kvp => kvp.Value.CurrentDurability / kvp.Value.MaxDurability)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(damaged)) return false;

            float repaired = user.ApplyRepair(damaged, REPAIR_AMOUNT);
            if (repaired <= 0f) return false;

            // 사용 기록용 StatusEffect (영구)
            var useMark = new StatusEffect("RepairKitUsed", -1, StatusEffectEvents.StatusEffectType.Buff_RepairField, StatType.None, ModificationType.None, 0f);
            user.AddStatusEffect(useMark);

            ctx?.Bus?.Publish(new CombatActionEvents.RepairAppliedEvent(user, user, CombatActionEvents.ActionType.RepairSelf, damaged, repaired, ctx?.CurrentTurn ?? 0));
            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;
            int uses = user.ActiveStatusEffects.Count(e => e.EffectName == "RepairKitUsed");
            if (uses >= MAX_USES) return false;
            // any damaged part?
            bool damaged = user.Parts.Values.Any(p => p.CurrentDurability < p.MaxDurability);
            return damaged && user.HasEnoughAP(data.APCost);
        }
    }
} 