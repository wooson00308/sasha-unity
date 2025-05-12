# AI 행동 트리: 자가 수리 기능 개선 계획

**상태: 데이터 모델 수정 완료, Excel 데이터 수정 완료. 다음 단계: SO 재생성 및 행동 트리 노드 구현**

## 1. 목표

-   모든 파일럿(SpecializationType 무관)에게 기본적인 **자가 수리(Self-Repair)** 능력을 부여한다.
-   자가 수리 행동에 **횟수 제한** 시스템을 도입한다.
-   **지원(Support)** 파일럿은 기존처럼 자가 수리 및 **타인 수리(Repair Ally)** 능력을 모두 유지한다.

## 2. 핵심 변경 사항

### 2.1. 자가 수리 보편화
-   현재 `SupportBT` 위주로 구현된 자가 수리 로직(관련 Condition/Action 노드)을 모든 행동 트리(`BasicAttackBT`, `RangedCombatBT`, `MeleeCombatBT`, `DefenderBT` 등)의 적절한 우선순위에 추가한다.
    -   예: 체력이 일정 수준 이하일 때 (`IsHealthLowNode` 사용) 자가 수리를 시도하는 로직.

### 2.2. 수리 횟수 제한 시스템 도입
-   **데이터 모델 변경**: **[완료]**
    -   `Stats.cs` (`Assets/AF/Scripts/Models/Stats.cs`): **[완료]**
        -   `[SerializeField] private float _maxRepairUses = 3.0f;` 필드 추가.
        -   `public float MaxRepairUses => _maxRepairUses;` 프로퍼티 추가.
        -   생성자, 덧셈 연산자 (`+`), `ApplyModifier`, `Clear`, `Add`, `ToString` 메서드에 `_maxRepairUses` 처리 로직 추가.
    -   `StatType.cs` (`Assets/AF/Scripts/Models/StatType.cs`): **[완료]**
        -   `MaxRepairUses` 열거형 값 추가.
    -   `ArmoredFrame.cs` (`Assets/AF/Scripts/Models/ArmoredFrame.cs`): **[완료]**
        -   `private int _currentRepairUses;` 필드 추가.
        -   생성자 내 `RecalculateStats()` 호출 후, `_currentRepairUses = (int)_combinedStats.MaxRepairUses;` 로직 추가하여 초기화.
        -   `RecalculateStats()` 메서드 수정: `_combinedStats` 계산 시, 모든 장착된 파츠의 `MaxRepairUses` 스탯을 합산하여 기체의 최종 `MaxRepairUses`를 결정하도록 로직 수정.
        -   `DecrementRepairUses()` 메서드 추가 (수리 시 호출).
    -   `PartData.cs` (`Assets/ExcelToSO/Scripts/DataModels/PartData.cs`): **[완료]**
        -   `public float MaxRepairUses { get; private set; }` 속성 추가.
        -   `FromExcelRow` 메서드 수정: Excel 17번째 컬럼(인덱스 16)에서 `MaxRepairUses` 값을 읽도록 수정 (`GetFloatValue(row, 16)`).
    -   `PartSO.cs` (`Assets/AF/Data/ScriptableObjects/PartSO.cs`): **[완료]**
        -   `public float MaxRepairUses;` 필드 추가.
        -   `Apply(PartData data)` 메서드 수정: `MaxRepairUses = data.MaxRepairUses;` 매핑 로직 추가.
    -   `Part.cs` (`Assets/AF/Scripts/Models/Part.cs`): **[확인]**
        -   `PartStats` 프로퍼티 존재 확인 (기존에 있었음). `ArmoredFrame.cs`에서 `PartStats`를 사용하도록 수정 완료.

-   **행동 트리 노드 변경/추가**: **[진행 예정]**
    -   `HasRepairUsesNode.cs` (`Assets/AF/Scripts/AI/BehaviorTree/Conditions/HasRepairUsesNode.cs`) 조건 노드 신규 생성:
        -   `ArmoredFrame`의 `GetCurrentRepairUses()`를 확인하여 남은 수리 횟수가 0보다 큰지 검사하는 로직 추가.
    -   `RepairSelfNode.cs` (`Assets/AF/Scripts/AI/BehaviorTree/Actions/RepairSelfNode.cs`) 액션 노드 수정:
        -   액션 성공 시 `ArmoredFrame`의 `DecrementRepairUses()` 메서드를 호출하여 수리 횟수를 차감하는 로직 추가.
    -   `CanRepairAllyNode.cs` (필요시 생성 또는 수정) -> `HasRepairUsesNode`로 대체 또는 조합 가능성 검토.
    -   `RepairAllyNode.cs` (`Assets/AF/Scripts/AI/BehaviorTree/Actions/RepairAllyNode.cs`) 액션 노드 수정:
        -   액션 성공 시 `ArmoredFrame`의 `DecrementRepairUses()` 메서드를 호출하여 수리 횟수를 차감하는 로직 추가 (공통 횟수 사용).

-   **모든 Pilot BT 수정**: **[진행 예정]**
    -   모든 관련 PilotBT 파일 (`Assets/AF/Scripts/AI/BehaviorTree/PilotBTs/` 내 `.cs` 파일) 편집:
        -   자가 수리 로직 추가 (예: `Sequence(HasRepairUsesNode, IsHealthLowNode, HasEnoughAPNode, RepairSelfNode)`). 적절한 우선순위에 배치.
    -   `SupportBT.cs` 수정: 기존 타인 수리(`RepairAlly`) 시퀀스 시작 부분에 `HasRepairUsesNode` 조건 추가.

-   **전투 로직 변경**: (BT 노드에서 처리하므로 직접 수정 불필요할 것으로 예상)
    -   `CombatActionExecutor.cs` (`Assets/AF/Scripts/Combat/CombatActionExecutor.cs`):
        -   `Execute` 메서드 내 `ActionType.RepairSelf` 및 `ActionType.RepairAlly` 처리 로직에서, 행동 성공 시 `actor.DecrementRepairUses()`를 직접 호출하는 대신, 해당 로직이 **`RepairSelfNode`/`RepairAllyNode` 액션 노드 내에서 처리되도록** 수정/확인. (BT 노드가 직접 상태를 변경하도록).

-   **Excel 데이터 수정**: **[완료]**
    -   `AF_Data.xlsx` 파일 (`Assets/AF/Data/AF_Data.xlsx` 확인):
        -   `Parts` 시트에 `MaxRepairUses` 컬럼 추가 (Q열, 17번째 컬럼).
        -   각 파츠 데이터에 `MaxRepairUses` 값 `0`으로 기본 입력 완료. (추후 특정 파츠에 값 부여 필요)
    -   **SO 재생성**: **[진행 예정]** Excel 수정 후 `Assets > ExcelToSO > Generate ScriptableObjects` 메뉴를 통해 `PartSO` 에셋 재생성 필요 (사용자 직접 실행).

### 2.3. 지원 파일럿 특성 유지
-   `SupportBT`는 기존의 타인 수리(`RepairAlly`) 로직을 유지하되, 이제 타인 수리 시에도 공통 `_currentRepairUses`를 소모하고 확인하도록 `CanRepairAllyNode`, `RepairAllyNode`를 수정/사용한다.

## 3. 테스트 계획 **[진행 예정]**
-   유닛 테스트 추가:
    -   `Stats` 클래스의 `MaxRepairUses` 관련 연산 검증.
    -   `ArmoredFrame`의 `RecalculateStats`에서 Body 파츠 `MaxRepairUses` 반영 및 `_currentRepairUses` 초기화 검증.
    -   `ArmoredFrame.DecrementRepairUses()` 동작 검증.
-   통합 테스트 (CombatTestRunner 활용):
    -   수리 횟수가 없는 AF는 수리 액션(자가/타인)을 시도하지 않는지 검증.
    -   수리 액션 성공 시 `_currentRepairUses`가 정상적으로 차감되는지 검증.
    -   수리 횟수를 모두 소모한 AF는 더 이상 수리 액션을 수행하지 못하는지 검증.
    -   다른 타입의 파일럿(BasicAttack 등)도 조건 만족 시 자가 수리를 수행하는지 검증.
    -   Support 파일럿이 자가/타인 수리 시 모두 동일한 횟수 풀을 사용하는지 검증.

## 4. 고려 사항
-   `Stats.cs`의 `ApplyModifier`: `StatType.MaxRepairUses`에 대한 `Multiplicative` 수정 방식이 필요한지 여부 (현재 기획상으로는 불필요해 보임).
-   `ArmoredFrame.cs`의 `RecalculateStats`: Body 파츠가 여러 개 장착되는 예외적인 경우가 있는지, 있다면 어떻게 처리할지 (현재는 첫 번째 Body 파츠 기준).
-   Excel 데이터 기본값: `MaxRepairUses` 컬럼이 비어있을 경우 기본값 처리 방식 정의 (현재 `GetFloatValue`는 0f 반환).
-   UI 표시: 남은 수리 횟수를 게임 UI에 표시할지 여부 및 방식. 