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
            // ServiceLocator를 통해 TextLogger 가져오기
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            string unitName = activeUnit?.Name ?? "Unknown";

            if (activeUnit == null || !activeUnit.IsOperational)
            {
                return (default, null, null, null);
            }

            // === 가장 가까운 '작동 가능한' 적 계산 ===
            var enemies = ctx.GetEnemies(activeUnit);
            if (enemies.Count == 0)
            {
                // 작동 가능한 적이 없을 때 방어 시도 로직으로 이동
            }

            ArmoredFrame closestOperationalEnemy = null;
            float minDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || !e.IsOperational) continue;

                float d = Vector3.Distance(activeUnit.Position, e.Position);
                if (d < minDist)
                {
                    minDist = d;
                    closestOperationalEnemy = e;
                }
            }

            if (closestOperationalEnemy == null)
            {
                // Fall through to defense check / end turn logic below
            }


            // === 1) 탄 없고 재장전 필요? (원거리 무기 우선) ===
            var reloadTarget = activeUnit.GetAllWeapons()
                                        .Where(w => w.Type != WeaponType.Melee && // 원거리 무기만
                                                    w.IsOperational &&
                                                    w.MaxAmmo > 0 &&
                                                    !w.HasAmmo() &&
                                                    !w.IsReloading)
                                        .OrderByDescending(w => w.Damage) // 데미지 높은 것 우선
                                        .FirstOrDefault();
            if (reloadTarget != null)
            {
                bool canReload = activeUnit.HasEnoughAP(reloadTarget.ReloadAPCost);
                if (canReload)
                {
                    return (CombatActionEvents.ActionType.Reload, null, null, reloadTarget);
                }
            }


            // === 2) 가장 좋은 발사 가능한 원거리 무기 선택 ===
            var fireWeapon = activeUnit.GetAllWeapons()
                                    .Where(w => w.Type != WeaponType.Melee && // 원거리 무기만
                                                w.IsOperational &&
                                                !w.IsReloading &&
                                                w.HasAmmo()) // 탄약 있고 재장전 중 아님
                                    .OrderByDescending(w => w.Damage) // 데미지 높은 것 우선
                                    .FirstOrDefault();

            if (fireWeapon == null) {
                // 근접 무기라도 있는지 확인하고 이동 결정할 수도 있지만, 원거리 전략이므로 일단 종료
                // return (default, null, null, null); // 아래에서 방어 시도
            }


            // === 3) 사거리&AP 되면 발사 (선택된 무기 및 작동 가능한 타겟이 있을 경우) ===
            if (fireWeapon != null && closestOperationalEnemy != null) {
                float attackCost = CalculateAttackAPCost(activeUnit, fireWeapon);
                bool inRange = minDist <= fireWeapon.Range + 0.001f; // 약간의 오차 허용
                bool hasAP = activeUnit.HasEnoughAP(attackCost);

                if (inRange && hasAP)
                {
                    return (CombatActionEvents.ActionType.Attack, closestOperationalEnemy, null, fireWeapon);
                }
            }


            // === 4) 위치 조정 (공격할 무기가 있거나, 공격 못했을 때, 작동 가능한 타겟이 있을 경우) ===
            // 공격할 무기가 없어도 최적 위치로 이동 시도 가능
            float moveCost = CalculateMoveAPCost(activeUnit);
            bool canMove = activeUnit.HasEnoughAP(moveCost);

            if (canMove && closestOperationalEnemy != null) {
                Weapon referenceWeapon = fireWeapon ?? activeUnit.GetAllWeapons().Where(w => w.Type != WeaponType.Melee && w.IsOperational).OrderByDescending(w => w.Range).FirstOrDefault(); // 주 공격 무기 없으면 사거리 가장 긴 원거리 무기 기준

                if (referenceWeapon != null)
                {
                    float optimalRange = referenceWeapon.Range * OPTIMAL_RANGE_FACTOR;

                    // a) 너무 가깝냐?
                    if (minDist < MIN_RANGED_SAFE_DISTANCE)
                    {
                        Vector3 dir = (minDist < 0.01f)
                                    ? Vector3.back
                                    : (activeUnit.Position - closestOperationalEnemy.Position).normalized;
                        Vector3 retreat = activeUnit.Position + dir * activeUnit.CombinedStats.Speed;
                        return (CombatActionEvents.ActionType.Move, closestOperationalEnemy, retreat, null);
                    }
                    // b) 너무 멀다 (또는 공격할 수 없었다면 최적 거리로 이동 시도)
                    else if (minDist > optimalRange || fireWeapon == null || !(minDist <= fireWeapon.Range + 0.001f && activeUnit.HasEnoughAP(CalculateAttackAPCost(activeUnit, fireWeapon)))) // 공격 못하는 경우 포함
                    {
                        Vector3 dir = (closestOperationalEnemy.Position - activeUnit.Position).normalized;
                        // 목표 지점: 최적 사거리 지점
                        Vector3 targetPos = closestOperationalEnemy.Position - dir * optimalRange;
                        // 현재 위치에서 목표 지점으로 이동 벡터
                        Vector3 moveDir = (targetPos - activeUnit.Position).normalized;
                        // 실제 이동 거리 계산 (최대 이동 속도 제한)
                        float distToTargetPos = Vector3.Distance(activeUnit.Position, targetPos);
                        float distToMove = Mathf.Min(activeUnit.CombinedStats.Speed, distToTargetPos);
                        // 최종 이동 위치
                        Vector3 approach = activeUnit.Position + moveDir * distToMove;

                        return (CombatActionEvents.ActionType.Move, closestOperationalEnemy, approach, null);
                    }
                }
            } else if (canMove && closestOperationalEnemy == null) {
                 // textLogger?.Log($"[{unitName} (Ranged)] 이동 가능하나 주변에 작동 가능한 적 없음. 이동 보류.", LogLevel.Debug);
            }


            // === 5) 방어 ===
            bool canDefend = activeUnit.HasEnoughAP(DEFEND_AP_COST);
            bool defended = ctx.HasUnitDefendedThisTurn(activeUnit);

            if (canDefend && !defended)
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            return (default, null, null, null); // 모든 조건 불만족 시 행동 없음
        }
    }
}
