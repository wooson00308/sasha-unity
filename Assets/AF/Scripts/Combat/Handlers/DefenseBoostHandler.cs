using AF.Combat;
using AF.Models;
using UnityEngine; // LogLevelのため

namespace AF.Combat.Handlers
{
    /// <summary>
    /// Handles "DefenseBoost" effects.
    /// Applies a defense modification on apply and reverts it on expire/remove.
    /// </summary>
    public class DefenseBoostHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.Defense, effect.ModificationType, effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 장갑 <color=green>강화</color>. 방어력 <color=green>상승</color>. (효과: {effect.ModificationValue:P0}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            // DefenseBoost is typically not a per-tick effect.
        }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 장갑 강화 효과 <color=yellow>만료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            var reverseModificationType = effect.ModificationType == ModificationType.Additive ? ModificationType.Additive : ModificationType.Multiplicative;
            var reverseValue = effect.ModificationType == ModificationType.Additive ? -effect.ModificationValue : (1f / effect.ModificationValue);
            target.CombinedStats.ApplyModifier(StatType.Defense, reverseModificationType, reverseValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 장갑 강화 효과 <color=red>해제</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 