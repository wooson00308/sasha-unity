using System.Collections.Generic;
using AF.EventBus;
using AF.Models;
using System.Linq;
using AF.Combat.Handlers;
using AF.Services; // IService 네임스페이스
// using AF.Combat.Events;   // StatusEffectEvents is in AF.Combat

namespace AF.Combat
{
    public interface IStatusEffectProcessor : IService // IService 상속 추가
    {
        void Tick(CombatContext ctx, ArmoredFrame unit);
        void RegisterHandler(StatusEffectEvents.StatusEffectType type, IStatusEffectHandler handler);
        void UnregisterHandler(StatusEffectEvents.StatusEffectType type);
        // +++ 핸들러 직접 가져오기 위한 메서드 (ArmoredFrame에서 사용 목적) +++
        IStatusEffectHandler GetHandler(StatusEffectEvents.StatusEffectType type);
    }

    public sealed class StatusEffectProcessor : IStatusEffectProcessor // IService는 인터페이스를 통해 이미 상속됨
    {
        private readonly Dictionary<StatusEffectEvents.StatusEffectType, IStatusEffectHandler> _handlers =
            new Dictionary<StatusEffectEvents.StatusEffectType, IStatusEffectHandler>();

        public StatusEffectProcessor()
        {
            _handlers[StatusEffectEvents.StatusEffectType.Debuff_DamageOverTime] = new DamageOverTimeHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_RepairOverTime] = new RepairOverTimeHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_DefenseBoost] = new DefenseBoostHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_MaxAPBoost] = new MaxAPBoostHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_APRecoveryBoost] = new APRecoveryBoostHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_EvasionBoost] = new EvasionBoostHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Debuff_DefenseReduced] = new DefenseReducedHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_ShieldGenerator] = new ShieldGeneratorHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_AccuracyBoost] = new AccuracyBoostHandler();
            _handlers[StatusEffectEvents.StatusEffectType.Buff_Utility] = new UtilityBuffHandler();
        }

        // IService 구현
        public void Initialize()
        {
            // 필요한 경우 초기화 로직 추가 (현재는 비워둠)
            UnityEngine.Debug.Log("StatusEffectProcessor initialized.");
        }

        public void Shutdown()
        {
            // 필요한 경우 종료 로직 추가 (핸들러 클리어 등, 현재는 비워둠)
            _handlers.Clear();
            UnityEngine.Debug.Log("StatusEffectProcessor shutdown.");
        }

        public void RegisterHandler(StatusEffectEvents.StatusEffectType type, IStatusEffectHandler handler)
        {
            _handlers[type] = handler;
        }

        public void UnregisterHandler(StatusEffectEvents.StatusEffectType type)
        {
            _handlers.Remove(type);
        }

        // +++ 핸들러 가져오기 메서드 구현 +++
        public IStatusEffectHandler GetHandler(StatusEffectEvents.StatusEffectType type)
        {
            _handlers.TryGetValue(type, out IStatusEffectHandler handler);
            return handler;
        }

        public void Tick(CombatContext ctx, ArmoredFrame unit)
        {
            if (unit == null || !unit.IsOperational) return;

            var activeEffects = new List<StatusEffect>(unit.ActiveStatusEffects);
            if (activeEffects.Count == 0) return;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                if (effect == null) continue;

                if (_handlers.TryGetValue(effect.EffectType, out IStatusEffectHandler handler))
                {
                    handler.OnTick(ctx, unit, effect);
                }
                else
                {
                    // Debug.LogWarning($"No handler registered for effect type: {effect.EffectType} on effect {effect.EffectName}");
                }

                if (effect.DurationTurns > 0)
                {
                    effect.DurationTurns--;
                    if (effect.DurationTurns <= 0)
                    {
                        if (_handlers.TryGetValue(effect.EffectType, out IStatusEffectHandler expiredHandler))
                        {
                            expiredHandler.OnExpire(ctx, unit, effect);
                        }
                        
                        // RemoveStatusEffect 내부에서 OnRemove 및 StatusEffectExpiredEvent를 발행하므로 중복 발행 방지
                        unit.RemoveStatusEffect(effect.EffectName);
                    }
                }
            }
        }
    }
}
