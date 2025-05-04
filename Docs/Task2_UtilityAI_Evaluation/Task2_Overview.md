# Task 2: 핵심 유틸리티 평가 시스템 구현

## 목표
- 다양한 게임 상황에 맞춰 유틸리티 점수를 유연하게 계산할 수 있는 평가 시스템 구축.
- 공격 행동(`AttackUtilityAction`)에 필요한 핵심 고려사항(Consideration) 구현.
- 평가에 필요한 전투 정보(`CombatContext`) 접근 방식 개선.

## 작업 내용
1.  **반응 곡선 시스템 구현:**
    *   다양한 형태의 점수 곡선을 정의하기 위한 `UtilityCurveType` enum 생성 ([`UtilityCurveType.cs`](mdc:Assets/AF/Scripts/AI/UtilityAI/UtilityCurveType.cs)). (Linear, Polynomial, Logistic, Logarithmic 등)
    *   주어진 입력 값과 곡선 타입에 따라 0~1 사이의 점수를 계산하는 `UtilityCurveEvaluator` 정적 클래스 구현 ([`UtilityCurveEvaluator.cs`](mdc:Assets/AF/Scripts/AI/UtilityAI/UtilityCurveEvaluator.cs)).
    *   기존 `TargetDistanceConsideration` 등이 `UtilityCurveEvaluator`를 사용하도록 수정.
2.  **핵심 Consideration 구현:**
    *   `AttackUtilityAction`의 유틸리티 점수 계산에 필요한 다양한 고려사항들을 구현하고, 반응 곡선 시스템을 적용함.
    *   구현된 Consideration 목록 ([Considerations 폴더](mdc:Assets/AF/Scripts/AI/UtilityAI/Considerations/)):
        *   `TargetHealthConsideration`: 대상의 현재 체력 비율 기반 점수 계산.
        *   `HitChanceConsideration`: 대상 및 무기를 고려한 예상 명중률 기반 점수 계산.
        *   `AmmoLevelConsideration`: 무기의 현재 탄약 잔량 비율 기반 점수 계산.
        *   `WeaponReloadingConsideration`: 무기가 현재 재장전 중인지 확인 (Blocking).
        *   (Task 1에서 구현된 Blocking 고려사항들 포함)
    *   Consideration 구현 중 필요한 경우 관련 모델(`ArmoredFrame.cs` 등)에 속성 추가 (`TotalCurrentDurability`, `TotalMaxDurability`).
3.  **CombatContext 접근 개선:**
    *   `UtilityAIPilotBehaviorStrategy` 및 각 Consideration에서 현재 전투 상황 정보(`CombatContext`)에 더 쉽게 접근할 수 있도록 `CombatSimulatorService` 수정.
    *   `CombatSimulatorService` 내부에 현재 `CombatContext`의 주요 정보를 복사하여 제공하는 `GetCurrentContext()` 메서드 추가 ([`CombatSimulatorService.cs`](mdc:Assets/AF/Scripts/Combat/CombatSimulatorService.cs)).
    *   `CombatContext` 자체에 필요한 편의 메서드 추가 (예: `GetEnemies(ArmoredFrame actor)`). ([`CombatContext.cs`](mdc:Assets/AF/Scripts/Combat/CombatContext.cs))
4.  **버그 수정 및 리팩토링:**
    *   Consideration 추가 및 시스템 연동 과정에서 발생한 타입 오류, Null 참조 오류 등 해결.
    *   Unity 에디터 재컴파일을 통한 타입 인식 문제 해결.

## 다음 단계 (Task 2 완료 시점)
- Task 3: 행동 선택 로직 개발 (`HighestScoreSelector` 개선 및 Selector 주입 방식 변경)
- Task 4: MVP 액션 및 고려사항 구현 (공격 외 이동, 방어 등 다른 액션과 그에 맞는 Consideration 추가) 