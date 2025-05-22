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
                // float distanceToTargetSqr = (closestTarget.Position - agent.Position).sqrMagnitude; // 이제 SelectedWeapon 로직에서 거리 제곱근 대신 거리 사용

                Weapon weaponToUse = null;
                // Weapon primaryWeapon = agent.GetPrimaryWeapon(); // 이제 primaryWeapon 변수 대신 직접 접근

                // 필터링: 사용 가능하고 재장전 중이 아닌 무기
                var usableWeapons = agent.EquippedWeapons
                    .Where(w => w != null && w.IsOperational && w.HasAmmo() && !w.IsReloading)
                    .ToList();

                if (!usableWeapons.Any())
                {
                    // 사용할 무기가 전혀 없으면 실패
                    var textLoggerWarning = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLoggerWarning?.Log($"[{this.GetType().Name}] {agent.Name}: Selected target: {closestTarget.Name}. BUT no usable weapons found. Setting SelectedWeapon to null. Failure.", LogLevel.Debug);
                    blackboard.SelectedWeapon = null; 
                    return NodeStatus.Failure;
                }

                // **수정된 무기 선택 로직:**
                // 1. 현재 사거리 내에서 가장 적합한 무기 (예: 주무기 우선, 없으면 다른 무기)
                weaponToUse = usableWeapons
                    .Where(w => {
                        float dist = Vector3.Distance(agent.Position, closestTarget.Position);
                        return dist >= w.MinRange && dist <= w.MaxRange;
                    })
                    .OrderByDescending(w => w == agent.GetPrimaryWeapon()) // 주무기 우선
                    .ThenByDescending(w => w.Damage) // 같은 우선순위면 공격력 높은 무기
                    .FirstOrDefault();

                // 2. 사거리 내 무기가 없다면, 사거리에 관계없이 사용 가능한 무기 선택 (예: 주무기 우선, 없으면 다른 무기)
                if (weaponToUse == null)
                {
                    weaponToUse = usableWeapons
                         .OrderByDescending(w => w == agent.GetPrimaryWeapon()) // 주무기 우선
                         .ThenByDescending(w => w.Damage) // 같은 우선순위면 공격력 높은 무기
                         .FirstOrDefault(); // 사거리 관계없이 첫 번째 사용 가능 무기 선택 (또는 다른 기준)
                }


                if (weaponToUse != null)
                {
                    blackboard.SelectedWeapon = weaponToUse;
                    var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected target: {closestTarget.Name}. Selected weapon: {weaponToUse.Name}. Success.", LogLevel.Debug);
                    return NodeStatus.Success;
                }
                else
                {
                     // 이 경우는 usableWeapons.Any() 체크에서 이미 걸러지겠지만, 혹시 모를 안전 장치
                    var textLoggerWarning = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                     textLoggerWarning?.Log($"[{this.GetType().Name}] {agent.Name}: Selected target: {closestTarget.Name}. BUT no suitable weapon found (after all checks). Setting SelectedWeapon to null. Failure.", LogLevel.Debug);
                    blackboard.SelectedWeapon = null; 
                    return NodeStatus.Failure;
                }
            }
            else
            {
                // 위에서 potentialTargets.Count == 0 체크로 이 경우는 거의 없겠지만, 안전 장치
                var textLoggerWarning = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                 textLoggerWarning?.Log($"[{this.GetType().Name}] {agent.Name}: No potential targets found. Setting CurrentTarget to null. Failure.", LogLevel.Debug);
                blackboard.CurrentTarget = null;
                return NodeStatus.Failure;
            }
        }
    }
} 