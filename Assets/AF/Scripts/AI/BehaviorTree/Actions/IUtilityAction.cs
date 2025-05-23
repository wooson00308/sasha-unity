using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// 유틸리티 기반 행동을 정의하는 인터페이스
    /// </summary>
    public interface IUtilityAction
    {
        /// <summary>
        /// 현재 상황에서 이 행동의 효용값을 계산합니다.
        /// </summary>
        /// <param name="agent">행동을 수행할 주체</param>
        /// <param name="blackboard">데이터 공유 객체</param>
        /// <param name="context">전투 컨텍스트</param>
        /// <returns>0.0 ~ 1.0 사이의 효용값 (높을수록 더 유용함)</returns>
        float CalculateUtility(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
        
        /// <summary>
        /// 이 행동을 실행합니다.
        /// </summary>
        /// <returns>행동 실행 결과</returns>
        NodeStatus Execute(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
        
        /// <summary>
        /// 행동의 이름 (디버깅용)
        /// </summary>
        string ActionName { get; }
    }
} 