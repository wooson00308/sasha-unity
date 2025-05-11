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
            if (context == null || agent == null || blackboard == null)
            {
                Debug.LogError("SelectLowestHealthAllyNode: CombatContext, Agent, or Blackboard is null.");
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
                return NodeStatus.Failure; // No allies
            }

            foreach (var ally in allies)
            {
                if (ally == agent || !ally.IsOperational)
                {
                    continue;
                }

                Part partToExamine = ally.GetPart(partSlotToConsider);
                if (partToExamine == null || Mathf.Approximately(partToExamine.MaxDurability, 0f))
                {
                    continue;
                }

                float currentHealthRatio = partToExamine.CurrentDurability / partToExamine.MaxDurability;

                // Check if below threshold AND is more damaged than previously found ally
                if (currentHealthRatio < healthThresholdPercentage && currentHealthRatio < lowestHealthRatio)
                {
                    lowestHealthRatio = currentHealthRatio;
                    mostDamagedAlly = ally;
                }
            }

            if (mostDamagedAlly != null)
            {
                blackboard.CurrentTarget = mostDamagedAlly;
                // Optionally log: Debug.Log($"[BT] {agent.Name} selected ally {mostDamagedAlly.Name} (Health: {lowestHealthRatio * 100:F1}%) for support.");
                return NodeStatus.Success;
            }
            else
            {
                blackboard.CurrentTarget = null; // No suitable ally found
                return NodeStatus.Failure;
            }
        }
    }
} 