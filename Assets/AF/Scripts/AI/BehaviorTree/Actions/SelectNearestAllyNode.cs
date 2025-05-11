using AF.Combat;
using AF.Models;
using System.Linq;
using UnityEngine;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// Finds the nearest ally to the agent and stores it as CurrentTarget in the blackboard.
    /// </summary>
    public class SelectNearestAllyNode : BTNode
    {
        // No longer needs a blackboard key, will use agent.AICtxBlackboard.CurrentTarget
        public SelectNearestAllyNode() { }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            if (context == null || agent == null || blackboard == null)
            {
                Debug.LogError("SelectNearestAllyNode: CombatContext, Agent, or Blackboard is null.");
                return NodeStatus.Failure;
            }

            var allies = context.Participants
                .Where(p => p != agent && 
                            context.TeamAssignments.TryGetValue(p, out int teamId) && 
                            teamId == agent.TeamId && 
                            p.IsOperational)
                .ToList();

            if (!allies.Any())
            {
                blackboard.CurrentTarget = null; // Set CurrentTarget to null
                // context.Logger?.TextLogger?.Log($"[BT] {agent.Name} - SelectNearestAllyNode: No allies found. RESULT: FAILURE", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            ArmoredFrame nearestAlly = null;
            float minDistance = float.MaxValue;

            foreach (var ally in allies)
            {
                if (ally == null) continue;
                float distance = Vector3.Distance(agent.Position, ally.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestAlly = ally;
                }
            }

            if (nearestAlly != null)
            {
                blackboard.CurrentTarget = nearestAlly; // Set CurrentTarget directly
                // context.Logger?.TextLogger?.Log($"[BT] {agent.Name} - SelectNearestAllyNode: Selected '{nearestAlly.Name}' as CurrentTarget. RESULT: SUCCESS", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                blackboard.CurrentTarget = null; // Fallback, set CurrentTarget to null
                // context.Logger?.TextLogger?.Log($"[BT] {agent.Name} - SelectNearestAllyNode: Could not find nearest ally. RESULT: FAILURE", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 