using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree; // 통합된 네임스페이스 사용
using AF.AI.BehaviorTree.Conditions; // IsAnyWeaponReloadingNode 사용을 위해 추가
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;

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
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility)
                }),
                // 0. 재장전 중 방어 시퀀스 (최우선)
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),
                // 1. 거리 벌리기 시퀀스 (신규)
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
                // 2. 즉시 공격 시퀀스 (최우선)
                new SequenceNode(new List<BTNode>
                {
                    new HasValidTargetNode(),     
                    new IsTargetInRangeNode(),   
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                    new AttackTargetNode()       
                }),
                // 3. 타겟팅 및 교전 시퀀스 (이동 또는 공격)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),       
                    new HasValidTargetNode(), // SASHA: SelectTarget 이후에도 유효성 검사 추가 (제안된 문서에 있었음)
                    new SelectorNode(new List<BTNode> 
                    {
                        // 3a. 사거리 내면 바로 공격
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetInRangeNode(),
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
                // 4. 필수 재장전 시퀀스 (탄약 없음) - 공격/이동 불가 시
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo), // 탄약 없음 조건 사용
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // RELOAD_AP_COST 대신 ActionType.Reload
                    new ReloadWeaponNode()       
                }),
                // 5. 조건부 방어 시퀀스 (★ Refactor_AI_BT_Defense_AP_Optimization.md 기반 수정)
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
                 // 6. 낮은 탄약 재장전 시퀀스 - 다른 할 일 없을 때 최후의 수단
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.1f), // 낮은 탄약(10%) 비율 조건 사용
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload), // RELOAD_AP_COST 대신 ActionType.Reload
                    new ReloadWeaponNode()
                }),
                // 7. 대기 노드 (최후)
                new WaitNode() 
            });
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