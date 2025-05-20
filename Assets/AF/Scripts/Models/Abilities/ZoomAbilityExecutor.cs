using AF.Combat;
using AF.Models;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_HD_001_Zoom" 어빌리티 실행 로직.
    /// 1턴 동안 정확도를 30%p 증가시키는 버프를 부여합니다.
    /// </summary>
    public class ZoomAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_HD_001_Zoom";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || !user.IsOperational) return false;

            // 이미 버프가 있는지 체크 (중첩 방지)
            if (user.HasStatusEffect("ZoomBuff"))
            {
                return false; // 중복 적용 방지
            }

            var zoomBuff = new StatusEffect(
                effectName: "ZoomBuff",
                durationTurns: 1,
                effectType: StatusEffectEvents.StatusEffectType.Buff_AccuracyBoost,
                statToModify: StatType.Accuracy,
                modType: ModificationType.Additive,
                modValue: 0.3f);

            user.AddStatusEffect(zoomBuff);

            ctx?.Bus?.Publish(new StatusEffectEvents.StatusEffectAppliedEvent(
                target: user,
                source: user,
                effectType: StatusEffectEvents.StatusEffectType.Buff_AccuracyBoost,
                duration: 1,
                magnitude: 0.3f,
                effectId: "ZoomBuff"));

            return true;
        }
    }
} 