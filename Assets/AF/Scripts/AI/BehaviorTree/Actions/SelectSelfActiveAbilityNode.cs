using System.Linq;
using AF.Combat;
using AF.Models;
using AF.Models.Abilities;
using AF.Services;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// Self 대상 Active 어빌리티를 하나 선택해 Blackboard에 기록하고 사용 의사를 설정합니다.
    /// </summary>
    public class SelectSelfActiveAbilityNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            string nodeName = this.GetType().Name;

            if (agent == null)
            {
                textLogger?.Log($"[{nodeName}] Agent is null. Failure.", LogLevel.Warning);
                return NodeStatus.Failure;
            }
            if (blackboard == null)
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: Blackboard is null. Failure.", LogLevel.Warning);
                return NodeStatus.Failure;
            }

            string nodeKey = nodeName; // 같은 노드 타입은 같은 조건을 체크하므로
            if (blackboard.FailedNodesThisActivation?.Contains(nodeKey) == true)
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: Node already failed this activation. Skipping. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            textLogger?.Log($"[{nodeName}] {agent.Name}: Node Ticked.", LogLevel.Debug);

            if (blackboard.SelectedAbility != null && blackboard.DecidedActionType == CombatActionEvents.ActionType.UseAbility)
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: Ability '{blackboard.SelectedAbility.AbilityID}' already set to be used. Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }

            if (blackboard.SelectedAbility != null)
            {
                var existingAbility = blackboard.SelectedAbility;
                if (AbilityEffectRegistry.TryGetExecutor(existingAbility.AbilityID, out var existingExec) &&
                    existingExec.CanExecute(context, agent, agent, existingAbility.SourceSO) &&
                    agent.HasEnoughAP(existingAbility.SourceSO.APCost))
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Existing ability '{existingAbility.AbilityID}' still usable. Success.", LogLevel.Debug);
                    return NodeStatus.Success;
                }
                else
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Existing ability '{existingAbility.AbilityID}' no longer usable. Clearing and selecting new one.", LogLevel.Debug);
                    blackboard.SelectedAbility = null;
                }
            }

            var abilityIds = agent.Parts?.Values
                                .SelectMany(p => p.Abilities)
                                .Distinct();

            if (abilityIds == null || !abilityIds.Any())
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: No ability IDs found on parts. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }
            textLogger?.Log($"[{nodeName}] {agent.Name}: Found abilities on parts: {string.Join(", ", abilityIds)}. Iterating...", LogLevel.Debug);

            foreach (var id in abilityIds)
            {
                textLogger?.Log($"[{nodeName}] {agent.Name}: Checking ability ID: {id}", LogLevel.Debug);
                if (!AbilityDatabase.TryGetAbility(id, out var so))
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: AbilitySO for ID '{id}' not found in AbilityDatabase. Skipping.", LogLevel.Debug);
                    continue;
                }
                textLogger?.Log($"[{nodeName}] {agent.Name}: Found AbilitySO: {so.AbilityName} (APCost: {so.APCost})", LogLevel.Debug);

                if (so.AbilityType != Models.AbilityType.Active || so.TargetType != Models.AbilityTargetType.Self)
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Ability '{so.AbilityName}' is not Active or not Self-target. Skipping.", LogLevel.Debug);
                    continue;
                }

                if (!AbilityEffectRegistry.TryGetExecutor(id, out var exec))
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Executor for ID '{id}' not found in AbilityEffectRegistry. Skipping.", LogLevel.Debug);
                    continue;
                }
                textLogger?.Log($"[{nodeName}] {agent.Name}: Found Executor for {so.AbilityName}. Checking CanExecute...", LogLevel.Debug);

                if (!agent.HasEnoughAP(so.APCost))
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Not enough AP for '{so.AbilityName}'. Required: {so.APCost}, Current: {agent.CurrentAP}. Skipping.", LogLevel.Debug);
                    continue;
                }

                if (!exec.CanExecute(context, agent, agent, so))
                {
                    textLogger?.Log($"[{nodeName}] {agent.Name}: Executor.CanExecute for '{so.AbilityName}' returned false. Skipping.", LogLevel.Debug);
                    continue;
                }
                textLogger?.Log($"[{nodeName}] {agent.Name}: Executor.CanExecute for '{so.AbilityName}' returned true.", LogLevel.Debug);

                var effect = new AbilityEffect(so);
                blackboard.SelectedAbility = effect;
                textLogger?.Log($"[{nodeName}] {agent.Name}: Selected ability '{so.AbilityName}' on blackboard. (ActionType NOT set yet). Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }

            textLogger?.Log($"[{nodeName}] {agent.Name}: No suitable self-active ability found after checking all. Failure.", LogLevel.Debug);
            
            // +++ 실패 추적 기록 +++
            if (blackboard.FailedNodesThisActivation != null)
            {
                blackboard.FailedNodesThisActivation.Add(nodeKey);
            }
            // +++ 실패 추적 기록 끝 +++
            
            return NodeStatus.Failure;
        }
    }
} 