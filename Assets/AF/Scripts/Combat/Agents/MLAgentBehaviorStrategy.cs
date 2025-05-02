using UnityEngine;
using AF.Combat;
using AF.Models;
using AF.Combat.Behaviors;
using Cysharp.Threading.Tasks; // Added for UniTask
using System.Threading; // Added for CancellationToken
using System;

namespace AF.Combat.Agents // Use Agents namespace or move file to Behaviours?
{
    /// <summary>
    /// ML-Agent를 파일럿 행동 전략으로 사용하기 위한 래퍼 클래스.
    /// </summary>
    public class MLAgentBehaviorStrategy : PilotBehaviorStrategyBase // Inherit from base
    {
        // Implement the abstract synchronous method (required by base class)
        public override (CombatActionEvents.ActionType, ArmoredFrame, Vector3?, Weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatContext context)
        {
            // This synchronous method shouldn't ideally be called for ML Agents.
            // Throw an exception or return a default action.
            Debug.LogWarning("Synchronous DetermineAction called for MLAgentBehaviorStrategy. Use DetermineActionAsync instead.");
            return (CombatActionEvents.ActionType.None, null, null, null);
            // Or: throw new NotImplementedException("Use DetermineActionAsync for ML Agents.");
        }

        // Override the virtual asynchronous method
        public override async UniTask<(CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)>
            DetermineActionAsync(ArmoredFrame activeUnit, CombatContext context, CancellationToken cancellationToken = default)
        {
            if (activeUnit == null)
            {
                Debug.LogError("[MLAgentBehaviorStrategy] DetermineActionAsync called with null unit.");
                return (CombatActionEvents.ActionType.None, null, null, null);
            }

            // 1. ArmoredFrame에서 PilotAgent 컴포넌트 가져오기
            var agentComponent = activeUnit.AgentComponent;
            if (agentComponent == null)
            {
                Debug.LogWarning($"[MLAgentBehaviorStrategy] PilotAgent component reference not found on ArmoredFrame {activeUnit.Name}. Defaulting to None action.");
                return (CombatActionEvents.ActionType.None, null, null, null);
            }

            try
            {
                // 2. PilotAgent에게 비동기적으로 행동 결정 요청 및 대기
                //    (PilotAgent.cs에 RequestDecisionAsync 구현 필요)
                var actionData = await agentComponent.RequestDecisionAsync(cancellationToken);
                return actionData;
            }
            catch (OperationCanceledException)
            {
                 Debug.Log($"[MLAgentBehaviorStrategy] Action determination for {activeUnit.Name} cancelled.");
                 return (CombatActionEvents.ActionType.None, null, null, null); // Return default on cancellation
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[MLAgentBehaviorStrategy] Error during action determination for {activeUnit.Name}: {ex.Message}");
                 return (CombatActionEvents.ActionType.None, null, null, null); // Return default on error
            }
        }
    }
} 