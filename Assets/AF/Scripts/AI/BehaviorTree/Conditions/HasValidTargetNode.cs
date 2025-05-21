using AF.Combat;
using AF.Models;
using UnityEngine;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 블랙보드에 설정된 현재 유닛이 유효한 목표(및 선택적으로 목표 파츠)를 가지고 있는지 검사하는 조건 노드입니다.
    /// </summary>
    public class HasValidTargetNode : ConditionNode
    {
        private readonly bool _checkTargetPartSlot;

        /// <summary>
        /// 생성자입니다.
        /// </summary>
        /// <param name="checkTargetPartSlot">true일 경우 blackboard.TargetPartSlot도 유효한지 검사합니다.</param>
        public HasValidTargetNode(bool checkTargetPartSlot = false) 
        {
            this._checkTargetPartSlot = checkTargetPartSlot;
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            bool hasValidTarget = blackboard.CurrentTarget != null && !blackboard.CurrentTarget.IsDestroyed;
            
            if (!hasValidTarget)
            {
                // Debug.Log($"[{GetType().Name}] {agent.name}: No valid CurrentTarget on blackboard. Failure.");
                return NodeStatus.Failure;
            }

            // TargetPartSlot 검사가 요구되는 경우
            if (_checkTargetPartSlot)
            {
                if (string.IsNullOrEmpty(blackboard.TargetPartSlot))
                {
                    // Debug.Log($"[{GetType().Name}] {agent.name}: TargetPartSlot check required, but TargetPartSlot is null or empty. Failure.");
                    return NodeStatus.Failure;
                }
            }
            
            // Debug.Log($"[{GetType().Name}] {agent.name}: Has a valid target on blackboard: {blackboard.CurrentTarget.name}{( _checkTargetPartSlot ? " and TargetPartSlot: " + blackboard.TargetPartSlot : "")}. Success.");
            return NodeStatus.Success;
        }
    }
} 