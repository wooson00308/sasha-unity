using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.Actions // 경로에 맞게 네임스페이스 수정
{
    public class RestorePreservedRepairTargetNode : BTNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var preservedTarget = blackboard.GetData<ArmoredFrame>("PreservedTarget_Ally");
            var preservedPartSlot = blackboard.GetData<string>("PreservedPartSlot_Ally");

            if (preservedTarget != null && !string.IsNullOrEmpty(preservedPartSlot))
            {
                blackboard.CurrentTarget = preservedTarget;
                blackboard.TargetPartSlot = preservedPartSlot;
                // context.Logger?.TextLogger?.Log($"[BT DEBUG] {agent.Name}: Restored Target: {blackboard.CurrentTarget.Name}, PartSlot: {blackboard.TargetPartSlot}", LogLevel.Debug);
                
                // blackboard.ClearData("PreservedTarget_Ally"); // 복원 후 즉시 삭제하지 않음!
                // blackboard.ClearData("PreservedPartSlot_Ally"); // 이동 또는 수리 완료/불가 시 삭제하도록 변경 필요
                return NodeStatus.Success;
            }
            return NodeStatus.Failure;
        }
    }
} 