using AF.Models;
using UnityEngine;
using AF.Combat;

namespace AF.AI.BehaviorTree.Conditions
{
    public class HasSelectedWeaponNode : ConditionNode
    {
        public HasSelectedWeaponNode()
        {
            // Constructor can be empty
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (blackboard == null)
            {
                Debug.LogError("HasSelectedWeaponNode: Blackboard is null.");
                return NodeStatus.Failure;
            }

            if (blackboard.SelectedWeapon != null && blackboard.SelectedWeapon.IsOperational)
            {
                // Optionally log: Debug.Log($"[BT] {agent?.Name} has a selected weapon: {blackboard.SelectedWeapon.Name}");
                return NodeStatus.Success;
            }
            else
            {
                // Optionally log: Debug.Log($"[BT] {agent?.Name} does not have a valid selected weapon.");
                return NodeStatus.Failure;
            }
        }
    }
} 