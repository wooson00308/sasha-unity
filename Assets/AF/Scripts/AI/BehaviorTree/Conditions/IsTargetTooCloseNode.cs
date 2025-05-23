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
        private const float DefaultMaxRangeRatioForSafetyMargin = 0.1f; // Safety margin as a ratio of MaxRange
        private const float MinimalSafetyMarginAbsolute = 1.0f;     // Minimum absolute safety margin

        /// <summary>
        /// Initializes a new instance of the <see cref="IsTargetTooCloseNode"/> class.
        /// Uses weapon's MinRange + a default buffer as the kiting threshold.
        /// </summary>
        public IsTargetTooCloseNode()
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

            float safetyMargin = MinimalSafetyMarginAbsolute; // 고정 안전 마진 1.0m
            kitingThreshold = selectedWeapon.MinRange + safetyMargin;
            
            // MinRange가 매우 작거나 0에 가까울 때는 safetyMargin만 사용 (너무 가까운 기준)
            if (selectedWeapon.MinRange < MinimalSafetyMarginAbsolute) // MinRange가 안전 마진보다 작으면
            {
                kitingThreshold = MinimalSafetyMarginAbsolute; // 안전 마진 자체를 기준으로 사용
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