# Task 4: MVP 액션 및 고려사항 구현

## 목표
- 공격 외 핵심 게임 액션(이동, 방어, 재장전, 수리)에 대한 `IUtilityAction` 구현체 생성.
- 각 액션의 유틸리티 점수 계산에 필요한 기본적인 고려사항(Consideration) 구현 및 연결.
- `UtilityAIPilotBehaviorStrategy`에서 새로운 액션들을 동적으로 생성하도록 확장.

## 작업 내용
1.  **이동 (Move) 액션:**
    *   `MoveUtilityAction.cs` 클래스 생성 ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Actions/MoveUtilityAction.cs)).
    *   `GeneratePossibleActions`에 생성 로직 추가 (가장 가까운 적 방향). ([링크](mdc:Assets/AF/Scripts/AI/PilotBehavior/UtilityAIPilotBehaviorStrategy.cs))
    *   관련 Consideration 생성 및 추가:
        *   `TargetPositionSafetyConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/TargetPositionSafetyConsideration.cs)): 목표 지점 안전성.
        *   `DistanceToEnemyConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/DistanceToEnemyConsideration.cs)): 목표 지점과 적과의 거리.
        *   `ActionPointCostConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/ActionPointCostConsideration.cs)): AP 소모량 (임시값 3f 사용).
    *   `CalculateUtility` 메서드 업데이트.
2.  **방어 (Defend) 액션:**
    *   `DefendUtilityAction.cs` 클래스 생성 ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Actions/DefendUtilityAction.cs)).
    *   `GeneratePossibleActions`에 생성 로직 추가.
    *   관련 Consideration 생성 및 추가:
        *   `IncomingThreatConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/IncomingThreatConsideration.cs)): 주변 위협 수준.
        *   `SelfHealthConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/SelfHealthConsideration.cs)): 자신의 체력.
        *   `ActionPointCostConsideration`: AP 소모량 (`DEFEND_AP_COST` 값 1f 사용).
    *   `CalculateUtility` 메서드 업데이트.
3.  **재장전 (Reload) 액션:**
    *   `ReloadUtilityAction.cs` 클래스 생성 ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Actions/ReloadUtilityAction.cs)).
    *   `GeneratePossibleActions`에 생성 로직 추가.
    *   관련 Consideration 추가:
        *   `AmmoLevelConsideration` (기존 것 활용, `invert=true`).
        *   `ActionPointCostConsideration`: AP 소모량 (`Weapon.ReloadAPCost` 사용).
    *   `CalculateUtility` 메서드 업데이트.
4.  **수리 (Repair) 액션:**
    *   `RepairUtilityAction.cs` 클래스 생성 ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Actions/RepairUtilityAction.cs)).
    *   `GeneratePossibleActions`에 생성 로직 추가 (`CombatContext`에 `GetAllies` 추가).
    *   `CombatActionEvents.ActionType` enum에 `Repair` 추가 및 `RepairUtilityAction` 수정.
    *   관련 Consideration 생성 및 추가:
        *   `TargetDamagedConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/TargetDamagedConsideration.cs)): 대상 손상 여부 (Blocking).
        *   `TargetHealthConsideration` (기존 것 활용, `invert=true`).
        *   `IsAllyOrSelfConsideration` ([링크](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/IsAllyOrSelfConsideration.cs)): 대상 아군/자신 여부 (Blocking).
        *   `ActionPointCostConsideration`: AP 소모량 (고정값 2.0f 사용 결정).
    *   `CalculateUtility` 메서드 업데이트.
5.  **기타:**
    *   작업 과정에서 발생한 각종 린터 오류 수정 (스탯 접근, enum 멤버 누락, struct 람다 캡처, `using` 누락 등).
    *   리컴파일을 통해 타입 인식 문제 해결.

## 다음 단계 (Task 4 완료 시점)
- Task 5: 디버깅 및 로깅 시스템 구현.
- (추가) 각 액션의 Consideration 상세 구현 및 튜닝 (반응 곡선 조정, 추가 요소 반영 등).
- (추가) `CalculateUtility`의 점수 계산 방식 개선 고려 (단순 곱셈 외 가중 평균 등). 