using System.Collections.Generic;
using System.Linq;
using AF.Combat;
using AF.Models;
using AF.Services;
using AF.AI.BehaviorTree.Actions;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 유틸리티 기반 선택 노드
    /// 각 자식 액션의 효용값을 계산하여 가장 높은 효용값을 가진 액션을 실행합니다.
    /// </summary>
    public class UtilitySelectorNode : BTNode
    {
        private readonly List<IUtilityAction> utilityActions;
        private readonly bool enableDebugLogging;
        
        // 현재 실행 중인 액션 추적
        private IUtilityAction currentAction;
        
        public UtilitySelectorNode(List<IUtilityAction> utilityActions, bool enableDebugLogging = false)
        {
            this.utilityActions = utilityActions ?? new List<IUtilityAction>();
            this.enableDebugLogging = enableDebugLogging;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            
            // 모든 액션의 효용값 계산
            var actionUtilities = new List<(IUtilityAction action, float utility)>();
            
            foreach (var action in utilityActions)
            {
                float utility = action.CalculateUtility(agent, blackboard, context);
                actionUtilities.Add((action, utility));
                
                if (enableDebugLogging)
                {
                    textLogger?.Log($"[UtilitySelector] {agent.Name}: {action.ActionName} = {utility:F3}", LogLevel.Debug);
                }
            }
            
            // 효용값이 가장 높은 액션 선택
            var bestAction = actionUtilities
                .Where(x => x.utility > 0f) // 효용값이 0보다 큰 액션만 고려
                .OrderByDescending(x => x.utility)
                .FirstOrDefault();
            
            if (bestAction.action == null)
            {
                if (enableDebugLogging)
                {
                    textLogger?.Log($"[UtilitySelector] {agent.Name}: No viable actions found!", LogLevel.Debug);
                }
                return NodeStatus.Failure;
            }
            
            // 액션이 변경되었는지 확인
            bool actionChanged = currentAction != bestAction.action;
            currentAction = bestAction.action;
            
            if (enableDebugLogging && actionChanged)
            {
                textLogger?.Log($"[UtilitySelector] {agent.Name}: Selected '{currentAction.ActionName}' (utility: {bestAction.utility:F3})", LogLevel.Debug);
            }
            
            // 선택된 액션 실행
            NodeStatus result = currentAction.Execute(agent, blackboard, context);
            
            // 액션이 완료되면 현재 액션 초기화
            if (result != NodeStatus.Running)
            {
                currentAction = null;
            }
            
            return result;
        }
        
        /// <summary>
        /// 새로운 유틸리티 액션을 추가합니다.
        /// </summary>
        public void AddUtilityAction(IUtilityAction action)
        {
            if (action != null && !utilityActions.Contains(action))
            {
                utilityActions.Add(action);
            }
        }
        
        /// <summary>
        /// 유틸리티 액션을 제거합니다.
        /// </summary>
        public void RemoveUtilityAction(IUtilityAction action)
        {
            utilityActions.Remove(action);
        }
        
        /// <summary>
        /// 현재 등록된 모든 액션의 효용값을 반환합니다 (디버깅용)
        /// </summary>
        public Dictionary<string, float> GetAllUtilities(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var utilities = new Dictionary<string, float>();
            foreach (var action in utilityActions)
            {
                utilities[action.ActionName] = action.CalculateUtility(agent, blackboard, context);
            }
            return utilities;
        }
    }
} 