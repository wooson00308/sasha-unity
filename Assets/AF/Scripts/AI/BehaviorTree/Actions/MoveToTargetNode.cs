using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.BehaviorTree.Actions
{
    public class MoveToTargetNode : BTNode
    {
        // Execute 메서드가 context와 unit을 파라미터로 받으므로,
        // 생성자에서 미리 받아 저장할 필요가 없습니다.
        // ArmoredFrame storedUnit;
        // CombatContext storedContext;

        // This is a basic check. The actual AP cost will be determined by the path length and unit's move stats,
        // handled by the CombatActionExecutor or a similar system.
        const int MIN_AP_TO_INITIATE_MOVE = 1;

        // 생성자에서 파라미터 제거
        public MoveToTargetNode()
        {
            // 필드 초기화 제거
            // this.storedUnit = unit;
            // this.storedContext = context;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;

            if (blackboard.CurrentTarget == null || blackboard.CurrentTarget.IsDestroyed)
            {
                textLogger?.Log($"[{GetType().Name}] {agent.Name}: CurrentTarget is null or destroyed. Failure.", LogLevel.Debug);
                blackboard.IntendedMovePosition = null;
                blackboard.DecidedActionType = null;
                return NodeStatus.Failure;
            }

            // 무기 선택 로직: Blackboard에 SelectedWeapon이 있으면 사용, 없으면 주무기 사용
            Weapon weaponToConsider = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();

            if (weaponToConsider == null || !weaponToConsider.IsOperational)
            {
                textLogger?.Log($"[{GetType().Name}] {agent.Name}: No usable weapon to consider for move range check. Failure.", LogLevel.Debug);
                blackboard.IntendedMovePosition = null;
                blackboard.DecidedActionType = null;
                return NodeStatus.Failure;
            }

            float distanceSqr = (agent.Position - blackboard.CurrentTarget.Position).sqrMagnitude;
            float minRange = weaponToConsider.MinRange;
            float maxRange = weaponToConsider.MaxRange;
            // Ensure minRange is not greater than maxRange to avoid issues with range checks
            if (minRange > maxRange) 
            {
                minRange = maxRange; 
            }

            float minRangeSqr = minRange * minRange;
            float maxRangeSqr = maxRange * maxRange;

            bool isInRange = distanceSqr >= minRangeSqr && distanceSqr <= maxRangeSqr;

            if (isInRange)
            {
                textLogger?.Log($"[{GetType().Name}] {agent.Name} is already in optimal range of {blackboard.CurrentTarget.Name} with {weaponToConsider.Name}. No move needed.", LogLevel.Debug);
                blackboard.IntendedMovePosition = null; 
                blackboard.DecidedActionType = null;
                return NodeStatus.Success; // Already in optimal range, no action decided by this node
            }
            else
            {
                // Not in range, needs to move.
                if (agent.CurrentAP < MIN_AP_TO_INITIATE_MOVE)
                {
                    textLogger?.Log($"[{GetType().Name}] {agent.Name}: Not enough AP ({agent.CurrentAP}) to move towards {blackboard.CurrentTarget.Name}. Required: {MIN_AP_TO_INITIATE_MOVE}. Failure.", LogLevel.Debug);
                    blackboard.IntendedMovePosition = null;
                    blackboard.DecidedActionType = null;
                    return NodeStatus.Failure;
                }

                Vector3 directionToTarget = (blackboard.CurrentTarget.Position - agent.Position).normalized;
                if (directionToTarget == Vector3.zero) // Failsafe if agent and target are at the same exact position
                {
                    directionToTarget = (Random.insideUnitSphere).normalized;
                    directionToTarget.y = 0; 
                    if (directionToTarget == Vector3.zero) directionToTarget = Vector3.forward;
                    directionToTarget.Normalize();
                }

                Vector3 calculatedIntendedPosition;

                if (distanceSqr > maxRangeSqr) // Too far, move to just inside MaxRange
                {
                    float targetDist = Mathf.Max(0f, maxRange * 0.9f); // Aim for 90% of maxRange
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position - directionToTarget * targetDist;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} is too far. Moving towards {blackboard.CurrentTarget.Name} to engage at ~{targetDist}m (MaxRange: {maxRange}m).", LogLevel.Debug);
                }
                else if (distanceSqr < minRangeSqr && minRange > 0.1f) // Too close for a ranged weapon, move to just outside MinRange
                {
                    float targetDist = minRange * 1.05f; // Aim for 105% of minRange (or just minRange)
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position - directionToTarget * targetDist;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} is too close. Adjusting position relative to {blackboard.CurrentTarget.Name} to ~{targetDist}m (MinRange: {minRange}m).", LogLevel.Debug);
                }
                else // Default: Melee (minRange is 0 or very small) or other unhandled cases when !isInRange
                {
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} moving to target {blackboard.CurrentTarget.Name} default position (MinRange: {minRange}m, MaxRange: {maxRange}m).", LogLevel.Debug);
                }
                
                blackboard.IntendedMovePosition = calculatedIntendedPosition;
                blackboard.DecidedActionType = CombatActionEvents.ActionType.Move; 
                
                return NodeStatus.Success;
            }
        }
    }
} 