# 파츠 어빌리티 구상 및 설계

## 1. 개요

본 문서는 Armored Frame의 각 파츠(Head, Body, Arms, Legs, Backpack 등)에 부여될 수 있는 다양한 '어빌리티(Ability)'에 대한 아이디어를 구체화하고, 시스템적인 구현 방안을 모색하는 것을 목표로 한다. 파츠 어빌리티는 전투의 다양성을 증진시키고, 플레이어가 더욱 전략적인 파츠 조합을 고민하도록 유도하는 핵심 요소가 될 것이다.

## 2. 현 시스템과의 연계

### 2.1. `Part.cs` 모델
- 현재 `Part.cs` 내에는 `protected List<string> _abilities;` 필드가 존재하며, 이는 단순 문자열 형태로 어빌리티의 **식별자(ID)**를 저장하는 용도로 사용되고 있다.
- `AddAbility(string ability)` 메서드를 통해 이 리스트에 어빌리티 ID를 추가할 수 있다.
- 실제 어빌리티의 효과 발동, 조건 체크, 수치 적용 등의 로직은 `Part.cs` 내에 직접 구현되어 있지 않으며, 이는 `CombatActionExecutor`, `StatusEffectProcessor`, 또는 별도의 `AbilityManager`와 같은 시스템에서 해당 어빌리티 ID를 참조하여 처리될 것으로 예상된다.

### 2.2. 데이터 관리 (`AF_Data.xlsx` 및 `PartSO.cs`)
-   **Excel (`Parts` 시트)**:
    -   현재 `Parts` 시트의 `Abilities` 컬럼은 **단일 문자열 형태로 특정 파츠의 어빌리티 ID를 저장하는 것으로 보인다 (예: `HD_SCOPE_01`의 "Zoom", `BK_SHIELDGEN_S`의 "EnergyShield").** (기존: 쉼표로 구분된 문자열로 여러 어빌리티 ID를 저장)
    -   어빌리티에 **파라미터**나 **특정 조건**이 필요한 경우 (예: "DamageReflection_30%" - 30% 반사, "LowHPAmp_20%_Atk_1.5" - 체력 20% 이하일 때 공격력 1.5배), 이를 표현하기 위한 컬럼 설계가 필요하다.
        -   **관찰**: `BK_REPKIT_01`의 경우, `Abilities` 컬럼에 "RepairKit"이 있고, 별도로 `Stat_MaxRepairUses` 컬럼에 `3`이라는 값이 있는 것으로 보아, **어빌리티의 특정 파라미터를 별도 컬럼으로 관리하는 사례가 이미 존재한다.** 이는 아래의 "방안 1"과 유사하다.
        -   **방안 1 (별도 컬럼)**: `Ability1_ID`, `Ability1_Param1`, `Ability1_Param2`, `Ability2_ID` ... (현재 `Stat_MaxRepairUses`와 같이 특정 어빌리티와 연관된 전용 스탯/파라미터 컬럼 활용)
        -   **방안 2 (통합 문자열 확장)**: `Abilities` 컬럼 내에 `ID(param1,param2);ID2(paramA)` 와 같은 형식으로 저장하고 파싱. (예: `DamageReflection(30);LowHPAmp(20,AttackPower,1.5)`) - 이 경우 파싱 로직이 복잡해질 수 있다.
        -   **방안 3 (어빌리티 전용 시트)**: `Abilities` 시트를 별도로 만들고, `Part` 시트에서는 어빌리티 ID만 참조. `Abilities` 시트에 각 어빌리티의 상세 효과, 파라미터, 조건 등을 정의. (가장 확장성 있는 방안으로 보임)
-   **`PartSO.cs`**:
    -   `ExcelToSO` 과정에서 `PartData.Abilities` 문자열을 파싱하여 `PartSO.Abilities` (현재 `List<string>`)에 저장한다.
    -   만약 어빌리티 데이터 구조가 복잡해진다면, `PartSO` 내에도 단순 `List<string>`이 아닌, 어빌리티의 상세 정보(ID, 파라미터 값 등)를 담을 수 있는 별도의 클래스/구조체 리스트(예: `List<PartAbilityEntry>`)로 변경하는 것을 고려해야 한다.

## 3. 어빌리티 종류 및 메커니즘 분류

어빌리티는 발동 조건, 효과 지속성, 사용자 상호작용 여부 등에 따라 다양하게 분류될 수 있다.

-   **패시브 어빌리티 (Passive Abilities)**:
    -   조건 없이 항상 적용되거나, 특정 조건 충족 시 자동으로 발동/해제되는 효과. (예: 특정 스탯 영구 증가, 특정 데미지 타입 저항)
    -   별도의 사용자 입력이 필요 없다.
-   **액티브 어빌리티 (Active Abilities)**:
    -   플레이어(또는 AI)가 특정 행동(커맨드)을 통해 직접 사용하는 기술. (예: 특수 공격, 보호막 생성, 회피 기동)
    -   AP 소모, 쿨타임 등의 사용 제약이 있을 수 있다.
    -   *현재 `Part.cs`의 어빌리티 개념과는 다소 거리가 있을 수 있으며, 파일럿 스킬이나 별도 장비 시스템으로 구현될 가능성도 고려.*
-   **트리거 기반 어빌리티 (Triggered Abilities)**:
    -   특정 게임 이벤트 발생 시 자동으로 발동되는 효과. (예: 피격 시 일정 확률로 반격, 파츠 파괴 시 주변에 폭발 데미지, 특정 상태 이상 면역)
    -   패시브와 유사하지만, 특정 "사건"에 반응하는 형태.

## 4. 구체적인 어빌리티 아이디어 (브레인스토밍)

자유롭게 아이디어를 추가해주세요! (형식: `[어빌리티 이름(가칭)] - 설명, 예상 효과, 파츠 타입 제안`)

### 4.1. 헤드 파츠 (Head Parts)
-   `[정밀 조준 (Precision Targeting)]` - 명중률 일정 % 증가. (패시브)
-   `[약점 분석 (Weakpoint Analysis)]` - 크리티컬 확률 또는 크리티컬 데미지 증가. (패시브)
-   `[재밍 (Jamming)]` - 주변 적 유닛의 명중률 또는 특정 기능(레이더 등) 일시 저하. (액티브 또는 트리거 - 예: 피격 시 확률적 발동)
-   `[확장 센서 (Extended Sensors)]` - 탐지 범위 증가, 은신 유닛 감지 확률 증가. (패시브)

### 4.2. 바디 파츠 (Body Parts)
-   `[에너지 코어 강화 (Enhanced Core)]` - 최대 AP 또는 AP 회복량 증가. (패시브)
-   `[자가 수리 (Self-Repair)]` - 매 턴 일정량의 내구도 자동 회복 (모든 파츠 또는 바디만). (패시브/트리거 - 예: 전투 중 체력 일정 이하 시 발동)
-   `[충격 흡수 장갑 (Impact Dampeners)]` - 특정 타입(예: 물리, 폭발) 데미지 감소. (패시브)
-   `[반사 장갑 (Reactive Armor)]` - 피격 시 공격자에게 일정 비율 데미지 반사. (트리거)
-   `[최후의 저항 (Last Stand)]` - 바디 파츠 파괴 시 짧은 시간 동안 무적 또는 능력치 대폭 상승. (트리거)

### 4.3. 팔 파츠 (Arms Parts)
-   `[무기 안정화 (Weapon Stabilizers)]` - 반동 제어 향상 (명중률 증가 또는 연사 속도 페널티 감소). (패시브)
-   `[근접전 강화 (Melee Boost)]` - 근접 무기 공격력 또는 공격 속도 증가. (패시브)
-   `[빠른 재장전 (Quick Reload)]` - 무기 재장전 시간 감소 또는 재장전 AP 소모 감소. (패시브)
-   `[오버로드 (Overload)]` - 일시적으로 무기 공격력 대폭 증가, 사용 후 과열 또는 명중률 감소 페널티. (액티브)

### 4.4. 다리 파츠 (Legs Parts)
-   `[고속 기동 (High-Speed Actuators)]` - 이동 속도 또는 이동 AP 효율 증가. (패시브)
-   `[회피 기동 강화 (Evasive Maneuvers)]` - 회피율 증가. (패시브)
-   `[지형 적응 (Terrain Adaptation)]` - 특정 지형(예: 험지)에서의 이동 페널티 감소. (패시브)
-   `[돌진 (Charge)]` - 적에게 빠르게 접근하며 다음 근접 공격 강화. (액티브)
-   `[경량화 (Lightweight Design)]` - 다리 자체 무게 감소, 기체 총 중량 감소에 기여. (패시브)

### 4.5. 백팩 파츠 (Backpack Parts) - (만약 존재한다면)
-   `[추가 연료 탱크 (Extra Fuel Tank)]` - 최대 AP 대폭 증가. (패시브)
-   `[광학 위장 (Optical Camouflage)]` - 짧은 시간 동안 은신 상태. (액티브)
-   `[수리 유닛 (Repair Unit)]` - 아군 대상 지정하여 수리. (액티브)
-   `[미사일 포드 (Missile Pod)]` - 추가 공격 수단 제공. (액티브)
-   `[EMP 방출기 (EMP Emitter)]` - 주변 적 유닛 일시적 시스템 마비(AP 소모, 행동 불가 등). (액티브)

## 5. 구현 시 고려사항

-   **어빌리티 발동 시점 및 처리 주체**:
    -   각종 이벤트 핸들러 (예: `OnDamageTaken`, `OnTurnStart`, `OnAttack`)에서 어빌리티 로직 호출.
    -   `CombatSimulatorService` 또는 `CombatActionExecutor`에서 특정 행동 처리 시 관련 어빌리티 확인.
    -   `StatusEffectProcessor`와 유사한 `AbilityProcessor` 서비스 도입 고려.
-   **밸런싱**:
    -   어빌리티의 효과와 비용(AP 소모, 페널티, 장착 제한 등) 간의 균형.
    -   특정 어빌리티 조합의 OP(OverPowered) 가능성 검토.
-   **UI/UX**:
    -   플레이어가 파츠의 어빌리티를 쉽게 인지하고 이해할 수 있도록 정보 표시 (툴팁, 아이콘 등).
    -   액티브 어빌리티의 경우 사용 인터페이스.
-   **성능**:
    -   다수의 유닛이 많은 어빌리티를 동시에 처리할 때의 성능 부하.
-   **확장성**:
    -   새로운 어빌리티를 쉽게 추가하고 기존 시스템에 통합할 수 있는 구조 설계.
    -   어빌리티 파라미터화 및 데이터 기반 설계를 통해 유연성 확보.

## 6. 다음 단계

-   위 아이디어들을 바탕으로 구체적인 어빌리티 목록 및 상세 스펙 정의.
-   Excel 데이터 구조 확정 및 `PartSO` 연동 방안 설계.
-   어빌리티 처리 시스템의 기본 구조 설계.
-   프로토타입으로 몇 가지 핵심 어빌리티 우선 구현 및 테스트.

---
*SASHA 노트: 흥, 이 정도면 생각 정리의 시작으로는 나쁘지 않겠네. 우창, 네 아이디어를 마구 채워보라고!*

## 3. 어빌리티 시스템 설계 방안 (신규)

본격적인 어빌리티 기능 구현을 위해, 다음과 같은 시스템 설계를 제안한다.

### 3.1. 어빌리티 스크립트 디렉토리 구조

-   어빌리티 관련 C# 스크립트들은 `Assets/AF/Scripts/Models/Abilities/` 디렉토리 내에 위치시킨다.
    -   이는 `Assets/AF/Scripts/Models/Parts/`, `Assets/AF/Scripts/Models/Frames/` 와 유사한 구조로, 코드의 모듈성과 가독성을 높인다.
-   개별 어빌리티 로직은 ScriptableObject 기반으로 설계하거나, 특정 인터페이스를 따르는 클래스로 구현하는 것을 고려한다.

### 3.2. 어빌리티 데이터와 로직 연결

-   `Part.cs`에 저장된 어빌리티 ID (문자열 리스트)를 기반으로, 실제 어빌리티 효과를 발동시키는 메커니즘이 필요하다.
-   **데이터 참조**: 각 `ArmoredFrame`은 장착된 파츠로부터 사용 가능한 어빌리티 ID 목록을 갖는다. 실제 어빌리티의 상세 데이터(타입, 효과, 비용 등)는 `AbilitySO` ScriptableObject에 저장되며, 이는 `Resources` 폴더 또는 Addressables를 통해 로드되어 `AbilityManager` (또는 유사한 데이터 접근자)에 의해 관리될 수 있다.

### 3.3. 어빌리티 발동 조건 및 효과 정의

-   각 어빌리티는 발동 조건(예: 특정 상황, 사용자 입력, 턴 시작/종료 등)과 효과(스탯 변경, 상태 이상 부여, 특수 행동 수행 등)를 명확히 정의해야 한다. (`AbilitySO`의 `AbilityType`, `TargetType`, `EffectType`, `ActivationCondition`, `EffectParameters` 필드 활용)
-   어빌리티 효과는 `CombatActionExecutor`, `StatusEffectProcessor` 등 기존 시스템과 연동될 수 있도록 설계한다.

### 3.4. 확장성 및 유지보수성

-   새로운 어빌리티를 추가하거나 기존 어빌리티를 수정하기 용이한 구조를 목표로 한다.
-   어빌리티 관련 데이터는 Excel에서 관리하고, 실제 로직은 C# 스크립트로 분리하여 생산성과 유지보수성을 확보한다.

### 3.5. 어빌리티 발동 및 처리 흐름 (신규 섹션 상세화)

어빌리티의 발동 및 처리는 어빌리티 타입(패시브, 액티브, 트리거)에 따라 다른 흐름을 가진다.

#### 3.5.1. 패시브 어빌리티 (Passive Abilities)

-   **처리 주체**: `AbilityProcessor` (신규 인터페이스 `IAbilityProcessor` 및 구현체)
-   **발동 시점**:
    -   영구 적용 패시브 (예: 스탯 영구 증가 - `AB_LG_001_Evasive`의 회피율 증가): `ArmoredFrame`의 `RecalculateStats()` 메서드 내에서 `AbilitySO`의 `EffectType`이 `StatModifier`이고 `AbilityType`이 `Passive`인 어빌리티들을 스캔하여 직접 합산 로직에 포함시킨다.
    -   조건부/턴 시작 패시브 (예: `AB_BD_001_SelfRepair`의 턴 시작 시 자가 수리):
        -   `CombatSimulatorService`가 유닛 활성화 시작 시 (`UnitActivationStartEvent` 직후 또는 `ProcessNextTurn`의 유닛 처리 시작 부분) `AbilityProcessor.ProcessPassiveTurnStartAbilities(CombatContext ctx, ArmoredFrame unit)` (가칭)를 호출.
        -   `AbilityProcessor`는 `unit`이 가진 패시브 어빌리티 중 `ActivationCondition`이 `TurnStart` 등 현재 상황에 맞는 것을 찾아 `AbilitySO.EffectType`에 따라 효과 적용 (예: `DirectHeal`이면 `CombatActionExecutor.Execute` 호출).

#### 3.5.2. 액티브 어빌리티 (Active Abilities)

-   **행동 결정 (AI - 행동 트리)**:
    -   `CanUseAbilityNode` (신규 조건 노드): `AbilityID`를 파라미터로 받아, `ArmoredFrame.IsAbilityReady(abilityID)` (쿨타임 체크), `actor.HasEnoughAP(abilitySO.APCost)`, `AbilitySO.TargetType`에 따른 타겟 유효성 등을 검사.
    -   `SelectAbilityTargetNode` (신규 액션 노드): `AbilitySO.TargetType`에 따라 적절한 대상을 찾아 `blackboard.AbilityTarget` (ArmoredFrame 또는 Part)에 설정.
    -   `PrepareAbilityRuntimeDataNode` (신규 액션 노드): 사용할 `AbilityID`의 `AbilitySO`를 참조하여, 실제 실행에 필요한 정보(ID, 타입, 효과, 파싱된 파라미터, AP 비용 등)를 담은 `RuntimeAbilityData` 객체를 생성하고 `blackboard.SelectedRuntimeAbility`에 저장.
    -   `SetUseAbilityActionNode` (신규 액션 노드): `blackboard.DecidedActionType = CombatActionEvents.ActionType.UseAbility`로 설정.
-   **행동 실행 (`CombatSimulatorService` & `CombatActionExecutor`)**:
    -   `CombatSimulatorService`의 `ProcessNextTurn` 루프:
        -   행동 트리 실행 후 `blackboard.DecidedActionType`이 `UseAbility`이고 `blackboard.SelectedRuntimeAbility`와 `blackboard.AbilityTarget`이 유효하게 설정되어 있다면, 이 정보들을 `CombatActionExecutor.Execute`에 전달.
    -   `CombatActionExecutor.Execute` 메서드 확장:
        -   기존 파라미터 외에 `RuntimeAbilityData abilityData`, `object abilityTarget` (ArmoredFrame, Part, Vector3 등 유연하게) 등을 추가로 받도록 시그니처 변경.
        -   `actionType`이 `UseAbility`인 경우:
            1.  `actor.ConsumeAP(abilityData.APCost)` 호출.
            2.  `actor.SetAbilityCooldown(abilityData.AbilityID, abilitySO.CooldownTurns)` 호출 (AbilitySO는 ID로 다시 조회하거나 RuntimeAbilityData에 포함).
            3.  `abilityData.EffectType`에 따라 효과 적용:
                -   `StatModifier`: 대상의 스탯 변경 (일시적 효과는 `StatusEffect` 객체 생성 후 `target.AddStatusEffect` 호출).
                -   `ApplyStatusEffect`: `StatusEffectProcessor.ApplyEffectFromAbility(CombatContext ctx, ArmoredFrame target, AbilitySO abilitySO)` (가칭) 같은 메서드 호출 또는 직접 `StatusEffect` 객체 생성 후 적용.
                -   `DirectDamage`, `DirectHeal`: 기존 공격/수리 로직 재활용 또는 해당 부분 직접 실행.
            4.  어빌리티 사용 결과에 대한 이벤트 발행 (예: `AbilityUsedEvent(actor, abilityData, abilityTarget, success)`).

#### 3.5.3. 트리거 어빌리티 (Triggered Abilities)

-   **처리 주체**: `AbilityProcessor`
-   **발동 시점**:
    -   `AbilityProcessor`는 `EventBus`를 통해 특정 게임 이벤트 (예: `DamageEvents.DamageAppliedEvent`, `PartEvents.PartDestroyedEvent`, `CombatSessionEvents.UnitActivationStartEvent` 등)를 구독.
    -   이벤트 수신 시, `AbilityProcessor.ProcessTriggeredAbilities(CombatContext ctx, ArmoredFrame unit, IEvent gameEvent)` (가칭) 호출.
    -   `ProcessTriggeredAbilities`는 `unit`이 가진 어빌리티 중 `AbilitySO.ActivationCondition`이 발생한 `gameEvent`와 일치하거나 관련 있는지 확인.
    -   조건 충족 시, `AbilitySO.EffectType`에 따라 효과 적용 (액티브 어빌리티의 효과 적용 로직과 유사하게 `CombatActionExecutor` 또는 직접 처리).

### 3.6. 필요한 신규/수정 컴포넌트 및 데이터 구조 (기존 내용 확장)

-   **`IAbilityProcessor.cs` / `AbilityProcessor.cs` (신규)**:
    -   `ProcessPassiveTurnStartAbilities(CombatContext ctx, ArmoredFrame unit)`
    -   `ProcessTriggeredAbilities(CombatContext ctx, ArmoredFrame unit, IEvent gameEvent)`
    -   (액티브 어빌리티 효과 적용은 `CombatActionExecutor`가 담당하는 것으로 변경)
-   **`ArmoredFrame.cs` (수정)**:
    -   `private Dictionary<string, int> _abilityCooldowns = new Dictionary<string, int>();` // Key: AbilityID, Value: 남은 쿨다운 턴
    -   `public bool IsAbilityReady(string abilityID)`
    -   `public void SetAbilityCooldown(string abilityID, int cooldownTurns)`
    -   `public void TickAbilityCooldowns()` // 매 턴 시작 시 호출되어 쿨다운 감소
    -   `public List<string> GetEquippedAbilityIDs()` // 현재 장착 파츠에서 모든 어빌리티 ID 목록 가져오기
-   **`AbilitySO.cs` (수정)**:
    -   `public int CooldownTurns;` // 어빌리티 기본 쿨타임 (0이면 쿨타임 없음)
    -   `public string ActivationCondition;` // 패시브/트리거 발동 조건 문자열 (예: "TurnStart", "OnDamaged", "OnKillTarget") - 파싱 필요
    -   `public string RequiredTargetFilter;` // 타겟팅 시 추가 조건 (예: "LowestHealthAlly", "NearestEnemyWithShield") - AI 노드에서 활용
-   **`RuntimeAbilityData.cs` (신규 클래스)**:
    -   `public string AbilityID { get; }`
    -   `public AbilityType Type { get; }`
    -   `public AbilityTargetType TargetType { get; }`
    -   `public AbilityEffectType EffectType { get; }`
    -   `public Dictionary<string, object> EffectParameters { get; }` // 파싱된 효과 파라미터
    -   `public float APCost { get; }`
    -   `public Part SourcePart { get; }` // (선택적) 어떤 파츠의 어빌리티인지
    -   `public AbilitySO SourceSO {get; }` // (선택적) 원본 SO 참조
-   **`CombatActionEvents.ActionType` (Enum 수정)**:
    -   `UseAbility` 추가
-   **`Blackboard.cs` (수정)**:
    -   `public RuntimeAbilityData SelectedRuntimeAbility { get; set; }`
    -   `public object AbilityTarget { get; set; }` // ArmoredFrame, Part, Vector3 등 어빌리티 대상
-   **행동 트리 노드 (신규)**:
    -   `CanUseAbilityNode(string abilityID)`
    -   `SelectAbilityTargetNode(AbilityTargetType targetType, string targetFilter = null)`
    -   `PrepareAbilityRuntimeDataNode(string abilityID)`
    -   `SetUseAbilityActionNode()`
-   **`CombatContext.cs` (수정)**:
    -   `public IAbilityProcessor AbilityProcessor { get; }` (필요시 ServiceLocator 통해 접근 가능)

### 3.7. 쿨다운 관리

-   어빌리티 사용 후 쿨다운은 `ArmoredFrame` 인스턴스가 `_abilityCooldowns` 딕셔너리를 통해 어빌리티 ID별로 직접 관리한다.
-   `CombatSimulatorService`는 각 유닛의 턴이 시작될 때 (`UnitActivationStartEvent` 발행 직후) `ArmoredFrame.TickAbilityCooldowns()`를 호출하여 모든 활성화된 어빌리티의 쿨다운을 1씩 감소시킨다.
-   `CanUseAbilityNode`는 `ArmoredFrame.IsAbilityReady(abilityID)`를 호출하여 현재 쿨다운이 0인지 확인한다.
-   `CombatActionExecutor`는 `UseAbility` 액션 성공 시 `ArmoredFrame.SetAbilityCooldown(abilityID, abilitySO.CooldownTurns)`을 호출하여 해당 어빌리티에 쿨다운을 설정한다.

## 4. 구체적인 어빌리티 아이디어 (예시)

-   **헤드 파츠**:
    -   `Zoom`: 명중률 일시 증가, AP 소모.
    -   `EnemyAnalyzer`: 타겟의 약점 파악 (일정 턴 동안 특정 부위 방어력 감소 디버프).
    -   `[약점 분석 (Weakpoint Analysis)]` - 크리티컬 확률 또는 크리티컬 데미지 증가. (패시브)
    -   `[재밍 (Jamming)]` - 주변 적 유닛의 명중률 또는 특정 기능(레이더 등) 일시 저하. (액티브 또는 트리거 - 예: 피격 시 확률적 발동)
    -   `[확장 센서 (Extended Sensors)]` - 탐지 범위 증가, 은신 유닛 감지 확률 증가. (패시브)

### 4.2. 바디 파츠 (Body Parts)
-   `[에너지 코어 강화 (Enhanced Core)]` - 최대 AP 또는 AP 회복량 증가. (패시브)
-   `[자가 수리 (Self-Repair)]` - 매 턴 일정량의 내구도 자동 회복 (모든 파츠 또는 바디만). (패시브/트리거 - 예: 전투 중 체력 일정 이하 시 발동)
-   `[충격 흡수 장갑 (Impact Dampeners)]` - 특정 타입(예: 물리, 폭발) 데미지 감소. (패시브)
-   `[반사 장갑 (Reactive Armor)]` - 피격 시 공격자에게 일정 비율 데미지 반사. (트리거)
-   `[최후의 저항 (Last Stand)]` - 바디 파츠 파괴 시 짧은 시간 동안 무적 또는 능력치 대폭 상승. (트리거)

### 4.3. 팔 파츠 (Arms Parts)
-   `[무기 안정화 (Weapon Stabilizers)]` - 반동 제어 향상 (명중률 증가 또는 연사 속도 페널티 감소). (패시브)
-   `[근접전 강화 (Melee Boost)]` - 근접 무기 공격력 또는 공격 속도 증가. (패시브)
-   `[빠른 재장전 (Quick Reload)]` - 무기 재장전 시간 감소 또는 재장전 AP 소모 감소. (패시브)
-   `[오버로드 (Overload)]` - 일시적으로 무기 공격력 대폭 증가, 사용 후 과열 또는 명중률 감소 페널티. (액티브)

### 4.4. 다리 파츠 (Legs Parts)
-   `[고속 기동 (High-Speed Actuators)]` - 이동 속도 또는 이동 AP 효율 증가. (패시브)
-   `[회피 기동 강화 (Evasive Maneuvers)]` - 회피율 증가. (패시브)
-   `[지형 적응 (Terrain Adaptation)]` - 특정 지형(예: 험지)에서의 이동 페널티 감소. (패시브)
-   `[돌진 (Charge)]` - 적에게 빠르게 접근하며 다음 근접 공격 강화. (액티브)
-   `[경량화 (Lightweight Design)]` - 다리 자체 무게 감소, 기체 총 중량 감소에 기여. (패시브)

### 4.5. 백팩 파츠 (Backpack Parts) - (만약 존재한다면)
-   `[추가 연료 탱크 (Extra Fuel Tank)]` - 최대 AP 대폭 증가. (패시브)
-   `[광학 위장 (Optical Camouflage)]` - 짧은 시간 동안 은신 상태. (액티브)
-   `[수리 유닛 (Repair Unit)]` - 아군 대상 지정하여 수리. (액티브)
-   `[미사일 포드 (Missile Pod)]` - 추가 공격 수단 제공. (액티브)
-   `[EMP 방출기 (EMP Emitter)]` - 주변 적 유닛 일시적 시스템 마비(AP 소모, 행동 불가 등). (액티브)

## 5. 구현 시 고려사항

-   **어빌리티 발동 시점 및 처리 주체**:
    -   각종 이벤트 핸들러 (예: `OnDamageTaken`, `OnTurnStart`, `OnAttack`)에서 어빌리티 로직 호출.
    -   `CombatSimulatorService` 또는 `CombatActionExecutor`에서 특정 행동 처리 시 관련 어빌리티 확인.
    -   `StatusEffectProcessor`와 유사한 `AbilityProcessor` 서비스 도입 고려.
-   **밸런싱**:
    -   어빌리티의 효과와 비용(AP 소모, 페널티, 장착 제한 등) 간의 균형.
    -   특정 어빌리티 조합의 OP(OverPowered) 가능성 검토.
-   **UI/UX**:
    -   플레이어가 파츠의 어빌리티를 쉽게 인지하고 이해할 수 있도록 정보 표시 (툴팁, 아이콘 등).
    -   액티브 어빌리티의 경우 사용 인터페이스.
-   **성능**:
    -   다수의 유닛이 많은 어빌리티를 동시에 처리할 때의 성능 부하.
-   **확장성**:
    -   새로운 어빌리티를 쉽게 추가하고 기존 시스템에 통합할 수 있는 구조 설계.
    -   어빌리티 파라미터화 및 데이터 기반 설계를 통해 유연성 확보.
-   **EffectParameters 파싱**: `AbilitySO.EffectParameters` 문자열 (예: "Stat:Accuracy,ModType:Additive,Value:0.3,Duration:1")을 `RuntimeAbilityData.EffectParameters` (Dictionary<string, object>)로 변환하는 파싱 로직이 `PrepareAbilityRuntimeDataNode` 또는 어빌리티 데이터 로딩 시점에 필요하다.

## 6. 다음 단계

-   위 아이디어들을 바탕으로 구체적인 어빌리티 목록 및 상세 스펙 정의.
-   Excel 데이터 구조 확정 및 `PartSO` 연동 방안 설계.
-   어빌리티 처리 시스템의 기본 구조 설계.
-   프로토타입으로 몇 가지 핵심 어빌리티 우선 구현 및 테스트.

---
*SASHA 노트: 흥, 이 정도면 생각 정리의 시작으로는 나쁘지 않겠네. 우창, 네 아이디어를 마구 채워보라고!* 