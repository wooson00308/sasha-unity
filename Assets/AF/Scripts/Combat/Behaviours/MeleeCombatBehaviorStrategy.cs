using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.MeleeCombat 전용 전략</summary>
    public sealed class MeleeCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            string unitName = activeUnit?.Name ?? "Unknown";

            if (activeUnit == null || !activeUnit.IsOperational)
            {
                return (default, null, null, null);
            }


            // === 가장 가까운 '작동 가능한' 적 계산 ===
            var enemies = ctx.GetEnemies(activeUnit);
            if (enemies.Count == 0)
            {
                // Fall through to defense / end turn
            }

            ArmoredFrame closestOperationalEnemy = null; // Renamed variable
            float minDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || !e.IsOperational) continue; // *** ADDED CHECK ***

                float d = Vector3.Distance(activeUnit.Position, e.Position);
                if (d < minDist)
                {
                    minDist = d;
                    closestOperationalEnemy = e; // Use new variable name
                }
            }

            if (closestOperationalEnemy == null)
            {
                // Fall through to defense check / end turn logic below
            }


            // === 1) 재장전 우선 (근접 무기) ===
            var reloadTarget = activeUnit.GetAllWeapons()
                                         .FirstOrDefault(w => w.Type == WeaponType.Melee && // 근접 무기 재장전? (일반적이지 않지만 로직 유지)
                                                              w.IsOperational &&
                                                              w.MaxAmmo > 0 &&
                                                              !w.HasAmmo() &&
                                                              !w.IsReloading);
            if (reloadTarget != null && activeUnit.HasEnoughAP(reloadTarget.ReloadAPCost))
            {
                return (CombatActionEvents.ActionType.Reload, null, null, reloadTarget);
            }


            // === 2) 근접 공격 시도 (작동 가능한 타겟이 있을 경우) ===
            if (closestOperationalEnemy != null) // Check target validity
            {
                var meleeWeapon = activeUnit.EquippedWeapons
                                         .FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);

                if (meleeWeapon != null)
                {
                    float attackCost = CalculateAttackAPCost(activeUnit, meleeWeapon);
                    bool canAttack = minDist <= meleeWeapon.Range + 0.001f && // 이미 사거리 내인가? (약간의 오차 허용)
                                     activeUnit.HasEnoughAP(attackCost) &&
                                     !meleeWeapon.IsReloading &&
                                     meleeWeapon.HasAmmo();

                    if (canAttack)
                    {
                        return (CombatActionEvents.ActionType.Attack, closestOperationalEnemy, null, meleeWeapon);
                    }
                    else
                    {
                        // Fall through to move logic
                    }
                }
            }


            // === 3) 이동 (공격 못했고, 작동 가능한 타겟이 있을 경우) ===
            float moveCost = CalculateMoveAPCost(activeUnit);
            bool canMove = activeUnit.HasEnoughAP(moveCost);

            if (canMove && closestOperationalEnemy != null) // Check target validity
            {
                var meleeWeaponForRange = activeUnit.EquippedWeapons.FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
                float targetDistance = meleeWeaponForRange?.Range * 0.5f ?? 0.1f; // 무기 사거리의 절반 또는 최소 거리 목표

                // 이미 목표 거리보다 가까우면 이동 불필요
                if (minDist <= targetDistance + 0.01f) {
                    // Fall through to defense
                } else {
                    // 목표를 향해 이동
                    Vector3 direction = (closestOperationalEnemy.Position - activeUnit.Position).normalized;
                    Vector3 targetPos = activeUnit.Position + direction * activeUnit.CombinedStats.Speed;
                    return (CombatActionEvents.ActionType.Move, closestOperationalEnemy, targetPos, null);
                }
            }


            // === 4) 최후 방어 시도 === (Original step 6 is now step 4)
            bool canDefend = activeUnit.HasEnoughAP(DEFEND_AP_COST);
            bool defended = ctx.HasUnitDefendedThisTurn(activeUnit);

            if (canDefend && !defended)
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            return (default, null, null, null); // 모든 조건 불만족
        }
    }
}
