using AF.Combat;
using AF.Models;
using AF.Data;
using UnityEngine;
using System.Linq;

namespace AF.Models.Abilities
{
    /// <summary>
    /// "AB_BP_002_EnergyShield" : 3턴 동안 100 HP 흡수 실드 부여.
    /// 현재는 StatusEffect 사용해 데미지 흡수 로직은 TODO.
    /// </summary>
    public class EnergyShieldAbilityExecutor : IAbilityEffectExecutor
    {
        public const string ABILITY_ID = "AB_BP_002_EnergyShield";

        public bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData)
        {
            if (user == null || !user.IsOperational) return false;
            if (user.HasStatusEffect("EnergyShield")) return false;

            var shield = new StatusEffect(
                effectName: "EnergyShield",
                durationTurns: 3,
                effectType: StatusEffectEvents.StatusEffectType.Buff_ShieldGenerator,
                statToModify: StatType.None,
                modType: ModificationType.None,
                modValue: 100f);
            user.AddStatusEffect(shield);

            return true;
        }

        public bool CanExecute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilitySO data)
        {
            if (user == null || data == null) return false;
            if (user.HasStatusEffect("EnergyShield")) return false;
            if (!user.HasEnoughAP(data.APCost)) return false;

            // SASHA FIX: 전략적 상황에서만 실드 사용 (균형 조정)
            
            // 1. 체력이 80% 이하일 때 (90% → 80%로 조정)
            float healthRatio = user.CurrentDurabilityRatio;
            if (healthRatio <= 0.8f) return true;

            // 2. 적이 가까이 있고 실제 위험할 때만 방어적으로 실드 사용
            if (ctx?.TeamAssignments != null && ctx?.Participants != null)
            {
                // 사용자의 팀 ID 획득
                if (!ctx.TeamAssignments.TryGetValue(user, out int userTeam)) 
                    return false;

                // 적 목록 획득 (다른 팀의 작전 가능한 유닛들)
                var enemies = ctx.Participants
                    .Where(p => p.IsOperational && 
                               ctx.TeamAssignments.TryGetValue(p, out int pTeam) && 
                               pTeam != userTeam)
                    .ToList();

                if (enemies.Any())
                {
                    var threateningEnemies = enemies
                        .Where(e => 
                        {
                            var enemyWeapon = e.GetPrimaryWeapon();
                            if (enemyWeapon == null || enemyWeapon.IsReloading || !enemyWeapon.HasAmmo()) 
                                return false;
                            
                            float distance = Vector3.Distance(user.Position, e.Position);
                            // 적이 매우 가까이 있을 때만 (사거리의 60% 이내)
                            return distance <= enemyWeapon.MaxRange * 0.6f;
                        })
                        .ToList();

                    // 위협적인 적이 매우 가까이 있고, 체력이 90% 이하일 때만 실드 사용
                    if (threateningEnemies.Any() && healthRatio <= 0.9f) return true;
                }
            }

            return false; // 그 외의 경우 실드 사용 안함
        }
    }
} 