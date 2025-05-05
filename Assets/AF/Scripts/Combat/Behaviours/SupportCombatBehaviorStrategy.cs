using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;
using System.Collections.Generic;
using System.Linq;
using AF.AI.UtilityAI;

namespace AF.Combat.Behaviors
{
    /// <summary>SpecializationType.Support 전용 전략</summary>
    public sealed class SupportCombatBehaviorStrategy : PilotBehaviorStrategyBase
    {
        public override IUtilityAction
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService ctx)
        {
            if (activeUnit == null || !activeUnit.IsOperational)
                return null;

            // HP 가장 낮은 아군
            ArmoredFrame lowest = FindLowestHPDamagedAlly(activeUnit, ctx);
            float repairSelfCost  = 2.0f;
            float repairAllyCost  = 2.5f;

            Part body = activeUnit.GetPart("Body");
            bool selfRepair = body != null &&
                              body.CurrentDurability < body.MaxDurability &&
                              activeUnit.HasEnoughAP(repairSelfCost);

            if (lowest != null && activeUnit.HasEnoughAP(repairAllyCost))
                return null;

            if (selfRepair)
                return null;

            // 그 외엔 표준
            return null;
        }

        private ArmoredFrame FindLowestHPDamagedAlly(ArmoredFrame unit, CombatSimulatorService ctx)
        {
            ArmoredFrame lowest = null;
            float ratio = float.MaxValue;

            var allies = ctx.GetAllies(unit);
            foreach (var a in allies)
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
