using AF.Combat;
using AF.Models;
using System.Linq;
using UnityEngine; // LogLevelのため

namespace AF.Combat.Handlers
{
    /// <summary>
    /// Handles "DamageOverTime" effects.
    /// </summary>
    public class DamageOverTimeHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=red>부식성 물질</color> 부착 감지. (틱당 피해량: {effect.TickValue}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            if (target == null || !target.IsOperational || effect == null) return;

            // Assuming effect.TickValue stores the damage per tick.
            // And effect.StatToModify might indicate a specific damage type if needed in future.
            float damageAmount = effect.TickValue; 

            // find body slot or first part
            string targetPartSlot = target.Parts.FirstOrDefault(p => p.Value.Type == PartType.Body).Key ?? target.Parts.Keys.FirstOrDefault();
            if (string.IsNullOrEmpty(targetPartSlot)) return;

            target.ApplyDamage(targetPartSlot, damageAmount, ctx.CurrentTurn, null, false, false);
            
            ctx.Logger.TextLogger.Log($"<color=red>부식 피해</color> 발생. ({targetPartSlot} 슬롯에 {damageAmount} 피해).", LogLevel.Info, LogEventType.StatusEffectTicked);

            // Publish tick event for log/UIs
            ctx.Bus.Publish(new StatusEffectEvents.StatusEffectTickEvent(target, effect));
        }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"기체의 부식성 물질 <color=yellow>중화</color> 완료.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"기체의 부식성 물질 <color=green>긴급 제거</color>.", LogLevel.Debug, LogEventType.StatusEffectRemoved);
        }
    }
} 