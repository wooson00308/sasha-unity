using AF.Combat;
using AF.Models;
using AF.Data;
namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_HD_002_Analyze" 어빌리티 실행 로직.
    /// 적 유닛 방어력을 20% 감소시키는 디버프를 2턴 동안 부여합니다.
    /// </summary>
    public class AnalyzeAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_HD_002_Analyze";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || target == null) return false;
            if (!user.IsOperational || !target.IsOperational) return false;

            // 중복 적용 방지: 이미 디버프가 있는지 확인
            if (target.HasStatusEffect("ArmorBreakDebuff"))
            {
                return false;
            }

            var debuff = new StatusEffect(
                effectName: "ArmorBreakDebuff",
                durationTurns: 2,
                effectType: StatusEffectEvents.StatusEffectType.Debuff_DefenseReduced,
                statToModify: StatType.Defense,
                modType: ModificationType.Multiplicative, // 20% 감소 = 0.8배
                modValue: 0.8f);

            target.AddStatusEffect(debuff);

            ctx?.Bus?.Publish(new StatusEffectEvents.StatusEffectAppliedEvent(
                target: target,
                source: user,
                effectType: StatusEffectEvents.StatusEffectType.Debuff_DefenseReduced,
                duration: 2,
                magnitude: -0.2f,
                effectId: "ArmorBreakDebuff"));

            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || target == null || data == null) return false;
            if (!user.IsOperational || !target.IsOperational) return false;
            // 어빌리티 실행에 필요한 AP가 충분한지 확인
            if (!user.HasEnoughAP(data.APCost)) return false;
            return true;
        }
    }
} 