using AF.Combat; // CombatContext를 찾기 위함
using AF.Models;

namespace AF.AI.BehaviorTree
{
    public enum NodeStatus
    {
        Running,  // 노드가 아직 실행 중이며, 다음 틱에도 계속 실행되어야 함
        Success,  // 노드가 성공적으로 실행 완료됨
        Failure   // 노드가 실행에 실패함
    }

    public abstract class BTNode
    {
        /// <summary>
        /// 행동 트리 노드를 실행합니다.
        /// </summary>
        /// <param name="agent">이 노드를 실행하는 주체 ArmoredFrame</param>
        /// <param name="blackboard">데이터 공유 및 결과 저장을 위한 Blackboard</param>
        /// <param name="context">현재 전투의 컨텍스트 정보</param>
        /// <returns>노드 실행 결과 (Running, Success, Failure)</returns>
        public abstract NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
    }
} 