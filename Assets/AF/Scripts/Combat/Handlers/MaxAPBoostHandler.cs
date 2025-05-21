using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Handlers
{
    public class MaxAPBoostHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.MaxAP, ModificationType.Additive, effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 최대 AP <color=green>증가</color>. (효과: {effect.ModificationValue}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            // MaxAPBoost는 Tick에서 별도 처리 없음
        }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 최대 AP 증가 효과 <color=yellow>만료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            target.CombinedStats.ApplyModifier(StatType.MaxAP, ModificationType.Additive, -effect.ModificationValue);
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 최대 AP 증가 효과 <color=red>해제</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
}