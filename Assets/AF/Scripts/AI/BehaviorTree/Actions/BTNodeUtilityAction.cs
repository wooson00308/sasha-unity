using AF.Combat;
using AF.Models;
using AF.AI.BehaviorTree.Evaluators;
namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// BT 노드를 감싸는 유틸리티 액션 구현체
    /// </summary>
    public class BTNodeUtilityAction : IUtilityAction
    {
        private readonly BTNode wrappedNode;
        private readonly IUtilityEvaluator evaluator;
        
        public string ActionName { get; private set; }

        public BTNodeUtilityAction(BTNode node, IUtilityEvaluator evaluator, string actionName)
        {
            this.wrappedNode = node;
            this.evaluator = evaluator;
            this.ActionName = actionName;
        }

        public float CalculateUtility(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            return evaluator.Evaluate(agent, blackboard, context);
        }

        public NodeStatus Execute(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            return wrappedNode.Tick(agent, blackboard, context);
        }
    }
} 