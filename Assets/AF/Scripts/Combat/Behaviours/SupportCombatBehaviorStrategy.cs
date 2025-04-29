using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.Support 전용 전략</summary>
    public sealed class SupportCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return (default, null, null, null);

            // HP 가장 낮은 아군
            ArmoredFrame lowest = FindLowestHPDamagedAlly(activeUnit, ctx);
            float repairSelfCost  = 2.0f;
            float repairAllyCost  = 2.5f;

            Part body = activeUnit.GetPart("Body");
            bool selfRepair = body != null &&
                              body.CurrentDurability < body.MaxDurability &&
                              activeUnit.HasEnoughAP(repairSelfCost);

            if (lowest != null && activeUnit.HasEnoughAP(repairAllyCost))
                return (CombatActionEvents.ActionType.RepairAlly, lowest, null, null);

            if (selfRepair)
                return (CombatActionEvents.ActionType.RepairSelf, activeUnit, null, null);

            // 그 외엔 표준
            return new StandardCombatBehaviorStrategy()
                   .DetermineAction(activeUnit, ctx);
        }

        private ArmoredFrame FindLowestHPDamagedAlly(ArmoredFrame unit, CombatSimulatorService ctx)
        {
            ArmoredFrame lowest = null;
            float ratio = float.MaxValue;

            foreach (var a in ctx.GetAllies(unit))
            {
                if (a == unit || a == null || !a.IsOperational) continue;
                var body = a.GetPart("Body");
                if (body == null || body.MaxDurability <= 0) continue;

                float r = body.CurrentDurability / body.MaxDurability;
                if (r < 1.0f && r < ratio)
                {
                    ratio = r;
                    lowest = a;
                }
            }
            return lowest;
        }
    }
}
