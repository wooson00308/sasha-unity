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

        public AttackUtilityAction(ArmoredFrame target, Weapon weapon)
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

            // --- TargetDistanceConsideration 설정 ---
            var distanceConsideration = new TargetDistanceConsideration(
                Target,
                UtilityCurveType.Gaussian,
                minDistance: 0f,
                maxDistance: AssociatedWeapon.Range.MaxRange,
                offsetX: AssociatedWeapon.Range.OptimalRange,
                steepness: 3f
            );

            // --- HitChanceConsideration 설정 ---
            var hitChanceConsideration = new HitChanceConsideration(Target, AssociatedWeapon);

            // --- ActionPointCostConsideration 설정 ---
            var apCostConsideration = new ActionPointCostConsideration(AssociatedWeapon.BaseAPCost);

            // --- TargetHealthConsideration 설정 ---
            var healthConsideration = new TargetHealthConsideration(
                Target,
                UtilityCurveType.Linear,
                invert: false
            );

            Considerations = new List<IConsideration>
            {
                distanceConsideration,
                hitChanceConsideration,
                apCostConsideration,
                healthConsideration
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
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Attack, _target, null, _weapon, isCounter: false, freeCounter: false);
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