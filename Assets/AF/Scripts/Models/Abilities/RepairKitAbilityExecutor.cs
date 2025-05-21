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
        private const float HEALTH_THRESHOLD_FOR_KIT = 0.5f; // 체력 50% 미만일 때만 사용 고려

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null) return false;

            int currentUses = user.ActiveStatusEffects.Count(e => e.EffectName == "RepairKitUsed");
            if (currentUses >= MAX_USES) return false; 

            string damagedPartSlot = user.Parts
                .Where(kvp => kvp.Value.CurrentDurability < kvp.Value.MaxDurability)
                .OrderBy(kvp => kvp.Value.CurrentDurability / kvp.Value.MaxDurability)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(damagedPartSlot)) return false;

            float repairedAmount = user.ApplyRepair(damagedPartSlot, REPAIR_AMOUNT);
            if (repairedAmount <= 0f) return false;

            var useMark = new StatusEffect(
                effectName: "RepairKitUsed", 
                durationTurns: -1, 
                effectType: StatusEffectEvents.StatusEffectType.Buff_Utility,
                statToModify: StatType.None, 
                modType: ModificationType.None,
                modValue: 0f);
            user.AddStatusEffect(useMark);

            ctx?.Bus?.Publish(new CombatActionEvents.RepairAppliedEvent(user, user, CombatActionEvents.ActionType.RepairSelf, damagedPartSlot, repairedAmount, ctx?.CurrentTurn ?? 0));
            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;

            // 사용 횟수 체크
            int uses = user.ActiveStatusEffects.Count(e => e.EffectName == "RepairKitUsed");
            if (uses >= MAX_USES) return false;

            // AP 체크
            if (!user.HasEnoughAP(data.APCost)) return false;

            // 체력 조건 체크 (ArmoredFrame의 새 프로퍼티 사용)
            if (user.CurrentDurabilityRatio >= HEALTH_THRESHOLD_FOR_KIT)
            {
                return false; // 체력이 설정된 기준치 이상이면 사용 안 함
            }

            // 손상된 파츠가 하나라도 있는지 체크
            bool hasDamagedPart = user.Parts.Values.Any(p => p.CurrentDurability < p.MaxDurability);
            if (!hasDamagedPart) return false;
            
            return true;
        }
    }
} 