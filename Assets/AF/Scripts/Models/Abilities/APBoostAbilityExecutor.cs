using AF.Combat;
using AF.Models;
using AF.Data;
namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BP_003_APBoost" : MaxAP +1, APRecovery +2 영구 버프.
    /// </summary>
    public class APBoostAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BP_003_APBoost";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null) return false;
            if (user.HasStatusEffect("APBoostPassive")) return false;
            if (user.HasStatusEffect("APBoostRecovery")) return false;

            // MaxAP +1
            var maxApBuff = new StatusEffect(
                effectName: "APBoostPassive",
                durationTurns: -1,
                effectType: StatusEffectEvents.StatusEffectType.Buff_MaxAPBoost,
                statToModify: StatType.MaxAP,
                modType: ModificationType.Additive,
                modValue: 1f);
            user.AddStatusEffect(maxApBuff);

            // APRecovery +2 (별도 효과로 표시)
            var recoveryBuff = new StatusEffect(
                effectName: "APBoostRecovery",
                durationTurns: -1,
                effectType: StatusEffectEvents.StatusEffectType.Buff_APRecoveryBoost,
                statToModify: StatType.APRecovery,
                modType: ModificationType.Additive,
                modValue: 2f);
            user.AddStatusEffect(recoveryBuff);

            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;
            if (user.HasStatusEffect("APBoostPassive") || user.HasStatusEffect("APBoostRecovery")) return false;
            return user.HasEnoughAP(data.APCost);
        }
    }
} 