# 4. 구현 계획 개요

ML-Agents 파일럿 AI 통합을 위한 주요 개발 단계.

1.  **기본 환경 설정 (Setup):**
    *   Unity ML-Agents 패키지 설치 (`com.unity.ml-agents`).
    *   Python 환경 및 `mlagents` 패키지 설치.
    *   필요한 Unity 프로젝트 설정 확인.

2.  **에이전트 스크립트 구현 (`PilotAgent.cs`):**
    *   `Agent` 클래스 상속.
    *   `CollectObservations()` 구현 (설계 초안 기반).
    *   `OnActionReceived()` 구현 (행동 변환 로직).
    *   `Heuristic()` 구현 (테스트용).
    *   `EndEpisode()`, `AddReward()`, `SetReward()` 등 구현.

3.  **기존 시스템 통합 (Integration):**
    *   `MLAgentBehaviorStrategy.cs` 구현 (`IPilotBehaviorStrategy` 상속).
        *   내부 `PilotAgent` 컴포넌트와 연결.
        *   `DetermineAction()`: `PilotAgent`로부터 행동 결정 요청 및 결과 반환.
        *   에이전트의 주 행동 결정을 바탕으로 세부 목표(타겟, 위치) 설정 로직 추가.
    *   `CombatSimulatorService` 수정:
        *   파일럿 정보에 따라 `MLAgentBehaviorStrategy` 할당 로직 추가.
        *   학습된 모델(`.onnx`) 로드 및 연결 로직 추가.
        *   ML-Agents 스텝(관측-결정-행동-보상) 연동 로직 (학습 환경 구성 시 중요).
    *   `CombatContext` 등 필요 데이터 구조체에 에이전트 관측용 데이터 접근/추출 메서드 추가 고려.

4.  **학습 환경 구성:**
    *   전투 씬을 ML-Agents 학습 환경으로 설정.
    *   여러 에이전트(파일럿) 동시 학습 환경 구성 (Self-Play 또는 대전 상대 필요).
    *   에피소드 시작/종료 조건 명확화 (전투 시작/종료).

5.  **학습 (Training):**
    *   학습 설정 파일 (`config.yaml`) 작성 (하이퍼파라미터, 보상, 네트워크 구조 등).
    *   `mlagents-learn` 명령어로 학습 실행.
    *   TensorBoard로 학습 과정 모니터링 및 분석.
    *   학습된 모델 (`.onnx`) 저장.

6.  **추론 및 평가 (Inference & Evaluation):**
    *   학습된 모델을 Unity 프로젝트에 임포트.
    *   `PilotAgent`의 Behavior Type을 `Inference Only`로 설정.
    *   게임 내에서 AI 성능 테스트 및 평가 (다양한 상대, 맵, 상황).
    *   결과 피드백 -> 설계/파라미터 수정 -> 재학습 반복. 