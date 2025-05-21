using AF.Combat;
using AF.Models;
using AF.Data;

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
                durationTurns: 2,
                effectType: StatusEffectEvents.StatusEffectType.Buff_AccuracyBoost,
                statToModify: StatType.Accuracy,
                modType: ModificationType.Additive,
                modValue: 0.3f);

            user.AddStatusEffect(zoomBuff);

            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;
            if (user.HasStatusEffect("ZoomBuff")) return false;
            if (!user.HasEnoughAP(data.APCost)) return false;

            // 주무기가 사용 가능한 상태인지 확인
            Weapon primaryWeapon = user.GetPrimaryWeapon();
            if (primaryWeapon == null || primaryWeapon.IsReloading || !primaryWeapon.HasAmmo())
            {
                return false; // 주무기 발사 불가능하면 정밀 조준 사용 불가
            }

            return true;
        }
    }
} 