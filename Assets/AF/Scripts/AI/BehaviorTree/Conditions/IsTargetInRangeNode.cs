using AF.Combat;
using AF.Models;
using UnityEngine; // Vector3.Distance 때문에 추가
using AF.Services;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 현재 목표 대상이 블랙보드에 설정된 무기(또는 주무기)의 유효 사거리 내에 있는지 검사하는 조건 노드입니다.
    /// </summary>
    public class IsTargetInRangeNode : ConditionNode
    {
        private float? _customRange; // Nullable float for custom range

        // Existing constructor for weapon-based range check
        public IsTargetInRangeNode() 
        {
            _customRange = null;
        }

        // New constructor for custom range check
        public IsTargetInRangeNode(float customRange)
        {
            _customRange = customRange;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            ArmoredFrame currentTarget = blackboard.CurrentTarget;
            if (currentTarget == null || currentTarget.IsDestroyed)
            {
                return NodeStatus.Failure;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, currentTarget.Position);
            bool isInRange;
            string rangeSourceForLog;
            string rangeValuesForLog;

            if (_customRange.HasValue)
            {
                // Use custom range if provided
                isInRange = distanceToTarget <= _customRange.Value;
                rangeSourceForLog = "Custom";
                rangeValuesForLog = $"(0.0-{_customRange.Value:F1})";
            }
            else
            {
                // Use weapon-based range if custom range is not provided
                Weapon weaponToUse = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
                if (weaponToUse == null || !weaponToUse.IsOperational)
                {
                    var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLogger?.Log($"[{GetType().Name}] {agent.Name}: No usable weapon. Failure.", LogLevel.Debug);
                    return NodeStatus.Failure;
                }
                isInRange = distanceToTarget >= weaponToUse.MinRange && distanceToTarget <= weaponToUse.MaxRange;
                rangeSourceForLog = weaponToUse.Name;
                rangeValuesForLog = $"({weaponToUse.MinRange:F1}-{weaponToUse.MaxRange:F1})";
            }

            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            logger?.Log(
                $"[{GetType().Name}] {agent.Name} to {currentTarget.Name}: " +
                $"Source='{rangeSourceForLog}', Dist={distanceToTarget:F1}, " +
                $"Range={rangeValuesForLog}, InRange={isInRange}. " +
                $"Result: {(isInRange ? NodeStatus.Success : NodeStatus.Failure)}",
                LogLevel.Debug
            );

            return isInRange ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
} 