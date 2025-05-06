using AF.Combat;
using AF.Models;
using UnityEngine;

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
            if (blackboard.CurrentTarget == null || blackboard.CurrentTarget.IsDestroyed)
            {
                blackboard.IntendedMovePosition = null;
                return NodeStatus.Failure;
            }

            // 무기 선택 로직: Blackboard에 SelectedWeapon이 있으면 사용, 없으면 주무기 사용
            Weapon weaponToConsider = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();

            if (weaponToConsider == null || !weaponToConsider.IsOperational)
            {
                blackboard.IntendedMovePosition = null;
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
                // Already in optimal range for the primary weapon.
                blackboard.IntendedMovePosition = null; // Clear any prior move intention.
                // Debug.Log($"[MoveToTargetNode] {unit.Pilot.PilotName} is already in range of {unit.CurrentTarget.Pilot.PilotName}. SUCCESS.");
                return NodeStatus.Success;
            }
            else
            {
                // Not in range, needs to move.
                if (agent.CurrentAP < MIN_AP_TO_INITIATE_MOVE)
                {
                    // Not enough AP to even consider initiating a move.
                    blackboard.IntendedMovePosition = null;
                    // Debug.Log($"[MoveToTargetNode] {unit.Pilot.PilotName} has not enough AP ({unit.CurrentAP}) to initiate move. FAILURE.");
                    return NodeStatus.Failure;
                }

                // Set the intention to move towards the target.
                // The CombatActionExecutor will be responsible for pathfinding,
                // calculating the exact destination (e.g., closest attackable tile), and AP cost.
                blackboard.IntendedMovePosition = blackboard.CurrentTarget.Position;
                // Debug.Log($"[MoveToTargetNode] {unit.Pilot.PilotName} needs to move towards {unit.CurrentTarget.Pilot.PilotName}. Setting IntendedMovePosition. SUCCESS.");
                return NodeStatus.Success;
            }
        }
    }
} 