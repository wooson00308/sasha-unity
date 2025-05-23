using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Handlers
{
    public class EvasionBoostHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.Evasion, effect.ModificationType, effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"기동 시스템 <color=green>강화</color>. 회피율 <color=green>증가</color>. (효과: {effect.ModificationValue:P0}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"기동 강화 효과 <color=yellow>만료</color>. 회피율 표준값 복귀.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            var reverseModificationType = effect.ModificationType == ModificationType.Additive ? ModificationType.Additive : ModificationType.Multiplicative; 
            var reverseValue = effect.ModificationType == ModificationType.Additive ? -effect.ModificationValue : (1f / effect.ModificationValue); 
            target.CombinedStats.ApplyModifier(StatType.Evasion, reverseModificationType, reverseValue);
            ctx.Logger.TextLogger.Log($"기동 강화 시스템 <color=red>해제</color>. 회피율 조정됨.", LogLevel.Debug, LogEventType.StatusEffectRemoved);
        }
    }
} 