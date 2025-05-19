using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree.Conditions;
using AF.AI.BehaviorTree.Decorators;
using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.PilotBTs
{
    public static class MeleeCombatBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            // 근접 전투 행동 트리:
            // 0. 재장전 중이면 방어 시도 (최우선)
            // 1. 타겟을 정하고 교전을 시도 (공격 우선, 안되면 이동)
            // 2. 교전이 불가능하면 방어 시도
            // 3. 모든 행동이 불가능하면 대기
            return new SelectorNode(new List<BTNode>
            {
                // 0. 재장전 중 방어 시퀀스
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),
                // NEW: Self-Repair Sequence (High Priority Survival)
                new SequenceNode(new List<BTNode>
                {
                    new HasRepairUsesNode(),
                    new IsHealthLowNode(0.5f),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                    new RepairSelfNode()
                }),
                // 1. 타겟팅 및 교전 시도
                new SequenceNode(new List<BTNode>
                {
                    // 타겟 확보 로직 (기존 타겟 유효성 검사 또는 신규 타겟 선택)
                    new SelectorNode(new List<BTNode>
                    {
                        new HasValidTargetNode(), // 이미 유효한 타겟이 있는가?
                        new SelectTargetNode()    // 없다면 새로운 타겟을 선택 (블랙보드에 CurrentTarget, SelectedWeapon 설정)
                    }),
                    // 확보된 타겟에 대한 교전 옵션
                    new SelectorNode(new List<BTNode>
                    {
                        // 옵션 1: 공격 시도
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetAliveNode(), // 타겟이 살아있는지 재확인 (SelectTargetNode 이후 또는 프레임 시작 시점에 따라)
                            new IsTargetInRangeNode(), // 선택된 무기(근접)의 사거리 내에 있는가?
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                            new AttackTargetNode()   // 블랙보드의 타겟과 무기로 공격 결정
                        }),
                        // 옵션 2: 타겟에게 이동 시도
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetAliveNode(),
                            new CanMoveThisActivationNode(), // 이번 활성화에 이동한 적이 없는가?
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode()     // 블랙보드의 타겟을 향해 이동 결정 (근접 무기 사거리 고려)
                        })
                    })
                }),
                // 조건부 방어 행동 (★ Refactor_AI_BT_Defense_AP_Optimization.md 기반 수정)
                new SequenceNode(new List<BTNode>
                {
                    new InverterNode(
                        new SequenceNode(new List<BTNode>
                        {
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move)
                        })
                    ),
                    new CanDefendThisActivationNode(), // 이번 활성화에 방어 행동을 한 적이 없는가?
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()             // 방어 결정
                }),
                // 모든 행동이 불가능할 경우 대기
                new WaitNode()
            });
        }
    }
} 