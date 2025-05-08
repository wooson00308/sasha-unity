# 리팩토링: 콜사인 기반 식별 시스템 도입

## 1. 목표

-   플레이어 스쿼드 유닛 식별의 일관성을 확보한다.
-   `CombatTestRunner`에서 플레이어 유닛의 `ArmoredFrame.Name`과 이를 참조하는 시스템(예: `CombatRadarUIService`) 간의 불일치로 발생하는 버그를 해결한다.
-   향후 고유 식별자로서 콜사인의 역할을 명확히 하고, 코드 전체의 데이터 흐름을 개선한다.

## 2. 주요 변경 사항

### 2.1. `CombatTestRunner.cs` 핵심 변경

-   **`AFSetup` 클래스 수정:**
    -   `public string callsign;` 필드를 추가한다. 이 필드는 Unity 인스펙터에서 플레이어 유닛의 고유한 콜사인(한/영, 중복 불가)을 직접 입력받는다.
    -   기존 `persistentId` 필드는 **제거**한다. 콜사인으로 모든 식별자 역할을 통합한다.
    -   `UpdatePreviews()` 메서드 내 `persistentId` 관련 로직을 모두 제거한다.

-   **`StartCombatTestAsync()` 메서드 내 플레이어 스쿼드 유닛 처리 로직 변경:**
    -   `playerSquadSetups` 리스트의 각 `AFSetup` 인스턴스를 처리할 때:
        -   `string.IsNullOrEmpty(playerSetup.callsign)`을 확인하여, 콜사인이 비어있으면 오류 로그를 기록하고 해당 유닛 처리를 건너뛴다.
        -   플레이어 유닛의 상태를 저장하는 `_persistentPlayerFrames` 딕셔너리의 키로 `playerSetup.callsign`을 사용한다.
        -   `CreateCustomArmoredFrame(AFSetup setup, ...)` 또는 `CreateTestArmoredFrame(string callsignForName, ...)` 호출 시, `ArmoredFrame` 생성자의 `name` 파라미터로 **`playerSetup.callsign` 값을 명시적으로 전달**한다.
            -   `CreateTestArmoredFrame` 메서드 시그니처 변경: `CreateTestArmoredFrame(string callsignForName, string assemblyId, int teamId, Vector3 position)` 와 같이 `callsignForName`을 첫 번째 인자로 받도록 수정하여, `ArmoredFrame`의 `Name`으로 사용한다. `assemblyId`는 여전히 `AssemblySO`를 찾는 데 사용된다.
            -   `CreateCustomArmoredFrame` 메서드 시그니처 변경: `CreateCustomArmoredFrame(AFSetup setup, ...)` 내부에서 `ArmoredFrame` 생성 시 `setup.callsign`을 `Name`으로 사용한다. `setup.customAFName`은 이제 단순 표시용 또는 부가 정보로만 활용될 수 있다.

-   **기타 메서드 변경:**
    -   `GetPlayerSquadUnitNames()`: `playerSquadSetups` 리스트에서 `setup.callsign` (null 또는 빈 문자열이 아닌 경우)을 수집하여 `HashSet<string>`으로 반환하도록 수정한다.
    -   `ValidateSetup()`: 기존 `persistentId` 중복 검사 대신 `callsign` 중복 검사를 수행하도록 수정한다.
    -   `CanStartCombat()`: `persistentId` 관련 유효성 검사 대신, 플레이어 스쿼드 `AFSetup`의 `callsign`이 비어 있는지 여부를 검사하도록 수정한다.

### 2.2. `ArmoredFrame.cs` 생성자 활용

-   `ArmoredFrame(string name, Frame frameBase, Vector3 initialPosition, int teamId)` 생성자는 이미 `name` 파라미터를 받고 있으므로, `CombatTestRunner`에서 플레이어 유닛 생성 시 이 `name` 파라미터에 `AFSetup.callsign` 값을 전달하면 `ArmoredFrame.Name`이 콜사인으로 설정된다.

### 2.3. `CombatRadarUIService.cs` 영향

-   `playerSquadNames` 변수는 `CombatTestRunner.Instance.GetPlayerSquadUnitNames()`를 통해 이미 콜사인 목록을 받게 된다.
-   `ev.ActiveUnitName`은 `CombatLogPlaybackUpdateEvent`를 통해 전달받으며, 이 이름은 실제 `ArmoredFrame.Name`이다.
-   플레이어 스쿼드 유닛의 경우, `ArmoredFrame.Name`이 해당 유닛의 콜사인으로 설정되었으므로, `playerSquadNames.Contains(activeUnitName)` 비교 로직이 의도대로 정확하게 작동하여 플레이어 유닛을 올바르게 식별한다.
-   결과적으로 "Last Known Player Pos: null" 문제가 해결될 것으로 기대된다.

### 2.4. 기타 시스템 영향 최소화

-   적군 유닛이나 플레이어 스쿼드에 속하지 않는 기타 시나리오 유닛 (`afSetups` 리스트)의 경우, `ArmoredFrame.Name` 생성 방식은 기존과 동일하게 유지한다 (예: `AssemblySO.AFName` 또는 `AFSetup.customAFName` 사용).
-   이를 통해 로그 시스템, 이벤트 시스템 등 기존에 `ArmoredFrame.Name`을 참조하던 다른 부분들은 플레이어 유닛의 경우 일관된 콜사인 값을, 그 외 유닛은 기존 방식의 이름을 참조하게 되어 변경의 영향을 최소화한다.

## 3. 용어 정리

-   **`Callsign` (콜사인):**
    -   `AFSetup` 클래스에 새로 추가되는 필드.
    -   플레이어가 각 플레이어 스쿼드 유닛에게 부여하는 고유한 닉네임/호출부호 (한/영 무관, 중복 불가 권장).
    -   플레이어 스쿼드 유닛을 식별하고 상태를 유지하는 핵심 키 값.
    -   플레이어 스쿼드 유닛의 경우, `ArmoredFrame` 인스턴스의 `Name` 속성값으로 설정된다.
-   **`AFName` (기체 명칭):**
    -   `AssemblySO`의 필드 또는 `AFSetup`의 `customAFName` 필드에 해당.
    -   특정 파츠 조합으로 구성된 기체 템플릿의 종류를 나타내는 이름 (예: "경량형 저격기체 TYPE-A").
    -   플레이어 유닛이 아닌 경우, 또는 플레이어 유닛이더라도 콜사인이 `ArmoredFrame.Name`으로 사용되기 전의 기본 이름으로 활용될 수 있다.

## 4. 기대 효과

-   플레이어 스쿼드 유닛의 식별 방식이 콜사인으로 통일되어 명확해진다.
-   `CombatRadarUIService`가 플레이어 유닛 및 마지막 플레이어 유닛 위치를 정확하게 추적하여 레이더 표시가 의도대로 작동한다.
-   `CombatTestRunner` 내 플레이어 유닛 상태 관리 로직(`_persistentPlayerFrames` 등)이 콜사인 기준으로 일관성 있게 작동한다.
-   코드 가독성 및 유지보수성이 향상된다. 