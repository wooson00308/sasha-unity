using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Actions
{
    public class ClearPreservedRepairDataNode : BTNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            blackboard.ClearData("PreservedTarget_Ally");
            blackboard.ClearData("PreservedPartSlot_Ally");
            // context.Logger?.TextLogger?.Log($"[BT DEBUG] {agent.Name}: Cleared Preserved Repair Data.", LogLevel.Debug);
            return NodeStatus.Success;
        }
    }
} 