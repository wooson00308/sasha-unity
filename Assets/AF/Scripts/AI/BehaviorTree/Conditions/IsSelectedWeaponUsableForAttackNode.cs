using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine; // Debug.Log 용 (필요시 ServiceLocator를 통해 TextLoggerService 사용)

namespace AF.AI.BehaviorTree.Conditions
{
    /// <summary>
    /// 블랙보드에 선택된 무기(SelectedWeapon)가 현재 공격 가능한 상태인지 확인하는 조건 노드입니다.
    /// (작동 가능, 탄약 있음, 재장전 중 아님)
    /// </summary>
    public class IsSelectedWeaponUsableForAttackNode : ConditionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            Weapon selectedWeapon = blackboard.SelectedWeapon;

            if (selectedWeapon == null)
            {
                 var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                 textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: No weapon selected in Blackboard. Failure.", LogLevel.Debug);
                return NodeStatus.Failure; // 선택된 무기가 없으면 실패
            }

            // 무기가 작동 가능하고, 탄약이 있고, 재장전 중이 아닌지 확인
            bool isUsable = selectedWeapon.IsOperational && selectedWeapon.HasAmmo() && !selectedWeapon.IsReloading;

            if (isUsable)
            {
                 var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                 textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected weapon ({selectedWeapon.Name}) is usable for attack. Success.", LogLevel.Debug);
                return NodeStatus.Success; // 공격 가능하면 성공
            }
            else
            {
                 var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                 textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected weapon ({selectedWeapon.Name}) is NOT usable for attack (Operational: {selectedWeapon.IsOperational}, HasAmmo: {selectedWeapon.HasAmmo()}, IsReloading: {selectedWeapon.IsReloading}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure; // 공격 불가능하면 실패
            }
        }
    }
} 