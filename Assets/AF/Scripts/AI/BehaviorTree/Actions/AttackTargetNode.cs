using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 용
using System.Linq; // Contains 확장 메서드를 사용하기 위해 추가

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 현재 목표 대상을 블랙보드에 설정된 무기로 공격할 것을 결정하는 행동 노드입니다.
    /// 이 노드는 실제 공격을 실행하지 않고, 공격 의사를 블랙보드에 기록합니다.
    /// </summary>
    public class AttackTargetNode : ActionNode
    {
        // 생성자에서 특정 무기 슬롯을 받지 않음.
        // 사용할 무기는 사전에 Blackboard에 SelectedWeapon으로 설정되어 있어야 함.
        public AttackTargetNode() {}

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            ArmoredFrame currentTarget = blackboard.CurrentTarget;
            Weapon selectedWeapon = blackboard.SelectedWeapon;

            // 1. 타겟 유효성 확인
            if (currentTarget == null || currentTarget.IsDestroyed)
            {
                //Debug.Log($"[{this.GetType().Name}] {agent.name}: Attack failed - No valid target on blackboard.");
                return NodeStatus.Failure;
            }

            // 2. 무기 유효성 확인
            if (selectedWeapon == null)
            {
                //Debug.Log($"[{this.GetType().Name}] {agent.name}: Attack failed - No selected weapon on blackboard.");
                return NodeStatus.Failure;
            }

            // 3. 선택된 무기가 실제로 agent가 장착한 무기인지 확인 (선택적이지만 안전함)
            if (!agent.EquippedWeapons.Contains(selectedWeapon))
            {
                //Debug.LogWarning($"[{this.GetType().Name}] {agent.name}: Attack failed - Selected weapon '{selectedWeapon.WeaponName}' is not equipped by the agent.");
                return NodeStatus.Failure;
            }

            // 4. 무기 준비 상태 확인 (탄약, 재장전 등)
            if (!selectedWeapon.IsOperational || selectedWeapon.IsReloading || !selectedWeapon.HasAmmo())
            {
                // Debug.Log($"[{this.GetType().Name}] {agent.name}: Attack failed - Weapon '{selectedWeapon.WeaponName}' is not ready (Operational: {selectedWeapon.IsOperational}, Reloading: {selectedWeapon.IsReloading}, Ammo: {selectedWeapon.HasAmmo()}).");
                return NodeStatus.Failure;
            }

            // (옵션) 5. AP 및 사거리 검사 - 이 책임은 다른 Condition 노드에 맡기는 것이 일반적임
            // float requiredAP = context.ActionExecutor.CalculateAttackAPCost(agent, selectedWeapon); // 예시, 실제 AP 계산기 필요
            // if (!agent.HasEnoughAP(requiredAP)) return NodeStatus.Failure;
            // float distanceToTarget = Vector3.Distance(agent.Position, currentTarget.Position);
            // if (distanceToTarget < selectedWeapon.MinRange || distanceToTarget > selectedWeapon.MaxRange) return NodeStatus.Failure;

            // 모든 조건 만족: 공격 의사를 블랙보드에 기록
            blackboard.DecidedActionType = CombatActionEvents.ActionType.Attack;
            // blackboard.SelectedWeapon은 이미 설정되어 있다고 가정 (이 노드에서 바꾸지 않음)
            // Debug.Log($"[{this.GetType().Name}] {agent.name}: Decided to attack {currentTarget.name} with {selectedWeapon.WeaponName}. Success.");
            return NodeStatus.Success;
        }
    }
} 