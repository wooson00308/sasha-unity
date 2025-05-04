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

        public DefendUtilityAction()
        {
            InitializeConsiderations(); // <<< 생성자 끝에서 호출
        }

        protected override void InitializeConsiderations() // <<< override 추가
        {
            Considerations = new List<IConsideration>
            {
                // --- 방어 관련 Consideration 추가 ---
                new IncomingThreatConsideration(       // <<< 이전 수정 내용 복구 >>>
                    threatRadius: 15f,                 
                    maxThreatCount: 5,                 
                    curveType: UtilityCurveType.Logistic, 
                    steepness: 6f,                     
                    offsetX: 0.3f,                     
                    offsetY: 0f,                       
                    invert: false // <<< 수정: 위협이 높을수록 점수가 높아야 하므로 false (Logistic 기본은 입력값 클수록 1에 가까워짐)
                ),                                     
                new SelfHealthConsideration(),
                new ActionPointCostConsideration(1f)
                // ----------------------------------------
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
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Defend, null, null, null, isCounter: false, freeCounter: false);
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