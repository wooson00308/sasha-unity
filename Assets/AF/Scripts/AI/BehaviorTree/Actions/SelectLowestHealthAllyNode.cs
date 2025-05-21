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
        private readonly Dictionary<string, float> partWeights;

        /// <summary>
        /// 가장 손상된 아군을 선택합니다. 파츠별 가중치를 적용하여 우선순위를 결정합니다.
        /// </summary>
        /// <param name="healthThresholdPercentage">이 체력 비율 미만인 파츠만 고려합니다.</param>
        /// <param name="partWeights">파츠 슬롯 ID와 가중치(높을수록 우선). null이면 기본 가중치 사용.</param>
        public SelectLowestHealthAllyNode(float healthThresholdPercentage = 0.8f, Dictionary<string, float> partWeights = null)
        {
            this.healthThresholdPercentage = healthThresholdPercentage;
            this.partWeights = partWeights ?? GetDefaultPartWeights();
        }

        private Dictionary<string, float> GetDefaultPartWeights()
        {
            // 실제 SlotIdentifier 문자열을 키로 사용
            return new Dictionary<string, float>
            {
                { "Body", 1.0f },
                { "Head", 0.8f },
                { "Arm_Left", 0.7f },
                { "Arm_Right", 0.7f },
                { "Legs", 0.6f },
                { "Backpack", 0.4f }
                // Frame은 Parts 딕셔너리에 없으므로 제거
            };
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var logger = context?.Logger?.TextLogger;

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
                    if (participant != null && participant != agent && 
                        context.TeamAssignments.TryGetValue(participant, out int participantTeamId) && 
                        participantTeamId == agentTeamId && participant.IsOperational)
                    {
                        allies.Add(participant);
                    }
                }
            }
            
            ArmoredFrame mostDamagedAllyOverall = null;
            Part mostCriticalPartToRepairObject = null; // Part 객체 저장
            string mostCriticalPartSlotId = null; // Slot ID 문자열 저장
            float highestWeightedDamageScore = 0f;

            if (allies.Count == 0)
            {
                blackboard.CurrentTarget = null;
                blackboard.TargetPartSlot = null;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: No operational allies found. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            foreach (var ally in allies)
            {
                if (!ally.IsOperational) { continue; }

                // ally.Parts는 Dictionary<string, Part> 형태 (string은 SlotIdentifier)
                foreach (var partEntry in ally.Parts) 
                {
                    string slotIdentifier = partEntry.Key; // 실제 슬롯 ID (e.g., "Arm_Left")
                    Part part = partEntry.Value;

                    if (part == null || !part.IsOperational || Mathf.Approximately(part.MaxDurability, 0f) || part.CurrentDurability >= part.MaxDurability)
                    {
                        continue;
                    }

                    float currentHealthRatio = part.CurrentDurability / part.MaxDurability;

                    if (currentHealthRatio < healthThresholdPercentage)
                    {
                        float damageTaken = part.MaxDurability - part.CurrentDurability;
                        // slotIdentifier를 사용해 가중치 조회
                        float weight = partWeights.TryGetValue(slotIdentifier, out float w) ? w : 0.5f; 
                        float weightedDamageScore = damageTaken * weight;

                        if (weightedDamageScore > highestWeightedDamageScore)
                        {
                            highestWeightedDamageScore = weightedDamageScore;
                            mostDamagedAllyOverall = ally;
                            mostCriticalPartToRepairObject = part; // Part 객체 저장
                            mostCriticalPartSlotId = slotIdentifier; // Slot ID 저장
                        }
                    }
                }
            }

            if (mostDamagedAllyOverall != null && mostCriticalPartToRepairObject != null && mostCriticalPartSlotId != null)
            {
                blackboard.CurrentTarget = mostDamagedAllyOverall;
                blackboard.TargetPartSlot = mostCriticalPartSlotId; // 실제 Slot ID를 Blackboard에 저장
                float finalPartHealthRatio = (mostCriticalPartToRepairObject.CurrentDurability / mostCriticalPartToRepairObject.MaxDurability) * 100f;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: Selected ally {mostDamagedAllyOverall.Name} to repair part '{mostCriticalPartSlotId}' (Type: {mostCriticalPartToRepairObject.Type}, WeightedScore: {highestWeightedDamageScore:F1}, Current Part Health: {finalPartHealthRatio:F1}%). Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }
            else
            {
                blackboard.CurrentTarget = null;
                blackboard.TargetPartSlot = null;
                logger?.Log($"[{this.GetType().Name}] {agent.Name}: No ally found with any part below health threshold ({healthThresholdPercentage * 100:F1}%) considering weights. Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }
        }
    }
} 