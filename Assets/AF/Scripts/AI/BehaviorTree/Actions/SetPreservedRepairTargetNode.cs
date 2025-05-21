using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Actions // 경로에 맞게 네임스페이스 수정
{
    public class SetPreservedRepairTargetNode : BTNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (blackboard.CurrentTarget != null && !string.IsNullOrEmpty(blackboard.TargetPartSlot))
            {
                blackboard.SetData("PreservedTarget_Ally", blackboard.CurrentTarget);
                blackboard.SetData("PreservedPartSlot_Ally", blackboard.TargetPartSlot);
                // context.Logger?.TextLogger?.Log($"[BT DEBUG] {agent.Name}: Preserving Target: {blackboard.CurrentTarget.Name}, PartSlot: {blackboard.TargetPartSlot}", LogLevel.Debug);
            }
            return NodeStatus.Success;
        }
    }
} 