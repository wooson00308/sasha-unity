using AF.Models;

namespace AF.Combat.Handlers
{
    /// <summary>
    /// Defines the contract for status effect handlers.
    /// Each method corresponds to a specific lifecycle event of a status effect.
    /// </summary>
    public interface IStatusEffectHandler
    {
        /// <summary>
        /// Called when the status effect is initially applied to the target.
        /// Use this to apply immediate changes (e.g., stat modifications).
        /// </summary>
        /// <param name="ctx">The current combat context.</param>
        /// <param name="target">The ArmoredFrame affected by the status effect.</param>
        /// <param name="effect">The status effect being applied.</param>
        void OnApply(CombatContext ctx, ArmoredFrame target, StatusEffect effect);

        /// <summary>
        /// Called each turn (or tick) while the status effect is active.
        /// Use this for effects that occur over time (e.g., damage over time, healing over time).
        /// </summary>
        /// <param name="ctx">The current combat context.</param>
        /// <param name="target">The ArmoredFrame affected by the status effect.</param>
        /// <param name="effect">The active status effect.</param>
        void OnTick(CombatContext ctx, ArmoredFrame target, StatusEffect effect);

        /// <summary>
        /// Called when the status effect naturally expires (e.g., duration runs out).
        /// Use this to revert any changes made by OnApply or perform cleanup.
        /// </summary>
        /// <param name="ctx">The current combat context.</param>
        /// <param name="target">The ArmoredFrame previously affected by the status effect.</param>
        /// <param name="effect">The status effect that has expired.</param>
        void OnExpire(CombatContext ctx, ArmoredFrame target, StatusEffect effect);

        /// <summary>
        /// Called when the status effect is explicitly removed (e.g., dispelled) before its natural expiration.
        /// Use this to revert any changes made by OnApply or perform cleanup.
        /// </summary>
        /// <param name="ctx">The current combat context.</param>
        /// <param name="target">The ArmoredFrame from which the status effect is being removed.</param>
        /// <param name="effect">The status effect being removed.</param>
        void OnRemove(CombatContext ctx, ArmoredFrame target, StatusEffect effect);
    }
} 