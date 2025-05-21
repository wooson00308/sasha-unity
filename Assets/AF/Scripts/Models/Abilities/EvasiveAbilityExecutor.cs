using AF.Combat;
using AF.Models;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_LG_001_Evasive" : 영구적으로 Evasion +10%.
    /// 전투 개시 시 1회만 적용되면 충분하므로 중복 검사 후 무한 버프 부여.
    /// </summary>
    public class EvasiveAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_LG_001_Evasive";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null) return false;
            if (user.HasStatusEffect("EvasivePassive")) return false;

            var buff = new StatusEffect(
                effectName: "EvasivePassive",
                durationTurns: -1, // 영구
                effectType: StatusEffectEvents.StatusEffectType.Buff_EvasionBoost, // 수정된 EffectType 사용
                statToModify: StatType.Evasion,
                modType: ModificationType.Additive,
                modValue: 0.1f);

            user.AddStatusEffect(buff);
            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null) return false;
            if (user.HasStatusEffect("EvasivePassive")) return false;
            return true;
        }
    }
} 