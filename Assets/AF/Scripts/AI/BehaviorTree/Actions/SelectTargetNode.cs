using System.Collections.Generic;
using System.Linq;
using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 및 Vector3.sqrMagnitude 용
using AF.Services;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 가장 가까운 유효한 적 유닛을 찾아 현재 유닛의 타겟으로 설정하는 액션 노드입니다.
    /// </summary>
    public class SelectTargetNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (context.Participants == null || context.TeamAssignments == null)
            {
                // Debug.LogError($"[{this.GetType().Name}] {agent.Pilot?.PilotName ?? agent.name}: CombatContext is missing Participants or TeamAssignments.");
                return NodeStatus.Failure;
            }

            // 현재 유닛의 팀 ID 확인
            if (!context.TeamAssignments.TryGetValue(agent, out int agentTeamId))
            {
                // Debug.LogError($"[{this.GetType().Name}] {agent.Pilot?.PilotName ?? agent.name}: Could not find own team ID.");
                return NodeStatus.Failure; // 자신의 팀 ID를 모르면 적 판별 불가
            }

            // 적 후보 필터링: 다른 팀 & 파괴되지 않은 유닛
            List<ArmoredFrame> potentialTargets = context.Participants
                .Where(p => p != agent && // 자기 자신 제외
                            !p.IsDestroyed && // 파괴되지 않은 유닛
                            context.TeamAssignments.TryGetValue(p, out int targetTeamId) && // 팀 ID 확인 가능하고
                            targetTeamId != agentTeamId) // 다른 팀 소속
                .ToList();

            if (potentialTargets.Count == 0)
            {
                // Debug.Log($"[{this.GetType().Name}] {agent.Pilot?.PilotName ?? agent.name}: No potential targets found.");
                blackboard.CurrentTarget = null; // 타겟 없음을 명확히 설정
                return NodeStatus.Failure; // 적합한 타겟 없음
            }

            // 가장 가까운 적 찾기
            ArmoredFrame closestTarget = null;
            float minDistanceSqr = float.MaxValue;

            foreach (var target in potentialTargets)
            {
                // Position 프로퍼티가 public getter를 가지고 있다고 가정
                float distanceSqr = (target.Position - agent.Position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestTarget = target;
                }
            }

            // 타겟 설정 및 성공 반환
            if (closestTarget != null)
            {
                blackboard.CurrentTarget = closestTarget;
                float distanceToTargetSqr = (closestTarget.Position - agent.Position).sqrMagnitude;

                Weapon weaponToUse = null;
                Weapon primaryWeapon = agent.GetPrimaryWeapon();

                // Filter for generally usable weapons
                var usableWeapons = agent.EquippedWeapons
                    .Where(w => w != null && w.IsOperational && w.HasAmmo() && !w.IsReloading)
                    .ToList();

                // 1. Check primary weapon if it's usable and in range
                if (primaryWeapon != null && usableWeapons.Contains(primaryWeapon))
                {
                    bool primaryInRange = distanceToTargetSqr >= (primaryWeapon.MinRange * primaryWeapon.MinRange) &&
                                          distanceToTargetSqr <= (primaryWeapon.MaxRange * primaryWeapon.MaxRange);
                    if (primaryInRange)
                    {
                        weaponToUse = primaryWeapon;
                    }
                }

                // 2. If primary not suitable or not in range, check other usable weapons in range
                if (weaponToUse == null)
                {
                    weaponToUse = usableWeapons
                        .Where(w => w != primaryWeapon && // Exclude primary if already checked or not usable
                                    distanceToTargetSqr >= (w.MinRange * w.MinRange) &&
                                    distanceToTargetSqr <= (w.MaxRange * w.MaxRange))
                        .FirstOrDefault(); // Or some other ordering like highest damage, etc.
                }

                // 3. If still no weapon, check primary weapon if usable (regardless of range)
                if (weaponToUse == null && primaryWeapon != null && usableWeapons.Contains(primaryWeapon))
                {
                    weaponToUse = primaryWeapon;
                }
                
                // 4. If still no weapon, check other usable weapons (regardless of range)
                if (weaponToUse == null)
                {
                    weaponToUse = usableWeapons
                        .Where(w => w != primaryWeapon) // Exclude primary if already checked or not usable
                        .FirstOrDefault();
                }
                
                // If after all checks, weaponToUse is still null, it means no operational, loaded weapon is available at all.

                if (weaponToUse != null)
                {
                    blackboard.SelectedWeapon = weaponToUse;
                    var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected target: {closestTarget.Name}. Selected weapon: {weaponToUse.Name}. Success.", LogLevel.Debug);
                    return NodeStatus.Success;
                }
                else
                {
                    string reason = "No operational, loaded weapon available after all checks.";
                    var primaryStatus = primaryWeapon == null ? "null" : 
                                        usableWeapons.Contains(primaryWeapon) ? "usable" : "not usable (no ammo/reloading)";
                    var usableWeaponCount = usableWeapons.Count(w => w != primaryWeapon);

                    reason = $"Primary weapon ({primaryWeapon?.Name ?? "N/A"}) status: {primaryStatus}. Other usable weapons count: {usableWeaponCount}.";
                    
                    if (primaryWeapon != null && !usableWeapons.Contains(primaryWeapon))
                    {
                        reason += $" Primary reason for no weapon: Primary '{primaryWeapon.Name}' is not usable (e.g., no ammo, reloading).";
                    }
                    else if (usableWeapons.All(w => w == primaryWeapon && weaponToUse == null)) // Only primary was usable, but it wasn't chosen (e.g. out of range and no other option)
                    {
                        reason += " Only primary was usable but potentially out of range, and no other options.";
                    }
                    else if (!usableWeapons.Any())
                    {
                        reason += " No weapons are currently usable (e.g., all reloading or out of ammo).";
                    }

                    var textLoggerWarning = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLoggerWarning?.Log($"[{this.GetType().Name}] {agent.Name}: Selected target: {closestTarget.Name}. BUT no suitable weapon found. Detailed Reason: {reason}. Setting SelectedWeapon to null.", LogLevel.Warning);
                    blackboard.SelectedWeapon = null; 
                    return NodeStatus.Failure;
                }
            }
            else
            {
                // 위에서 potentialTargets.Count == 0 체크로 이 경우는 거의 없겠지만, 안전 장치
                blackboard.CurrentTarget = null;
                return NodeStatus.Failure;
            }
        }
    }
} 