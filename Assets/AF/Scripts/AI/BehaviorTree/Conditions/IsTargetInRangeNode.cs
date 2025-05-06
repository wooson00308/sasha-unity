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
        // 생성자에서 특정 무기 슬롯을 받지 않음.
        // 검사할 무기는 사전에 Blackboard에 SelectedWeapon으로 설정되어 있거나, agent의 주무기를 사용.
        public IsTargetInRangeNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            ArmoredFrame currentTarget = blackboard.CurrentTarget;
            if (currentTarget == null || currentTarget.IsDestroyed)
            {
                return NodeStatus.Failure;
            }

            // 검사할 무기 결정: Blackboard에 SelectedWeapon이 있으면 그것을 사용, 없으면 agent의 주무기 사용
            Weapon weaponToUse = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
            if (weaponToUse == null || !weaponToUse.IsOperational)
            {
                var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                textLogger?.Log($"[{GetType().Name}] {agent.Name}: No usable weapon found (Selected: {blackboard.SelectedWeapon?.Name ?? "null"}, Primary: {agent.GetPrimaryWeapon()?.Name ?? "null"}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            float distanceToTarget = Vector3.Distance(agent.Position, currentTarget.Position);
            bool isInRange = distanceToTarget >= weaponToUse.MinRange && distanceToTarget <= weaponToUse.MaxRange;

            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            logger?.Log(
                $"[{GetType().Name}] {agent.Name} to {currentTarget.Name}: " +
                $"Weapon='{weaponToUse.Name}', Dist={distanceToTarget:F1}, " +
                $"Range=({weaponToUse.MinRange:F1}-{weaponToUse.MaxRange:F1}), InRange={isInRange}. " +
                $"Result: {(isInRange ? NodeStatus.Success : NodeStatus.Failure)}",
                LogLevel.Debug
            );

            return isInRange ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
} 