using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Handlers
{
    public class APRecoveryBoostHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.APRecovery, effect.ModificationType, effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 에너지 회복 시스템 <color=green>최적화</color>. (AP 회복량 증가: {effect.ModificationValue}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 에너지 회복 최적화 효과 <color=yellow>만료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            var reverseModificationType = effect.ModificationType == ModificationType.Additive ? ModificationType.Additive : ModificationType.Multiplicative;
            var reverseValue = effect.ModificationType == ModificationType.Additive ? -effect.ModificationValue : (1f / effect.ModificationValue);
            target.CombinedStats.ApplyModifier(StatType.APRecovery, reverseModificationType, reverseValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 에너지 회복 시스템 <color=red>원상 복구</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 