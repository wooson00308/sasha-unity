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
            var actualLogger = context?.Logger?.TextLogger;
            Weapon weaponToReload = blackboard.WeaponToReload; // 블랙보드에서 재장전할 무기 가져오기

            if (weaponToReload == null)
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: No weapon specified in blackboard.WeaponToReload. Failure.", LogLevel.Warning);
                return NodeStatus.Failure;
            }

            // AP 비용은 HasEnoughAPNode에서 이미 확인했다고 가정.
            // 여기서는 실제 재장전 로직만 수행.

            // CombatActionExecutor를 통해 재장전 실행 요청
            // Execute 메서드가 Reload 액션에 대해 weapon 파라미터를 사용하도록 수정되었다고 가정
            bool success = context.ActionExecutor.Execute(
                context, 
                agent, 
                CombatActionEvents.ActionType.Reload, 
                null, // targetFrame for Reload is null
                null, // targetPosition for Reload is null
                weaponToReload); // 재장전할 무기 전달

            if (success)
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Reload action for '{weaponToReload.Name}' initiated/completed. Success.", LogLevel.Debug);
                blackboard.WeaponToReload = null; // 재장전 작업 시작/완료 후 블랙보드에서 해당 정보 제거
                // blackboard.SelectedWeapon은 NeedsReloadNode에서 이미 null로 설정했을 것이므로 여기서 다시 할 필요는 없음
                return NodeStatus.Success;
            }
            else
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Reload action for '{weaponToReload.Name}' failed to execute. Failure.", LogLevel.Warning);
                // 실패 시 WeaponToReload를 null로 할지는 정책에 따라 결정 (예: 다음 틱에 재시도 가능하게 둘 수도 있음)
                // 여기서는 일단 그대로 둠. 하지만 보통은 실패하면 다음 턴에 다시 시도할 것이므로 null로 해도 무방.
                // blackboard.WeaponToReload = null; 
                return NodeStatus.Failure;
            }
        }
    }
} 