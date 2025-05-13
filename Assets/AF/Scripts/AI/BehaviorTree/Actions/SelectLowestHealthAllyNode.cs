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
            float highestDamageTakenOnPart = 0f;

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

                // Check if below threshold
                if (currentHealthRatio < healthThresholdPercentage)
                {
                    float damageTakenOnPart = partToExamine.MaxDurability - partToExamine.CurrentDurability;
                    logger?.Log($"[{this.GetType().Name}] {agent.Name}: Ally {ally.Name} (Part: '{partSlotToConsider}') is below threshold. Damage taken on part: {damageTakenOnPart:F1}. Current highest: {highestDamageTakenOnPart:F1}", LogLevel.Debug);

                    // And if this ally's part has taken more damage than previously found ones
                    if (damageTakenOnPart > highestDamageTakenOnPart)
                    {
                        highestDamageTakenOnPart = damageTakenOnPart;
                        mostDamagedAlly = ally;
                        logger?.Log($"[{this.GetType().Name}] {agent.Name}: New most damaged ally candidate: {ally.Name} (Part: '{partSlotToConsider}', Damage: {damageTakenOnPart:F1})", LogLevel.Debug);
                    }
                }
            }

            if (mostDamagedAlly != null)
            {
                blackboard.CurrentTarget = mostDamagedAlly;
                float finalPartHealthRatio = (mostDamagedAlly.GetPart(partSlotToConsider).CurrentDurability / mostDamagedAlly.GetPart(partSlotToConsider).MaxDurability) * 100f;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected ally {mostDamagedAlly.Name} (Part: '{partSlotToConsider}', Damage Taken: {highestDamageTakenOnPart:F1}, Current Part Health: {finalPartHealthRatio:F1}%). Success.", LogLevel.Debug);
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