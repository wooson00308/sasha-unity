using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Handlers
{
    public class UtilityBuffHandler : IStatusEffectHandler // 범용 마커/유틸리티용
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            // 범용 핸들러이므로, 효과 이름(effect.EffectName)을 로그에 포함하여 어떤 효과인지 명시
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체에 <color=yellow>{effect.EffectName}</color> 효과 <color=green>적용</color>. (지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }
        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }
        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체의 <color=yellow>{effect.EffectName}</color> 효과 <color=yellow>만료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }
        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체의 <color=yellow>{effect.EffectName}</color> 효과 <color=red>해제</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 