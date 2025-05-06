using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 특정 행동을 수행하는 행동 트리의 잎새 노드입니다.
    /// 행동의 결과에 따라 Success, Failure, 또는 Running 상태를 반환할 수 있습니다.
    /// </summary>
    public abstract class ActionNode : BTNode
    {
        // ActionNode는 자식 노드를 가지지 않습니다.
        // Tick 메서드는 이 클래스를 상속받는 구체적인 액션 노드에서
        // 실제 행동을 수행하고 그 결과를 반환하도록 구현되어야 합니다.
        public abstract override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context);
    }
} 