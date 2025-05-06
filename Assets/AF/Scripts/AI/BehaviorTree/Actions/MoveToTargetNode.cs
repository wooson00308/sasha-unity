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
                textLogger?.Log($"[{GetType().Name}] {agent.Name}: No usable weapon to consider for move range check (Selected: {blackboard.SelectedWeapon?.Name ?? "null"}, Primary: {agent.GetPrimaryWeapon()?.Name ?? "null"}). Failure.", LogLevel.Debug);
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
                textLogger?.Log($"[{GetType().Name}] {agent.Name} is already in range of {blackboard.CurrentTarget.Name} with {weaponToConsider.Name}. No move needed. Success (but no action decided by this node).", LogLevel.Debug);
                blackboard.IntendedMovePosition = null; 
                blackboard.DecidedActionType = null;
                return NodeStatus.Success;
            }
            else
            {
                // Not in range, needs to move.
                if (agent.CurrentAP < MIN_AP_TO_INITIATE_MOVE)
                {
                    textLogger?.Log($"[{GetType().Name}] {agent.Name}: Not enough AP ({agent.CurrentAP}) to initiate move towards {blackboard.CurrentTarget.Name}. Required: {MIN_AP_TO_INITIATE_MOVE}. Failure.", LogLevel.Debug);
                    blackboard.IntendedMovePosition = null;
                    blackboard.DecidedActionType = null;
                    return NodeStatus.Failure;
                }

                // Set the intention to move towards the target.
                // The CombatActionExecutor will be responsible for pathfinding,
                // calculating the exact destination (e.g., closest attackable tile), and AP cost.
                blackboard.IntendedMovePosition = blackboard.CurrentTarget.Position;
                blackboard.DecidedActionType = CombatActionEvents.ActionType.Move; 
                
                textLogger?.Log($"[{GetType().Name}] {agent.Name} needs to move towards {blackboard.CurrentTarget.Name}. Set IntendedMovePosition. Set DecidedActionType=Move. Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }
        }
    }
} 