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

        private float? _desiredStoppingDistance; // Nullable float for specific stopping distance

        // 생성자에서 파라미터 제거
        public MoveToTargetNode()
        {
            _desiredStoppingDistance = null;
            // 필드 초기화 제거
            // this.storedUnit = unit;
            // this.storedContext = context;
        }

        // New constructor for specific stopping distance
        public MoveToTargetNode(float desiredStoppingDistance)
        {
            _desiredStoppingDistance = desiredStoppingDistance;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;

            if (blackboard.CurrentTarget == null || blackboard.CurrentTarget.IsDestroyed)
            {
                textLogger?.Log($"[{GetType().Name}] {agent.Name}: CT null/destroyed. Failure.", LogLevel.Debug);
                blackboard.IntendedMovePosition = null;
                blackboard.DecidedActionType = null;
                return NodeStatus.Failure;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, blackboard.CurrentTarget.Position);

            if (_desiredStoppingDistance.HasValue)
            {
                // Mode 1: Move to a specific stopping distance
                float stoppingDist = _desiredStoppingDistance.Value;
                // Allow a small tolerance to prevent jittering or endless attempts if already very close
                if (Mathf.Abs(distanceToTarget - stoppingDist) < 0.5f) 
                {
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} already at desired distance ({stoppingDist}m) from {blackboard.CurrentTarget.Name}. No move needed.", LogLevel.Debug);
                    blackboard.IntendedMovePosition = null;
                    blackboard.DecidedActionType = null; // No move action decided by this node
                    return NodeStatus.Success; 
                }

                // If not at the desired distance, calculate move
                Vector3 directionToTarget = (blackboard.CurrentTarget.Position - agent.Position).normalized;
                 if (directionToTarget == Vector3.zero) // Failsafe
                {
                    directionToTarget = (Random.insideUnitSphere).normalized; directionToTarget.y = 0; 
                    if (directionToTarget == Vector3.zero) directionToTarget = Vector3.forward; 
                    directionToTarget.Normalize();
                }
                blackboard.IntendedMovePosition = blackboard.CurrentTarget.Position - directionToTarget * stoppingDist;
                blackboard.DecidedActionType = CombatActionEvents.ActionType.Move;
                textLogger?.Log($"[{GetType().Name}] {agent.Name} moving towards {blackboard.CurrentTarget.Name} to custom stop distance: {stoppingDist}m.", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                // Mode 2: Original weapon-range based movement
                Weapon weaponToConsider = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
                if (weaponToConsider == null || !weaponToConsider.IsOperational)
                {
                    textLogger?.Log($"[{GetType().Name}] {agent.Name}: No usable weapon. Failure.", LogLevel.Debug);
                    blackboard.IntendedMovePosition = null;
                    blackboard.DecidedActionType = null;
                    return NodeStatus.Failure;
                }

                float minRange = weaponToConsider.MinRange;
                float maxRange = weaponToConsider.MaxRange;
                if (minRange > maxRange) { minRange = maxRange; }

                // Using squared distances for efficiency, but original log used Distance
                // Let's use Distance for consistency with logging, and check if already in range first
                bool isInOptimalRange = distanceToTarget >= minRange && distanceToTarget <= maxRange;
                // Special case for melee/very short range weapons where minRange is effectively 0
                if (minRange < 0.1f && distanceToTarget <= maxRange) isInOptimalRange = true;

                if (isInOptimalRange)
                {
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} already in weapon range of {blackboard.CurrentTarget.Name}. No move.", LogLevel.Debug);
                    blackboard.IntendedMovePosition = null;
                    blackboard.DecidedActionType = null;
                    return NodeStatus.Success;
                }
                
                // ... (rest of original move logic: calculate IntendedMovePosition based on weapon ranges) ...
                Vector3 directionToTargetNormalized = (blackboard.CurrentTarget.Position - agent.Position).normalized;
                if (directionToTargetNormalized == Vector3.zero) { /* Failsafe from above */ directionToTargetNormalized = Vector3.forward; }

                Vector3 calculatedIntendedPosition;
                if (distanceToTarget > maxRange) 
                {
                    float targetDist = Mathf.Max(0f, maxRange * 0.9f); 
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position - directionToTargetNormalized * targetDist;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} too far. Moving to weapon range {targetDist}m.", LogLevel.Debug);
                }
                else if (distanceToTarget < minRange && minRange > 0.1f) 
                {
                    float targetDist = minRange * 1.05f; 
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position - directionToTargetNormalized * targetDist;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} too close. Adjusting to weapon range {targetDist}m.", LogLevel.Debug);
                }
                else 
                {
                    calculatedIntendedPosition = blackboard.CurrentTarget.Position;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name} moving to target default weapon pos.", LogLevel.Debug);
                }
                blackboard.IntendedMovePosition = calculatedIntendedPosition;
                blackboard.DecidedActionType = CombatActionEvents.ActionType.Move;
                return NodeStatus.Success;
            }
        }
    }
} 