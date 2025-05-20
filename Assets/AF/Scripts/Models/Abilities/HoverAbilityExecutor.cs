using AF.Combat;
using AF.Models;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_LG_002_Hover" : 영구적으로 Speed +1, 지형 페널티 무시 특성은 TODO.
    /// </summary>
    public class HoverAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_LG_002_Hover";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null) return false;
            if (user.HasStatusEffect("HoverPassive")) return false;

            var buff = new StatusEffect(
                effectName: "HoverPassive",
                durationTurns: -1,
                effectType: StatusEffectEvents.StatusEffectType.Buff_SpeedBoost,
                statToModify: StatType.Speed,
                modType: ModificationType.Additive,
                modValue: 1f);

            user.AddStatusEffect(buff);
            ctx?.Bus?.Publish(new StatusEffectEvents.StatusEffectAppliedEvent(
                target: user,
                source: user,
                effectType: StatusEffectEvents.StatusEffectType.Buff_SpeedBoost,
                duration: -1,
                magnitude: 1f,
                effectId: "HoverPassive"));
            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null) return false;
            if (user.HasStatusEffect("HoverPassive")) return false;
            return true;
        }
    }
} 