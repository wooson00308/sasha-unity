using AF.Combat;
using AF.Models;
using AF.Services;
using UnityEngine;
using System.Collections.Generic;
using AF.AI.UtilityAI.Considerations;

namespace AF.AI.UtilityAI.Actions
{
    /// <summary>
    /// Represents the action of reloading a specific weapon.
    /// </summary>
    public class ReloadUtilityAction : UtilityActionBase
    {
        private Weapon _weaponToReload;

        public override string Name => $"Reload {_weaponToReload?.Name ?? "Unknown"}";

        // +++ IUtilityAction 속성 구현 +++
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Reload;
        public override ArmoredFrame Target => null; // 재장전은 특정 대상을 지정하지 않음
        public override Weapon AssociatedWeapon => _weaponToReload;
        public override Vector3? TargetPosition => null; // 재장전은 특정 위치를 지정하지 않음
        // +++ 속성 구현 끝 +++

        public ReloadUtilityAction(Weapon weaponToReload, SpecializationType pilotSpec)
            : base(pilotSpec)
        {
            _weaponToReload = weaponToReload;
            InitializeConsiderations();
        }

        protected override void InitializeConsiderations()
        {
            if (_weaponToReload == null)
            {
                Debug.LogError($"[{Name}] WeaponToReload is null during initialization.");
                Considerations = new List<IConsideration>();
                return;
            }

            // --- Blocking Considerations ---
            var weaponOperational = new WeaponIsOperationalConsideration(_weaponToReload);
            var notReloading = new WeaponReloadingConsideration(_weaponToReload); // Score is 1 if NOT reloading, 0 if reloading.
            var apCostConsideration = new ActionPointCostConsideration(_weaponToReload.ReloadAPCost);
            // Add a consideration to check if ammo is already full (score 0 if full)
            var needsReload = new AmmoLevelConsideration(_weaponToReload, UtilityCurveType.Step, offsetX: 0.99f, invert: true); // Score 1 if ammo < 99%, 0 otherwise

            // --- Scoring Considerations (Vary by Specialization?) ---
            // For now, use a standard AmmoLevel consideration. Adjust curve/invert based on general reload desire.
            // Typically, lower ammo = higher score to reload.
            var ammoLevel = new AmmoLevelConsideration(_weaponToReload, UtilityCurveType.Linear, invert: true); 

            // Potentially add Threat Level? (Less likely to reload under high threat?)
            // IConsideration threatConsideration = ...;

            Considerations = new List<IConsideration>
            {
                // Blocking considerations first
                weaponOperational,
                notReloading, // Ensure we are not already reloading (consideration returns 0 if reloading)
                needsReload,  // Ensure ammo is not already full
                apCostConsideration,
                
                // Scoring considerations
                ammoLevel
                // threatConsideration? 
            };
        }

        public override void Execute(ArmoredFrame actor, CombatContext context)
        {
            if (!actor.IsOperational || _weaponToReload == null || !_weaponToReload.IsOperational)
            {
                Debug.LogWarning($"{Name}: Invalid execution state - Actor Op: {actor.IsOperational}, Weapon: {_weaponToReload?.Name ?? "NULL"}, Weapon Op: {_weaponToReload?.IsOperational ?? false}");
                return;
            }

            try
            {
                var executor = ServiceLocator.Instance.GetService<ICombatActionExecutor>();
                if (executor != null)
                {
                    // 재장전 액션 실행 호출 (target, position 등은 null)
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Reload, null, null, _weaponToReload, this, isCounter: false, freeCounter: false);
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

        // 재장전 액션의 유틸리티 계산
        public override float CalculateUtility(ArmoredFrame actor, CombatContext context)
        {
            // <<< 수정: Consideration 기반 점수 계산 및 기본 검사 통합 >>>
            // 기본 검사: 재장전할 무기가 없거나, 탄약이 가득 차 있거나, 이미 재장전 중이면 0점
            if (_weaponToReload == null || _weaponToReload.CurrentAmmo >= _weaponToReload.MaxAmmo || _weaponToReload.IsReloading)
            {
                return 0f; 
            }

            if (Considerations == null || Considerations.Count == 0)
            {
                Debug.LogWarning($"ReloadUtilityAction for {actor.Name} weapon {_weaponToReload.Name} has no considerations!");
                return 0f; // Consideration 없으면 0점 반환 (또는 기본 점수? 여기선 0이 맞을 듯)
            }

            float totalScore = 1f;
            bool possible = true;

            foreach (var consideration in Considerations)
            {
                if (consideration == null) continue;

                float score = consideration.CalculateScore(actor, context);

                // Blocking Consideration 처리 (AP 비용 등)
                if (consideration is ActionPointCostConsideration && score <= 0f)
                {
                    possible = false;
                    break; // AP 부족하면 더 계산할 필요 없음
                }
                // TODO: 만약 CanReloadConsideration을 만든다면 여기서 blocking 처리 추가
                // if (consideration is CanReloadConsideration && score <= 0f) { ... }

                // 점수 누적 (곱하기 방식 사용)
                totalScore *= score;
            }

            // 최종 점수 반환 (불가능한 액션은 0점)
            float finalScore = possible ? totalScore : 0f;
            // Debug.Log($"ReloadUtilityAction ({_weaponToReload.Name}): Possible={possible}, FinalScore={finalScore}");
            return finalScore;
            // <<< 수정 끝 >>>
        }
    }
} 