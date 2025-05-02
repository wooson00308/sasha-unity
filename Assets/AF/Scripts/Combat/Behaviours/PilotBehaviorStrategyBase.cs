using AF.Combat;
using AF.Services;
using AF.Models;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

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
        protected const float CLOSE_RANGE_THRESHOLD     = 0.6f;

        // === 필수 구현 ===
        public abstract (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatContext context);

        // 새로운 비동기 메서드 (virtual로 선언하여 하위 클래스에서 필요시 재정의 가능)
        public virtual async UniTask<(CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)>
            DetermineActionAsync(ArmoredFrame activeUnit, CombatContext context, CancellationToken cancellationToken = default)
        {
            // 기본 구현: 기존 동기 메서드 호출
            var result = DetermineAction(activeUnit, context);
            return await UniTask.FromResult(result); // 결과를 UniTask로 래핑하여 반환
        }

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

        // +++ 체력 확인 헬퍼 메서드 추가 +++
        protected bool IsLowHealth(ArmoredFrame unit, float threshold = 0.3f)
        {
            if (unit == null) return false;
            Part bodyPart = unit.GetPart("Body"); // "Body" 슬롯 가정
            if (bodyPart != null && bodyPart.MaxDurability > 0)
            {
                // 현재 내구도 비율이 임계값보다 낮은지 확인
                return (bodyPart.CurrentDurability / bodyPart.MaxDurability) < threshold;
            }
            // Body 파츠가 없거나 최대 내구도가 0 이하면 체력 낮음으로 판단하지 않음
            return false;
        }
        // +++ 체력 확인 헬퍼 메서드 추가 끝 +++
    }
}
