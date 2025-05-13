using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// Sets the agent's decided action to RepairSelf.
    /// </summary>
    public class RepairSelfNode : BTNode
    {
        public RepairSelfNode()
        {
            // Constructor can be empty or take parameters if needed in the future
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // Get logger instance first
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;

            if (agent == null)
            {
                textLogger?.Log($"[{this.GetType().Name}] UnknownAgent: Agent is null. Failure.", LogLevel.Error);
                return NodeStatus.Failure;
            }
            
            if (blackboard == null)
            {
                textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Blackboard is null. Failure.", LogLevel.Error);
                return NodeStatus.Failure;
            }

            blackboard.DecidedActionType = CombatActionEvents.ActionType.RepairSelf;
            textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Decided action: RepairSelf. Success.", LogLevel.Debug);
            return NodeStatus.Success;
        }
    }
} 