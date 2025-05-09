using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Conditions
{
    /// <summary>
    /// 에이전트가 장착한 무기 중 하나라도 현재 재장전 중인지 확인하는 조건 노드입니다.
    /// </summary>
    public class IsAnyWeaponReloadingNode : ConditionNode // 또는 BTNode를 직접 상속
    {
        public IsAnyWeaponReloadingNode()
        {
            // 필요시 초기화
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null)
            {
                return NodeStatus.Failure;
            }

            var weapons = agent.GetAllWeapons();
            if (weapons == null || weapons.Count == 0)
            {
                return NodeStatus.Failure; // 무기가 없으면 재장전 중일 수 없음
            }

            foreach (Weapon weapon in weapons)
            {
                if (weapon != null && weapon.IsReloading)
                {
                    // context?.Logger?.TextLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weapon.Name} is reloading. Success.", LogLevel.Debug);
                    return NodeStatus.Success; // 하나라도 재장전 중이면 성공
                }
            }

            // context?.Logger?.TextLogger?.Log($"[{this.GetType().Name}] {agent.Name}: No weapon is currently reloading. Failure.", LogLevel.Debug);
            return NodeStatus.Failure; // 어떤 무기도 재장전 중이 아니면 실패
        }
    }
} 