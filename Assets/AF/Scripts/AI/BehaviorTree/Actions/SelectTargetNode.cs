using System.Collections.Generic;
using System.Linq;
using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 및 Vector3.sqrMagnitude 용

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
                // Debug.Log($"[{this.GetType().Name}] {agent.Pilot?.PilotName ?? agent.name}: Selected target: {closestTarget.Pilot?.PilotName ?? closestTarget.name} (Distance: {Mathf.Sqrt(minDistanceSqr):F1}). Success.");
                return NodeStatus.Success;
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