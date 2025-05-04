using System.Collections.Generic;
using UnityEngine;
using AF.Combat;
using AF.Models;
using System.Linq; // For LINQ methods like Average

namespace AF.AI.UtilityAI
{
    /// <summary>
    /// Abstract base class for all utility actions.
    /// Provides common functionality for managing considerations and calculating utility.
    /// </summary>
    public abstract class UtilityActionBase : IUtilityAction
    {
        public abstract string Name { get; } // Concrete actions must provide a name
        public List<IConsideration> Considerations { get; protected set; }

        // +++ IUtilityAction 속성 추가 (abstract 또는 virtual) +++
        public abstract CombatActionEvents.ActionType AssociatedActionType { get; } // 구체적인 액션 타입
        public virtual ArmoredFrame Target => null; // 기본적으로 대상 없음
        public virtual Weapon AssociatedWeapon => null; // 기본적으로 무기 없음
        public virtual Vector3? TargetPosition => null; // 기본적으로 위치 없음
        // +++ 속성 추가 끝 +++

        // +++ 추가: 마지막 계산된 유틸리티 점수 +++
        public float LastCalculatedUtility { get; set; } = 0f;
        // +++ 속성 추가 끝 +++

        protected UtilityActionBase()
        {
            // Considerations = new List<IConsideration>(); // <<< 생성자에서 직접 초기화 제거
            // InitializeConsiderations(); // <<< 생성자에서 호출 제거
        }

        /// <summary>
        /// Initializes the list of Considerations for this action.
        /// Should be overridden by subclasses to add specific considerations.
        /// Called by the subclass constructor AFTER necessary fields (like target) are set.
        /// </summary>
        protected virtual void InitializeConsiderations() // <<< virtual 추가
        {
            Considerations = new List<IConsideration>(); // <<< 여기서 초기화
        }

        /// <summary>
        /// Default utility calculation: Multiplies the scores of all considerations.
        /// Subclasses can override this for different scoring methods.
        /// </summary>
        public virtual float CalculateUtility(ArmoredFrame actor, CombatContext context)
        {
            if (Considerations == null || Considerations.Count == 0)
            {
                Debug.LogWarning($"Action '{Name}' has no considerations. Returning 0 utility.");
                this.LastCalculatedUtility = 0f;
                return 0f;
            }

            float finalScore = 1f;
            foreach (var consideration in Considerations)
            {
                if (consideration == null) continue;
                float currentScore = consideration.CalculateScore(actor, context);
                
                finalScore *= currentScore;

                if (finalScore == 0f)
                {
                    break;
                }
            }

            this.LastCalculatedUtility = finalScore;
            return finalScore;
        }

        /// <summary>
        /// Abstract method for executing the action. Must be implemented by subclasses.
        /// </summary>
        public abstract void Execute(ArmoredFrame actor, CombatContext context);
    }
} 