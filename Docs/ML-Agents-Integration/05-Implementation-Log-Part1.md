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

## 다음 단계 구상

*   **보상 로직 구체화:** 전투 이벤트 구독하여 실제 결과 기반 보상 설정.
*   **`CombatSimulatorService` 비동기 처리 검토/수정:** AI 행동 결정 타이밍 문제 해결.
*   학습 환경 구성 및 실제 학습 진행. 