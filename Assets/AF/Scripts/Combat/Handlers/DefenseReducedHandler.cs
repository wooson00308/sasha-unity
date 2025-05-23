using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Handlers
{
    public class DefenseReducedHandler : IStatusEffectHandler // Debuff용
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.Defense, effect.ModificationType, effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"기체 장갑 <color=red>약화</color> 발생. (효과: {effect.ModificationValue:P0}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }
        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }
        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"기체 장갑 약화 효과 <color=yellow>소멸</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }
        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            var reverseModificationType = effect.ModificationType == ModificationType.Additive ? ModificationType.Additive : ModificationType.Multiplicative;
            var reverseValue = effect.ModificationType == ModificationType.Additive ? -effect.ModificationValue : (1f / effect.ModificationValue);
            target.CombinedStats.ApplyModifier(StatType.Defense, reverseModificationType, reverseValue);
            ctx.Logger.TextLogger.Log($"기체 장갑 약화 효과 <color=green>복구</color>.", LogLevel.Debug, LogEventType.StatusEffectRemoved);
        }
    }
} 