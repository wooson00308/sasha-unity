using AF.Combat;
using AF.Models;
using System.Linq;
using UnityEngine; // LogLevelのため

namespace AF.Combat.Handlers
{
    /// <summary>
    /// Handles "RepairOverTime" effects.
    /// </summary>
    public class RepairOverTimeHandler : IStatusEffectHandler
    {
        public void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체에 <color=green>나노 수리봇</color> 활성화. (틱당 회복량: {effect.TickValue}, 지속시간: {effect.DurationTurns}턴)", LogLevel.Info, LogEventType.StatusEffectApplied);
        }

        public void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            if (target == null || !target.IsOperational || effect == null) return;

            float repairAmount = effect.TickValue; // Assuming TickValue is repair per tick

            // find most damaged operational part
            string targetPartSlot = target.Parts
                .Where(kvp => kvp.Value.IsOperational && kvp.Value.CurrentDurability < kvp.Value.MaxDurability)
                .OrderBy(kvp => kvp.Value.CurrentDurability / kvp.Value.MaxDurability)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(targetPartSlot))
            {
                // No repairable part, or already at max durability
                // ctx.EventBus.Publish(new CombatLogEvents.SystemMessageEvent($"{target.Name} has no damaged parts to repair with {effect.EffectName}."));
                return;
            }

            float actualRepairedAmount = target.ApplyRepair(targetPartSlot, repairAmount);

            if (actualRepairedAmount > 0)
            {
                // OnTick의 로그는 너무 빈번할 수 있으므로, 실제 회복이 발생했을 때 상태 효과 Tick 이벤트만 발생시킴.
                // 필요하다면 여기에 로그 추가 가능 (예: $"{target.Name}의 {targetPartSlot} 부위 {actualRepairedAmount}만큼 수리됨.")
                ctx.Bus.Publish(new StatusEffectEvents.StatusEffectTickEvent(target, effect));
                ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 나노 수리봇 작동: 기체 내구도 <color=green>{actualRepairedAmount} 회복</color>.", LogLevel.Info, LogEventType.StatusEffectTicked);
            }
        }

        public void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 나노 수리봇 활동 <color=yellow>종료</color>.", LogLevel.Info, LogEventType.StatusEffectExpired);
        }

        public void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect)
        {
            ctx.Logger.TextLogger.Log($"<color=#00FFFF>{target.Name}</color> 기체 나노 수리봇 <color=red>강제 비활성화</color>.", LogLevel.Info, LogEventType.StatusEffectRemoved);
        }
    }
} 