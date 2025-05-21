using AF.Combat;
using AF.Models;
using AF.Models.Abilities; // AbilityEffect 사용을 위해 추가
using AF.Services; // TextLoggerService 사용을 위해 추가
using UnityEngine; // Debug 사용을 위해 추가

namespace AF.AI.BehaviorTree.Actions
{
    public class ConfirmAbilityUsageNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            string nodeName = this.GetType().Name;

            if (agent == null || blackboard == null)
            {
                textLogger?.Log($"[{nodeName}] Agent or Blackboard is null. Failure.", LogLevel.Warning);
                return NodeStatus.Failure;
            }

            AbilityEffect selectedAbilityEffect = blackboard.SelectedAbility;

            if (selectedAbilityEffect == null || selectedAbilityEffect.SourceSO == null)
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: No valid ability selected on blackboard. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            float requiredAP = selectedAbilityEffect.SourceSO.APCost;
            if (agent.HasEnoughAP(requiredAP))
            {
                blackboard.DecidedActionType = CombatActionEvents.ActionType.UseAbility;
                textLogger?.Log($"[{nodeName}] {agent.Name}: Confirmed usage of ability '{selectedAbilityEffect.SourceSO.AbilityName}'. DecidedAction: UseAbility. AP OK. Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: Not enough AP to confirm usage of ability '{selectedAbilityEffect.SourceSO.AbilityName}'. Required: {requiredAP}, Current: {agent.CurrentAP}. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 