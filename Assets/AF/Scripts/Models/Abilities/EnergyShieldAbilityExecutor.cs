using AF.Combat;
using AF.Models;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BP_002_EnergyShield" : 3턴 동안 100 HP 흡수 실드 부여.
    /// 현재는 StatusEffect 사용해 데미지 흡수 로직은 TODO.
    /// </summary>
    public class EnergyShieldAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BP_002_EnergyShield";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || !user.IsOperational) return false;
            if (user.HasStatusEffect("EnergyShield")) return false;

            var shield = new StatusEffect(
                effectName: "EnergyShield",
                durationTurns: 3,
                effectType: StatusEffectEvents.StatusEffectType.Buff_ShieldGenerator,
                statToModify: StatType.None,
                modType: ModificationType.None,
                modValue: 100f);
            user.AddStatusEffect(shield);

            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;
            if (user.HasStatusEffect("EnergyShield")) return false;
            return user.HasEnoughAP(data.APCost);
        }
    }
} 