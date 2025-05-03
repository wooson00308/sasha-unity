using AF.Combat;
using AF.Combat.Behaviors;
using AF.Models;
using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ
using AF.AI.UtilityAI; // For IUtilityAction, IActionSelector etc.
using AF.AI.UtilityAI.Actions; // For example actions
using AF.AI.UtilityAI.Selectors; // For HighestScoreSelector

namespace AF.AI.PilotBehavior
{
    /// <summary>
    /// Utility AI를 사용하여 파일럿 행동을 결정하는 전략
    /// </summary>
    public class UtilityAIPilotBehaviorStrategy : IPilotBehaviorStrategy
    {
        private IActionSelector _actionSelector;

        public UtilityAIPilotBehaviorStrategy()
        {
            // TODO: Action과 Selector를 외부에서 주입받거나 설정 파일 로드 방식으로 변경 (Deferred)
            _actionSelector = new HighestScoreSelector(); // Selector는 일단 유지
        }

        public (CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon)
            DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService context)
        {
            // 1. CombatContext 가져오기 (CombatSimulatorService의 public 메서드 사용)
            CombatContext combatContext = context.GetCurrentContext();

            // 2. 동적 액션 목록 생성
            List<IUtilityAction> possibleActions = GeneratePossibleActions(activeUnit, combatContext);

            // 3. Action Selector 및 액션 목록 유효성 검사
            if (_actionSelector == null || possibleActions == null || possibleActions.Count == 0)
            {
                Debug.LogWarning($"UtilityAIPilotBehaviorStrategy: ActionSelector not initialized or no possible actions generated for {activeUnit.Name}. Falling back to Idle.");
                return (CombatActionEvents.ActionType.None, null, null, null); // Idle -> None 으로 변경
            }

            // 4. 최적의 액션 선택
            IUtilityAction bestAction = _actionSelector.SelectAction(activeUnit, possibleActions, combatContext);

            // 5. 선택된 액션 기반으로 결과 반환
            if (bestAction != null)
            {
                // IUtilityAction 인터페이스의 속성을 사용하여 파라미터 추출 (타입 캐스팅 불필요)
                Debug.Log($"{activeUnit.Name} decided action: {bestAction.Name} (Type: {bestAction.AssociatedActionType}) Target: {bestAction.Target?.Name ?? "None"} Weapon: {bestAction.AssociatedWeapon?.Name ?? "None"} Position: {bestAction.TargetPosition?.ToString() ?? "None"}");

                return (bestAction.AssociatedActionType,
                        bestAction.Target, // 공격, 수리 대상
                        bestAction.TargetPosition, // 이동 목표 위치
                        bestAction.AssociatedWeapon); // 공격, 재장전 무기
            }
            else
            {
                Debug.LogWarning($"{activeUnit.Name} could not determine a utility action. Falling back to Idle.");
                return (CombatActionEvents.ActionType.None, null, null, null); // Idle -> None 으로 변경
            }
        }

        // 동적 액션 생성 메서드
        private List<IUtilityAction> GeneratePossibleActions(ArmoredFrame activeUnit, CombatContext context)
        {
            var actions = new List<IUtilityAction>();
            var enemies = context.GetEnemies(activeUnit); // CombatContext에 GetEnemies 필요

            // --- 공격 액션 생성 ---
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.IsOperational) continue;

                    foreach (var weapon in activeUnit.GetAllWeapons().Where(w => w.IsOperational && w.HasAmmo() && !w.IsReloading))
                    {
                        // 사거리 체크 등 기본적인 실행 가능 여부 확인은 Consideration에 맡길 수도 있음
                        // 여기서는 일단 모든 무기-대상 조합으로 액션 생성
                        actions.Add(new AttackUtilityAction(enemy, weapon));
                    }
                }
            }

            // --- TODO: 이동 액션 생성 로직 추가 ---
            // 예: 주변의 안전한 위치, 적과의 거리 조절 위치 등으로 이동하는 MoveUtilityAction 생성

            // --- TODO: 방어 액션 생성 로직 추가 ---
            // 예: 현재 AP가 충분하고 위협적인 적이 근처에 있을 때 DefendUtilityAction 생성
            // actions.Add(new DefendUtilityAction());

            // --- TODO: 재장전 액션 생성 로직 추가 ---

            // --- TODO: 수리 액션 생성 로직 추가 ---

            return actions;
        }
    }
} 