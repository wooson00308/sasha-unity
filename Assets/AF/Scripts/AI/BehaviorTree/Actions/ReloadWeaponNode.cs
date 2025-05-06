using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// 블랙보드에 지정된 무기 또는 주무기를 재장전할 것을 결정하는 액션 노드입니다.
    /// 실제 재장전 및 AP 소모는 외부 시스템에서 처리한다고 가정합니다.
    /// </summary>
    public class ReloadWeaponNode : ActionNode
    {
        // 생성자에서 특정 무기 슬롯을 받지 않음.
        public ReloadWeaponNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // 재장전할 무기 결정: Blackboard에 SelectedWeapon이 있으면 그것을 사용, 없으면 주무기 사용
            Weapon weaponToReload = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();

            if (weaponToReload == null || !weaponToReload.IsOperational)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: No valid weapon to reload.");
                return NodeStatus.Failure;
            }

            bool needsReload = weaponToReload.MaxAmmo > 0 && weaponToReload.CurrentAmmo < weaponToReload.MaxAmmo && !weaponToReload.IsReloading;
            float reloadAPCost = weaponToReload.ReloadAPCost;

            if (!needsReload)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: {weaponToReload.WeaponName} does not need reload.");
                return NodeStatus.Failure; // 재장전 필요 없음
            }

            // AP 체크는 agent의 현재 AP를 사용
            if (agent.CurrentAP < reloadAPCost)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: Not enough AP ({agent.CurrentAP}) to reload {weaponToReload.WeaponName} (needs {reloadAPCost} AP).");
                return NodeStatus.Failure; // AP 부족
            }

            // 재장전 의사를 블랙보드에 기록
            blackboard.DecidedActionType = CombatActionEvents.ActionType.Reload;
            blackboard.SelectedWeapon = weaponToReload; // 어떤 무기를 재장전할지 명시
            // Debug.Log($"[{GetType().Name}] {agent.name}: Decided to reload {weaponToReload.WeaponName}. Success.");
            return NodeStatus.Success;
        }
    }
} 