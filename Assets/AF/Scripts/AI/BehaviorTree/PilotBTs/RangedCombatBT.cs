using System.Collections.Generic;
using AF.Combat;
using AF.Models;
using AF.AI.BehaviorTree; // For BTNode, SelectorNode, SequenceNode, MoveAwayFromTargetNode, IsTargetTooCloseNode etc.
using AF.AI.BehaviorTree.Actions; // For AttackTargetNode, MoveToTargetNode, ReloadWeaponNode, SelectTargetNode, WaitNode
using AF.AI.BehaviorTree.Conditions; // IsAnyWeaponReloadingNode 사용을 위해 추가
// NeedsReloadNode, HasEnoughAPNode, IsTargetInRangeNode, IsTargetAliveNode, HasValidTargetNode are in AF.AI.BehaviorTree namespace based on previous analysis
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

            return new SelectorNode(new List<BTNode>
            {
                // 0. 재장전 중 후퇴 시퀀스 (방어 대신)
                new SequenceNode(new List<BTNode>
                {
                    new IsAnyWeaponReloadingNode(),
                    new HasValidTargetNode(), // 후퇴하려면 타겟이 필요
                    new CanMoveThisActivationNode(),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                    new MoveAwayFromTargetNode()
                }),

                // 1. OutOfAmmo Reload Sequence (Still highest practical priority for RELOAD DECISION)
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(ReloadCondition.OutOfAmmo),
                    new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                    new ReloadWeaponNode()
                }),

                // 2. Main Combat Logic (Select Target FIRST, then decide action)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),      
                    new HasValidTargetNode(),    
                    new SelectorNode(new List<BTNode> 
                    {
                        // 2a. Kiting: Move Away If Too Close
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new CanMoveThisActivationNode(),
                            isTargetTooCloseNode,
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveAwayFromTargetNode()
                        }),
                        // 2b. Attack: If Target In Range and NOT Too Close
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new IsTargetInRangeNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Attack),
                            new AttackTargetNode()
                        }),
                        // 2c. Reposition: Move To Target If Target Is Too Far
                        new SequenceNode(new List<BTNode>
                        {
                            new HasValidTargetNode(),
                            new CanMoveThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Move),
                            new MoveToTargetNode()
                        }),
                        // 2d. LowAmmo Reload Sequence (Moved higher in action consideration)
                        // 만약 공격/이동 다 여의치 않고 탄약이 적다면 재장전 시도
                        new SequenceNode(new List<BTNode>
                        {
                            new NeedsReloadNode(ReloadCondition.LowAmmo),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Reload),
                            new ReloadWeaponNode()
                        }),
                        // 2e. 방어 (신규) - 일반 상황에서의 방어는 여기 유지
                        new SequenceNode(new List<BTNode>
                        {
                            new CanDefendThisActivationNode(),
                            new HasEnoughAPNode(CombatActionEvents.ActionType.Defend),
                            new DefendNode()
                        })
                    })
                }),

                // 3. Fallback: Wait if nothing else to do
                // 위 로직에서 아무것도 결정되지 않으면 기본 대기
                // 명시적 WaitNode 대신, Selector가 모두 실패하면 Success를 반환하도록 하거나,
                // 혹은 정말 아무것도 할 게 없을 때를 위한 낮은 우선순위의 WaitNode를 둘 수 있음.
                // 현재 구조에서는 Main Combat Logic Sequence가 성공하면 이쪽으로 오지 않음.
                // SelectTargetNode가 실패하는 경우 (예: 모든 적이 쓰러짐) 등에는 이쪽으로 올 수 있음.
                new WaitNode() // 기본 생성자 사용
            });
        }
    }
} 