using UnityEngine;
using AF.Combat;
using AF.Models;
using AF.Combat.Behaviors;
using Unity.MLAgents; // PilotAgent 참조를 위해 필요할 수 있음

namespace AF.Combat.Agents // 동일 네임스페이스 사용
{
    /// <summary>
    /// ML-Agent를 파일럿 행동 전략으로 사용하기 위한 래퍼 클래스.
    /// </summary>
    public class MLAgentBehaviorStrategy : IPilotBehaviorStrategy
    {
        public (CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)
            DetermineAction(ArmoredFrame unit, CombatSimulatorService simulator)
        {
            if (unit == null)
            {
                Debug.LogError("[MLAgentBehaviorStrategy] DetermineAction called with null unit.");
                return (CombatActionEvents.ActionType.None, null, null, null);
            }

            // 1. ArmoredFrame에서 PilotAgent 컴포넌트 가져오기 (수정됨)
            var agentComponent = unit.AgentComponent;
            if (agentComponent == null)
            {
                // PilotAgent가 없는 유닛일 수 있음 (예: 플레이어가 직접 조종)
                // 또는 CombatTestRunner에서 설정이 누락된 경우
                Debug.LogWarning($"[MLAgentBehaviorStrategy] PilotAgent component reference not found on ArmoredFrame {unit.Name}. This unit might not be AI controlled or setup is missing.");
                return (CombatActionEvents.ActionType.None, null, null, null);
            }

            // 2. ML-Agent에게 행동 결정 요청
            agentComponent.RequestDecision();
            Debug.Log($"[MLAgentBehaviorStrategy] Requested decision for agent: {unit.Name}");

            // 3. 즉시 반환 (실제 행동은 PilotAgent.OnActionReceived에서 처리)
            //    반환값은 사용되지 않음.
            return (CombatActionEvents.ActionType.None, null, null, null);
        }
    }
} 