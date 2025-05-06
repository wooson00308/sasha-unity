using System.Collections.Generic;
using AF.AI.BehaviorTree.Actions;
using AF.AI.BehaviorTree; // 통합된 네임스페이스 사용
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

        // 각 노드에 필요한 AP 비용 등은 임시값 또는 기본값을 사용합니다.
        // 실제 비용은 Weapon, Unit Stats 등에서 가져와야 하거나, 노드 생성 시 파라미터로 동적으로 주입될 수 있습니다.
        private const float ATTACK_AP_COST = 2f; 
        private const float RELOAD_AP_COST = 1.5f;
        private const float DEFEND_AP_COST = 1f;  
        private const float MIN_MOVE_AP = 1f;   

        /// <summary>
        /// 기본적인 공격형 행동 트리를 생성하여 반환합니다.
        /// </summary>
        /// <returns>생성된 행동 트리의 루트 노드</returns>
        public static BTNode Create()
        {
            return new SelectorNode(new List<BTNode>
            {
                // 1. 재장전 시퀀스
                new SequenceNode(new List<BTNode>
                {
                    new NeedsReloadNode(),       // 파라미터 없이 생성
                    new HasEnoughAPNode(RELOAD_AP_COST),
                    new ReloadWeaponNode()       // 파라미터 없이 생성
                }),
                // 2. 즉시 공격 시퀀스
                new SequenceNode(new List<BTNode>
                {
                    new HasValidTargetNode(),     // 파라미터 없이 생성
                    new IsTargetInRangeNode(),   // 파라미터 없이 생성
                    new HasEnoughAPNode(ATTACK_AP_COST),
                    new AttackTargetNode()       // 파라미터 없이 생성
                }),
                // 3. 타겟팅 및 교전 시퀀스 (이동 또는 공격)
                new SequenceNode(new List<BTNode>
                {
                    new SelectTargetNode(),       // 파라미터 없이 생성
                    new SelectorNode(new List<BTNode> 
                    {
                        // 3a. 사거리 내면 바로 공격
                        new SequenceNode(new List<BTNode>
                        {
                            new IsTargetInRangeNode(),
                            new HasEnoughAPNode(ATTACK_AP_COST),
                            new AttackTargetNode()
                        }),
                        // 3b. 사거리 밖이면 이동
                        new SequenceNode(new List<BTNode>
                        {
                            new HasEnoughAPNode(MIN_MOVE_AP),
                            new MoveToTargetNode() // 파라미터 없이 생성
                        })
                    })
                }),
                // 4. 방어 시퀀스
                new SequenceNode(new List<BTNode>
                {
                    new HasEnoughAPNode(DEFEND_AP_COST),
                    new DefendNode()            // 파라미터 없이 생성
                }),
                // 5. 대기 노드 (위 모든 행동 실패 시)
                new WaitNode() // 파라미터 없이 생성
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