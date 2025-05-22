using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Conditions;
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;
using System.Collections.Generic;
using UnityEngine;
using AF.Services; // TextLoggerService 사용을 위해 추가

namespace AF.AI.BehaviorTree.PilotBTs
{
    /// <summary>
    /// 지원 전문 파일럿의 비헤비어 트리입니다.
    /// 아군을 지원하거나 필요 시 자신을 방어합니다.
    /// </summary>
    public static class SupportBT
    {
        /// <summary>
        /// Support Pilot의 비헤비어 트리를 생성합니다.
        /// </summary>
        public static BTNode Create(ArmoredFrame agent)
        {
            float defaultRepairRange = 5f;
            float allyHealthThresholdForRepair = 0.85f;
            float desiredProximityToAllyForGeneralSupport = 3f; // 수리 외 목적으로 아군 근처 이동 시 거리

            // 아군 수리 및 지원 로직을 그룹화하는 Selector 노드
            var allySupportSelector = new SelectorNode(new List<BTNode>
            {
                // --- Sequence 3.A, 3.B, 3.C 통합 또는 재구성: 수리 대상 선정, 이동, 수리 시도 ---
                // 복원 시도 후 수리 로직 실행 (기존 구조 유지)
                new SequenceNode(new List<BTNode> // 복원 시도 + 수리 대상 선정/이동/처리 Sequence
                {
                    new AlwaysSuccessDecorator(new RestorePreservedRepairTargetNode()), // 실패해도 다음으로 넘어감
                    new SelectorNode(new List<BTNode> // 수리 대상 선정 및 처리 Selector (기존 3.A, 3.B, 3.C)
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
                            new SelectLowestHealthAllyNode(allyHealthThresholdForRepair, null), // 작동중인 파츠만 선택함
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

                // --- Sequence 5: 일반 지원 - 수리할 아군이 없을 때, 가장 가까운 아군에게 다가가기 ---
                // (수리 시퀀스가 실패했을 때 이 시퀀스를 시도)
                new SequenceNode(new List<BTNode>
                {
                    new LogMessageNode("[SupportBT.allySupportSelector] Repair sequences failed. Trying to approach ally."), // 디버그 로그 추가
                    new SelectNearestAllyNode(), // 가장 가까운 아군 찾기
                    new HasValidTargetNode(), // 아군 찾았는지 확인
                    new MoveToTargetNode(desiredProximityToAllyForGeneralSupport), // 지정된 거리까지 이동
                    new LogMessageNode("[SupportBT.allySupportSelector] Approached nearest ally.") // 디버그 로그 추가
                }),
            });

            return new SelectorNode(
                new List<BTNode> // Root Selector
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
                                new MoveAwayFromTargetNode()
                            }),
                            new SequenceNode(new List<BTNode> // 방어 시도
                            {
                                new CanDefendThisActivationNode(),
                                new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                                new DefendNode()
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
                        new HasRepairUsesNode(),
                        new IsHealthLowNode(0.5f), // 자신의 체력이 50% 미만이면
                        new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                        new RepairSelfNode()
                    }),

                    // --- 아군 수리 및 지원 로직 그룹 (새로 추가된 Selector) ---
                    allySupportSelector,

                    // --- SASHA: 새로 추가할 적 공격 시퀀스 ---
                    new SequenceNode(new List<BTNode>
                    {
                        new LogMessageNode("[SupportBT.RootSelector] Ally support failed. Trying to attack enemy."), // 디버그 로그 추가
                        new SelectTargetNode(), // 공격할 적 찾기
                        new HasValidTargetNode(), // 적 찾았는지 확인
                        new MoveToTargetNode(), // 적에게 접근 (무기 사거리 내에서 멈추는 로직은 MoveToTargetNode에 포함된다고 가정)
                        new IsTargetInRangeNode(), // 선택된 무기 사거리 안에 있는지 체크 (무기 사거리 사용)
                        new AttackTargetNode() // 공격 실행
                    }),
                    // --- 새로 추가된 적 공격 시퀀스 끝 ---

                    // --- Sequence 4 (기존): 보조 역할 - 사용 가능한 자기 버프가 있다면 사용 ---
                    new SequenceNode(new List<BTNode>
                    {
                        new SelectorNode(new List<BTNode>
                        {
                            new SequenceNode(new List<BTNode> // 실제 버프 사용 결정 시퀀스
                            {
                                new SelectSelfActiveAbilityNode(), // SelectedAbility만 설정
                                new HasSelectedWeaponNode(), // TODO: 버프 사용 시 무기 선택 확인이 필요한가? 논의 필요.
                                new InverterNode(new IsAnyWeaponReloadingNode()),
                                new InverterNode(new NeedsReloadNode(ReloadCondition.OutOfAmmo)),
                                new ConfirmAbilityUsageNode() // 모든 조건 만족 시 DecidedActionType을 UseAbility로 설정
                            }),
                            new WaitNode() // 위 시퀀스 실패 시 (버프 사용 안 함) Success 반환하여 다음으로 진행
                        })
                    }),

                    // --- Sequence 7 (기존 6): 마무리 - 낮은 탄약 재장전 (공격 후 여유되면) ---
                    new SequenceNode(new List<BTNode>
                    {
                        new NeedsReloadNode(ReloadCondition.LowAmmo, 0.3f), // 탄약 30% 미만이면
                        new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                        new ReloadWeaponNode()
                    }),

                     // --- Sequence 8 (기존 7): 마무리 - 이동도 공격도 할거 없으면 방어 ---
                     new SequenceNode(new List<BTNode>
                     {
                         new InverterNode(
                            new SequenceNode(new List<BTNode>
                            {
                                new CanMoveThisActivationNode(),
                                new HasEnoughAPNode(CombatActionEvents.ActionType.Move)
                            })
                         ), // 이동 불가/불필요
                          new InverterNode(
                            new SequenceNode(new List<BTNode>
                             {
                                 new SelectTargetNode(), // 적 자동 선택 시도
                                 new HasValidTargetNode(),
                                 new HasSelectedWeaponNode(),
                                 new HasEnoughAPNode(CombatActionEvents.ActionType.Attack)
                                 // IsTargetInAttackRangeNode 체크는 AttackTargetNode에서 하므로 여기서는 생략 가능
                             })
                          ), // 공격 불가/불필요
                         new CanDefendThisActivationNode(),
                         new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                         new DefendNode()
                     }),

                    // --- Sequence 9 (기존 8): 최후의 수단 - 대기 ---
                    new LogMessageNode("[SupportBT] Reached end of Selector, waiting."), // 디버그 로그 노드 추가
                    new WaitNode() // (이것으로 턴 종료)
                }
            );
        }

        // 임시 로그 노드
        public class LogMessageNode : ActionNode
        {
            private string message;

            public LogMessageNode(string message)
            {
                this.message = message;
            }

            public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
            {
                context.Logger?.TextLogger?.Log(message, LogLevel.Debug);
                return NodeStatus.Success; // 로그만 찍고 성공 반환
            }
        }
    }
} 