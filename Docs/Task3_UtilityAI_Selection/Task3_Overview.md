# Task 3: 행동 선택 로직 개발

## 목표
- 계산된 유틸리티 점수를 바탕으로 최적의 행동을 선택하는 로직 구현 및 개선.
- 행동 선택 컴포넌트(`IActionSelector`)의 유연성 및 확장성 확보.

## 현재까지 진행 내용 (2025-05-04)
1.  **`HighestScoreSelector` 개선:** ([`HighestScoreSelector.cs`](mdc:Assets/AF/Scripts/AI/UtilityAI/Selectors/HighestScoreSelector.cs))
    *   **최소 유틸리티 임계값 추가:** `MinUtilityThreshold` 상수(현재 0.1f)를 정의하여, 계산된 최고 점수가 이 값 미만일 경우 어떤 행동도 선택하지 않도록 함 (null 반환).
    *   **동점 처리 로직 추가:** 여러 액션이 동일한 최고 점수를 가질 경우, 해당 액션들을 리스트에 모아 `System.Random`을 사용하여 무작위로 하나를 선택하도록 수정.
    *   `System.Random` 사용 시 발생한 `UnityEngine.Random`과의 모호성 오류 해결 (명시적 타입 지정).
2.  **`IActionSelector` 주입 방식 변경:** ([`UtilityAIPilotBehaviorStrategy.cs`](mdc:Assets/AF/Scripts/AI/PilotBehavior/UtilityAIPilotBehaviorStrategy.cs))
    *   `UtilityAIPilotBehaviorStrategy`의 생성자를 수정하여 외부에서 `IActionSelector` 인터페이스 구현체를 주입받을 수 있도록 변경.
    *   주입된 Selector가 없을 경우(`null`)를 대비하여 기본적으로 `HighestScoreSelector`를 사용하도록 함.
    *   기존의 기본 생성자는 그대로 유지하여 `HighestScoreSelector`를 사용.

## 다음 단계
- Task 4와 연계하여 `UtilityAIPilotBehaviorStrategy`의 `GeneratePossibleActions` 메서드에 공격 외 다른 행동들(이동, 방어, 재장전 등)의 `IUtilityAction` 생성 로직 추가.
- 필요에 따라 새로운 `IActionSelector` 구현체 개발 고려 (예: 특정 상황 우선순위 부여 Selector). 