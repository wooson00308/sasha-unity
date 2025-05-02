using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;
using System.Collections.Generic;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.StandardCombat 전용 전략</summary>
    public sealed class StandardCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatContext context)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return (default, null, null, null);

            // === 가장 가까운 적 계산 ===
            var enemies = context.Simulator.GetEnemies(activeUnit);
            if (enemies.Count == 0)
                return (CombatActionEvents.ActionType.None, null, null, null);

            ArmoredFrame closest = null;
            float minDist = float.MaxValue;
            foreach (var e in enemies)
            {
                float d = Vector3.Distance(activeUnit.Position, e.Position);
                if (d < minDist) { minDist = d; closest = e; }
            }

            // === 0. 재장전 필요? ===
            var reloadW = activeUnit.GetAllWeapons()
                                    .Where(w => w.IsOperational &&
                                                w.MaxAmmo > 0 &&
                                                !w.HasAmmo() &&
                                                !w.IsReloading)
                                    .OrderByDescending(w => w.Damage)
                                    .FirstOrDefault();
            if (reloadW != null &&
                activeUnit.HasEnoughAP(reloadW.ReloadAPCost))
            {
                return (CombatActionEvents.ActionType.Reload, null, null, reloadW);
            }

            // === 1. 근접 시도 ===
            var meleeW = activeUnit.GetAllWeapons()
                                   .FirstOrDefault(w => w.Type == WeaponType.Melee &&
                                                        w.IsOperational);
            if (meleeW != null)
            {
                bool inRange = minDist <= meleeW.Range;
                bool canAtk  = inRange &&
                               activeUnit.HasEnoughAP(CalculateAttackAPCost(activeUnit, meleeW)) &&
                               !meleeW.IsReloading &&
                               meleeW.HasAmmo();
                if (canAtk)
                    return (CombatActionEvents.ActionType.Attack, closest, null, meleeW);
            }

            // === 2. 원거리 시도 ===
            var rangedW = activeUnit.GetAllWeapons()
                                    .Where(w => w.Type != WeaponType.Melee &&
                                                w.IsOperational &&
                                                !w.IsReloading &&
                                                w.HasAmmo())
                                    .OrderByDescending(w => w.Damage)
                                    .FirstOrDefault();
            if (rangedW != null)
            {
                bool inRange = minDist <= rangedW.Range + 0.001f;
                bool canAtk  = inRange &&
                               activeUnit.HasEnoughAP(CalculateAttackAPCost(activeUnit, rangedW));
                if (canAtk)
                    return (CombatActionEvents.ActionType.Attack, closest, null, rangedW);
            }

            // === 3. 이동 ===
            float mvCost = CalculateMoveAPCost(activeUnit);
            if (activeUnit.HasEnoughAP(mvCost))
            {
                if (context.MovedThisActivation.Contains(activeUnit))
                {
                    // 이미 이동했다면, 다른 행동(예: 방어)을 고려하도록 이동 결정 안 함
                }
                else
                {
                    if (rangedW != null) // 원거리 보유
                    {
                        float optimal = rangedW.Range * OPTIMAL_RANGE_FACTOR;

                        if (minDist < MIN_RANGED_SAFE_DISTANCE)
                        {
                            Vector3 dir = (minDist < 0.01f) ? Vector3.back :
                                          (activeUnit.Position - closest.Position).normalized;
                            Vector3 retreat = activeUnit.Position + dir * activeUnit.CombinedStats.Speed;
                            return (CombatActionEvents.ActionType.Move, closest, retreat, null);
                        }
                        else if (minDist > optimal)
                        {
                            Vector3 dir = (closest.Position - activeUnit.Position).normalized;
                            float distToMove = Mathf.Min(activeUnit.CombinedStats.Speed, minDist - optimal);
                            Vector3 approach = activeUnit.Position + dir * distToMove;
                            return (CombatActionEvents.ActionType.Move, closest, approach, null);
                        }
                    }
                    else if (meleeW != null) // 근접 전용
                    {
                        if (minDist > meleeW.Range)
                            return (CombatActionEvents.ActionType.Move, closest, closest.Position, null);
                    }
                }
            }

            // === 4. 방어 ===
            if (activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                !context.Simulator.HasUnitDefendedThisTurn(activeUnit) &&
                (!context.MovedThisActivation.Contains(activeUnit) || IsLowHealth(activeUnit)))
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            return (default, null, null, null);
        }
    }
}
