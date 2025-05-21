using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using AF.AI.BehaviorTree; // For BTNode, SelectorNode, SequenceNode, MoveAwayFromTargetNode, IsTargetTooCloseNode etc.
using AF.AI.BehaviorTree.Actions; // For AttackTargetNode, MoveToTargetNode, ReloadWeaponNode, SelectTargetNode, WaitNode, ConfirmAbilityUsageNode
using AF.AI.BehaviorTree.Conditions; // IsAnyWeaponReloadingNode, HasSelectedWeaponNode, NeedsReloadNode 사용을 위해 추가
using AF.AI.BehaviorTree.Decorators;
using UnityEngine;

namespace AF.AI.BehaviorTree.PilotBTs
{
    public static class RangedCombatBT
    {
        /// <summary>
        /// 원거리 전투에 특화된 행동 트리입니다.
        /// 카이팅(적절한 거리 유지하며 공격)을 주로 시도합니다.
        /// </summary>
        public static BTNode Create(ArmoredFrame agent)
        {
            float kitingDistanceOverride = -1f;
            if (agent != null)
            {
                Weapon primaryWeapon = agent.GetPrimaryWeapon();
                if (primaryWeapon != null)
                {
                    // 주무기 최대 사거리의 50%를 카이팅 거리로 사용, 단 최소 2m는 확보
                    kitingDistanceOverride = Mathf.Max(primaryWeapon.MaxRange * 0.5f, 2f); 
                }
            }

            IsTargetTooCloseNode isTargetTooCloseNode;
            if (kitingDistanceOverride >= 0f)
            {
                isTargetTooCloseNode = new IsTargetTooCloseNode(kitingDistanceOverride);
            }
            else
            {
                isTargetTooCloseNode = new IsTargetTooCloseNode(); // 기본 동작 사용
            }

            return new SelectorNode(new List<BTNode> // Root Selector
            {
                // 0. 재장전 중 후퇴 시퀀스 (방어 대신)
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(), // 현재 어떤 무기든 재장전 애니메이션/타이머가 돌고 있는지
                    new HasValidTargetNode(), 
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    new MoveAwayFromTargetNode()
                }),

                // 1. OutOfAmmo Reload Sequence (탄약이 완전히 바닥났을 때 최우선 재장전)
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // 2. Self-Repair Sequence
                new SequenceNode(new List<BTNode>
                {
                    new HasRepairUsesNode(),
                    new IsHealthLowNode(0.5f),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                    new RepairSelfNode()
                }),

                // 3. Main Combat Logic Sequence
                new SequenceNode(new List<BTNode>
                {
                    // 3a. 자기 버프 사용 시도 
                    new SelectorNode(new List<BTNode>
                    {
                        new SequenceNode(new List<BTNode> // 실제 버프 사용 결정 시퀀스
                        {
                            new SelectSelfActiveAbilityNode(), // SelectedAbility만 설정
                            new HasSelectedWeaponNode(), 
                            new InverterNode(new IsAnyWeaponReloadingNode()), 
                            new InverterNode(new NeedsReloadNode(ReloadCondition.OutOfAmmo)), 
                            new ConfirmAbilityUsageNode() // 모든 조건 만족 시 DecidedActionType을 UseAbility로 설정
                        }),
                        new WaitNode() // 위 시퀀스 실패 시 (버프 사용 안 함) Success 반환하여 다음으로 진행
                    }),

                    // 3b. 타겟 선택 (버프 사용 여부와 관계없이 진행)
                    new SelectTargetNode(),      
                    new HasValidTargetNode(),    

                    // 3c. 선택된 타겟에 대한 행동 결정 (SelectorNode)
                    new SelectorNode(new List<BTNode> 
                    {
                        // Kiting: Move Away If Too Close
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new CanMoveThisActivationNode(),
                            isTargetTooCloseNode,
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveAwayFromTargetNode()
                        }),
                        // Attack: If Target In Range and NOT Too Close
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new IsTargetInRangeNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                            new AttackTargetNode()
                        }),
                        // Reposition: Move To Target If Target Is Too Far
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode()
                        }),
                        // LowAmmo Reload Sequence (탄약이 적을 때 재장전)
                        new SequenceNode(new List<BTNode>
                        {
                            new NeedsReloadNode(ReloadCondition.LowAmmo),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                            new ReloadWeaponNode()
                        }),
                        // 조건부 방어
                        new SequenceNode(new List<BTNode>
                        {
                            new InverterNode(
                                new SequenceNode(new List<BTNode> 
                                {
                                    new CanMoveThisActivationNode(),
                                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move)
                                })
                            ),
                            new CanDefendThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode()
                        })
                    })
                }),

                // 4. Fallback: Wait if nothing else to do
                new WaitNode() 
            });
        }
    }
} 