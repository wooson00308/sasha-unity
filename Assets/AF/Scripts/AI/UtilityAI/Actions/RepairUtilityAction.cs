using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;
using System.Collections.Generic;
using AF.AI.UtilityAI.Considerations;

namespace AF.AI.UtilityAI.Actions
{
    /// <summary>
    /// Represents the action of repairing a target ArmoredFrame (self or ally).
    /// </summary>
    public class RepairUtilityAction : UtilityActionBase
    {
        private ArmoredFrame _targetToRepair;

        public override string Name => $"Repair {_targetToRepair?.Name ?? "Unknown"}";

        // +++ IUtilityAction 속성 구현 +++
        // <<< 수정: AssociatedActionType은 일반적인 Repair 반환 >>>
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Repair;
        public override ArmoredFrame Target => _targetToRepair;
        public override Weapon AssociatedWeapon => null; // 수리 액션은 특정 무기를 사용하지 않음 (별도 수리 도구 있다면 변경 가능)
        public override Vector3? TargetPosition => _targetToRepair?.Position; // 수리 대상의 현재 위치
        // +++ 속성 구현 끝 +++

        public RepairUtilityAction(ArmoredFrame targetToRepair, SpecializationType pilotSpec)
            : base(pilotSpec)
        {
            _targetToRepair = targetToRepair;
            InitializeConsiderations();
        }

        protected override void InitializeConsiderations()
        {
            if (_targetToRepair == null)
            {
                Debug.LogError($"[{Name}] TargetToRepair is null during initialization.");
                Considerations = new List<IConsideration>();
                return;
            }

            // --- Blocking Considerations ---
            var targetOperational = new TargetIsOperationalConsideration(_targetToRepair);
            var targetDamaged = new TargetDamagedConsideration(_targetToRepair);      // Check if target actually needs repair
            var isAllyOrSelf = new IsAllyOrSelfConsideration(_targetToRepair);      // Check if target is valid (self or ally)
            // Estimate AP cost (could vary slightly if self vs ally)
            // float estimatedApCost = (_targetToRepair == actor) ? 2.0f : 2.5f; // Requires actor context, handle differently?
            float estimatedApCost = 2.5f; // Use a general high estimate or average for now
            var apCostConsideration = new ActionPointCostConsideration(estimatedApCost);

            // --- Scoring Considerations (Vary by Specialization) ---
            IConsideration healthConsideration;

            switch (this.PilotSpecialization)
            {
                case SpecializationType.Support:
                    // Support: Highly sensitive to low health targets.
                    healthConsideration = new TargetHealthConsideration(
                        _targetToRepair,
                        UtilityCurveType.Logistic,   // Logistic curve for rapid score increase at low health
                        steepness: 15f,              // Very steep curve
                        offsetX: 0.1f,                // Midpoint very low (10% health = 0.5 score)
                        invert: true                  // Lower health = higher score
                    );
                    break;

                // case SpecializationType.Defense:
                // case SpecializationType.StandardCombat:
                // Might have slightly different curves or lower priority (handled by weights)
                default:
                    // Default/Others: Linear or polynomial curve, less sensitive than Support.
                    healthConsideration = new TargetHealthConsideration(
                        _targetToRepair,
                        UtilityCurveType.Linear, // Simple linear relationship
                        invert: true             // Lower health = higher score
                    );
                    break;
            }

            Considerations = new List<IConsideration>
            {
                // Blocking considerations first
                targetOperational,
                targetDamaged,
                isAllyOrSelf,
                apCostConsideration,
                
                // Scoring considerations
                healthConsideration
                // Add others? e.g., DistanceToTarget for ally repair?
                // e.g., IncomingThreatConsideration (less likely to repair under heavy fire?)
            };
        }

        public override void Execute(ArmoredFrame actor, CombatContext context)
        {
            if (!actor.IsOperational || _targetToRepair == null || !_targetToRepair.IsOperational)
            {
                Debug.LogWarning($"{Name}: Invalid execution state - Actor Op: {actor.IsOperational}, Target: {_targetToRepair?.Name ?? "NULL"}, Target Op: {_targetToRepair?.IsOperational ?? false}");
                return;
            }

            // <<< 수정: Execute 호출 시 대상에 따라 RepairSelf/RepairAlly 결정 >>>
            CombatActionEvents.ActionType actualRepairType = (actor == _targetToRepair) 
                ? CombatActionEvents.ActionType.RepairSelf 
                : CombatActionEvents.ActionType.RepairAlly;
                
            // 자신 또는 아군만 수리 가능 확인은 CalculateUtility에서 이미 함

            try
            {
                var executor = ServiceLocator.Instance.GetService<ICombatActionExecutor>();
                if (executor != null)
                {
                    // <<< 수정: actualRepairType 변수 사용 >>>
                    // <<< 수정: executedAction 파라미터에 'this' 전달 >>>
                    executor.Execute(context, actor, actualRepairType, _targetToRepair, null, null, this, isCounter: false, freeCounter: false);
                    Debug.Log($"{actor.Name} executed {Name} (Type: {actualRepairType})");
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
 