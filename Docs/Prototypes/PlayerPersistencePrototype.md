# 프로토타입: CombatTestRunner 플레이어 상태 유지

## 1. 목표

현재 `CombatTestRunner`는 모든 전투 참가자를 일회성 테스트 데이터로 취급합니다. 이 프로토타입의 목표는 `CombatTestRunner` 내에서 **플레이어 소유의 유닛(ArmoredFrame 및 Pilot) 상태가 여러 전투 시뮬레이션에 걸쳐 유지**되도록 수정하여, 기초적인 게임 진행 루프와 성장 요소를 테스트할 수 있는 환경을 구축하는 것입니다.

## 2. 변경 사항

### 2.1. CombatTestRunner 스크립트 수정 (`Assets/AF/Scripts/Tests/CombatTestRunner.cs`)

*   **플레이어 스쿼드 설정 분리:**
    *   기존 `afSetups` 리스트 필드의 이름을 `enemySetups` 또는 유사한 이름으로 변경하여, **적군 및 임시 유닛 설정** 전용임을 명확히 합니다. (Odin Inspector의 `[TitleGroup]`, `[ListDrawerSettings]` 등 속성 업데이트 필요)
    *   플레이어 스쿼드를 설정하기 위한 **새로운 리스트 필드를 추가**합니다. (예: `public List<AFSetup> playerSquadSetups;`) 이 리스트 역시 `AFSetup` 타입을 사용하며, 플레이어가 소유하고 상태가 유지될 유닛들을 정의합니다. Odin Inspector 속성을 사용하여 인스펙터에서 명확히 구분되도록 설정합니다.

*   **플레이어 유닛 상태 저장소 추가:**
    *   플레이 모드가 유지되는 동안 플레이어 유닛의 `ArmoredFrame` 인스턴스를 저장할 **private 멤버 변수**를 추가합니다. 딕셔너리 형태가 관리하기 용이할 수 있습니다.
      ```csharp
      // 예시: Key는 playerSquadSetups 리스트의 인덱스나 고유 ID가 될 수 있음
      private Dictionary<string, ArmoredFrame> _persistentPlayerFrames = new Dictionary<string, ArmoredFrame>();
      ```

*   **전투 시작 로직 수정 (`StartCombatTestAsync` 메서드 내부):**
    *   메서드가 호출되면, 최종 전투 참가자 리스트(`List<ArmoredFrame> allParticipants`)를 새로 생성합니다.
    *   **플레이어 스쿼드 처리:**
        *   `playerSquadSetups` 리스트를 순회합니다.
        *   각 `playerSetup`에 대해 고유 식별자(예: 리스트 인덱스 또는 `customAFName` 등)를 사용하여 `_persistentPlayerFrames` 딕셔너리를 조회합니다.
        *   **저장된 인스턴스가 있으면:** 해당 `ArmoredFrame` 인스턴스를 `allParticipants` 리스트에 추가합니다. 이 인스턴스의 위치(`transform.position`)는 현재 `playerSetup.startPosition` 값으로 업데이트합니다. (상태는 이전 전투에서 이어짐)
        *   **저장된 인스턴스가 없으면 (첫 실행):** `playerSetup` 설정을 기반으로 **새로운 `ArmoredFrame` 인스턴스를 생성**합니다 (`CreateCustomArmoredFrame` 또는 유사 메서드 활용). 생성된 인스턴스를 `_persistentPlayerFrames` 딕셔너리에 **추가**하고, `allParticipants` 리스트에도 추가합니다.
    *   **적군/임시 유닛 처리:**
        *   `enemySetups` (기존 `afSetups`) 리스트를 순회합니다.
        *   각 `enemySetup`에 대해 **항상 새로운 `ArmoredFrame` 인스턴스를 생성**합니다 (`CreateTestArmoredFrame` 또는 `CreateCustomArmoredFrame` 활용).
        *   생성된 인스턴스를 `allParticipants` 리스트에 추가합니다.
    *   **전투 개시:** 최종적으로 구성된 `allParticipants` 리스트를 `combatSimulator.StartCombat()` 메서드에 전달합니다.

*   **(선택적) 리셋 기능:**
    *   `_persistentPlayerFrames` 딕셔너리를 비우고 플레이어 상태를 초기화하는 버튼/메서드를 추가하면 테스트 시 유용할 수 있습니다. (예: `[Button("Reset Player Squad State")]`)

### 2.2. 팀 ID 활용

*   `CombatSimulatorService`는 기존과 동일하게 각 `ArmoredFrame`의 `TeamId` 속성을 사용하여 전투 중 아군/적군을 판별합니다.
*   `playerSquadSetups`의 각 항목에는 플레이어 팀 ID (예: 0)를 설정하고, `enemySetups`에는 다른 팀 ID를 설정하여 전투가 올바르게 진행되도록 합니다.

## 3. 기대 효과

*   `CombatTestRunner`를 통해 여러 번 전투를 실행해도 `playerSquadSetups`에 정의된 플레이어 유닛들은 이전 전투에서 입은 **파츠 데미지 등의 상태를 유지**한 채 다음 전투에 참가하게 됩니다.
*   파일럿 경험치 누적 등 더 복잡한 상태 유지는 다음 단계로 미루더라도, 가장 기본적인 '상태 지속성'을 프로토타이핑하여 실제 게임 루프에 가까운 테스트 환경을 제공합니다.
*   적 유닛은 매 전투마다 초기 설정대로 생성되므로 다양한 전투 시나리오 테스트가 가능합니다.

## 4. 제외되는 범위

*   UI 변경 없음 (`CombatTestRunner`의 인스펙터만 수정).
*   게임 플레이 상태의 디스크 저장/로드 기능 없음 (플레이 모드 중에만 유지됨).
*   파일럿 경험치, 스킬 습득 등 복잡한 성장 시스템 연동은 포함하지 않음 (추후 확장). 