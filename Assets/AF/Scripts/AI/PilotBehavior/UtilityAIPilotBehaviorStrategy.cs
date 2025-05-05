using AF.Combat;
using AF.Combat.Behaviors;
using AF.Models;
using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ
using AF.AI.UtilityAI; // For IUtilityAction, IActionSelector etc.
using AF.AI.UtilityAI.Actions; // For example actions
using AF.AI.UtilityAI.Selectors; // For HighestScoreSelector
using AF.Services; // For ServiceLocator
using AF.Tests; // For CombatTestRunner

namespace AF.AI.PilotBehavior
{
    /// <summary>
    /// Utility AI를 사용하여 파일럿 행동을 결정하는 전략
    /// </summary>
    public class UtilityAIPilotBehaviorStrategy : IPilotBehaviorStrategy
    {
        private IActionSelector _actionSelector;
        private TextLoggerService _textLogger; // TextLoggerService reference
        private bool _logAIDecisions = false; // AI decision logging flag

        // +++ 추가: 인스턴스별 액션 가중치 +++
        private readonly Dictionary<CombatActionEvents.ActionType, float> _weights;
        // +++ 추가 끝 +++

        // +++ 생성자 수정: 가중치 딕셔너리 주입 (기존 생성자 대체) +++
        public UtilityAIPilotBehaviorStrategy(
            Dictionary<CombatActionEvents.ActionType, float> weights, 
            IActionSelector actionSelector = null // ActionSelector는 선택적으로 받도록 유지
            )
        {
            // 가중치가 null이면 빈 딕셔너리 사용 (기본값 1.0 처리를 위해)
            _weights = weights ?? new Dictionary<CombatActionEvents.ActionType, float>(); 
            _actionSelector = actionSelector ?? new HighestScoreSelector();
            InitializeLogger(); // Initialize logger
        }
        // +++ 생성자 수정 끝 +++

        // Logger initialization method
        private void InitializeLogger()
        {
            try
            {
                _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();
                 // Get the initial state of the toggle from CombatTestRunner (optional, could default to false)
                  var testRunner = Object.FindFirstObjectByType<CombatTestRunner>(); // Use FindObjectOfType carefully
                  if (testRunner != null)
                  {
                      _logAIDecisions = testRunner.logAIDecisions;
                  }
                  else {
                       Debug.LogWarning("CombatTestRunner instance not found. AI Decision logging will be off by default.");
                       _logAIDecisions = false;
                  }

                 Debug.Log("Utility AI Decision Logging is forcefully enabled for debugging."); // <<< 확인용 로그 추가
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get TextLoggerService: {e.Message}. AI decision logging will be disabled.");
                _textLogger = null;
                _logAIDecisions = false;
            }
        }

        public IUtilityAction DetermineAction(ArmoredFrame activeUnit, CombatSimulatorService context)
        {
            // 1. CombatContext 가져오기
            CombatContext combatContext = context.GetCurrentContext();

            // <<< Get Pilot Specialization >>>
            SpecializationType pilotSpec = activeUnit.Pilot?.Specialization ?? SpecializationType.StandardCombat;

            // 2. 동적 액션 목록 생성 (Pass specialization)
            List<IUtilityAction> possibleActions = GeneratePossibleActions(activeUnit, combatContext, pilotSpec);

            // 3. Action Selector 및 액션 목록 유효성 검사
            if (_actionSelector == null || possibleActions == null || possibleActions.Count == 0)
            {
                Debug.LogWarning($"UtilityAIPilotBehaviorStrategy: ActionSelector not initialized or no possible actions generated for {activeUnit.Name}. Falling back to Idle.");
                return null;
            }

            // 4. 최적의 액션 선택
            IUtilityAction bestAction = SelectBestWeightedAction(activeUnit, possibleActions, combatContext);

            // +++ AI Decision Logging +++
            if (_logAIDecisions && _textLogger?.TextLogger != null)
            {
                var logBuilder = new System.Text.StringBuilder();
                logBuilder.AppendLine($"--- AI Decision Log for [{activeUnit.Name}] (Turn: {combatContext.CurrentTurn}) ---");

                // 액션 목록 정렬 기준 변경: 가중 점수 (Action 객체에 임시 저장 필요 or 여기서 재계산)
                // 여기서는 간단하게 기본 유틸리티 기준 정렬 유지하고, 로그에 가중치/가중점수 추가
                foreach (var action in possibleActions.OrderByDescending(a => a.LastCalculatedUtility * (_weights.TryGetValue(a.AssociatedActionType, out var w) ? w : 1.0f))) // <<< 가중 점수 기준으로 정렬
                {
                    float baseUtility = action.LastCalculatedUtility; // 이미 계산된 기본 유틸리티 사용
                    float weight = _weights.TryGetValue(action.AssociatedActionType, out var currentWeight) ? currentWeight : 1.0f; // 인스턴스 가중치 사용
                    float weightedScore = baseUtility * weight;

                    logBuilder.Append($"  - Action: {action.Name ?? action.GetType().Name}");
                    if (action.Target != null) logBuilder.Append($" (Target: {action.Target.Name})");
                    if (action.AssociatedWeapon != null) logBuilder.Append($" (Weapon: {action.AssociatedWeapon.Name})");
                    if (action.TargetPosition.HasValue) logBuilder.Append($" (Pos: {action.TargetPosition.Value})");
                    logBuilder.AppendLine($": BaseScore={baseUtility:F3}, Weight={weight:F2}, WeightedScore={weightedScore:F3}"); // <<< 로그 형식 수정

                    // 고려사항 점수 기록
                    foreach (var consideration in action.Considerations)
                    {
                        logBuilder.AppendLine($"    * Consideration: {consideration.Name ?? consideration.GetType().Name}, Score: {consideration.LastScore:F3}");
                    }
                }

                if (bestAction != null)
                {
                    // 선택된 액션의 최종 가중 점수 표시 (SelectBestWeightedAction에서 최고 점수를 저장해두거나 여기서 재계산)
                    float finalWeight = _weights.TryGetValue(bestAction.AssociatedActionType, out var finalW) ? finalW : 1.0f;
                    float finalWeightedScore = bestAction.LastCalculatedUtility * finalWeight;
                    logBuilder.AppendLine($"--- Selected Action: {bestAction.Name ?? bestAction.GetType().Name} (BaseScore: {bestAction.LastCalculatedUtility:F3}, Weight: {finalWeight:F2}, WeightedScore: {finalWeightedScore:F3}) ---"); // <<< 로그 형식 수정
                }
                else
                {
                    logBuilder.AppendLine("--- No Action Selected (Idle/None) ---");
                }
                 // CombatTextUIService가 LogLevel.Debug를 표시하도록 설정되어 있다고 가정
                _textLogger.TextLogger.Log(logBuilder.ToString(), LogLevel.Debug);
            }
            // +++ End AI Decision Logging +++

            // 5. 선택된 액션 반환
            if (bestAction != null)
            {
                Debug.Log($"{activeUnit.Name} decided action: {bestAction.Name} (Type: {bestAction.AssociatedActionType}) Target: {bestAction.Target?.Name ?? "None"} Weapon: {bestAction.AssociatedWeapon?.Name ?? "None"} Position: {bestAction.TargetPosition?.ToString() ?? "None"}");
                return bestAction;
            }
            else
            {
                Debug.LogWarning($"{activeUnit.Name} could not determine a utility action. Falling back to Idle.");
                return null;
            }
        }

        // +++ 가중치를 적용하여 최적 액션을 선택하는 메서드 추가 +++
        private IUtilityAction SelectBestWeightedAction(ArmoredFrame activeUnit, List<IUtilityAction> possibleActions, CombatContext combatContext)
        {
             if (possibleActions == null || possibleActions.Count == 0)
             {
                 Debug.LogWarning($"[{activeUnit.Name}] No possible actions generated.");
                 return null; // 또는 기본 액션 (예: Idle or Defend) 반환
             }

             IUtilityAction bestAction = null;
             float highestWeightedScore = float.MinValue; // 최고 가중 점수
             List<IUtilityAction> bestActions = new List<IUtilityAction>(); // 동점 액션 리스트

             foreach (var action in possibleActions)
             {
                 float baseUtility = action.CalculateUtility(activeUnit, combatContext); // 기본 유틸리티 계산 (LastCalculatedUtility에 저장됨)
                 
                 // <<< 인스턴스 변수 _weights 사용 >>>
                 float weight = _weights.TryGetValue(action.AssociatedActionType, out var w) ? w : 1.0f; 
                 float weightedScore = baseUtility * weight; // 가중 점수 계산

                 // 최고 점수 갱신 또는 동점 리스트에 추가
                 if (weightedScore > highestWeightedScore)
                 {
                     highestWeightedScore = weightedScore;
                     bestActions.Clear();
                     bestActions.Add(action);
                 }
                 else if (weightedScore == highestWeightedScore)
                 {
                     bestActions.Add(action);
                 }
             }

             // 최고 점수 액션 선택 (동점 시 무작위)
             if (bestActions.Count > 0)
             {
                 if (bestActions.Count == 1)
                 {
                     bestAction = bestActions[0];
                 }
                 else
                 {
                     // 동점 액션 중 무작위 선택
                     int randomIndex = Random.Range(0, bestActions.Count); // UnityEngine.Random 사용
                     bestAction = bestActions[randomIndex];
                 }
             }

             return bestAction;
        }
        // +++ 가중치 적용 메서드 추가 끝 +++

        // 동적 액션 생성 메서드
        private List<IUtilityAction> GeneratePossibleActions(ArmoredFrame activeUnit, CombatContext context, SpecializationType pilotSpec)
        {
            var actions = new List<IUtilityAction>();
            var enemies = context.GetEnemies(activeUnit); // CombatContext에 GetEnemies 필요

            // --- 공격 액션 생성 --- (Pass specialization)
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.IsOperational) continue;

                    foreach (var weapon in activeUnit.GetAllWeapons().Where(w => w.IsOperational && w.HasAmmo() && !w.IsReloading))
                    {
                        // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
                        actions.Add(new AttackUtilityAction(enemy, weapon, pilotSpec));
                    }
                }
            }

            // --- 이동 액션 생성 로직 추가 --- (Pass specialization)
            ArmoredFrame closestEnemy = FindClosestEnemy(activeUnit, enemies);
            if (closestEnemy != null)
            {
                // 적 방향으로 이동력의 절반만큼 이동하는 지점 계산 (단순화된 방식)
                Vector3 directionToEnemy = (closestEnemy.Position - activeUnit.Position).normalized;
                // TODO: 실제 이동력(AP 소모 고려) 또는 원하는 교전 거리 등을 반영하여 이동 거리 계산 필요
                float desiredMoveDistance = activeUnit.CombinedStats.Speed * 0.5f;
                Vector3 targetPosition = activeUnit.Position + directionToEnemy * desiredMoveDistance;

                // <<< 추가 로그 >>>
                _textLogger?.TextLogger?.Log($"[Move Action Gen] Actor: {activeUnit.Name}, ClosestEnemy: {closestEnemy.Name}, Direction: {directionToEnemy}, DesiredDist: {desiredMoveDistance:F1}, TargetPos: {targetPosition}", LogLevel.Debug);
                // <<< 로그 끝 >>>

                // TODO: 실제 이동 가능한 위치인지 검증하는 로직 필요 (경로 탐색 등)

                // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
                actions.Add(new MoveUtilityAction(targetPosition, pilotSpec, closestEnemy));
            }
            // --- 이동 액션 생성 로직 끝 ---

            // --- 방어 액션 생성 로직 추가 --- (Pass specialization)
            // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
            actions.Add(new DefendUtilityAction(pilotSpec));
            // --- 방어 액션 생성 로직 끝 ---

            // --- 재장전 액션 생성 로직 추가 --- (Pass specialization)
            foreach (var weapon in activeUnit.GetAllWeapons())
            {
                // 작동 중이고, 탄약이 최대치가 아니며, 현재 재장전 중이 아닌 경우 재장전 액션 생성
                if (weapon.IsOperational && weapon.CurrentAmmo < weapon.MaxAmmo && !weapon.IsReloading)
                {
                    // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
                    actions.Add(new ReloadUtilityAction(weapon, pilotSpec));
                }
            }
            // --- 재장전 액션 생성 로직 끝 ---

            // --- 수리 액션 생성 로직 추가 --- (Pass specialization)
            var allies = context.GetAllies(activeUnit); // CombatContext에 GetEnemies처럼 GetAllies 필요

            // 자신 수리 액션 추가 (손상되었을 경우에만)
            if (activeUnit.TotalCurrentDurability < activeUnit.TotalMaxDurability)
            {
                // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
                actions.Add(new RepairUtilityAction(activeUnit, pilotSpec)); // Self repair
            }

            if (allies != null)
            {
                foreach (var ally in allies)
                {
                    // 살아있고, 손상되었으며, 자신이 아닌 아군
                    if (ally != null && ally.IsOperational && ally != activeUnit && 
                        ally.TotalCurrentDurability < ally.TotalMaxDurability) // <<< 손상 여부 확인 추가
                    {
                        // <<< Pass pilotSpec to constructor (Anticipating Step 3.3) >>>
                        actions.Add(new RepairUtilityAction(ally, pilotSpec)); // Ally repair
                    }
                }
            }
            // --- 수리 액션 생성 로직 끝 ---

            return actions;
        }

        // 가장 가까운 적 찾는 도우미 메서드 (필요 시 별도 클래스로 분리 가능)
        private ArmoredFrame FindClosestEnemy(ArmoredFrame activeUnit, IEnumerable<ArmoredFrame> enemies)
        {
            if (enemies == null || !enemies.Any())
            {
                return null;
            }

            ArmoredFrame closest = null;
            float minDistanceSqr = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsOperational) continue;

                float distSqr = (enemy.Position - activeUnit.Position).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {                    minDistanceSqr = distSqr;
                    closest = enemy;
                }
            }
            return closest;
        }
    }
} 