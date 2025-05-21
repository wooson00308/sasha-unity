using AF.Combat;
using AF.Models;
using UnityEngine; // LogLevelのため

namespace AF.Combat.Handlers
{
    public class ShieldGeneratorHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect) 
        { 
            if (target == null || effect == null) return;

            float shieldHp = effect.ModificationValue;
            if (shieldHp <= 0f) shieldHp = 0f; // 음수 값 방지
            target.AddShield(shieldHp);

            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 기체에 <color=blue>실드</color> 생성. (실드량: {shieldHp}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }
        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }
        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect) 
        { 
            if (target == null) return;
            float oldShieldValue = target.CurrentShieldHP; // 사라지기 전 값 참조
            target.ClearShield();

            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 기체 실드 효과 <color=yellow>만료</color>. 실드 소멸.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }
        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect) 
        { 
            if (target == null) return;
            float oldShieldValue = target.CurrentShieldHP; // 사라지기 전 값 참조
            target.ClearShield();
            
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 기체 실드 효과 <color=red>강제 해제</color>. 실드 즉시 소멸.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 