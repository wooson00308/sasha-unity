using AF.Combat;
using AF.Models;

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
                statToModify: StatType.None, // 데미지 흡수량은 별도 로직 필요
                modType: ModificationType.None,
                modValue: 0f);
            user.AddStatusEffect(shield);

            ctx?.Bus?.Publish(new StatusEffectEvents.StatusEffectAppliedEvent(user, user, StatusEffectEvents.StatusEffectType.Buff_ShieldGenerator, 3, 100f, "EnergyShield"));
            return true;
        }
    }
} 