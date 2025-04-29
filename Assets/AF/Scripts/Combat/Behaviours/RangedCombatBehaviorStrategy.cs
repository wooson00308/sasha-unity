using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.RangedCombat 전용 전략</summary>
    public sealed class RangedCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return (default, null, null, null);

            var enemies = ctx.GetEnemies(activeUnit);
            if (enemies.Count == 0)
                return (default, null, null, null);

            ArmoredFrame closest = null;
            float minDist = float.MaxValue;
            foreach (var e in enemies)
            {
                float d = Vector3.Distance(activeUnit.Position, e.Position);
                if (d < minDist) { minDist = d; closest = e; }
            }

            // 1) 탄 없고 재장전 필요?
            var reloadTarget = activeUnit.GetAllWeapons()
                                         .Where(w => w.Type != WeaponType.Melee &&
                                                     w.IsOperational &&
                                                     w.MaxAmmo > 0 &&
                                                     !w.HasAmmo() &&
                                                     !w.IsReloading)
                                         .OrderByDescending(w => w.Damage)
                                         .FirstOrDefault();
            if (reloadTarget != null &&
                activeUnit.HasEnoughAP(reloadTarget.ReloadAPCost))
            {
                return (CombatActionEvents.ActionType.Reload, null, null, reloadTarget);
            }

            // 2) 가장 좋은 발사 가능한 원거리 무기
            var fireWeapon = activeUnit.GetAllWeapons()
                                       .Where(w => w.Type != WeaponType.Melee &&
                                                   w.IsOperational &&
                                                   !w.IsReloading &&
                                                   w.HasAmmo())
                                       .OrderByDescending(w => w.Damage)
                                       .FirstOrDefault();
            if (fireWeapon == null)
                return (default, null, null, null);  // 무기 없음

            float attackCost = CalculateAttackAPCost(activeUnit, fireWeapon);
            bool inRange = minDist <= fireWeapon.Range + 0.001f;

            // 3) 사거리&AP 되면 발사
            if (inRange && activeUnit.HasEnoughAP(attackCost))
                return (CombatActionEvents.ActionType.Attack, closest, null, fireWeapon);

            // 4) 위치 조정
            float moveCost = CalculateMoveAPCost(activeUnit);
            if (activeUnit.HasEnoughAP(moveCost))
            {
                // a) 너무 가깝냐?
                if (minDist < MIN_RANGED_SAFE_DISTANCE)
                {
                    Vector3 dir = (minDist < 0.01f)
                                  ? Vector3.back
                                  : (activeUnit.Position - closest.Position).normalized;
                    Vector3 retreat = activeUnit.Position + dir * activeUnit.CombinedStats.Speed;
                    return (CombatActionEvents.ActionType.Move, closest, retreat, null);
                }
                // b) 너무 멀다
                float optimalRange = fireWeapon.Range * OPTIMAL_RANGE_FACTOR;
                if (minDist > optimalRange)
                {
                    Vector3 dir = (closest.Position - activeUnit.Position).normalized;
                    float distToMove = Mathf.Min(activeUnit.CombinedStats.Speed, minDist - optimalRange);
                    Vector3 approach = activeUnit.Position + dir * distToMove;
                    return (CombatActionEvents.ActionType.Move, closest, approach, null);
                }
            }

            // 5) 방어
            bool defendable = activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                              !ctx.HasUnitDefendedThisTurn(activeUnit);
            return defendable
                ? (CombatActionEvents.ActionType.Defend, null, null, null)
                : (default, null, null, null);
        }
    }
}
