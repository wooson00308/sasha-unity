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
            // InitializeConsiderations(); // 생성자에서 호출하도록 변경 가능
        }

        protected override void InitializeConsiderations()
        {
            if (_weapon == null || _target == null) return;

            Considerations = new List<IConsideration>
            {
                // Essential checks
                new TargetIsEnemyConsideration(_target),         // Is the target an enemy?
                new TargetIsOperationalConsideration(_target),   // Is the target still operational?
                new WeaponHasAmmoConsideration(_weapon),         // Does the weapon have ammo?
                new WeaponIsOperationalConsideration(_weapon),   // Is the weapon usable (not reloading/broken)?

                // Scoring factors
                // new TargetDistanceConsideration(_target, _weapon.Range * 0.8f, _weapon.Range), // <<< 기존 코드 주석 처리
                new TargetDistanceConsideration(
                    targetUnit: _target, 
                    curveType: UtilityCurveType.Polynomial, // 예: 거리가 가까울수록 점수가 제곱으로 증가 (Polynomial, invert=true)
                    minDistance: 0f, 
                    maxDistance: _weapon.Range, 
                    steepness: 2f, // 지수 (제곱)
                    invert: true // 가까울수록 점수 높음
                ), // Is target in optimal range?

                // +++ Target Health Consideration 추가 +++
                new TargetHealthConsideration(
                    targetUnit: _target,
                    curveType: UtilityCurveType.Polynomial, // 예: 체력이 낮을수록 점수가 제곱으로 증가
                    steepness: 2f, // 지수 (제곱)
                    invert: true // 체력 낮을수록 점수 높음 (기본값)
                ),

                // --- Placeholder for other considerations ---
                // TODO: Implement and add these considerations
                // new TargetHealthConsideration(_target, CurveType.InverseLinear), // Lower health = higher score?
                // new HitChanceConsideration(_weapon, _target) // Estimated hit chance?
                // ----------------------------------------
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
            float totalScore = 1f; // Start with 1, multiply by scores (0 to 1)
            int scoringConsiderationsCount = 0;

            if (Considerations == null || Considerations.Count == 0)
            {
                // Debug.LogWarning($"Action '{Name}' has no considerations. Returning 0 utility.");
                return 0f;
            }

            foreach (var consideration in Considerations)
            {
                if (consideration == null) continue;
                float score = consideration.CalculateScore(actor, context);

                // --- Check for 'Blocking' Considerations --- 
                // If any essential check fails (returns 0), the action is impossible (utility 0).
                if (consideration is TargetIsEnemyConsideration ||
                    consideration is TargetIsOperationalConsideration ||
                    consideration is WeaponHasAmmoConsideration ||
                    consideration is WeaponIsOperationalConsideration)
                {
                    if (score <= 0f) return 0f; // If essential check fails, utility is 0
                }
                else
                {
                    // --- Accumulate 'Scoring' Considerations --- 
                    // Multiply scores together (alternative: average, weighted average etc.)
                    totalScore *= score; 
                    scoringConsiderationsCount++;
                }
            }

            // Optional: Compensation factor if multiplying scores
            // Can prevent scores from getting too low with many considerations.
            // float modificationFactor = 1f / scoringConsiderationsCount;
            // float compensatedScore = Mathf.Pow(totalScore, modificationFactor);
            // return compensatedScore;
            
            // If no scoring considerations, return 1 (as all essential checks passed), else return the calculated score.
            return scoringConsiderationsCount > 0 ? totalScore : 1f; 
        }
        // --- End Overriding utility calculation --- 
    }
} 