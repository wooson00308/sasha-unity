using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// Action node that decides to move the agent away from the current target, 
    /// dynamically calculating the separation distance based on the selected weapon's minimum range.
    /// </summary>
    public class MoveAwayFromTargetNode : ActionNode
    {
        // desiredSeparationDistance is no longer taken in constructor.
        // It will be dynamically determined.
        private const float DEFAULT_SEPARATION_BUFFER = 2.0f; // Default buffer to add to MinRange
        private const float FALLBACK_SEPARATION_DISTANCE = 5.0f; // Fallback if no weapon or MinRange is not suitable

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveAwayFromTargetNode"/> class.
        /// </summary>
        public MoveAwayFromTargetNode()
        {
            // Constructor is now parameterless
        }

        /// <summary>
        /// Called each frame to execute the action.
        /// Sets the blackboard's IntendedMovePosition to a point away from the current target.
        /// The distance is based on the selected weapon's MinRange + a buffer, or a fallback distance.
        /// </summary>
        /// <param name="agent">The agent this node is attached to.</param>
        /// <param name="blackboard">The blackboard for data sharing.</param>
        /// <param name="context">The combat context.</param>
        /// <returns>Success if a move away intention is set, Failure otherwise (e.g., no target).</returns>
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null)
            {
                return NodeStatus.Failure;
            }

            ArmoredFrame currentTarget = blackboard.CurrentTarget;

            if (currentTarget == null || currentTarget.IsDestroyed)
            {
                return NodeStatus.Failure;
            }

            Vector3 directionAwayFromTarget = (agent.Position - currentTarget.Position).normalized;

            if (directionAwayFromTarget == Vector3.zero)
            {
                directionAwayFromTarget = Random.insideUnitSphere.normalized;
                directionAwayFromTarget.y = 0;
                if (directionAwayFromTarget == Vector3.zero) directionAwayFromTarget = Vector3.forward;
                directionAwayFromTarget.Normalize();
            }
            
            float actualSeparationDistance;
            Weapon selectedWeapon = blackboard.SelectedWeapon;

            // Determine separation distance based on weapon's MinRange
            // Similar logic to IsTargetTooCloseNode: only consider moving away if MinRange is significant.
            if (selectedWeapon != null && selectedWeapon.MinRange >= 1.0f) 
            {
                actualSeparationDistance = selectedWeapon.MinRange + DEFAULT_SEPARATION_BUFFER;
            }
            else
            {
                actualSeparationDistance = FALLBACK_SEPARATION_DISTANCE;
            }
            
            Vector3 intendedPosition = agent.Position + directionAwayFromTarget * actualSeparationDistance;

            blackboard.IntendedMovePosition = intendedPosition;
            blackboard.DecidedActionType = AF.Combat.CombatActionEvents.ActionType.Move; 

            return NodeStatus.Success;
        }
    }
} 