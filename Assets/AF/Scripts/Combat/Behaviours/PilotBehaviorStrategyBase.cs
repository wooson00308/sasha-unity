using AF.Combat;
using AF.Services;
using AF.Models;
using UnityEngine;

namespace AF.Combat.Behaviors
{
    /// <summary>
    /// 전략 공통 로직(상수, AP 계산 등)을 담은 베이스 클래스
    /// </summary>
    public abstract class PilotBehaviorStrategyBase : IPilotBehaviorStrategy
    {
        // === 공통 상수 ===
        protected const float DEFEND_AP_COST            = 1f;
        protected const float MIN_RANGED_SAFE_DISTANCE  = 2.0f;
        protected const float OPTIMAL_RANGE_FACTOR      = 0.8f;

        // === 필수 구현 ===
        public abstract (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService context);

        // === 공통 유틸 ===
        protected float CalculateMoveAPCost(ArmoredFrame unit)
        {
            if (unit == null) return float.MaxValue;

            const float baseMoveCost        = 1.0f;
            const float weightPenaltyFactor = 0.01f;
            const float speedBonusFactor    = 0.05f;

            float weightPenalty = unit.TotalWeight * weightPenaltyFactor;
            float speedBonus    = unit.CombinedStats.Speed * speedBonusFactor;

            return Mathf.Max(0.5f, baseMoveCost + weightPenalty - speedBonus);
        }

        protected float CalculateAttackAPCost(ArmoredFrame unit, Weapon weapon)
        {
            if (unit == null || weapon == null) return 999f;

            float efficiency = Mathf.Max(0.1f, unit.CombinedStats.EnergyEfficiency);
            float cost       = weapon.BaseAPCost / efficiency;
            return Mathf.Max(0.5f, cost);
        }
    }
}
