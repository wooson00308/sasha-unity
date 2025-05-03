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

        protected UtilityActionBase()
        {
            Considerations = new List<IConsideration>();
            InitializeConsiderations();
        }

        /// <summary>
        /// Method to be overridden by subclasses to add specific considerations for the action.
        /// </summary>
        protected virtual void InitializeConsiderations()
        {
            // Subclasses will add their specific considerations here
            // Example: Considerations.Add(new SelfHPConsideration());
        }

        /// <summary>
        /// Default utility calculation: Averages the scores of all considerations.
        /// Subclasses can override this for different scoring methods (e.g., weighted average, highest score).
        /// </summary>
        public virtual float CalculateUtility(ArmoredFrame actor, CombatContext context)
        {
            if (Considerations == null || Considerations.Count == 0)
            {
                Debug.LogWarning($"Action '{Name}' has no considerations. Returning 0 utility.");
                return 0f;
            }

            float totalScore = 0f;
            foreach (var consideration in Considerations)
            {
                if (consideration == null) continue;
                totalScore += consideration.CalculateScore(actor, context);
            }

            return totalScore / Considerations.Count; // Simple average
        }

        /// <summary>
        /// Abstract method for executing the action. Must be implemented by subclasses.
        /// </summary>
        public abstract void Execute(ArmoredFrame actor, CombatContext context);
    }
} 