using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 용
using AF.Services;
using AF.Models.Abilities;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 에이전트가 특정 행동을 수행하기에 충분한 AP를 가지고 있는지 확인하는 조건 노드입니다.
    /// </summary>
    public class HasEnoughAPNode : ConditionNode
    {
        private CombatActionEvents.ActionType _actionType;
        private float _specificApCost = -1f; // 특정 AP 비용을 저장할 필드, 기본값 -1f

        /// <summary>
        /// HasEnoughAPNode를 생성합니다.
        /// </summary>
        /// <param name="actionType">필요한 행동 타입입니다.</param>
        public HasEnoughAPNode(CombatActionEvents.ActionType actionType)
        {
            _actionType = actionType;
        }

        // 특정 AP 비용을 받는 새 생성자
        public HasEnoughAPNode(CombatActionEvents.ActionType actionType, float specificApCost)
        {
            _actionType = actionType;
            _specificApCost = specificApCost;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var actualLogger = context?.Logger?.TextLogger;
            float actualRequiredAP = 0f;

            // CombatContext와 ActionExecutor가 유효한지 확인
            if (context == null || context.ActionExecutor == null)
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name} for {_actionType}: Failed - CombatContext or ActionExecutor is null.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            // _specificApCost가 유효한 값(0 이상)으로 설정되었는지 확인
            if (_specificApCost >= 0f)
            {
                actualRequiredAP = _specificApCost;
            }
            else
            {
                // _actionType에 따라 필요한 AP 계산
                // 이제 CombatActionExecutor의 GetActionAPCost를 직접 사용한다.
                // Reload의 경우, blackboard.WeaponToReload를 weapon 파라미터로 전달한다.
                actualRequiredAP = context.ActionExecutor.GetActionAPCost(_actionType, agent,
                    (_actionType == CombatActionEvents.ActionType.Attack) 
                        ? blackboard.SelectedWeapon 
                        : (_actionType == CombatActionEvents.ActionType.Reload)
                            ? blackboard.WeaponToReload 
                            : null,
                    _actionType == CombatActionEvents.ActionType.UseAbility ? blackboard.SelectedAbility : null);
            }

            // AP 계산 결과가 float.MaxValue이면 (GetActionAPCost에서 오류 또는 유효하지 않은 조건 시 반환 가능)
            // 또는 Reload인데 WeaponToReload가 null이면 실패로 처리한다.
            if (actualRequiredAP == float.MaxValue || 
                (_actionType == CombatActionEvents.ActionType.Reload && blackboard.WeaponToReload == null && _specificApCost < 0f)) // specificCost가 없을 때만 Reload && WeaponToReload null 체크
            {
                string failureReason = (_actionType == CombatActionEvents.ActionType.Reload && blackboard.WeaponToReload == null)
                                        ? "WeaponToReload is null for Reload AP check."
                                        : "Could not determine AP cost (possibly invalid weapon or action).";
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name} for {_actionType}: Failed - {failureReason}", LogLevel.Debug);
                return NodeStatus.Failure;
            }
            
            bool hasEnough = agent.HasEnoughAP(actualRequiredAP);

            if (hasEnough)
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name} for {_actionType}: Has enough AP (Required: {actualRequiredAP:F1}, Current: {agent.CurrentAP:F1}). Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                actualLogger?.Log($"[{this.GetType().Name}] {agent.Name} for {_actionType}: Not enough AP (Required: {actualRequiredAP:F1}, Current: {agent.CurrentAP:F1}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 