using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Evaluators;
using AF.AI.BehaviorTree.Conditions;
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;
using UnityEngine; // Mathf.Max 사용 가능성을 위해 추가

namespace AF.AI.BehaviorTree.PilotBTs
{
    /// <summary>
    /// 방어 전문 AI를 위한 행동 트리의 "구조"를 정의하는 클래스입니다.
    /// 이 클래스의 Create() 메서드는 재사용 가능한 BTNode 인스턴스를 반환합니다.
    /// </summary>
    public static class DefenderBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            // AP 기준값 정의 (예: 최대 AP의 30% 또는 최소 3AP)
            float defenseApThreshold = Mathf.Max(agent.CombinedStats.MaxAP * 0.3f, 3f);

            // 적절한 교전 거리를 계산 (예: 주무기 사거리의 75%)
            float preferredEngagementDistance = 10f; // 기본값 설정 (필요시 조정)
            Weapon primaryWeapon = agent.GetPrimaryWeapon();
            if (primaryWeapon != null)
            {
                preferredEngagementDistance = primaryWeapon.MaxRange * 0.75f;
            }

            return new SelectorNode(new List<BTNode>
            {
                // NEW: Self Active Ability Usage - 0
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility),
                    new ConfirmAbilityUsageNode()
                }),

                // 0. 재장전 중 방어 - 1
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),

                // NEW: 필수 재장전 시퀀스 (탄약 없음) - 2
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo), // 탄약 없음 조건 사용
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // RELOAD_AP_COST 대신 ActionType.Reload
                    new ReloadWeaponNode()
                }),

                // SASHA 신규: 방어 전문 유틸리티 기반 전술 결정
                CreateDefensiveTacticalUtilitySelector(),

                // 2. 타겟팅 및 교전 시퀀스 (공격 또는 이동) - 3
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),
                    new SelectAlternativeWeaponNode(),
                    new HasValidTargetNode(),
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
                            new MoveToTargetNode() // 이제 무기 사거리 사용
                        })
                    })
                }),

                // 5. 조건부 방어 시퀀스 (★ Refactor_AI_BT_Defense_AP_Optimization.md 기반 수정) - 4
                //    : "이동할 여지가 없었거나 이미 이동을 완료했고", "방어는 가능한" 경우
                new SequenceNode(new List<BTNode>
                {
                    // "이번 활성화에 이동할 여지가 없었거나 이미 이동을 했다면" Success
                    new InverterNode(
                        new SequenceNode(new List<BTNode> // "이동할 여지가 있었는가?" 체크
                        {
                            new CanMoveThisActivationNode(), // 아직 안 움직였고
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move) // 이동할 AP도 있다면
                        })
                    ),
                    new CanDefendThisActivationNode(), // 그리고 이번 활성화에 방어 안했고
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend), // 방어 AP는 있고
                    new DefendNode()
                }),

                 // 6. 낮은 탄약 재장전 시퀀스 - 다른 할 일 없을 때 최후의 수단 - 5
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.1f), // 낮은 탄약(10%) 비율 조건 사용
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // RELOAD_AP_COST 대신 ActionType.Reload
                    new ReloadWeaponNode()
                }),

                // 7. 대기 노드 (최후) - 6
                new WaitNode()
            });
        }

        /// <summary>
        /// 방어 전문 유틸리티 선택기를 생성합니다.
        /// 생존과 방어를 최우선으로 하는 보수적인 전술 결정을 합니다.
        /// </summary>
        private static UtilitySelectorNode CreateDefensiveTacticalUtilitySelector()
        {
            var utilityActions = new List<IUtilityAction>();

            // 1. 방어적 어빌리티 사용 (에너지 실드 등 생존 어빌리티 우선)
            var defensiveAbilitySequence = new SequenceNode(new List<BTNode>
            {
                new SelectSelfActiveAbilityNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility),
                new ConfirmAbilityUsageNode()
            });

            var defensiveAbilityEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new AbilityUtilityEvaluator(), // 어빌리티 상황 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.UseAbility, baseUtility: 0.3f, apThreshold: 6f)
            }, new float[] { 0.8f, 0.2f }); // 상황적 필요성 80%, AP 효율성 20% (방어적)

            utilityActions.Add(new BTNodeUtilityAction(defensiveAbilitySequence, defensiveAbilityEvaluator, "Defensive Ability"));

            // 2. 보수적 공격 (안전한 상황에서만)
            var conservativeAttackSequence = new SequenceNode(new List<BTNode>
            {
                new HasValidTargetNode(),     
                new IsTargetInRangeNode(),   
                new IsSelectedWeaponUsableForAttackNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                new AttackTargetNode()       
            });

            // 방어자는 더 보수적인 공격 평가 (체력이 충분할 때만 적극적)
            var conservativeAttackEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new DefensiveAttackUtilityEvaluator(), // 방어적 공격 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Attack, baseUtility: 0.6f, apThreshold: 5f)
            }, new float[] { 0.7f, 0.3f }); // 방어적 판단 70%, AP 효율성 30%

            utilityActions.Add(new BTNodeUtilityAction(conservativeAttackSequence, conservativeAttackEvaluator, "Conservative Attack"));

            // 3. 전술적 방어 (적극적 방어 자세)
            var tacticalDefenseSequence = new SequenceNode(new List<BTNode>
            {
                new CanDefendThisActivationNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                new DefendNode()
            });

            var tacticalDefenseEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new DefensiveUtilityEvaluator(), // 방어 상황 평가
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Defend, baseUtility: 0.4f, apThreshold: 4f)
            }, new float[] { 0.6f, 0.4f }); // 방어 필요성 60%, AP 효율성 40%

            utilityActions.Add(new BTNodeUtilityAction(tacticalDefenseSequence, tacticalDefenseEvaluator, "Tactical Defense"));

            return new UtilitySelectorNode(utilityActions, enableDebugLogging: true);
        }
    }
} 