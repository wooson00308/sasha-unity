using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree; // 통합된 네임스페이스 사용
using AF.AI.BehaviorTree.Conditions;
using AF.Combat;
using AF.Models;
using UnityEngine; // Mathf.Max 사용 가능성을 위해 추가

namespace AF.AI.BehaviorTree.PilotBTs
{
    public static class DefenderBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            // AP 기준값 정의 (예: 최대 AP의 30% 또는 최소 3AP)
            float defenseApThreshold = Mathf.Max(agent.CombinedStats.MaxAP * 0.3f, 3f);

            // 이동 목표 거리 계산 (주무기 최대 사거리의 75% 또는 기본 근접 거리)
            float preferredEngagementDistance = 3f; // 기본값
            Weapon primaryWeapon = agent.GetPrimaryWeapon();
            if (primaryWeapon != null)
            {
                preferredEngagementDistance = primaryWeapon.MaxRange * 0.75f;
            }

            return new SelectorNode(new List<BTNode>
            {
                // NEW: Self Active Ability Usage (Highest Priority)
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new ConfirmAbilityUsageNode()
                }),
                // 0. 최우선: 재장전 중 방어
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend), // 방어 최소 AP (1.0f) 체크
                    new DefendNode()
                }),
                // 1. 차선: 스스로 위험할 때 생존 시도 (자가 수리 우선, 그 후 방어)
                new SequenceNode(new List<BTNode>
                {
                    new IsHealthLowNode(0.5f), // 기준값을 0.6f에서 0.5f로 하향
                    new SelectorNode(new List<BTNode> 
                    {
                        new SequenceNode(new List<BTNode>
                        {
                            new HasRepairUsesNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                            new RepairSelfNode()
                        }),
                        new SequenceNode(new List<BTNode>
                        {
                            new CanDefendThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode()
                        })
                    })
                }),
                
                // 2. 타겟팅 및 교전 시퀀스 (공격 또는 이동) - 우선순위 상향
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(), 
                    new SelectorNode(new List<BTNode> 
                    {
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetInRangeNode(), 
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                            new AttackTargetNode()
                        }),
                        new SequenceNode(new List<BTNode>
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode(preferredEngagementDistance)
                        })
                    })
                }),

                // 3. 그 다음: 적이 나를 조준 중일 때 방어 (AP 여유 있을 때만) - 우선순위 하향
                new SequenceNode(new List<BTNode>
                {
                    new IsEnemyTargetingSelfNode(),
                    new CanDefendThisActivationNode(),
                    // AP가 일정 수준 이상 있을 때만 방어 고려 (최소 방어 AP 1.0f는 HasEnoughAPNode에서 체크)
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend, defenseApThreshold), 
                    new DefendNode()
                }),

                // 4. 필수 재장전 시퀀스 (탄약 없음) - 공격/이동 불가 시
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // 5. 낮은 탄약 재장전 시퀀스 - 다른 할 일 없을 때
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.3f), 
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),
                
                new WaitNode() 
            });
        }
    }
} 