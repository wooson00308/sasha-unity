using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AF.EventBus;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 모든 실질 행동(공격, 이동, 방어, 재장전 등)을 수행하고 이벤트를 발행한다.
    /// </summary>
    public sealed class CombatActionExecutor : ICombatActionExecutor
    {
        // <<< AP 비용 상수들을 public const로 변경 또는 신규 정의 >>>
        public const float BASE_REPAIR_AMOUNT = 50f; // 이건 수리량 상수이므로 그대로 둬도 무방
        public const float DEFEND_AP_COST = 1f;
        public const float REPAIR_ALLY_AP_COST = 2.5f;
        public const float REPAIR_SELF_AP_COST = 2.0f;

        #region Execute (PerformAction 대체) ----------------------------------

        public bool Execute(
            CombatContext ctx,
            ArmoredFrame actor,
            CombatActionEvents.ActionType actionType,
            ArmoredFrame targetFrame,
            Vector3? targetPosition,
            Weapon weapon,
            bool isCounter = false,
            bool freeCounter = false)
        {
            if (actor == null || !actor.IsOperational)
            {
                Debug.LogWarning("[CombatActionExecutor] 유효하지 않은 행동 요청입니다.");
                return false;
            }

            // +++ 이동 횟수 제한 체크 +++
            if (actionType == CombatActionEvents.ActionType.Move)
            {
                if (ctx.MovedThisActivation.Contains(actor))
                {
                    ctx.Bus.Publish(new CombatActionEvents.ActionCompletedEvent(
                        actor, actionType, false, "이미 이동함", ctx.CurrentTurn, null, null, targetFrame, isCounter));
                    return false; // 이미 이동했으므로 실패 처리
                }
            }
            // +++ 이동 횟수 제한 체크 끝 +++

            // === AP 비용 계산 ===
            float apCost = freeCounter ? 0f : actionType switch
            {
                CombatActionEvents.ActionType.Attack     => weapon != null ? CalculateAttackAPCost(actor, weapon) : float.MaxValue,
                CombatActionEvents.ActionType.Move       => CalculateMoveAPCost(actor),
                CombatActionEvents.ActionType.Defend     => DEFEND_AP_COST,
                CombatActionEvents.ActionType.Reload     => weapon != null ? weapon.ReloadAPCost : float.MaxValue,
                CombatActionEvents.ActionType.RepairAlly => REPAIR_ALLY_AP_COST,
                CombatActionEvents.ActionType.RepairSelf => REPAIR_SELF_AP_COST,
                _                                        => float.MaxValue
            };

            // AP 부족
            if (!actor.HasEnoughAP(apCost))
            {
                ctx.Bus.Publish(new CombatActionEvents.ActionCompletedEvent(
                    actor, actionType, false, "AP 부족", ctx.CurrentTurn, null, null, targetFrame));
                return false;
            }

            // ActionStartEvent
            ctx.Bus.Publish(new CombatActionEvents.ActionStartEvent(
                actor, actionType, ctx.CurrentTurn, targetFrame, targetPosition, weapon));

            bool   success           = false;
            string resultDescription = "";
            Vector3? finalPos        = null;
            float?  distMoved        = null;

            try
            {
                switch (actionType)
                {
                    // ============= 공격 ==========================
                    case CombatActionEvents.ActionType.Attack:
                        (success, resultDescription) =
                            ExecuteAttack(ctx, actor, targetFrame, weapon, isCounter);
                        break;

                    // ============= 이동 ==========================
                    case CombatActionEvents.ActionType.Move:
                        var mv = PerformMoveToPosition(actor,
                               targetPosition ?? targetFrame?.Position ?? actor.Position);
                        success          = mv.success;
                        resultDescription= mv.description;
                        finalPos         = mv.newPosition;
                        distMoved        = mv.distanceMoved;
                        if(success)
                        {
                             ctx.MovedThisActivation.Add(actor);
                        }
                        break;

                    // ============= 방어 ==========================
                    case CombatActionEvents.ActionType.Defend:
                        ApplyDefendStatus(actor, ctx.DefendedThisTurn);
                        success = true;
                        resultDescription = "방어 태세 돌입 (방어력 증가)";
                        break;

                    // ============= 재장전 ========================
                    case CombatActionEvents.ActionType.Reload:
                        (success, resultDescription) = ExecuteReload(actor, weapon, ctx);
                        break;

                    // ============= 수리 (수정) =====================
                    case CombatActionEvents.ActionType.RepairAlly:
                    {
                        ArmoredFrame actualTarget = targetFrame; // 명확성을 위해 변수 사용
                        if (actualTarget == null || actualTarget == actor || !actualTarget.IsOperational)
                        {
                            resultDescription = "유효한 아군 수리 대상 지정 필요";
                            success = false;
                        }
                        else
                        {
                            string targetSlot = GetMostDamagedPartSlot(actualTarget);
                            if (targetSlot != null)
                            {
                                float repairAmount = BASE_REPAIR_AMOUNT; // <<< 실제 수리량 >>>
                                float repairedAmount = actualTarget.ApplyRepair(targetSlot, repairAmount);

                                if (repairedAmount > 0)
                                {
                                    success = true;
                                    resultDescription = $"{actualTarget.Name}의 {targetSlot} 파츠를 {repairedAmount:F1} 만큼 수리";
                                    ctx.Bus.Publish(new CombatActionEvents.RepairAppliedEvent(
                                        actor, actualTarget, actionType, targetSlot, repairedAmount, ctx.CurrentTurn));
                                }
                                else
                                {
                                    success = false; // 수리할 필요 없거나 실패
                                    resultDescription = $"{actualTarget.Name}의 {targetSlot} 파츠는 수리 불필요";
                                    // 실패했지만 이벤트는 발행하지 않음 (선택적)
                                }
                            }
                            else
                            {
                                success = false;
                                resultDescription = $"{actualTarget.Name}에 수리할 파츠 없음";
                            }
                        }
                        break;
                    } // case RepairAlly 끝

                    case CombatActionEvents.ActionType.RepairSelf:
                    {
                        ArmoredFrame selfTarget = actor; // 자가 수리 대상은 자신
                        string targetSlot = GetMostDamagedPartSlot(selfTarget);
                        if (targetSlot != null)
                        {
                            float repairAmount = BASE_REPAIR_AMOUNT; // <<< 실제 수리량 >>>
                            float repairedAmount = selfTarget.ApplyRepair(targetSlot, repairAmount);

                            if (repairedAmount > 0)
                            {
                                success = true;
                                resultDescription = $"자가 수리: {targetSlot} 파츠 {repairedAmount:F1} 회복";
                                ctx.Bus.Publish(new CombatActionEvents.RepairAppliedEvent(
                                    actor, selfTarget, actionType, targetSlot, repairedAmount, ctx.CurrentTurn));
                            }
                            else
                            {
                                success = false;
                                resultDescription = $"자가 수리: {targetSlot} 파츠 수리 불필요";
                            }
                        }
                        else
                        {
                            success = false;
                            resultDescription = "자가 수리: 수리할 파츠 없음";
                        }
                        break;
                    } // case RepairSelf 끝

                    default:
                        resultDescription = "알 수 없는 행동 타입";
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Logger.TextLogger.Log($"[{actor.Name}] {actionType} 중 오류: {ex.Message}", LogLevel.Error);
                resultDescription = $"오류: {ex.Message}";
                success = false;
            }

            if (success && apCost > 0f) actor.ConsumeAP(apCost);

            ctx.Bus.Publish(new CombatActionEvents.ActionCompletedEvent(
                actor, actionType, success, resultDescription, ctx.CurrentTurn,
                finalPos, distMoved, targetFrame, isCounter));

            return success;
        }

        #endregion

        #region 공격 세부 (PerformAttack 완전 이식) ---------------------------

        private (bool success, string description) ExecuteAttack(
            CombatContext ctx,
            ArmoredFrame attacker,
            ArmoredFrame target,
            Weapon weapon,
            bool isCounter = false)
        {
            if (target == null || !ctx.Participants.Contains(target) || !target.IsOperational)
                return (false, "유효하지 않은 대상");

            if (weapon == null)
                return (false, "유효하지 않은 무기");

            if (ctx.TeamAssignments.TryGetValue(attacker, out var atkTeam) &&
                ctx.TeamAssignments.TryGetValue(target,   out var tgtTeam) &&
                atkTeam == tgtTeam)
                return (false, "같은 팀 공격 불가");

            float distance = Vector3.Distance(attacker.Position, target.Position);
            if (distance > weapon.MaxRange)
                return (false, "사거리 밖");

            if (weapon.IsReloading)
                return (false, "재장전 중");

            if (!weapon.HasAmmo())
                return (false, "탄약 부족");

            weapon.ConsumeAmmo(); // 발사 시 탄약 차감

            // ===== 명중 판정 =====
            float atkAcc  = attacker.CombinedStats.Accuracy;
            float tgtEva  = target.CombinedStats.Evasion;
            float baseHit = weapon.Accuracy
                          + (atkAcc - 1f) * 1f
                          -  tgtEva * 0.5f;

            float optRange = weapon.MaxRange * 0.8f;
            if (distance > optRange)
            {
                float penaltyRatio = (distance - optRange) / (weapon.MaxRange - optRange);
                baseHit *= Mathf.Clamp(1f - penaltyRatio * 0.5f, 0.5f, 1f);
            }

            const float MIN_HIT = 0.01f, MAX_HIT = 0.95f;
            baseHit = Mathf.Clamp(baseHit, MIN_HIT, MAX_HIT);

            float rand = UnityEngine.Random.value;
            bool  hit  = rand <= baseHit;

            ctx.Bus.Publish(new CombatActionEvents.WeaponFiredEvent(
                attacker, target, weapon, hit, rand, isCounter));

            if (hit)
            {
                // ===== 데미지 계산 & 적용 =====
                float rawDmg   = weapon.Damage;
                float calcDmg  = rawDmg * 0.8f;
                bool  critical = UnityEngine.Random.value <= 0.2f;
                float  finalD  = critical ? calcDmg * 1.5f : calcDmg;

                string slot = GetRandomTargetPartSlot(target, ctx);
                Part   part = target.GetPart(slot);

                ctx.Bus.Publish(new DamageEvents.DamageCalculatedEvent(
                    attacker, target, weapon, rawDmg, calcDmg,
                    weapon.DamageType, part.Type));

                bool destroyed = target.ApplyDamage(slot, finalD, ctx.CurrentTurn);

                ctx.Bus.Publish(new DamageEvents.DamageAppliedEvent(
                    attacker, target, weapon, finalD, part.Type, critical,
                    part.CurrentDurability, part.MaxDurability, isCounter));

                if (destroyed)
                {
                    ctx.Bus.Publish(new PartEvents.PartDestroyedEvent(
                        target,             // frame
                        part.Type,          // destroyedPartType
                        slot,               // destroyedSlotId
                        attacker,           // destroyer
                        $"{slot} 파트가 파괴되었습니다.", // effects[0]
                        $"{target.Name} 성능이 감소했습니다." // effects[1]
                    ));
                }

                // 전투 불능 → 결과는 시뮬레이터가 체크
            }
            else
            {
                ctx.Bus.Publish(new DamageEvents.DamageAvoidedEvent(
                    attacker, target, weapon.Damage,
                    DamageEvents.DamageAvoidedEvent.AvoidanceType.Dodge,
                    $"{target.Name}이(가) 회피했습니다.", isCounter));
            }
            TryCounterAttack(ctx, target, attacker, isCounter);
            return (true, hit ? "공격 성공" : "공격 실패");
        }

        #endregion

        #region 이동 / 재장전 / 방어 -----------------------------------------

        private (bool success,string description,Vector3? newPosition,float? distanceMoved)
            PerformMoveToPosition(ArmoredFrame actor, Vector3 targetPos)
        {
            Vector3 cur = actor.Position;
            Vector3 dir = (targetPos - cur).normalized;
            float   dTot= Vector3.Distance(cur, targetPos);
            float   dMax= actor.CombinedStats.Speed;
            float   dMv = Mathf.Min(dMax, dTot);

            if (dMv < 0.1f)
                return (false, "이동할 필요 없음", cur, 0f);

            Vector3 newPos = cur + dir * dMv;
            actor.SetPosition(newPos);

            return (true,
                $"목표 방향으로 {dMv:F1} 이동 → {newPos}",
                newPos, dMv);
        }

        private (bool success, string description) ExecuteReload(
            ArmoredFrame actor, Weapon weapon, CombatContext ctx)
        {
            if (weapon == null)
                return (false, "재장전할 무기 정보 없음");

            bool ok = weapon.StartReload(ctx.CurrentTurn);

            return ok
                ? (true, weapon.ReloadTurns == 0
                           ? $"{weapon.Name} 즉시 재장전 완료."
                           : $"{weapon.Name} 재장전 시작.")
                : (false, $"{weapon.Name} 재장전 시작 실패.");
        }

        private void ApplyDefendStatus(
            ArmoredFrame actor, HashSet<ArmoredFrame> defendedSet)
        {
            var eff = new StatusEffect(
                "Defense Buff", // effectName
                1, // durationTurns
                StatusEffectEvents.StatusEffectType.Buff_DefenseBoost, // <<< effectType 추가 >>>
                StatType.Defense, // statToModify
                ModificationType.Multiplicative, // modType
                1.5f); // modValue
            actor.AddStatusEffect(eff);
            defendedSet.Add(actor);
        }

        #endregion

        #region AP 계산 & 기타 -----------------------------------------------

        public float GetActionAPCost(
            CombatActionEvents.ActionType actionType,
            ArmoredFrame actor,
            Weapon weapon = null)
        {
            switch (actionType)
            {
                case CombatActionEvents.ActionType.Attack:
                    return weapon != null ? CalculateAttackAPCost(actor, weapon) : float.MaxValue;
                case CombatActionEvents.ActionType.Move:
                    return CalculateMoveAPCost(actor);
                case CombatActionEvents.ActionType.Defend:
                    return DEFEND_AP_COST;
                case CombatActionEvents.ActionType.Reload:
                    return weapon != null ? weapon.ReloadAPCost : float.MaxValue;
                case CombatActionEvents.ActionType.RepairAlly:
                    return REPAIR_ALLY_AP_COST;
                case CombatActionEvents.ActionType.RepairSelf:
                    return REPAIR_SELF_AP_COST;
                default:
                    Debug.LogWarning($"[CombatActionExecutor] GetActionAPCost: Unknown action type '{actionType}'");
                    return float.MaxValue;
            }
        }

        public float CalculateMoveAPCost(ArmoredFrame unit)
        {
            if (unit == null) return float.MaxValue;
            float baseCost  = 1f;
            float weightPen = unit.TotalWeight * 0.005f;
            float speedBon  = unit.CombinedStats.Speed * 0.075f;
            return Mathf.Max(0.5f, baseCost + weightPen - speedBon);
        }

        public float CalculateAttackAPCost(ArmoredFrame unit, Weapon weapon)
        {
            if (unit == null || weapon == null) return 999f;
            float eff = Mathf.Max(0.1f, unit.CombinedStats.EnergyEfficiency);
            return Mathf.Max(0.5f, weapon.BaseAPCost / eff);
        }

        private string GetRandomTargetPartSlot(ArmoredFrame target, CombatContext ctx)
        {
            List<string> ops = target.GetAllOperationalPartSlots();
            if (ops == null || ops.Count == 0)
            {
                Debug.LogWarning($"[{ctx.BattleId}-T{ctx.CurrentTurn}] {target.Name}: 작동 파츠 없음");
                return null;
            }
            int idx = UnityEngine.Random.Range(0, ops.Count);
            return ops[idx];
        }

        private string GetMostDamagedPartSlot(ArmoredFrame target)
        {
            string mostDamagedSlot = null;
            float lowestDurabilityRatio = float.MaxValue;

            // 작동 가능하고, 최대 내구도보다 현재 내구도가 낮은 파츠만 고려
            foreach (var kvp in target.Parts.Where(p => p.Value.IsOperational && p.Value.CurrentDurability < p.Value.MaxDurability))
            {
                Part part = kvp.Value;
                // 최대 내구도가 0 이하인 경우 방지
                if (part.MaxDurability <= 0) continue;

                float currentRatio = part.CurrentDurability / part.MaxDurability;

                if (currentRatio < lowestDurabilityRatio)
                {
                    lowestDurabilityRatio = currentRatio;
                    mostDamagedSlot = kvp.Key;
                }
            }

            // 수리할 파츠가 없다면 null 반환
            if (mostDamagedSlot == null)
            {
                 return null; // 수리할 대상 없음 명확히 하기
            }

            return mostDamagedSlot;
        }

        private void TryCounterAttack(
                CombatContext ctx,
                ArmoredFrame defender,
                ArmoredFrame attacker,
                bool thisWasCounter)
        {
            if (thisWasCounter) return;                  // 루프 방지
            if (!defender.IsOperational) return;

            Weapon w = defender.GetAllWeapons()
                            .FirstOrDefault(wep =>
                                !wep.IsReloading &&
                                wep.HasAmmo() &&
                                Vector3.Distance(defender.Position,
                                                    attacker.Position) <= wep.MaxRange);
            if (w == null) return;

            // --- Log Counter announcement BEFORE executing the counter attack ---
            string counterAnnounceMsg = $"<color=lightblue>[{defender.Name}]</color>의 <color=lightblue>카운터!</color>";
            ctx.Logger.TextLogger.Log(counterAnnounceMsg, LogLevel.Info); 
            // --- End Counter announcement ---

            // AP 0, Delay 0 ▶ freeCounter=true 로 호출
            Execute(ctx, defender,
                    CombatActionEvents.ActionType.Attack,
                    attacker, null, w,
                    /*isCounter*/ true,
                    /*freeCounter*/ true);
        }

        #endregion
    }
}
