using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Conditions
{
    public class IsTargetInAttackRangeNode : ConditionNode
    {
        public IsTargetInAttackRangeNode()
        {
            // Constructor can be empty or take parameters if needed in the future
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null || blackboard.CurrentTarget == null || blackboard.SelectedWeapon == null)
            {
                // Debug.Log($"[BT] {agent?.Name} - IsTargetInAttackRangeNode: Missing agent, blackboard, target, or selected weapon.");
                return NodeStatus.Failure;
            }

            if (!blackboard.CurrentTarget.IsOperational || !blackboard.SelectedWeapon.IsOperational)
            {
                // Debug.Log($"[BT] {agent?.Name} - IsTargetInAttackRangeNode: Target or weapon is not operational.");
                return NodeStatus.Failure;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, blackboard.CurrentTarget.Position);
            float attackRange = blackboard.SelectedWeapon.MaxRange;

            if (distanceToTarget <= attackRange)
            {
                // Debug.Log($"[BT] {agent?.Name} - Target {blackboard.CurrentTarget.Name} is IN attack range ({distanceToTarget} <= {attackRange}).");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[BT] {agent?.Name} - Target {blackboard.CurrentTarget.Name} is OUT of attack range ({distanceToTarget} > {attackRange}).");
                return NodeStatus.Failure;
            }
        }
    }
} 