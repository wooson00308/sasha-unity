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

        public override string Name => $"Move to {_targetPosition}";

        // +++ IUtilityAction 속성 구현 +++
        public override CombatActionEvents.ActionType AssociatedActionType => CombatActionEvents.ActionType.Move;
        public override ArmoredFrame Target => null; // 이동 액션은 특정 유닛을 대상으로 하지 않음
        public override Weapon AssociatedWeapon => null; // 이동 액션은 무기를 사용하지 않음
        public override Vector3? TargetPosition => _targetPosition;
        // +++ 속성 구현 끝 +++

        public MoveUtilityAction(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            InitializeConsiderations(); // <<< 생성자 끝에서 호출
        }

        protected override void InitializeConsiderations() // <<< override 추가
        {
            Considerations = new List<IConsideration>
            {
                // --- 이동 관련 Consideration 추가 ---
                new TargetPositionSafetyConsideration(_targetPosition), // 안전성 평가 추가
                new DistanceToEnemyConsideration(_targetPosition, 100f), // <<< 수정: maxDistance 100f로 증가
                new ActionPointCostConsideration(3f) // <<< AP 비용 평가 추가 (임시값 3)
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
                    // 이동 액션 실행 호출 (targetFrame, weapon 등은 null)
                    executor.Execute(context, actor, CombatActionEvents.ActionType.Move, null, _targetPosition, null, isCounter: false, freeCounter: false);
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