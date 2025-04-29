using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.Defense 전용 전략</summary>
    public sealed class DefenseCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return (default, null, null, null);

            // 1) 먼저 방어 시도
            if (activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                !ctx.HasUnitDefendedThisTurn(activeUnit))
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            // 2) 불가하면 재장전 필요?
            var reloadWeapon = activeUnit.GetAllWeapons()
                                         .Where(w => w.IsOperational &&
                                                     w.MaxAmmo > 0 &&
                                                     !w.HasAmmo() &&
                                                     !w.IsReloading)
                                         .OrderByDescending(w => w.Damage)
                                         .FirstOrDefault();
            if (reloadWeapon != null &&
                activeUnit.HasEnoughAP(reloadWeapon.ReloadAPCost))
            {
                return (CombatActionEvents.ActionType.Reload, null, null, reloadWeapon);
            }

            return (default, null, null, null);
        }
    }
}
