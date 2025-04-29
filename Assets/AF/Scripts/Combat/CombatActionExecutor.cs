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

            // === AP 비용 계산 ===
            float apCost = freeCounter ? 0f : actionType switch
            {
                CombatActionEvents.ActionType.Attack     => weapon != null ? CalculateAttackAPCost(actor, weapon) : float.MaxValue,
                CombatActionEvents.ActionType.Move       => CalculateMoveAPCost(actor),
                CombatActionEvents.ActionType.Defend     => DEFEND_AP_COST,
                CombatActionEvents.ActionType.Reload     => weapon != null ? weapon.ReloadAPCost : float.MaxValue,
                CombatActionEvents.ActionType.RepairAlly => 2.5f,
                CombatActionEvents.ActionType.RepairSelf => 2.0f,
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
                        break;

                    // ============= 방어 ==========================
                    case CombatActionEvents.ActionType.Defend:
                        ApplyDefendStatus(actor, ctx.DefendedThisTurn);
                        success = true;
                        resultDescription = "방어 태세 돌입 (방어력 증가)";
                        break;

                    // ============= 재장전 ========================
                    case CombatActionEvents.ActionType.Reload:
                        (success, resultDescription) = ExecuteReload(actor, weapon, ctx.CurrentTurn);
                        break;

                    // ============= 수리 ==========================
                    case CombatActionEvents.ActionType.RepairAlly:
                        success = (targetFrame != null && targetFrame != actor);
                        resultDescription = success ? $"{targetFrame.Name} 수리 시도"
                                                     : "수리 대상 아군 지정 필요";
                        break;

                    case CombatActionEvents.ActionType.RepairSelf:
                        success = true;
                        resultDescription = "자가 수리 시도";
                        break;

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
                finalPos, distMoved, targetFrame));

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
            if (distance > weapon.Range)
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

            float optRange = weapon.Range * 0.8f;
            if (distance > optRange)
            {
                float penaltyRatio = (distance - optRange) / (weapon.Range - optRange);
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
                    attacker, target, finalD, part.Type, critical,
                    part.CurrentDurability, part.MaxDurability, isCounter));

                if (destroyed)
                {
                    ctx.Bus.Publish(new PartEvents.PartDestroyedEvent(
                        target, part.Type, attacker,
                        $"{slot} 파트가 파괴되었습니다.",
                        $"{target.Name} 성능이 감소했습니다."));
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
            actor.Position = newPos;

            return (true,
                $"목표 방향으로 {dMv:F1} 이동 → {newPos}",
                newPos, dMv);
        }

        private (bool success, string description) ExecuteReload(
            ArmoredFrame actor, Weapon weapon, int turn)
        {
            if (weapon == null)
                return (false, "재장전할 무기 정보 없음");

            bool ok = weapon.StartReload(turn);

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
                "Defense Buff", 1,
                StatType.Defense,
                ModificationType.Multiplicative,
                1.5f);
            actor.AddStatusEffect(eff);
            defendedSet.Add(actor);
        }

        #endregion

        #region AP 계산 & 기타 -----------------------------------------------

        private const float DEFEND_AP_COST = 1f;

        private float CalculateMoveAPCost(ArmoredFrame unit)
        {
            if (unit == null) return float.MaxValue;
            float baseCost  = 1f;
            float weightPen = unit.TotalWeight * 0.01f;
            float speedBon  = unit.CombinedStats.Speed * 0.05f;
            return Mathf.Max(0.5f, baseCost + weightPen - speedBon);
        }

        private float CalculateAttackAPCost(ArmoredFrame unit, Weapon weapon)
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
                                                    attacker.Position) <= wep.Range);
            if (w == null) return;

            // --- Log Counter announcement BEFORE executing the counter attack ---
            string counterAnnounceMsg = $"<sprite index=21> <color=lightblue>[{defender.Name}]</color>의 <color=lightblue>카운터!</color>";
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
