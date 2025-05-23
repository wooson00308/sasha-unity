using AF.Combat;
using AF.Models;
using AF.Data;
using System.Linq;
using UnityEngine;

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

            // SASHA FIX: 적이 근처에 있을 때만 정밀 조준 사용 (균형 조정)
            if (ctx?.TeamAssignments == null || ctx?.Participants == null) return false;

            // 사용자의 팀 ID 획득
            if (!ctx.TeamAssignments.TryGetValue(user, out int userTeam)) 
                return false;

            // 적 목록 획득 (다른 팀의 작전 가능한 유닛들)
            var enemies = ctx.Participants
                .Where(p => p.IsOperational && 
                           ctx.TeamAssignments.TryGetValue(p, out int pTeam) && 
                           pTeam != userTeam)
                .ToList();

            if (!enemies.Any()) return false;

            var nearestEnemy = enemies
                .Where(e => e.IsOperational)
                .OrderBy(e => Vector3.Distance(user.Position, e.Position))
                .FirstOrDefault();

            if (nearestEnemy == null) return false;

            float distanceToNearestEnemy = Vector3.Distance(user.Position, nearestEnemy.Position);
            float weaponRange = primaryWeapon.MaxRange;

            // 조건 추가: 적이 무기 사거리의 90% 이내에 있을 때만 정밀 조준 사용
            if (distanceToNearestEnemy > weaponRange * 0.9f) return false;

            // 추가 조건: 다음 공격할 충분한 AP가 있어야 함 (정밀 조준 후 바로 공격 가능)
            float attackAPCost = ctx?.ActionExecutor?.GetActionAPCost(
                CombatActionEvents.ActionType.Attack, user, null, primaryWeapon) ?? 2.0f;
            
            if (user.CurrentAP < data.APCost + attackAPCost) return false;

            return true;
        }
    }
} 