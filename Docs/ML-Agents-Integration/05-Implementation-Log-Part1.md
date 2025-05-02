# 5. 구현 기록 (Part 1)

ML-Agents 파일럿 AI 구현 초기 단계 기록.

1.  **패키지 설치 및 환경 설정 (완료)**
    *   `Packages/manifest.json` 파일에 `"com.unity.ml-agents": "3.0.0"` 추가하여 Unity 패키지 설치 완료.
    *   `pip install mlagents torch` 명령어를 통해 Python 환경에 `mlagents` (v0.28.0 확인) 및 관련 의존성(PyTorch 등) 설치 완료.
    *   Unity 프로젝트 설정(`Edit > Project Settings > ML-Agents`) 확인 결과, 당장 수정 필요한 필수 전역 설정은 없는 것으로 판단.

2.  **`PilotAgent.cs` 기본 구현 (완료)**
    *   `Assets/AF/Scripts/Combat/Agents/` 경로에 `PilotAgent.cs` 파일 생성.
    *   `Unity.MLAgents.Agent` 클래스를 상속받음.
    *   주요 메서드(`Awake`, `Start`, `Initialize`, `OnEpisodeBegin`, `CollectObservations`, `OnActionReceived`, `Heuristic`) 오버라이드 및 기본 구조 작성.
    *   `Heuristic()` 메서드는 플레이어 개입이 없는 게임 특성상 불필요하여 내용 제거 (테스트/디버깅 목적 외 사용 금지 명시).

3.  **구조 문제 해결: POCO <-> MonoBehaviour 연동 (완료)**
    *   핵심 문제: `PilotAgent`(MonoBehaviour)와 `ArmoredFrame`(POCO) 인스턴스 간의 연결 부재.
    *   해결 방안: `CombatTestRunner` 중심 구조 채택.
        *   `ArmoredFrame.cs`: `public PilotAgent AgentComponent { get; set; }` 프로퍼티 추가.
        *   `PilotAgent.cs`: `Awake()`에서 `GetComponent<ArmoredFrame>()` 제거, 외부 참조 설정을 위한 `SetArmoredFrameReference()` 유지.
        *   `CombatTestRunner.cs` (`CreateTestArmoredFrame`, `CreateCustomArmoredFrame`):
            *   `ArmoredFrame` POCO 생성 후, 해당 에이전트를 호스팅할 `GameObject`(_AgentHost) 생성.
            *   생성된 `GameObject`에 `PilotAgent` 컴포넌트 추가.
            *   `GameObject`를 `CombatTestRunner`의 자식으로 설정.
            *   `af.AgentComponent = pilotAgentComponent;` 와 `pilotAgentComponent.SetArmoredFrameReference(af);` 를 통해 양방향 참조 설정.
        *   `MLAgentBehaviorStrategy.cs`: `DetermineAction` 내부에서 `unit.AgentComponent`를 통해 `PilotAgent` 참조를 가져오도록 수정. `ICombatSimulatorService` 파라미터를 `CombatSimulatorService`로 변경하여 인터페이스 불일치 해결.

4.  **관측 (`CollectObservations`) 구현 (완료)**
    *   `OBSERVATION_SIZE` 상수를 12로 정의.
    *   관측 로직 구현 완료:
        1.  자신 상태 (HP 비율, AP 비율) - 2개
        2.  가장 가까운 적 상태 (거리 정규화, 방향 X, 방향 Z, HP 비율) - 4개 (없으면 기본값)
        3.  가장 가까운 *손상된* 아군 상태 (거리 정규화, 방향 X, 방향 Z, HP 비율) - 4개 (없으면 기본값)
        4.  적 교전 가능성 (사거리 내 사용 가능 무기 비율) - 1개 (적 없으면 0)
        5.  자신의 취약성 (가장 낮은 파츠 내구도 비율) - 1개 (파츠 없으면 1)
    *   관련 헬퍼 메서드 (`FindClosestUnit`, `AddUnitObservations`, `GetDurabilityRatio`) 구현.

5.  **행동 수신 (`OnActionReceived`) 기본 구현 (완료)**
    *   ML-Agents로부터 받은 이산 행동 인덱스(`actions.DiscreteActions[0]`)를 `CombatActionEvents.ActionType`으로 변환. (유효하지 않은 인덱스 예외 처리 포함)
    *   `chosenActionType`에 따라 기본적인 타겟/무기/위치 선정 로직 구현:
        *   Attack: 가장 가까운 적 대상, 사용 가능한 첫 무기 사용.
        *   Move: 가장 가까운 적 위치로 이동 시도.
        *   Defend: 대상 없음.
        *   Reload: 재장전 필요한 첫 무기.
        *   RepairAlly: 가장 가까운 손상된 아군 대상.
        *   RepairSelf: 자신 대상.
        *   None: 대기 (행동한 것으로 처리).
    *   결정된 파라미터로 `_combatSimulator.PerformAction()` 호출하여 실제 행동 실행 요청.
    *   임시 보상 로직 추가 (행동 시도/실패 시 작은 패널티).

6.  **이벤트 기반 보상 로직 구현 (완료)**
    *   **이벤트 식별:** `CombatActionEvents.cs`, `CombatSessionEvents.cs`, `DamageEvents.cs` 파일 분석하여 보상 로직에 필요한 이벤트(`DamageAppliedEvent`, `RepairAppliedEvent`, `CombatEndEvent`, `DamageAvoidedEvent`, `WeaponFiredEvent` 등) 식별. (`UnitDestroyedEvent`는 미사용 확인, `DamageAppliedEvent` 후 `IsUnitDefeated`로 처리).
    *   **이벤트 구독:** `PilotAgent.cs`의 `OnEpisodeBegin`에서 필요한 이벤트 구독, `OnDestroy`에서 구독 해제 로직 구현.
    *   **이벤트 핸들러:** 각 이벤트(`HandleDamageApplied`, `HandleRepairApplied` 등)를 처리하는 메서드 구현.
    *   **보상 로직:** 이벤트 핸들러 내에서 이벤트 데이터를 분석하여 `AddReward()` 호출. 데미지 가함/받음, 적 파괴, 수리, 회피, 빗나감, 비효율적 행동, 전투 승패 등에 따라 보상/패널티 부여 (`REWARD_*` 상수 사용).
    *   **임시 로직 제거:** `OnActionReceived` 내의 임시 보상/패널티 제거.
    *   **오류 수정:** 구현 과정 중 발생한 다수의 린터 오류 해결.
        *   `GetDurabilityRatio`, `AddUnitObservations` 메서드 중복 정의 오류: 파일 전체 검토 후 Helper Methods 섹션의 중복 메서드 제거.
        *   `Pilot.Name`, `ArmoredFrame.Name`, `ArmoredFrame.Position` 등 POCO 프로퍼티 참조 오류 수정.

7.  **비동기 턴 처리 구현 (UniTask) (완료)**
    *   **문제 인식:** ML-Agent의 `RequestDecision()` 호출과 `OnActionReceived()` 결과 수신 사이의 지연 시간으로 인해, 동기 방식의 `CombatSimulatorService.ProcessNextTurn()`에서 타이밍 문제 발생 가능성 확인.
    *   **UniTask 도입:** Coroutine 대신 UniTask를 사용하여 비동기 처리 구현 결정.
    *   **인터페이스/기반 클래스 수정:**
        *   `ICombatSimulatorService`: `ProcessNextTurn` -> `async UniTask ProcessNextTurnAsync(CancellationToken)`. (`MovedThisActivation` 프로퍼티, `HasUnitDefendedThisTurn` 메서드 추가).
        *   `IPilotBehaviorStrategy`: `DetermineAction` (제거 고려) -> `async UniTask<(...)> DetermineActionAsync(CombatContext, CancellationToken)`.
        *   `PilotBehaviorStrategyBase`: `abstract DetermineAction(CombatContext)` 유지, `virtual DetermineActionAsync` 추가 (기본 구현은 동기 호출 래핑).
    *   **`CombatSimulatorService` 수정:** `ProcessNextTurnAsync`, `DetermineActionForUnitAsync` 메서드를 `async`/`await` 사용하여 구현. `DetermineActionForUnitAsync` 내에서 `strategy.DetermineActionAsync` 호출.
    *   **`CombatContext` 수정:** `ICombatSimulatorService Simulator` 프로퍼티 추가 및 생성자 수정.
    *   **기존 전략 클래스 수정:** `DetermineAction` 파라미터를 `CombatContext`로 변경, 내부 로직에서 `context.Simulator` 사용하도록 수정.
    *   **`MLAgentBehaviorStrategy` 수정:** `PilotBehaviorStrategyBase` 상속, `DetermineActionAsync` 재정의하여 `PilotAgent.RequestDecisionAsync` 호출 및 `await`.
    *   **`PilotAgent` 수정:** `UniTaskCompletionSource`를 사용하여 비동기 대기 구현. `RequestDecisionAsync` 메서드 추가, `OnActionReceived`에서 `TrySetResult` 호출 및 행동 실행 로직 제거.
    *   **`CombatTestRunner` 수정:** `StartCombatTestAsync` 루프에서 `await combatSimulator.ProcessNextTurnAsync()` 호출.
    *   **리팩토링:** 파라미터 타입(`CombatContext` vs `ICombatSimulatorService`) 및 인터페이스 멤버 누락 관련 린터 오류 해결 위해 반복 수정 진행.

## 다음 단계 구상

*   **학습 환경 구성:** Unity 씬 설정, ML-Agents 컴포넌트(`DecisionRequester` 등) 구성, 학습 설정 파일(`.yaml`) 정의.
*   학습 환경 구성 및 실제 학습 진행. 