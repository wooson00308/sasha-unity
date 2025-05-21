using AF.Models;
using AF.Combat; // LogEventType enumのため
using UnityEngine; // LogLevelのため

namespace AF.Combat.Handlers
{
    public class AccuracyBoostHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            // Accuracy 스탯은 RecalculateStats 내부에서 여러 요소를 종합하여 계산되므로,
            // 여기서는 직접 스탯을 변경하기보다 RecalculateStats() 호출에 의존할 수 있음.
            // 이 핸들러는 스탯 직접 변경 로직 없이 로그만 남기는 것으로 가정.
            // 만약 스탯 변경이 필요하다면 target.CombinedStats.ApplyModifier(...) 와 target.RecalculateStats() 호출 추가.
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 조준 시스템 <color=green>보정</color>. 명중률 <color=green>상승</color>. (효과: {effect.ModificationValue:P0}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect) { }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 조준 시스템 보정 효과 <color=yellow>만료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Pilot.Name}</color> 조준 시스템 보정 <color=red>해제</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 