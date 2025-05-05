using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;
using System.Collections.Generic; // For List
using AF.AI.UtilityAI.Considerations; // <<< Considerations 네임스페이스 추가

namespace AF.AI.UtilityAI.Actions
{
    /// <summary>
    /// Represents the action of moving to a specific target position.
    /// </summary>
    public class MoveUtilityAction : UtilityActionBase
    {
        private Vector3 _targetPosition;
        public ArmoredFrame TargetUnit { get; private set; } // <<< 추가: 목표 유닛 정보

        // 이름도 목표 유닛 정보를 포함하도록 수정 가능 (선택 사항)
        public override string Name => $"Move towards {(TargetUnit != null ? TargetUnit.Name : _targetPosition.ToString())}";

        // +++ IUtilityAction 속성 구현 +++
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Move;
        public override ArmoredFrame Target => null; // 이동 액션은 특정 유닛을 대상으로 하지 않음
        public override Weapon AssociatedWeapon => null; // 이동 액션은 무기를 사용하지 않음
        public override Vector3? TargetPosition => _targetPosition;
        // +++ 속성 구현 끝 +++

        // <<< Modified Constructor: Call base constructor with pilotSpec >>>
        public MoveUtilityAction(Vector3 targetPosition, SpecializationType pilotSpec, ArmoredFrame targetUnit = null)
            : base(pilotSpec) // Call base constructor
        {
            _targetPosition = targetPosition;
            TargetUnit = targetUnit;
            InitializeConsiderations(); 
        }

        protected override void InitializeConsiderations()
        {
            // --- Common Considerations ---
            // Calculate base move AP cost (could potentially vary by spec slightly)
            // For now, using a placeholder or average cost.
            // TODO: Pass actor here or get cost from CombatActionExecutor dynamically if needed.
            float estimatedApCost = 3.0f; // Placeholder
            var apCostConsideration = new ActionPointCostConsideration(estimatedApCost);

            // --- Specialization-Specific Considerations ---
            IConsideration distanceConsideration;
            IConsideration safetyConsideration;
            float defaultMaxDistance = 100f; // Default max distance for calculations

            switch (this.PilotSpecialization)
            {
                case SpecializationType.MeleeCombat:
                    // Melee: Prefer getting close, safety less critical
                    distanceConsideration = new DistanceToEnemyConsideration(
                        _targetPosition, maxDistance: 50f); // Use a relevant max distance for melee
                    // Safety might use a flatter curve or smaller radius for melee
                    safetyConsideration = new TargetPositionSafetyConsideration(
                        _targetPosition, safetyRadius: 4f); // Slightly smaller radius
                    break;

                case SpecializationType.RangedCombat:
                    // Ranged: Prefer staying at optimal range (handled more by distance *from* enemy), safety more important
                    // This DistanceToEnemy is about the destination, not current pos.
                    // Ranged might prefer destinations further from enemies generally.
                    distanceConsideration = new DistanceToEnemyConsideration(
                        _targetPosition, maxDistance: defaultMaxDistance); // Invert might be false here (further from *closest* enemy at destination is safer?)
                    safetyConsideration = new TargetPositionSafetyConsideration(
                        _targetPosition, safetyRadius: 8f); // Larger safety radius
                    break;

                // case SpecializationType.Support:
                //     // Support might prioritize moving closer to damaged allies, or staying safe.
                //     distanceConsideration = ... // Maybe DistanceToAllyConsideration?
                //     safetyConsideration = ...
                //     break;

                // case SpecializationType.Defense:
                //     // Defense might prefer moving to highly defensible positions, maybe less emphasis on enemy distance.
                //     distanceConsideration = ...
                //     safetyConsideration = ... // Maybe factor in cover?
                //     break;

                default: // StandardCombat or others
                    // Standard: Balanced approach.
                    distanceConsideration = new DistanceToEnemyConsideration(_targetPosition, defaultMaxDistance);
                    safetyConsideration = new TargetPositionSafetyConsideration(_targetPosition);
                    break;
            }

            Considerations = new List<IConsideration>
            {
                // Blocking Considerations First
                apCostConsideration,
                // TODO: Add CanMoveConsideration? (Check if path exists, terrain allows, etc.)
                
                // Scoring Considerations
                safetyConsideration,
                distanceConsideration
                // Add others like ActionPointConsideration (remaining AP after move)?
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
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Move, null, _targetPosition, null, this, isCounter: false, freeCounter: false);
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