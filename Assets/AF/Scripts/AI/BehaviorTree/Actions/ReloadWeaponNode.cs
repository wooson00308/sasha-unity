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
            Weapon weaponToReload = blackboard.WeaponToReload; 

            if (weaponToReload == null)
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Blackboard에 WeaponToReload가 지정되지 않음. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            // 이미 재장전이 완료되었거나 더 이상 재장전할 필요가 없는 상태인지 확인 (예: 탄약 가득 참)
            if (!weaponToReload.IsReloading && (weaponToReload.MaxAmmo > 0 && weaponToReload.CurrentAmmo >= weaponToReload.MaxAmmo))
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weaponToReload.Name} 이미 탄약 가득 또는 재장전 불필요. Success (행동 결정 안함).", LogLevel.Debug);
                blackboard.WeaponToReload = null; // 더 이상 이 무기를 재장전 대상으로 고려하지 않음
                return NodeStatus.Success; // 재장전 행동을 결정할 필요 없음
            }

            // 현재 무기가 재장전 중인지 확인 (IsReloading 플래그와 실제 완료 여부)
            if (weaponToReload.IsReloading)
            {
                // Weapon.CheckReloadCompletion은 내부적으로 FinishReload를 호출하고 완료 시 true 반환
                bool justCompleted = weaponToReload.CheckReloadCompletion(context.CurrentTurn);
                if (justCompleted)
                {
                    actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weaponToReload.Name} 이번 틱에 재장전 완료됨. Success (행동 결정 안함).", LogLevel.Debug);
                    blackboard.WeaponToReload = null; 
                    return NodeStatus.Success; // 재장전 행동을 결정할 필요 없음
                }
                else
                {
                    // 아직 재장전 중이므로, BT는 다른 행동을 찾아야 함 (예: 재장전 중 방어/회피)
                    // 이 노드는 "재장전 시작 결정" 노드이므로, 이미 진행 중일 땐 Failure를 반환하여 다른 경로 탐색 유도 가능
                    // 또는, Running을 반환하여 상위에서 이 상태를 활용하도록 할 수도 있음.
                    // 현재 로직 (재장전 중 다른 행동)을 위해서는 여기서 Failure가 맞을 수 있음.
                    actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weaponToReload.Name} 여전히 재장전 진행 중. Failure (다른 행동 우선).", LogLevel.Debug);
                    return NodeStatus.Failure; 
                }
            }

            // 재장전 시작 결정 단계
            float requiredAP = weaponToReload.ReloadAPCost; // 무기 자체의 재장전 AP 비용 사용
            if (!agent.HasEnoughAP(requiredAP)) // ArmoredFrame의 AP 확인 메서드 사용
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weaponToReload.Name} 재장전을 위한 AP 부족 ({agent.CurrentAP}/{requiredAP}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            // 재장전 행동을 결정함
            blackboard.DecidedActionType = CombatActionEvents.ActionType.Reload;
            // 선택된 무기는 재장전 대상인 weaponToReload (이미 블랙보드에 있음)
            // blackboard.SelectedWeapon = weaponToReload; // 공격용이 아니므로 설정 불필요
            actualLogger?.Log($"[{this.GetType().Name}] {agent.Name}: {weaponToReload.Name} 재장전 결정. Success.", LogLevel.Debug);
            return NodeStatus.Success;
        }
    }
} 