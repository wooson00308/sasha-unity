# Task 1: 유틸리티 AI 아키텍처 설계

## 목표
- 유틸리티 AI 시스템의 핵심 인터페이스 및 기본 구조 정의
- 기존 전투 시스템과의 연동 방식 구상

## 작업 내용
- 유틸리티 AI 관련 스크립트 및 문서 디렉토리 생성:
    - `Assets/AF/Scripts/AI/UtilityAI/`
    - `Assets/AF/Scripts/AI/UtilityAI/Actions/`
    - `Assets/AF/Scripts/AI/UtilityAI/Considerations/`
    - `Assets/AF/Scripts/AI/UtilityAI/Selectors/`
    - `Docs/Task1_UtilityAI_Architecture/`
- 핵심 인터페이스 정의 및 수정:
    - `IUtilityAction.cs` (`CalculateUtility`, `Execute`에 `actor` 파라미터 추가)
    - `IConsideration.cs` (`CalculateScore`에 `actor` 파라미터 추가)
    - `IActionSelector.cs` (`SelectAction`에 `actor` 파라미터 추가)
- 기반 클래스 및 예시 클래스 생성:
    - `UtilityActionBase.cs`: 액션 공통 로직 추상 클래스
    - `Actions/AttackUtilityAction.cs`: 공격 액션 예시 (기본 틀 + `TargetDistanceConsideration` 추가, 기타 필수 Consideration 추가 및 점수 계산 로직 구체화)
    - `Considerations/TargetDistanceConsideration.cs`: 거리 평가 예시 (선형 감쇠 로직 구체화)
    - `Considerations/TargetIsEnemyConsideration.cs`: 적군 여부 확인
    - `Considerations/TargetIsOperationalConsideration.cs`: 대상 작동 여부 확인
    - `Considerations/WeaponHasAmmoConsideration.cs`: 무기 탄약 확인
    - `Considerations/WeaponIsOperationalConsideration.cs`: 무기 사용 가능 여부 확인
    - `Selectors/HighestScoreSelector.cs` (이제 `actor` 파라미터 사용하여 실제 점수 계산)

## 결정 사항
- 액션 점수 계산 시 필요한 행동 주체(`ArmoredFrame actor`) 정보는 `IActionSelector.SelectAction` 메서드에 파라미터로 직접 전달하는 방식으로 결정함.

## 유틸리티 AI 통합 계획 (`IPilotBehaviorStrategy` 연동)

1.  **새로운 전략 클래스 생성:**
    *   기존 `IPilotBehaviorStrategy` 인터페이스를 구현하는 `UtilityAIPilotBehaviorStrategy` 클래스를 새로 생성하여 유틸리티 AI 로직을 캡슐화한다. 이는 기존 전략(`StandardCombatBehaviorStrategy` 등)과의 명확한 분리를 위함이다.
2.  **Action/Consideration 관리:**
    *   각 파일럿 타입 또는 개별 파일럿이 사용할 `IUtilityAction` 목록과 각 액션에 필요한 `IConsideration` 목록을 관리할 방법을 결정한다. (예: ScriptableObject 활용, 팩토리 패턴 등). 우선은 전략 클래스 내에서 하드코딩 또는 간단한 초기화 로직으로 시작할 수 있다.
3.  **액션 선택 로직 (`IActionSelector` 사용):**
    *   `UtilityAIPilotBehaviorStrategy`의 `DecideNextAction` 메서드 내에서 `IActionSelector` (예: `HighestScoreSelector`)를 사용하여 현재 사용 가능한 모든 `IUtilityAction`의 점수를 계산하고, 가장 높은 점수를 받은 액션을 최종 행동으로 선택한다.
4.  **컨텍스트 활용 (`CombatContext`):**
    *   `IConsideration`의 `CalculateScore` 메서드에 전달되는 `CombatContext`를 적극 활용하여, 각 고려사항이 판단에 필요한 게임 상태 정보(적 위치, 아군 상태, 지도 정보 등)에 접근할 수 있도록 한다.

## 다음 단계
- `TargetDistanceConsideration` 등의 점수 계산 로직에 실제 반응 곡선(Response Curve) 적용 고려.
- `AttackUtilityAction`에 필요한 나머지 Consideration 클래스들(`TargetIsEnemy`, `WeaponHasAmmo` 등) 생성 및 구현.
- 더 많은 Action 클래스 설계 및 구현 (`MoveUtilityAction`, `DefendUtilityAction` 등).
- `UtilityAIPilotBehaviorStrategy` 기본 틀 생성 및 `DecideNextAction` 메서드에 통합 로직 초안 구현.
- 기존 `IPilotBehaviorStrategy`의 `DetermineAction` 메서드 내에서 유틸리티 시스템 통합 설계 구체화.
