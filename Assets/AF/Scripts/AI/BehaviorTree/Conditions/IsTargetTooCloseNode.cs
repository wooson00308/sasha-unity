using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// Checks if the current target is too close.
    /// Can use an explicit kiting distance or default to weapon's MinRange + a default buffer.
    /// </summary>
    public class IsTargetTooCloseNode : ConditionNode
    {
        private readonly float _explicitKitingDistance = -1f; // Explicit distance to check against. -1f means not set.
        private const float DefaultMaxRangeRatioForSafetyMargin = 0.1f; // Safety margin as a ratio of MaxRange
        private const float MinimalSafetyMarginAbsolute = 1.0f;     // Minimum absolute safety margin

        /// <summary>
        /// Initializes a new instance of the <see cref="IsTargetTooCloseNode"/> class with a specific kiting distance.
        /// </summary>
        /// <param name="explicitKitingDistance">The specific distance to consider "too close".</param>
        public IsTargetTooCloseNode(float explicitKitingDistance)
        {
            this._explicitKitingDistance = explicitKitingDistance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsTargetTooCloseNode"/> class.
        /// Uses default kiting behavior (weapon's MinRange + DefaultKitingBuffer).
        /// </summary>
        public IsTargetTooCloseNode() : this(-1f) // Calls the other constructor with -1f to indicate no explicit distance
        {
        }

        /// <summary>
        /// Called each frame to evaluate the condition.
        /// Checks if the current target is closer than the effective kiting threshold.
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
            Weapon selectedWeapon = blackboard.SelectedWeapon;

            if (currentTarget == null || currentTarget.IsDestroyed || selectedWeapon == null)
            {
                return NodeStatus.Failure;
            }

            float kitingThreshold;

            if (_explicitKitingDistance >= 0f) // An explicit distance was provided
            {
                kitingThreshold = _explicitKitingDistance;
            }
            else // No explicit distance, use weapon's MinRange + default buffer
            {
                float safetyMargin = selectedWeapon.MaxRange * DefaultMaxRangeRatioForSafetyMargin;
                safetyMargin = Mathf.Max(safetyMargin, MinimalSafetyMarginAbsolute);
                kitingThreshold = selectedWeapon.MinRange + safetyMargin;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, currentTarget.Position);

            if (distanceToTarget < kitingThreshold) // Strictly less than the threshold means "too close"
            {
                // Target is within the kiting threshold
                return NodeStatus.Success;
            }

            return NodeStatus.Failure;
        }
    }
} 