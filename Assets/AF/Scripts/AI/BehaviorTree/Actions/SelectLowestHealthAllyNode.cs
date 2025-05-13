using AF.Combat;
using AF.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // Required for Mathf.Approximately

namespace AF.AI.BehaviorTree.Actions
{
    public class SelectLowestHealthAllyNode : BTNode
    {
        private readonly float healthThresholdPercentage;
        private readonly string partSlotToConsider;

        public SelectLowestHealthAllyNode(float healthThresholdPercentage = 0.8f, string partSlotToConsider = "Body")
        {
            this.healthThresholdPercentage = healthThresholdPercentage;
            this.partSlotToConsider = string.IsNullOrEmpty(partSlotToConsider) ? "Body" : partSlotToConsider;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var logger = context?.Logger?.TextLogger; // Get logger instance

            if (context == null || agent == null || blackboard == null)
            {
                logger?.Log($"[{this.GetType().Name}] Context, Agent, or Blackboard is null. Agent: {(agent?.Name ?? "Unknown")}. Failure.", LogLevel.Error);
                return NodeStatus.Failure;
            }

            List<ArmoredFrame> allies = new List<ArmoredFrame>();
            if (context.Participants != null && context.TeamAssignments != null && context.TeamAssignments.TryGetValue(agent, out int agentTeamId))
            {
                foreach (var participant in context.Participants)
                {
                    if (participant != null && participant != agent && context.TeamAssignments.TryGetValue(participant, out int participantTeamId) && participantTeamId == agentTeamId)
                    {
                        allies.Add(participant);
                    }
                }
            }
            
            ArmoredFrame mostDamagedAlly = null;
            float lowestHealthRatio = float.MaxValue;

            if (allies == null || allies.Count == 0)
            {
                blackboard.CurrentTarget = null;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: No allies found. Failure.", LogLevel.Debug);
                return NodeStatus.Failure; // No allies
            }

            foreach (var ally in allies)
            {
                if (ally == agent) // Self check
                {
                    continue; 
                }

                if (!ally.IsOperational)
                {
                    logger?.Log($"[{this.GetType().Name}] {agent.Name}: Skipping ally {ally.Name} because they are not operational.", LogLevel.Debug);
                    continue;
                }

                Part partToExamine = ally.GetPart(partSlotToConsider);
                if (partToExamine == null || Mathf.Approximately(partToExamine.MaxDurability, 0f))
                {
                    logger?.Log($"[{this.GetType().Name}] {agent.Name}: Ally {ally.Name} has no part '{partSlotToConsider}' or part has 0 max durability. Skipping.", LogLevel.Debug);
                    continue;
                }

                float currentHealthRatio = partToExamine.CurrentDurability / partToExamine.MaxDurability;
                
                // Log the health ratio for the ally being considered
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Checking ally {ally.Name}, Part '{partSlotToConsider}' Health Ratio: {currentHealthRatio * 100:F1}%. (Threshold: {healthThresholdPercentage * 100:F1}%)", LogLevel.Debug);

                // Check if below threshold AND is more damaged than previously found ally
                if (currentHealthRatio < healthThresholdPercentage && currentHealthRatio < lowestHealthRatio)
                {
                    // Log when a potential candidate is found (optional, but can be helpful)
                    // logger?.Log($"[{this.GetType().Name}] {agent.Name}: Found potential target {ally.Name} with health ratio {currentHealthRatio * 100:F1}%.", LogLevel.Debug);
                    lowestHealthRatio = currentHealthRatio;
                    mostDamagedAlly = ally;
                }
            }

            if (mostDamagedAlly != null)
            {
                blackboard.CurrentTarget = mostDamagedAlly;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected ally {mostDamagedAlly.Name} (Part: {partSlotToConsider}, Health: {lowestHealthRatio * 100:F1}%, Threshold: {healthThresholdPercentage * 100:F1}%). Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                blackboard.CurrentTarget = null; // No suitable ally found
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: No ally found below health threshold ({healthThresholdPercentage * 100:F1}%) for part '{partSlotToConsider}'. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 