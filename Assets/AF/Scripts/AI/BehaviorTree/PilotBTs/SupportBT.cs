using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Conditions;
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;
using System.Collections.Generic;
using UnityEngine;

namespace AF.AI.BehaviorTree.PilotBTs
{
    // --- 노드 정의는 각자의 .cs 파일로 이동했으므로 여기서는 삭제 ---

    public static class SupportBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            float defaultRepairRange = 5f;
            float allyHealthThresholdForRepair = 0.85f;
            float desiredProximityToAllyForGeneralSupport = 3f; // 수리 외 목적으로 아군 근처 이동 시 거리

            return new SelectorNode(new List<BTNode>
            {
                // --- Sequence 0: 생존 - 재장전 중 긴급 후퇴 또는 방어 ---
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new HasValidTargetNode(), // 적 대상이 있어야 후퇴 판단 가능
                    new SelectorNode(new List<BTNode>
                    {
                        new SequenceNode(new List<BTNode> // 후퇴 시도
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveAwayFromTargetNode(),
                        }),
                        new SequenceNode(new List<BTNode> // 방어 시도
                        {
                            new CanDefendThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode(),
                        })
                    })
                }), 

                // --- Sequence 1: 생존 - 탄약 없으면 즉시 재장전 ---
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // --- Sequence 2: 생존 - 자기 체력 낮으면 자가 수리 ---
                new SequenceNode(new List<BTNode>
                {
                    new IsHealthLowNode(0.5f), // 자신의 체력이 50% 미만이면
                    new HasRepairUsesNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                    new RepairSelfNode() 
                }),
                
                // --- 아군 수리 로직 전체를 하나의 Selector로 묶음 ---
                new SequenceNode(new List<BTNode> // 복원 시도 후 수리 로직 실행
                {
                    new AlwaysSuccessDecorator(new RestorePreservedRepairTargetNode()), // 실패해도 다음으로 넘어감 (Selector가 처리)
                    new SelectorNode(new List<BTNode>
                    {
                        // --- Sequence 3.A: 즉시 수리 (복원/기존 타겟, 범위 내, 수리 가능) ---
                        new SequenceNode(new List<BTNode> 
                        {
                            new HasValidTargetNode(checkTargetPartSlot: true), 
                            new IsTargetAliveNode(),    
                            new IsTargetInRepairRangeNode(defaultRepairRange), 
                            new HasRepairUsesNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.RepairAlly),
                            new CanRepairTargetPartNode(), 
                            new SetRepairAllyActionNode(),
                            new ClearPreservedRepairDataNode() // 수리 성공(결정) 후 Preserved 데이터 삭제
                        }),

                        // --- Sequence 3.B: 기존 타겟에게 이동 (복원/기존 타겟, 범위 밖) ---
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(checkTargetPartSlot: true), 
                            new IsTargetAliveNode(),
                            new InverterNode(new IsTargetInRepairRangeNode(defaultRepairRange)), 
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new SetPreservedRepairTargetNode(), // 이동 결정 시 현재 타겟/파츠를 다음 턴을 위해 저장
                            new MoveToTargetNode(defaultRepairRange * 0.8f) 
                        }),

                        // --- Sequence 3.C: 새로운 수리 대상 탐색 및 처리 (위 조건 모두 실패 시) ---
                        new SequenceNode(new List<BTNode> 
                        {
                            new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, null), 
                            new HasValidTargetNode(checkTargetPartSlot: true), 
                            new IsTargetAliveNode(),
                            new SelectorNode(new List<BTNode> 
                            {
                                // 조건 1: 범위 내 즉시 수리
                                new SequenceNode(new List<BTNode>
                                {
                                    new IsTargetInRepairRangeNode(defaultRepairRange),
                                    new HasRepairUsesNode(),
                                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairAlly),
                                    new CanRepairTargetPartNode(), 
                                    new SetRepairAllyActionNode(),
                                    new ClearPreservedRepairDataNode() // 수리 성공(결정) 후 Preserved 데이터 삭제
                                }),
                                // 조건 2: 범위 밖 이동
                                new SequenceNode(new List<BTNode>
                                {
                                    new InverterNode(new IsTargetInRepairRangeNode(defaultRepairRange)), 
                                    new CanMoveThisActivationNode(),
                                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                                    new SetPreservedRepairTargetNode(), // 이동 결정 시 현재 타겟/파츠를 다음 턴을 위해 저장
                                    new MoveToTargetNode(defaultRepairRange * 0.8f)
                                })
                            })
                        })
                    }) // 아군 수리 Selector 끝
                }), // 복원+수리 전체 시퀀스 끝

                // --- Sequence 4: 보조 역할 - 사용 가능한 자기 버프가 있다면 사용 ---
                // (아군 수리가 최우선이므로, 수리할 대상이 없거나 수리가 불가능할 때 고려)
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new ConfirmAbilityUsageNode() // blackboard.SelectedAbility 사용, AP 체크 후 DecidedActionType.UseAbility 설정 (이것으로 턴 종료)
                }),

                // --- Sequence 5: 일반 지원 - 수리할 아군이 없을 때, 가장 가까운 아군에게 다가가기 (공격 지원 목적) ---
                // (위의 수리/버프 로직이 모두 해당 없을 경우 실행)
                new SequenceNode(new List<BTNode>
                {
                    new InverterNode( 
                        new SequenceNode(new List<BTNode>
                        {
                            // 수리 가능한 대상이 있는지 한번 더 체크 (Preserved 포함해서)
                            // 이 부분이 복잡해질 수 있으므로, 간단하게는 SelectLowestHealthAllyNode가 실패하면 넘어오도록 함.
                            // 위의 수리 Selector가 전체적으로 Failure를 반환해야 이쪽으로 넘어옴.
                            new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, null), // 작동중인 파츠만 선택함
                            new HasValidTargetNode(checkTargetPartSlot:true),
                            new IsTargetAliveNode()
                        })
                    ),
                    new SelectNearestAllyNode(), 
                    new HasValidTargetNode(),
                    new IsTargetAliveNode(),
                    new InverterNode(new IsTargetInRangeNode(desiredProximityToAllyForGeneralSupport)), 
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    new MoveToTargetNode(desiredProximityToAllyForGeneralSupport) 
                }),

                // --- Sequence 6: 일반 공격 - 위의 모든 행동이 해당 없을 시 최후의 수단 ---
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(), // 적 자동 선택
                    new HasValidTargetNode(),
                    new HasSelectedWeaponNode(),
                    new IsTargetInAttackRangeNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                    new AttackTargetNode() // (이것으로 턴 종료)
                }),

                // --- Sequence 7: 마무리 - 낮은 탄약 재장전 (공격 후 여유되면) ---
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.3f), // 탄약 30% 미만이면
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // --- Sequence 8: 마무리 - 이동도 공격도 할거 없으면 방어 ---
                new SequenceNode(new List<BTNode>
                {
                    // 이전에 다른 행동을 이미 결정했으면 방어하지 않도록 함 (예: 이동/공격/수리 등)
                    // 가장 간단하게는, AP가 거의 다 남았을 때만 고려하거나, CanDefendThisActivationNode로 이번 턴에 방어했는지 체크.
                    new CanDefendThisActivationNode(), // 이번 활성화에 아직 방어 안했는지
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),

                // --- Sequence 9: 최후의 수단 - 대기 ---
                new SequenceNode(new List<BTNode>
                {
                    new HasEnoughAPNode(CombatActionEvents.ActionType.None), // AP가 0이 아니더라도 대기 가능
                    new WaitNode() // (이것으로 턴 종료)
                })
            });
        }
    }
} 