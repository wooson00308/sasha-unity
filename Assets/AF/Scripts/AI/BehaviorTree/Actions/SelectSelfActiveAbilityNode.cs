using System.Linq;
using AF.Combat;
using AF.Models;
using AF.Models.Abilities;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// Self 대상 Active 어빌리티를 하나 선택해 Blackboard에 기록하고 사용 의사를 설정합니다.
    /// </summary>
    public class SelectSelfActiveAbilityNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (agent == null || blackboard == null) return NodeStatus.Failure;

            // 이미 Ability 선택돼 있으면 패스
            if (blackboard.SelectedAbility != null)
            {
                return NodeStatus.Success;
            }

            // 파츠에서 AbilityID 수집
            var abilityIds = agent.Parts?.Values
                                .SelectMany(p => p.Abilities)
                                .Distinct();
            if (abilityIds == null) return NodeStatus.Failure;

            foreach (var id in abilityIds)
            {
                if (!AbilityEffectRegistry.TryGetExecutor(id, out var exec)) continue; // 실행 가능 체크
                if (!AbilityDatabase.TryGetAbility(id, out var so)) continue;
                if (!exec.CanExecute(context, agent, agent, so)) continue;
                // AP 체크는 CanExecute 내부에서도 하지만 중복으로 안전망
                if (!agent.HasEnoughAP(so.APCost)) continue;

                var effect = new AbilityEffect(so);
                blackboard.SelectedAbility = effect;
                blackboard.DecidedActionType = CombatActionEvents.ActionType.UseAbility;
                return NodeStatus.Success;
            }
            return NodeStatus.Failure;
        }
    }
} 