using System.Collections.Generic;
using AF.EventBus;
using AF.Models;
namespace AF.Combat
{
    public interface IStatusEffectProcessor
    {
        void Tick(CombatContext ctx, ArmoredFrame unit);
    }

    public sealed class StatusEffectProcessor : IStatusEffectProcessor
    {
        public void Tick(CombatContext ctx, ArmoredFrame unit)
        {
            if (unit == null || !unit.IsOperational) return;

            var effects = new List<StatusEffect>(unit.ActiveStatusEffects);
            if (effects.Count == 0) return;

            foreach (var effect in effects)
            {
                if (effect.EffectName.Contains("DamageOverTime"))
                {
                    float dmg = effect.TickValue;
                    ctx.Bus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                    ctx.Bus.Publish(new DamageEvents.DamageAppliedEvent(null, unit, null, dmg, 
                        PartType.Body, false, 0f, 0f, false));
                }
                else if (effect.EffectName.Contains("RepairOverTime"))
                {
                    ctx.Bus.Publish(new StatusEffectEvents.StatusEffectTickEvent(unit, effect));
                }

                if (effect.DurationTurns != -1)
                {
                    effect.DurationTurns--;
                    if (effect.DurationTurns <= 0)
                    {
                        unit.RemoveStatusEffect(effect.EffectName);
                        var effectType = effect.EffectType;
                        ctx.Bus.Publish(new StatusEffectEvents.StatusEffectExpiredEvent(
                            unit, effectType, effect.EffectName));
                    }
                }
            }
        }
    }
}
