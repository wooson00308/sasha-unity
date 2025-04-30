using System.Linq;
using UnityEngine;
using AF.Combat;
using AF.Services;
using AF.Models;
using System.Collections.Generic;

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


            // === 3) 공격 시도 (수정됨: 80% ~ 100% 거리 조건) ===
            if (fireWeapon != null && closestOperationalEnemy != null) 
            {
                float attackCost = CalculateAttackAPCost(activeUnit, fireWeapon);
                float weaponRange = fireWeapon.Range;
                // <<< 거리 조건 수정: 80% 이상이고 최대 사거리 내일 때 공격 >>>
                bool isInAttackRange = minDist >= weaponRange * OPTIMAL_RANGE_FACTOR && minDist <= weaponRange + 0.001f;
                bool hasAP = activeUnit.HasEnoughAP(attackCost);

                if (isInAttackRange && hasAP)
                {
                    return (CombatActionEvents.ActionType.Attack, closestOperationalEnemy, null, fireWeapon);
                }
            }


            // === 4) 위치 조정 (공격 안 했고 이동 가능할 때) (수정됨) ===
            float moveCost = CalculateMoveAPCost(activeUnit);
            bool canMove = activeUnit.HasEnoughAP(moveCost);

            if (canMove && closestOperationalEnemy != null && !ctx.MovedThisActivation.Contains(activeUnit))
            {
                Weapon referenceWeapon = fireWeapon ?? activeUnit.GetAllWeapons()
                                                        .Where(w => w.Type != WeaponType.Melee && w.IsOperational)
                                                        .OrderByDescending(w => w.Range).FirstOrDefault();

                if (referenceWeapon != null)
                {
                    float weaponRange = referenceWeapon.Range;
                    float closeThresholdDist = weaponRange * CLOSE_RANGE_THRESHOLD; // 60% 거리
                    float optimalDist = weaponRange * OPTIMAL_RANGE_FACTOR;      // 80% 거리

                    // a) 너무 가깝다 (60% 미만): 후퇴해서 60% 거리 확보 시도 (기존 유지)
                    if (minDist < closeThresholdDist)
                    {
                        Vector3 dir = (minDist < 0.01f) ? Vector3.back : (activeUnit.Position - closestOperationalEnemy.Position).normalized;
                        Vector3 targetPos = closestOperationalEnemy.Position + dir * closeThresholdDist;
                        Vector3 moveVec = targetPos - activeUnit.Position;
                        float distToMove = Mathf.Min(activeUnit.CombinedStats.Speed, moveVec.magnitude);
                        Vector3 retreatPos = activeUnit.Position + moveVec.normalized * distToMove;
                        return (CombatActionEvents.ActionType.Move, closestOperationalEnemy, retreatPos, null);
                    }
                    // b) 너무 멀다 (100% 초과): 최대 사거리까지만 접근 (목표 지점 수정)
                    else if (minDist > weaponRange + 0.001f)
                    {
                       Vector3 dir = (closestOperationalEnemy.Position - activeUnit.Position).normalized;
                       // <<< 목표 지점 수정: 적 위치에서 최대 사거리만큼 떨어진 지점 >>>
                       Vector3 targetPos = closestOperationalEnemy.Position - dir * weaponRange;
                       // <<< 목표 지점 수정 끝 >>>
                        Vector3 moveVec = targetPos - activeUnit.Position;
                        float distToMove = Mathf.Min(activeUnit.CombinedStats.Speed, moveVec.magnitude);
                        Vector3 approachPos = activeUnit.Position + moveVec.normalized * distToMove;
                        return (CombatActionEvents.ActionType.Move, closestOperationalEnemy, approachPos, null);
                    }
                    // c) 60% ~ 100% 사이인데 공격 못했다면? -> 현재 위치 유지 (이동 안 함)
                }
                // else: 참조할 원거리 무기가 없음 -> 이동 결정 안 함
            } else if (canMove && closestOperationalEnemy == null) {
                 // textLogger?.Log($"[{unitName} (Ranged)] 이동 가능하나 주변에 작동 가능한 적 없음. 이동 보류.", LogLevel.Debug);
            }


            // === 5) 방어 (기존과 동일, 조건은 이미 수정됨) ===
            if (activeUnit.HasEnoughAP(DEFEND_AP_COST) &&
                !ctx.HasUnitDefendedThisTurn(activeUnit) &&
                (!ctx.MovedThisActivation.Contains(activeUnit) || IsLowHealth(activeUnit)))
            {
                return (CombatActionEvents.ActionType.Defend, null, null, null);
            }

            return (default, null, null, null); // 최종적으로 할 행동 없음
        }
    }
}
