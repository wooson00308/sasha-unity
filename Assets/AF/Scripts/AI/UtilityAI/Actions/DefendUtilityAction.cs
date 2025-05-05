using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;
using System.Collections.Generic;
using AF.AI.UtilityAI.Considerations;

namespace AF.AI.UtilityAI.Actions
{
    /// <summary>
    /// Represents the action of taking a defensive stance.
    /// </summary>
    public class DefendUtilityAction : UtilityActionBase
    {
        public override string Name => "Defend";

        // +++ IUtilityAction 속성 구현 +++
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Defend;
        public override ArmoredFrame Target => null; // 방어는 특정 대상을 지정하지 않음
        public override Weapon AssociatedWeapon => null; // 방어는 무기를 사용하지 않음
        public override Vector3? TargetPosition => null; // 방어는 특정 위치를 지정하지 않음
        // +++ 속성 구현 끝 +++

        public DefendUtilityAction(SpecializationType pilotSpec)
            : base(pilotSpec)
        {
            InitializeConsiderations();
        }

        protected override void InitializeConsiderations()
        {
            // --- Blocking Considerations ---
            // AP Cost is relatively low for defense
            var apCostConsideration = new ActionPointCostConsideration(1f); 
            // TODO: Add a CanDefendConsideration? (e.g., check for status effects preventing defense)

            // --- Scoring Considerations (Vary by Specialization) ---
            IConsideration threatConsideration;
            IConsideration healthConsideration;

            switch (this.PilotSpecialization)
            {
                case SpecializationType.Defense:
                    // Defense spec: Highly sensitive to threats and lower health.
                    threatConsideration = new IncomingThreatConsideration(
                        threatRadius: 20f, maxThreatCount: 3, // More sensitive to fewer nearby threats
                        curveType: UtilityCurveType.Logistic, steepness: 8f, offsetX: 0.2f, invert: false); // Score rises fast with threat
                    healthConsideration = new SelfHealthConsideration(); // Use default (invert=true, lower health = higher score)
                    break;

                case SpecializationType.MeleeCombat:
                    // Melee: Less likely to defend unless health is critical or threat is very high.
                    threatConsideration = new IncomingThreatConsideration(
                        threatRadius: 10f, maxThreatCount: 5,
                        curveType: UtilityCurveType.Linear, invert: false);
                    healthConsideration = new SelfHealthConsideration(); // Default
                    // Maybe add a Consideration that penalizes defense if target is in melee range?
                    break;
                    
                case SpecializationType.Support:
                     // Support: Defend if threatened, prioritize self-preservation moderately.
                     threatConsideration = new IncomingThreatConsideration(threatRadius: 15f, invert: false);
                     healthConsideration = new SelfHealthConsideration();
                     break;

                default: // StandardCombat, RangedCombat, etc.
                    // Standard: Moderate threat sensitivity.
                    threatConsideration = new IncomingThreatConsideration(
                        threatRadius: 15f, maxThreatCount: 4, 
                        curveType: UtilityCurveType.Polynomial, steepness: 2f, invert: false);
                    healthConsideration = new SelfHealthConsideration(); // Default
                    break;
            }

            Considerations = new List<IConsideration>
            {
                // Blocking
                apCostConsideration,
                // TODO: CanDefendConsideration?
                
                // Scoring
                threatConsideration,
                healthConsideration
                // Add others? e.g., ActionPointConsideration (less AP = more likely to defend?)
            };
        }

        public override void Execute(ArmoredFrame actor, CombatContext context)
        {
            if (!actor.IsOperational)
            {
                Debug.LogWarning($"{Name}: Invalid execution state - Actor Op: {actor.IsOperational}");
                return;
            }

            try
            {
                var executor = ServiceLocator.Instance.GetService<ICombatActionExecutor>();
                if (executor != null)
                {
                    // 방어 액션 실행 호출 (target, position, weapon 등은 null)
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Defend, null, null, null, this, isCounter: false, freeCounter: false);
                    Debug.Log($"{actor.Name} executed {Name}");
                }
                else
                {
                    Debug.LogError($"{Name}: Could not find ICombatActionExecutor service.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{Name}: Exception during execution: {ex.Message}");
            }
        }
    }
} 