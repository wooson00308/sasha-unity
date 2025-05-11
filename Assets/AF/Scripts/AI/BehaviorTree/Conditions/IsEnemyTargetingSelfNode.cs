using AF.Combat;
using AF.Models;
using System.Linq; // LINQ 사용을 위해 추가

namespace AF.AI.BehaviorTree.Conditions // 네임스페이스는 Conditions로 통일
{
    public class IsEnemyTargetingSelfNode : ConditionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (context == null || context.Participants == null)
            {
                // 전투 정보가 없으면 실패 처리
                return NodeStatus.Failure;
            }

            // 적 팀 유닛들만 필터링
            var enemyUnits = context.Participants
                .Where(p => p.TeamId != agent.TeamId && p.IsOperational && !p.IsDestroyed);

            foreach (var enemy in enemyUnits)
            {
                // 각 적 유닛의 현재 타겟이 자기 자신(agent)인지 확인
                if (enemy.CurrentTarget == agent)
                {
                    // 한 명이라도 나를 타겟팅하고 있다면 성공
                    return NodeStatus.Success;
                }
            }

            // 아무도 나를 타겟팅하고 있지 않으면 실패
            return NodeStatus.Failure;
        }
    }
} 