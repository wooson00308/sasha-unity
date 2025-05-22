using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree; // 통합된 네임스페이스 사용
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

                // 2. 타겟팅 및 교전 시퀀스 (공격 또는 이동) - 3
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),
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
    }
} 