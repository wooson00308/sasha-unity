using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 특정 조건을 검사하는 행동 트리의 잎새 노드입니다.
    /// 조건이 만족되면 Success, 그렇지 않으면 Failure를 반환합니다.
    /// </summary>
    public abstract class ConditionNode : BTNode
    {
        // ConditionNode는 자식 노드를 가지지 않습니다.
        // Tick 메서드는 이 클래스를 상속받는 구체적인 조건 노드에서
        // 실제 조건을 검사하고 그 결과를 반환하도록 구현되어야 합니다.
        public abstract override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
    }
} 