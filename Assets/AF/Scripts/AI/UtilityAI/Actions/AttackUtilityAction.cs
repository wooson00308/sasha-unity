using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;
using AF.AI.UtilityAI.Considerations;
using System.Collections.Generic;

namespace AF.AI.UtilityAI.Actions
{
    /// <summary>
    /// Represents the action of attacking a specific target with a specific weapon.
    /// </summary>
    public class AttackUtilityAction : UtilityActionBase
    {
        public override string Name => $"Attack {_target?.Name ?? "Unknown"} with {_weapon?.Name ?? "Unknown"}";

        private ArmoredFrame _target;
        private Weapon _weapon;

        // +++ IUtilityAction 속성 구현 +++
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Attack;
        public override ArmoredFrame Target => _target;
        public override Weapon AssociatedWeapon => _weapon;
        public override Vector3? TargetPosition => _target?.Position; // 공격 액션의 목표 위치는 대상의 현재 위치
        // +++ 속성 구현 끝 +++

        public AttackUtilityAction(ArmoredFrame target, Weapon weapon, SpecializationType pilotSpec)
            : base(pilotSpec)
        {
            _target = target;
            _weapon = weapon;
            InitializeConsiderations();
        }

        protected override void InitializeConsiderations()
        {
            if (Target == null || AssociatedWeapon == null)
            {
                Debug.LogError($"[{Name}] Target or AssociatedWeapon is null during initialization.");
                Considerations = new List<IConsideration>();
                return;
            }

            // --- Blocking Considerations (Common) ---
            var targetOperational = new TargetIsOperationalConsideration(Target);
            var weaponOperational = new WeaponIsOperationalConsideration(AssociatedWeapon);
            var weaponHasAmmo = new WeaponHasAmmoConsideration(AssociatedWeapon);
            var weaponReloading = new WeaponReloadingConsideration(AssociatedWeapon);
            // Basic AP cost check (can be refined per spec if needed)
            var apCostConsideration = new ActionPointCostConsideration(AssociatedWeapon.BaseAPCost);

            // --- Scoring Considerations (Vary by Specialization) ---
            IConsideration distanceConsideration;
            IConsideration healthConsideration;
            IConsideration hitChanceConsideration = new HitChanceConsideration(Target, AssociatedWeapon);

            float maxRange = AssociatedWeapon.Range.MaxRange;
            float optimalRange = AssociatedWeapon.Range.OptimalRange;

            switch (this.PilotSpecialization)
            {
                case SpecializationType.MeleeCombat:
                    // Melee: Must be very close. Score drops sharply beyond melee range.
                    distanceConsideration = new TargetDistanceConsideration(
                        Target, UtilityCurveType.Polynomial, 0f, maxRange, steepness: 3f, invert: true);
                    // Melee: Focus fire on lower health targets more aggressively.
                    healthConsideration = new TargetHealthConsideration(
                        Target, UtilityCurveType.Logistic, steepness: 10f, offsetX: 0.3f, invert: true);
                    break;

                case SpecializationType.RangedCombat:
                    // Ranged: Prefer optimal range. Gaussian curve.
                    distanceConsideration = new TargetDistanceConsideration(
                        Target, UtilityCurveType.Gaussian, 0f, maxRange * 1.1f, // Extend max slightly for curve shape
                        offsetX: optimalRange, steepness: 3f, invert: false);
                    // Ranged: Less emphasis on target health, more on distance/hit chance.
                    healthConsideration = new TargetHealthConsideration(
                        Target, UtilityCurveType.Linear, invert: false); // Lower health = lower score (less incentive to finish off)
                    break;

                // case SpecializationType.Support:
                //     // Support might have different attack considerations or lower priority overall (handled by weights)
                //     distanceConsideration = ...
                //     healthConsideration = ...
                //     break;

                // case SpecializationType.Defense:
                //     // Defense might prioritize attacking high-threat targets regardless of health?
                //     distanceConsideration = ...
                //     healthConsideration = ...
                //     break;

                default: // StandardCombat or others
                    // Standard: Balanced approach, similar to Ranged but maybe linear distance.
                    distanceConsideration = new TargetDistanceConsideration(
                        Target, UtilityCurveType.Linear, 0f, maxRange, invert: true); // Closer is generally better
                    // Standard: Moderate focus on lower health targets.
                    healthConsideration = new TargetHealthConsideration(
                        Target, UtilityCurveType.Polynomial, steepness: 1.5f, invert: true);
                    break;
            }

            Considerations = new List<IConsideration>
            {
                // Blocking considerations first
                targetOperational,
                weaponOperational,
                weaponHasAmmo,
                weaponReloading,
                apCostConsideration,
                new TargetIsEnemyConsideration(Target), // Ensure target is an enemy

                // Scoring considerations
                distanceConsideration,
                hitChanceConsideration,
                healthConsideration
                // Add other relevant considerations like ThreatLevel of the target?
            };
        }

        public override void Execute(ArmoredFrame actor, CombatContext context)
        {
            if (_target == null || _weapon == null || !actor.IsOperational || !_target.IsOperational)
            {
                Debug.LogWarning($"{Name}: Invalid execution state - Target: {_target?.Name ?? "NULL"}, Weapon: {_weapon?.Name ?? "NULL"}, Actor Op: {actor.IsOperational}, Target Op: {_target?.IsOperational ?? false}");
                return;
            }

            try
            {
                // 주석 해제 및 실제 Executor 호출
                var executor = ServiceLocator.Instance.GetService<ICombatActionExecutor>();
                if (executor != null)
                {
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Attack, _target, null, _weapon, this, isCounter: false, freeCounter: false);
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

        // --- Overriding utility calculation for 'All-Or-Nothing' checks ---
        public override float CalculateUtility(ArmoredFrame actor, CombatContext context)
        {
            float totalScore = 0f;
            int scoringConsiderationsCount = 0;

            if (Considerations == null || Considerations.Count == 0)
            {
                this.LastCalculatedUtility = 0f;
                return 0f;
            }

            foreach (var consideration in Considerations)
            {
                if (consideration == null) continue;
                float score = consideration.CalculateScore(actor, context);

                if (consideration is HitChanceConsideration && score <= 0.001f)
                {
                    this.LastCalculatedUtility = 0f;
                    return 0f;
                }

                if (consideration is ActionPointCostConsideration ||
                    consideration is TargetIsOperationalConsideration ||
                    consideration is WeaponHasAmmoConsideration ||
                    consideration is WeaponIsOperationalConsideration ||
                    consideration is WeaponReloadingConsideration)
                {
                    if (score <= 0f)
                    {
                        this.LastCalculatedUtility = 0f;
                        return 0f; 
                    }
                }
                else
                {
                    totalScore += score;
                    scoringConsiderationsCount++;
                }
            }

            float finalScore = 0f;
            if (scoringConsiderationsCount > 0)
            {
                finalScore = totalScore / scoringConsiderationsCount;
            }
            else
            {
                finalScore = 1f;
            }
            
            finalScore = Mathf.Clamp01(finalScore);

            this.LastCalculatedUtility = finalScore;
            return finalScore;
        }
        // --- End Overriding utility calculation --- 
    }
} 