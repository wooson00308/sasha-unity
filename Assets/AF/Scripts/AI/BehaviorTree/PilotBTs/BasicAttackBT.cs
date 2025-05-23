using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Conditions; // IsAnyWeaponReloadingNode 사용을 위해 추가
using AF.AI.BehaviorTree.Decorators;
using AF.AI.BehaviorTree.Evaluators;
using AF.Combat;

namespace AF.AI.BehaviorTree.PilotBTs
{
    /// <summary>
    /// 기본적인 공격형 AI를 위한 행동 트리의 "구조"를 정의하는 클래스입니다.
    /// 이 클래스의 Create() 메서드는 재사용 가능한 BTNode 인스턴스를 반환합니다.
    /// </summary>
    public static class BasicAttackBT // 정적 클래스로 변경
    {
        // 이 클래스는 더 이상 ArmoredFrame 인스턴스를 직접 참조하지 않음
        // public BTNode rootNode; // 인스턴스 필드 제거

        /// <summary>
        /// 기본적인 공격형 행동 트리를 생성하여 반환합니다.
        /// </summary>
        /// <returns>생성된 행동 트리의 루트 노드</returns>
        public static BTNode Create()
        {
            return new SelectorNode(new List<BTNode>
            {
                // NEW: 필수 재장전 시퀀스 (탄약 없음) - 최고 우선순위
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo), // 탄약 없음 조건 사용
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // 재장전 AP 충분한지 체크
                    new ReloadWeaponNode()       
                }),
                // 1. 거리 벌리기 시퀀스 (신규) - 생존 우선
                new SequenceNode(new List<BTNode>
                {
                    // 예시 임계값, 실제 값은 밸런싱 필요
                    new IsTargetTooCloseNode(),
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    // MoveAway 액션은 AP 비용 확인이 필요할 수 있음 (HasEnoughAPNode 추가 고려)
                    new MoveAwayFromTargetNode()
                }),
                // NEW: Self-Repair Sequence (High Priority Survival)
                new SequenceNode(new List<BTNode>
                {
                    new HasRepairUsesNode(),          // Check if repair uses are available
                    new IsHealthLowNode(0.5f),      // Check if health is below 50%
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                    new RepairSelfNode()              // Set action to RepairSelf
                }),
                
                // SASHA 신규: 유틸리티 기반 전술 결정 - 어빌리티와 공격을 스마트하게 선택
                CreateTacticalUtilitySelector(),
                
                // 3. 타겟팅 및 교전 시퀀스 (이동 또는 공격)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),       
                    new SelectAlternativeWeaponNode(), // 추가: 선택된 무기 사용 불가 시 다른 무기 선택
                    new HasValidTargetNode(), // SASHA: SelectTarget 이후에도 유효성 검사 추가 (제안된 문서에 있었음)
                    new SelectorNode(new List<BTNode> 
                    {
                        // 3a. 사거리 내면 바로 공격
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetInRangeNode(),
                            new IsSelectedWeaponUsableForAttackNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                            new AttackTargetNode()
                        }),
                        // 3b. 사거리 밖이면 이동
                        new SequenceNode(new List<BTNode>
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode() 
                        })
                    })
                }),
                // NEW: 낮은 탄약 재장전 시퀀스 - 공격 시퀀스 이후로 이동
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.2f), // 낮은 탄약(20%) 비율 조건으로 상향 조정
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // 재장전 AP 충분한지 체크
                    new ReloadWeaponNode()       
                }),
                // NEW: 무기 사용 불가능 시 방어 시퀀스 - 타겟팅/교전 실패 시 시도
                new SequenceNode(new List<BTNode>
                {
                    // 이 시퀀스는 타겟팅/교전 시퀀스가 실패했을 때 실행될 가능성이 높음.
                    // 즉, 공격/이동을 시도했지만 실패했거나 할 수 없었을 때.
                    // 여기서 다시 한번 방어 가능 여부와 AP 체크 후 방어.
                    new CanDefendThisActivationNode(), // 이번 활성화에 방어 안 했고
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend), // 방어 AP는 있고
                    new DefendNode() // 방어 결정
                }),
                // 5. 조건부 방어 시퀀스 (★ Refactor_AI_BT_Defense_AP_Optimization.md 기반 수정) - 낮은 탄약 재장전 뒤로 이동
                //    : "이동할 여지가 없었거나 이미 이동을 완료했고", "방어는 가능한" 경우
                //    이 시퀀스는 이제 '무기 사용 불가능 시 방어 시퀀스'보다 후순위로 밀림.
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
                // 7. 대기 노드 (최후)
                new WaitNode() 
            });
        }

        /// <summary>
        /// 전술적 유틸리티 선택기를 생성합니다.
        /// 어빌리티 사용과 즉시 공격 중에서 AP 효율성을 고려하여 최적의 선택을 합니다.
        /// </summary>
        private static UtilitySelectorNode CreateTacticalUtilitySelector()
        {
            var utilityActions = new List<IUtilityAction>();

            // 1. 어빌리티 사용 액션 (스마트한 조건부 실행)
            var abilitySequence = new SequenceNode(new List<BTNode>
            {
                new SelectSelfActiveAbilityNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility),
                new ConfirmAbilityUsageNode()
            });

            var abilityEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new AbilityUtilityEvaluator(), // 파라미터 없는 생성자 사용
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.UseAbility, baseUtility: 0.4f, apThreshold: 5f)
            }, new float[] { 0.6f, 0.4f }); // 상황적 판단 60%, AP 효율성 40%

            utilityActions.Add(new BTNodeUtilityAction(abilitySequence, abilityEvaluator, "Ability Usage"));

            // 2. 즉시 공격 액션 (기존 높은 우선순위 유지)  
            var immediateAttackSequence = new SequenceNode(new List<BTNode>
            {
                new HasValidTargetNode(),     
                new IsTargetInRangeNode(),   
                new IsSelectedWeaponUsableForAttackNode(),
                new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                new AttackTargetNode()       
            });

            var attackEvaluator = new CompositeUtilityEvaluator(new IUtilityEvaluator[]
            {
                new AttackUtilityEvaluator(),
                new APEfficiencyEvaluator(CombatActionEvents.ActionType.Attack, baseUtility: 0.8f, apThreshold: 4f)
            }, new float[] { 0.7f, 0.3f }); // 공격 상황 70%, AP 효율성 30%

            utilityActions.Add(new BTNodeUtilityAction(immediateAttackSequence, attackEvaluator, "Immediate Attack"));

            return new UtilitySelectorNode(utilityActions, enableDebugLogging: true);
        }

        // Execute 메서드 제거: BT 실행은 외부에서 ArmoredFrame의 BehaviorTreeRoot.Tick()을 통해 이루어짐
        /*
        public NodeStatus Execute(CombatContext context, ArmoredFrame unit)
        {
            return rootNode.Execute(context, unit);
        }
        */
    }
} 