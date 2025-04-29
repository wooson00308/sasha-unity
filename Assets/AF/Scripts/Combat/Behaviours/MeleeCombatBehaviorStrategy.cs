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
            if (activeUnit == null || !activeUnit.IsOperational) 
                return (default, null, null, null);

            // 가장 가까운 적 계산
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

            // 1) 재장전 우선
            var reloadTarget = activeUnit.GetAllWeapons()
                                         .FirstOrDefault(w => w.Type == WeaponType.Melee &&
                                                              w.IsOperational &&
                                                              w.MaxAmmo > 0 &&
                                                              !w.HasAmmo() &&
                                                              !w.IsReloading);
            if (reloadTarget != null && 
                activeUnit.HasEnoughAP(reloadTarget.ReloadAPCost))
            {
                return (CombatActionEvents.ActionType.Reload, null, null, reloadTarget);
            }

            // 2) 근거리 즉시 공격
            if (minDist < 0.1f)
            {
                var meleeWpn = activeUnit.EquippedWeapons
                                         .FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
                if (meleeWpn != null)
                {
                    bool can = activeUnit.HasEnoughAP(CalculateAttackAPCost(activeUnit, meleeWpn)) &&
                               !meleeWpn.IsReloading &&
                               meleeWpn.HasAmmo();
                    if (can)
                        return (CombatActionEvents.ActionType.Attack, closest, null, meleeWpn);
                }

                // 3) 방어
                bool defendable = activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                                  !ctx.HasUnitDefendedThisTurn(activeUnit);
                return defendable
                    ? (CombatActionEvents.ActionType.Defend, null, null, null)
                    : (default, null, null, null);
            }

            // 4) 사거리 안이라면 공격, 아니면 접근
            var nearWeapon = activeUnit.EquippedWeapons
                                       .FirstOrDefault(w => w.Type == WeaponType.Melee && w.IsOperational);
            if (nearWeapon != null)
            {
                bool inRange = minDist <= nearWeapon.Range &&
                               activeUnit.HasEnoughAP(CalculateAttackAPCost(activeUnit, nearWeapon)) &&
                               !nearWeapon.IsReloading &&
                               nearWeapon.HasAmmo();
                if (inRange)
                    return (CombatActionEvents.ActionType.Attack, closest, null, nearWeapon);
            }

            // 5) 이동
            if (activeUnit.HasEnoughAP(CalculateMoveAPCost(activeUnit)))
            {
                return (CombatActionEvents.ActionType.Move, closest, closest.Position, null);
            }

            // 6) 마지막 방어 시도
            if (activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                !ctx.HasUnitDefendedThisTurn(activeUnit))
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            return (default, null, null, null);
        }
    }
}
