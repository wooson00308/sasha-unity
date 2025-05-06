using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 무기(또는 주무기)가 재장전을 필요로 하는지 검사하는 조건 노드입니다.
    /// </summary>
    public class NeedsReloadNode : ConditionNode
    {
        public NeedsReloadNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // 검사할 무기 결정: Blackboard에 SelectedWeapon이 있으면 그것을 사용, 없으면 agent의 주무기 사용
            Weapon weaponToCheck = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();

            if (weaponToCheck == null || !weaponToCheck.IsOperational)
            {
                return NodeStatus.Failure;
            }

            // 무한 탄약이 아니고, 현재 탄약이 최대치보다 적고, 현재 재장전 중이 아니면 재장전 필요
            bool needsReload = weaponToCheck.MaxAmmo > 0 &&
                               weaponToCheck.CurrentAmmo < weaponToCheck.MaxAmmo &&
                               !weaponToCheck.IsReloading;

            if (needsReload)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: {weaponToCheck.WeaponName} needs reload. Success.");
                return NodeStatus.Success;
            }
            else
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: {weaponToCheck.WeaponName} does not need reload or is already reloading. Failure.");
                return NodeStatus.Failure;
            }
        }
    }
} 