using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// Checks if the current target is too close, based on the selected weapon's minimum range.
    /// </summary>
    public class IsTargetTooCloseNode : ConditionNode
    {
        // No longer takes minimumDistanceThreshold in constructor
        // It will be dynamically determined from the selected weapon.

        /// <summary>
        /// Initializes a new instance of the <see cref="IsTargetTooCloseNode"/> class.
        /// </summary>
        public IsTargetTooCloseNode()
        {
            // Constructor is now parameterless
        }

        /// <summary>
        /// Called each frame to evaluate the condition.
        /// Checks if the current target is closer than the selected weapon's effective minimum range.
        /// </summary>
        /// <param name="agent">The agent this node is attached to.</param>
        /// <param name="blackboard">The blackboard for data sharing.</param>
        /// <param name="context">The combat context.</param>
        /// <returns>Success if the target is too close according to weapon's MinRange, Failure otherwise.</returns>
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null)
            {
                return NodeStatus.Failure; 
            }

            ArmoredFrame currentTarget = blackboard.CurrentTarget;
            Weapon selectedWeapon = blackboard.SelectedWeapon; // Assuming Weapon type is available on Blackboard

            if (currentTarget == null || currentTarget.IsDestroyed || selectedWeapon == null)
            {
                // No valid target or no weapon selected, so cannot determine if "too close"
                return NodeStatus.Failure;
            }

            float actualMinDistanceThreshold = selectedWeapon.MinRange; // Assuming Weapon class has MinRange property

            // If the weapon's minimum range is very small (e.g., 0 for melee, or < 1m for some point-blank ranged),
            // it means the weapon is effective at this close distance. 
            // Thus, for the purpose of *moving away*, this condition should not trigger.
            // This threshold (1.0f) can be adjusted based on game design.
            if (actualMinDistanceThreshold < 1.0f) 
            {
                return NodeStatus.Failure;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, currentTarget.Position);

            if (distanceToTarget <= actualMinDistanceThreshold)
            {
                // Target is within or at the weapon's significant minimum engagement distance
                return NodeStatus.Success;
            }

            return NodeStatus.Failure;
        }
    }
} 