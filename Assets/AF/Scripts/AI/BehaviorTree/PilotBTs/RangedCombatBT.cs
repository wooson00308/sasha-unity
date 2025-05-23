using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using AF.AI.BehaviorTree.Evaluators;
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

                // SASHA 신규: 원거리 전투 특화 유틸리티 기반 전술 결정
                CreateRangedTacticalUtilitySelector(kitingDistanceOverride),

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
                    new SelectAlternativeWeaponNode(),
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
                        // NEW: 무기 사용 불가능 시 또는 이동 불가 시 방어 시퀀스 - 메인 로직 내 최후의 수단
                        // 조건부 방어
                        new SequenceNode(new List<BTNode>
                        {
                            // 무기 사용이 불가능하거나 (SelectAlternativeWeaponNode 실패 등), 이동할 수 없을 때 방어 시도
                            new SelectorNode(new List<BTNode> 
                            {
                                new InverterNode(new SelectAlternativeWeaponNode()), // 무기 사용 불가능
                                new InverterNode( // 이동 불가/불필요 체크
                                    new SequenceNode(new List<BTNode> 
                                    {
                                        new CanMoveThisActivationNode(),
                                        new HasEnoughAPNode(CombatActionEvents.ActionType.Move)
                                    })
                                )
                            }),
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

        /// <summary>
        /// 원거리 전투 특화 유틸리티 선택기를 생성합니다.
        /// 카이팅과 거리 조절에 특화된 전술 결정을 합니다.
        /// </summary>
        private static UtilitySelectorNode CreateRangedTacticalUtilitySelector(float kitingDistance)
        {
            var utilityActions = new List<IUtilityAction>();

            // 1. 원거리 버프 어빌리티 사용 (정밀 조준 등)
            var rangedAbilitySequence = new SequenceNode(new List<BTNode>
            {
                new SelectSelfActiveAbilityNode(),
                new HasSelectedWeaponNode(),
                new InverterNode(new IsAnyWeaponReloadingNode()),
                new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility),
                new ConfirmAbilityUsageNode()
            });

            var rangedAbilityEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new AbilityUtilityEvaluator(), // 어빌리티 상황 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.UseAbility, baseUtility: 0.4f, apThreshold: 5f)
            }, new float[] { 0.7f, 0.3f }); // 상황적 필요성 70%, AP 효율성 30%

            utilityActions.Add(new BTNodeUtilityAction(rangedAbilitySequence, rangedAbilityEvaluator, "Ranged Ability"));

            // 2. 카이팅 (적이 너무 가까우면 거리 벌리기)
            var kitingSequence = new SequenceNode(new List<BTNode>
            {
                new HasValidTargetNode(),
                new CanMoveThisActivationNode(),
                new IsTargetTooCloseNode(kitingDistance),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                new MoveAwayFromTargetNode()
            });

            var kitingEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new KitingUtilityEvaluator(kitingDistance), // 카이팅 필요성 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Move, baseUtility: 0.6f, apThreshold: 4f)
            }, new float[] { 0.8f, 0.2f }); // 카이팅 필요성 80%, AP 효율성 20%

            utilityActions.Add(new BTNodeUtilityAction(kitingSequence, kitingEvaluator, "Kiting Movement"));

            // 3. 원거리 공격 (최적 거리에서)
            var rangedAttackSequence = new SequenceNode(new List<BTNode>
            {
                new HasValidTargetNode(),
                new IsTargetInRangeNode(),
                new InverterNode(new IsTargetTooCloseNode(kitingDistance)),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                new AttackTargetNode()
            });

            var rangedAttackEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new RangedAttackUtilityEvaluator(), // 원거리 공격 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Attack, baseUtility: 0.7f, apThreshold: 3f)
            }, new float[] { 0.8f, 0.2f }); // 원거리 공격 우선 80%, AP 효율성 20%

            utilityActions.Add(new BTNodeUtilityAction(rangedAttackSequence, rangedAttackEvaluator, "Ranged Attack"));

            // 4. 포지셔닝 (최적 사격 위치로 이동)
            var positioningSequence = new SequenceNode(new List<BTNode>
            {
                new HasValidTargetNode(),
                new CanMoveThisActivationNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                new MoveToTargetNode()
            });

            var positioningEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new RangedPositioningUtilityEvaluator(), // 포지셔닝 필요성 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Move, baseUtility: 0.5f, apThreshold: 4f)
            }, new float[] { 0.7f, 0.3f }); // 포지셔닝 필요성 70%, AP 효율성 30%

            utilityActions.Add(new BTNodeUtilityAction(positioningSequence, positioningEvaluator, "Ranged Positioning"));

            return new UtilitySelectorNode(utilityActions, enableDebugLogging: true);
        }
    }
} 