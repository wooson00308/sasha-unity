using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;
using AF.AI.UtilityAI;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.Defense 전용 전략</summary>
    public sealed class DefenseCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override IUtilityAction
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return null;

            // 1) 먼저 방어 시도
            if (activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                !ctx.HasUnitDefendedThisTurn(activeUnit))
            {
                return null;
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
                return null;
            }

            // === 추가된 부분: 방어/재장전 외 다른 행동 시도 ===
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            string unitName = activeUnit?.Name ?? "Unknown";
            //textLogger?.Log($"[{unitName} (Defense)] 방어/재장전 외 표준 행동 시도.", LogLevel.Debug);
            return null;
            // ============================================
        }
    }
}
