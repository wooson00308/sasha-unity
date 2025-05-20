using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree; // 통합된 네임스페이스 사용
using AF.AI.BehaviorTree.Conditions;
using AF.Combat;
using AF.Models;

namespace AF.AI.BehaviorTree.PilotBTs
{
    public static class DefenderBT
    {
        public static BTNode Create(ArmoredFrame agent)
        {
            return new SelectorNode(new List<BTNode>
            {
                // NEW: Self Active Ability Usage (Highest Priority)
                new SequenceNode(new List<BTNode>
                {
                    new SelectSelfActiveAbilityNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.UseAbility)
                }),
                // 0. 최우선: 재장전 중 방어
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),
                // 1. 차선: 스스로 위험할 때 생존 시도 (자가 수리 우선, 그 후 방어)
                new SequenceNode(new List<BTNode>
                {
                    new IsHealthLowNode(0.8f), // 높은 체력 기준 유지 (기존 0.8f)
                    new SelectorNode(new List<BTNode> // 수리 또는 방어 선택
                    {
                        // 1a. 자가 수리 시도
                        new SequenceNode(new List<BTNode>
                        {
                            new HasRepairUsesNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.RepairSelf),
                            new RepairSelfNode()
                        }),
                        // 1b. 수리 불가 시 방어 시도
                        new SequenceNode(new List<BTNode>
                        {
                            new CanDefendThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode()
                        })
                    })
                }),
                // 2. 그 다음: 적이 나를 조준 중일 때 방어
                new SequenceNode(new List<BTNode>
                {
                    new IsEnemyTargetingSelfNode(),
                    new CanDefendThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                    new DefendNode()
                }),
                
                // 3. 타겟팅 및 교전 시퀀스 (공격 또는 이동)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(), // 타겟이 없거나 유효하지 않으면 새 타겟 선택
                    new SelectorNode(new List<BTNode> // 타겟이 있으면 다음 중 하나 실행
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
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // 5. 낮은 탄약 재장전 시퀀스 - 다른 할 일 없을 때
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.LowAmmo, 0.3f), // 탄약 30% 이하 시
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),
                
                // 최후의 수단
                new WaitNode() 
            });
        }
    }
} 