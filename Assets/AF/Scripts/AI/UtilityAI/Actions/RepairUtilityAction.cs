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

        public RepairUtilityAction(ArmoredFrame targetToRepair)
        {
            _targetToRepair = targetToRepair;
            InitializeConsiderations(); // <<< 생성자 끝에서 호출
        }

        protected override void InitializeConsiderations() // <<< override 추가
        {
            Considerations = new List<IConsideration> // <<< base 호출 불필요 (Base에서 이미 new List 함)
            {
                // --- 수리 관련 Consideration 추가 ---
                new TargetDamagedConsideration(_targetToRepair),      // 대상 손상 여부 (Blocking) 추가
                new TargetHealthConsideration(                        // <<< 수정: Logistic 사용 >>>
                    _targetToRepair,
                    UtilityCurveType.Logistic,   // Logistic 커브 사용
                    steepness: 15f,           // 매우 가파르게 (점수 급변)
                    offsetX: 0.1f,             // 중간점을 매우 낮게 (체력 10%일 때 0.5점)
                    invert: true                 // 체력 낮을수록 점수 높음
                ),                                                    // <<< 수정 끝 >>>
                new IsAllyOrSelfConsideration(_targetToRepair),      // 대상이 아군/자신인지 (Blocking) 추가
                new ActionPointCostConsideration(2.0f)         // <<< AP 비용 평가 추가 (고정값 2.0 사용)
                // ----------------------------------------
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
                    executor.Execute(context, actor, actualRepairType, _targetToRepair, null, null, isCounter: false, freeCounter: false);
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
 