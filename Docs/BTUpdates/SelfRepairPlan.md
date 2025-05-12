# AI 행동 트리: 자가 수리 기능 개선 계획

## 1. 목표

-   모든 파일럿(SpecializationType 무관)에게 기본적인 **자가 수리(Self-Repair)** 능력을 부여한다.
-   자가 수리 행동에 **횟수 제한** 시스템을 도입한다.
-   **지원(Support)** 파일럿은 기존처럼 자가 수리 및 **타인 수리(Repair Ally)** 능력을 모두 유지한다.

## 2. 핵심 변경 사항

### 2.1. 자가 수리 보편화
-   현재 `SupportBT` 위주로 구현된 자가 수리 로직(관련 Condition/Action 노드)을 모든 행동 트리(`BasicAttackBT`, `RangedCombatBT`, `MeleeCombatBT`, `DefenderBT` 등)의 적절한 우선순위에 추가한다.
    -   예: 체력이 일정 수준 이하일 때 (`IsHealthLowNode` 사용) 자가 수리를 시도하는 로직.

### 2.2. 수리 횟수 제한 시스템 도입
-   **데이터 모델 변경**:
    -   `Stats.cs` (`Assets/AF/Scripts/Models/Stats.cs`):
        -   `[SerializeField] private float _maxRepairUses = 3.0f;` 필드 추가 (기본값 예시: 3.0f).
        -   `public float MaxRepairUses => _maxRepairUses;` 프로퍼티 추가.
        -   생성자, 덧셈 연산자 (`+`), `ApplyModifier`, `Clear`, `Add` 메서드에 `_maxRepairUses` 처리 로직 추가.
    -   `ArmoredFrame.cs` (`Assets/AF/Scripts/Models/ArmoredFrame.cs`):
        -   `private int _currentRepairUses;` 필드 추가.
        -   생성자 내 `RecalculateStats()` 호출 후, `_currentRepairUses = (int)_combinedStats.MaxRepairUses;` 로직 추가하여 초기화.
        -   `RecalculateStats()` 메서드 수정: `_combinedStats` 계산 시, Body 파츠의 `MaxRepairUses` 값을 `_combinedStats.MaxRepairUses`에 할당 (예: `_combinedStats.SetMaxRepairUses(bodyPart?.Stats?.MaxRepairUses ?? 0f);` 와 같은 형태).
        -   **[확인 필요]** `ArmoredFrame`에 `_currentRepairUses` 값을 외부(특히 BT 노드)에서 안전하게 읽을 수 있는 방법 제공 (예: `public int GetCurrentRepairUses() => _currentRepairUses;` 메서드 추가).
    -   `PartData.cs` (`Assets/ExcelToSO/Scripts/DataModels/PartData.cs`):
        -   `public float MaxRepairUses { get; private set; }` 속성 추가.
        -   `FromExcelRow` 메서드 내 `MaxRepairUses = GetFloatValue(row, <새 컬럼 인덱스>);` 코드 추가 (Excel 컬럼 위치 확인 필요).
    -   `PartSO.cs` (`Assets/AF/Data/ScriptableObjects/PartSO.cs`):
        -   `public float MaxRepairUses;` 필드 추가.
        -   `Apply(PartData data)` 메서드 내 `MaxRepairUses = data.MaxRepairUses;` 코드 추가.
-   **행동 트리 노드 추가**:
    -   `HasRepairUsesNode.cs` 생성 (`Assets/AF/Scripts/AI/BehaviorTree/Conditions/` 경로):
        -   `ConditionNode` 상속.
        -   `Tick` 메서드에서 `agent.GetCurrentRepairUses() > 0` (가정) 조건 확인 후 `Success`/`Failure` 반환.
    -   `SetRepairSelfActionNode.cs` 생성 (`Assets/AF/Scripts/AI/BehaviorTree/Actions/` 경로):
        -   `ActionNode` 상속.
        -   `Tick` 메서드에서 `blackboard.DecidedActionType = CombatActionEvents.ActionType.RepairSelf;` 설정 후 `Success` 반환. (기존 `SetRepairAllyActionNode.cs` 참고)
-   **전투 로직 변경**:
    -   `CombatActionExecutor.cs` (`Assets/AF/Scripts/Combat/CombatActionExecutor.cs`):
        -   `Execute` 메서드 내 `case CombatActionEvents.ActionType.RepairSelf:` 및 `case CombatActionEvents.ActionType.RepairAlly:` 블록에서, 행동 성공 시 (`success = true` 이후, `actor.ConsumeAP` 이전) `actor._currentRepairUses--;` (또는 `actor.DecrementRepairUses();` 같은 메서드 호출) 코드 추가.
-   **Excel 데이터 수정**:
    -   `AF_Data.xlsx` 파일의 `Parts` 시트: `MaxRepairUses` 컬럼 추가 및 각 파츠 데이터에 기본값 입력 (Body 파츠 외에는 0 또는 작은 값 부여?).

### 2.3. 지원 파일럿 특성 유지
-   `SupportBT` (`Assets/AF/Scripts/AI/BehaviorTree/PilotBTs/SupportBT.cs`)는 기존의 타인 수리(`RepairAlly`) 로직을 유지하되, **이제 타인 수리도 자가 수리와 동일하게 `_currentRepairUses`를 소모하며 횟수 제한 시스템의 적용을 받는다.**
-   지원 파일럿만이 `RepairAlly` 행동을 수행할 수 있다는 점은 변하지 않는다.

## 3. 작업 단계

1.  **(완료)** `/Docs/BTUpdates/SelfRepairPlan.md` 계획 문서 작성 및 업데이트.
2.  **데이터 모델 수정**:
    -   `Stats.cs` 수정 (필드, 프로퍼티, 관련 메서드).
    -   `ArmoredFrame.cs` 수정 (필드, 생성자 초기화, `RecalculateStats`, `GetCurrentRepairUses` 메서드 추가 검토).
    -   `PartData.cs` 수정 (속성, `FromExcelRow`).
    -   `PartSO.cs` 수정 (필드, `Apply`).
3.  **Excel 데이터 업데이트**: `AF_Data.xlsx`의 `Parts` 시트에 `MaxRepairUses` 컬럼 추가 및 데이터 입력.
4.  **ScriptableObject 재생성**: Unity 에디터 메뉴 "Tools/Generate AF Data from Excel" 실행하여 변경된 데이터로 `PartSO` 에셋 업데이트.
5.  **조건/액션 노드 구현**:
    -   `HasRepairUsesNode.cs` 생성 및 구현 (`Conditions` 폴더).
    -   `SetRepairSelfActionNode.cs` 생성 및 구현 (`Actions` 폴더).
6.  **전투 액션 로직 수정**: `CombatActionExecutor.cs`의 `Execute` 메서드 수정하여 `RepairSelf` 및 `RepairAlly` 시 횟수 차감 로직 추가.
7.  **행동 트리 수정**:
    -   모든 관련 PilotBT 파일 (`Assets/AF/Scripts/AI/BehaviorTree/PilotBTs/` 내 `.cs` 파일) 편집:
        -   `new SequenceNode(...)` 로 자가 수리 시퀀스 추가 (포함 노드: `HasRepairUsesNode`, `IsHealthLowNode`, `HasEnoughAPNode(RepairSelf)`, `SetRepairSelfActionNode`). 적절한 우선순위에 배치.
    -   `SupportBT.cs` 수정: 기존 타인 수리(`RepairAlly`) 시퀀스 시작 부분에 `HasRepairUsesNode` 추가.
8.  **테스트 및 검증**: `CombatTestRunner`를 사용하여 다양한 시나리오 테스트.
    -   모든 파일럿이 체력이 낮을 때 자가 수리를 시도하는가?
    -   수리 횟수 제한이 올바르게 적용되는가? (최대 횟수만큼만 사용 가능한지)
    -   횟수를 모두 소진하면 더 이상 수리 행동(자가/타인)을 시도하지 않는가?
    -   지원 파일럿은 자가 수리 및 타인 수리 시 동일한 수리 횟수 풀을 공유하며, 횟수 제한을 올바르게 적용받는가?
    -   관련 로그가 정상적으로 출력되는가?

## 4. 고려 사항

-   `MaxRepairUses`의 합리적인 기본값 설정.
-   기체 전체의 `MaxRepairUses`를 어떤 파츠 기준으로 결정할지 최종 확정 (Body 파츠 안 유력).
-   AI가 수리 횟수를 모두 소진했을 때 다른 행동을 적절히 선택하는지 확인.
-   UI에 현재 남은 수리 횟수를 표시할 필요성 검토. 