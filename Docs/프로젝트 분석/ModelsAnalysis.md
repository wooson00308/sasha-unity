# Models 디렉토리 분석 (`Assets/AF/Scripts/Models`)

이 문서는 `Assets/AF/Scripts/Models` 디렉토리 및 그 하위 디렉토리(`Parts`, `Frames`)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 핵심 데이터 구조인 Armored Frame(AF)과 관련된 모델들을 정의하며, 행동 트리 AI 시스템에서 사용되는 일부 핵심 데이터 구조도 포함합니다.

## 핵심 모델 클래스

### 1. `ArmoredFrame.cs`

-   **역할**: 게임의 주요 유닛인 Armored Frame을 나타내는 핵심 클래스입니다. 프레임, 파츠, 파일럿, 무기 등을 조합하여 하나의 기체를 구성하며, 행동 트리 기반 AI 로직을 포함합니다.
-   **주요 속성**:
    -   `_name`: 기체 이름. 생성 시 외부에서 주입받으며, 고유 식별자(예: Callsign)로 사용될 수 있습니다.
    -   `_frameBase`: 기체의 기본 골격 (`Frame` 객체)
    -   `_parts`: 장착된 파츠 목록 (`Dictionary<string, Part>`, 슬롯 식별자 키)
    -   `_pilot`: 기체를 조종하는 파일럿 (`Pilot` 객체)
    -   `_equippedWeapons`: 장착된 무기 목록 (`List<Weapon>`)
    -   `_combinedStats`: 프레임, 모든 파츠, 파일럿의 스탯을 합산한 최종 스탯 (`Stats` 객체). `CombinedStats.Durability`는 기체의 최대 총 내구도를 나타냅니다.
    -   `_currentAP`: 현재 행동력 (Action Points). 생성자에서 `RecalculateStats()` 호출 전후로 `_combinedStats.MaxAP` 값으로 초기화/재설정됩니다.
    -   `_totalWeight`: 기체 총 무게 (프레임 + 모든 파츠)
    -   `_activeStatusEffects`: 현재 적용 중인 상태 효과 목록 (`List<StatusEffect>`)
    -   `_isOperational`: 기체 작동 가능 여부. `CheckOperationalStatus()`를 통해 주로 `BodyPart`의 상태에 따라 결정되며, `PartDestroyedEvent`의 `FrameWasActuallyDestroyed` 플래그와 연관될 수 있습니다. `IsDestroyed` 프로퍼티는 `!_isOperational`을 반환합니다.
    -   `_position`, `_teamId`: 위치 및 소속 팀 정보
    -   **`BehaviorTreeRoot` (행동 트리용)**: 이 유닛의 AI 로직을 담당하는 `AF.AI.BehaviorTree.BTNode` 루트 인스턴스. `CombatSimulatorService`에서 `StartCombat` 시 할당됩니다.
    -   **`AICtxBlackboard` (행동 트리용)**: 이 유닛의 행동 트리가 사용하는 `AF.AI.BehaviorTree.Blackboard` 인스턴스. 생성자에서 `new Blackboard()`로 초기화되며, 노드 간 데이터 공유 및 최종 행동 결정 사항을 저장합니다.
    -   `CurrentTarget` (행동 트리용): `public ArmoredFrame CurrentTarget { get; set; }` 프로퍼티로 존재. AI가 목표로 삼는 `ArmoredFrame`. (문서의 `AICtxBlackboard`를 통한 관리 언급과 코드상 직접 프로퍼티 존재 간의 실제 사용 방식 확인 필요)
    -   `IntendedMovePosition` (행동 트리용): `public Vector3? IntendedMovePosition { get; set; }` 프로퍼티로 존재. 이동 노드가 설정하는 목표 이동 위치. (문서의 `AICtxBlackboard`를 통한 관리 언급과 코드상 직접 프로퍼티 존재 간의 실제 사용 방식 확인 필요)
-   **주요 기능**:
    -   파츠 및 무기 장착/해제 (`AttachPart`, `DetachPart`, `AttachWeapon`, `DetachWeapon`): `AttachPart` 시 기존 파츠가 있으면 제거 후 교체. 파츠 변경 시 `CheckOperationalStatus()` 호출.
    -   파일럿 할당 (`AssignPilot`): 파일럿 변경 시 `RecalculateStats()` 호출 (실제 스탯 반영은 TODO 상태).
    -   스탯 재계산 (`RecalculateStats`)
    -   작동 상태 확인 (`CheckOperationalStatus`)
    -   데미지 및 수리 적용 (`ApplyDamage`, `ApplyRepair`): `ApplyDamage`는 `currentTurn`, `source`, `isCritical`, `isCounterAttack` 등 다양한 정보를 파라미터로 받도록 확장됨. **`ApplyRepair`는 수리량 계산 시 `PartStats.Durability` 대신 `partToRepair.MaxDurability` (Part 클래스의 `_maxDurability` 필드)를 사용하도록 수정되어, 파츠의 실제 최대 내구도를 기준으로 수리량이 결정된다.**
    -   AP 관리 (`RecoverAPOnTurnStart`, `ConsumeAP`, `HasEnoughAP`)
    -   상태 효과 관리 (`AddStatusEffect`, `RemoveStatusEffect`, `TickStatusEffects`)
    -   현재 총 체력 조회 (`GetCurrentAggregatedHP`): 모든 파츠의 현재 내구도 합계를 반환.
    -   장착된 작동 가능 파츠 슬롯 조회 (`GetAllOperationalPartSlots`): 신규 추가된 메서드.
    -   상태 스냅샷 생성 (전투 로그용, `ArmoredFrameSnapshot` 구조체 사용)
-   **특징**: 기체의 모든 구성 요소와 상태를 관리하며, 관련 로직 및 행동 트리 AI 실행을 위한 기반을 제공합니다. "현재 총 체력"은 `_combinedStats.Durability` (최대치)와 `GetCurrentAggregatedHP()` (현재 합계)로 구분하여 파악합니다.

### 2. `Part.cs` (추상 클래스, `Models/Parts/` 하위에 구체 클래스 존재)

-   **역할**: AF를 구성하는 모든 파츠(머리, 몸통, 팔, 다리 등)의 기본 추상 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_type`: 파츠 이름 및 타입 (`PartType` enum)
    -   `_stats`: 파츠 자체의 스탯 (`Stats` 객체)
    -   `_weight`: 파츠 무게. 기본값 10f로 초기화되거나 생성자에서 값을 받을 수 있습니다.
    -   `_maxDurability`, `_currentDurability`: 최대/현재 내구도
    -   `_isOperational`: 파츠 작동 가능 여부 (내구도 0 이하 시 false). `IsDestroyed` 프로퍼티는 `!_isOperational`을 반환합니다.
    -   `_abilities`: 파츠 고유 능력 목록 (`List<string>`)
-   **주요 기능**:
    -   **생성자**: 기본 생성자 외에 `weight`를 파라미터로 받는 생성자 오버로드가 존재합니다.
    -   내구도 설정 및 상태 변경 확인 (`SetDurability`): `Mathf.Clamp`로 내구도 범위를 (0 ~ 최대치)로 유지. 반환값은 `bool?`으로, 이 호출로 인해 파츠의 작동 가능 상태가 변경되었는지 여부를 나타냅니다 (true: 작동 가능해짐, false: 작동 불가능해짐, null: 상태 변경 없음). **이 메서드 내부에서 `_currentDurability`가 변경되는 시점에 디버그 로그를 추가하여 값의 변화를 추적할 수 있습니다 (실제 빌드에서는 제거될 수 있음).**
    -   데미지 적용 (`ApplyDamage`): 내부적으로 `SetDurability` 호출. 반환값은 `bool`로, 이 공격으로 인해 파츠가 작동 불능 상태가 되었는지 여부를 나타냅니다.
    -   능력 추가 (`AddAbility`)
    -   파괴 시 효과 정의 (`OnDestroyed(ArmoredFrame parentAF)`, 추상 메서드)
-   **특징**: 모든 파츠의 공통 속성과 기능을 정의합니다. 구체적인 파츠(예: `HeadPart`, `ArmPart`)는 이 클래스를 상속받아 구현됩니다.

#### 구체적인 파츠 구현 (`Models/Parts/`)

-   **`HeadPart.cs`**: `Part`를 상속받으며 `PartType.Head`로 지정됩니다. `OnDestroyed` 시 명중률 감소 등의 패널티가 예정되어 있으나 현재는 TODO 상태입니다. 그 외 특별한 추가 기능은 없습니다.
-   **`BodyPart.cs`**: `Part`를 상속받으며 `PartType.Body`로 지정됩니다. 이 파츠가 파괴될 경우 기체 전체가 작동 불능 상태가 될 수 있으며, 이 로직은 주로 `ArmoredFrame`의 `CheckOperationalStatus()` 메서드에서 Body 파츠의 상태를 확인함으로써 처리됩니다. `OnDestroyed` 메서드 자체에는 특별한 로직이 없습니다.
-   **`ArmsPart.cs`**: `Part`를 상속받으며 `PartType.Arm`으로 지정됩니다. `OnDestroyed` 시 무기 명중률 감소 또는 특정 무기 사용 불가 등의 패널티가 예정되어 있으나 현재는 TODO 상태입니다. 그 외 특별한 추가 기능은 없습니다.
-   **`LegsPart.cs`**: `Part`를 상속받으며 `PartType.Legs`로 지정됩니다. `OnDestroyed` 시 이동 속도 감소, 회피율 감소 등의 패널티가 예정되어 있으나 현재는 TODO 상태입니다. 그 외 특별한 추가 기능은 없습니다.
-   **`BackpackPart.cs`**: `Part`를 상속받으며 `PartType.Backpack`으로 지정됩니다. 특수 능력과 관련된 기능을 담당할 것으로 예상되며, `OnDestroyed` 메서드에는 현재 `Debug.Log`만 구현되어 있으나, 주석에 따르면 향후 파괴 시 특수 능력 제거 또는 폭발 유발 등의 로직이 추가될 수 있습니다.

### 3. `Frame.cs` (추상 클래스, `Models/Frames/` 하위에 구체 클래스 존재)

-   **역할**: AF의 기본 골격인 프레임의 추상 기본 클래스입니다. 파츠를 장착할 수 있는 슬롯의 정의와 파츠 타입별 호환성 정보를 가집니다.
-   **`PartSlotDefinition` 중첩 클래스**: `Frame.cs` 파일 내에 정의된 클래스로, 각 파츠 슬롯의 상세 정보를 나타냅니다.
    -   `SlotIdentifier` (string): 슬롯의 고유 식별자 (예: "Head", "Arm_Left").
    -   `RequiredPartType` (`PartType` enum): 해당 슬롯에 장착 가능한 파츠의 타입을 지정합니다.
-   **주요 속성**:
    -   `_name`, `_type`: 프레임 이름 및 타입 (`FrameType` enum - Light, Standard, Heavy 등)
    -   `_baseStats`: 프레임 자체의 기본 스탯 (`Stats` 객체)
    -   `_weight`: 프레임 자체 무게. 기본값 50f로 초기화되거나 생성자에서 값을 받을 수 있습니다.
    -   `_partCompatibility`: `Dictionary<PartType, float>` 형태. 각 파츠 타입에 대한 프레임의 기본 호환성 계수를 저장합니다 (1.0이 표준). 생성 시 `InitializeDefaultCompatibility()`로 모든 파츠 타입에 1.0을 할당한 후, `AdjustCompatibilityByType()`을 통해 프레임 타입별로 특정 파츠 타입의 호환성 값을 조정합니다 (예: 경량 프레임은 Body 파츠 호환성 0.8f).
-   **주요 기능**:
    -   **생성자**: 기본 생성자 외에 `weight`를 파라미터로 받는 생성자 오버로드가 존재합니다. 내부적으로 `InitializeDefaultCompatibility()`와 `AdjustCompatibilityByType()`를 호출합니다.
    -   파츠 슬롯 정의 반환 (`GetPartSlots()`): **추상 메서드**로 변경. `IReadOnlyDictionary<string, PartSlotDefinition>`를 반환하며, 각 구체 프레임 클래스에서 이 프레임이 제공하는 모든 파츠 슬롯과 각 슬롯의 `PartSlotDefinition`을 반환하도록 구현해야 합니다.
    -   파츠 장착 가능 여부 확인 (`CanEquipPart(Part part, string slotIdentifier)`): `GetPartSlots()`를 통해 얻은 `PartSlotDefinition`을 사용하여 슬롯에 맞는 `PartType`인지 확인합니다. 추가적으로 `_partCompatibility` 딕셔너리를 참조하여 호환성이 너무 낮은 경우(예: 0.9 미만) 경고를 출력하거나 장착을 막을 수 있습니다.
    -   파츠 호환성 계수 반환 (`GetCompatibilityFactor(PartType partType)`): `_partCompatibility` 딕셔너리에서 해당 파츠 타입의 호환성 계수를 반환합니다 (없으면 기본값 1.0f).
-   **특징**: 각기 다른 슬롯 구성과 파츠 호환성을 가진 다양한 프레임 타입을 만들기 위한 기반을 제공합니다. 구체적인 프레임(예: `StandardFrame`)은 이 클래스를 상속받아 `GetPartSlots()`를 구현해야 합니다.

#### 구체적인 프레임 구현 (`Models/Frames/`)

-   **`LightFrame.cs`**: `FrameType.Light`로 지정된 프레임. `GetPartSlots()`를 구현하여 경량 프레임의 파츠 슬롯 구성을 정의합니다. 현재 코드는 `StandardFrame`과 동일하게 모든 슬롯(백팩 포함)을 제공하지만, 주석에 따르면 향후 백팩 슬롯이 제외될 수도 있습니다. 제공되는 슬롯은 다음과 같습니다 (SlotIdentifier가 StandardFrame보다 간결함):
    -   키 "Head": `SlotIdentifier = "Head"`, `RequiredPartType = PartType.Head`
    -   키 "Body": `SlotIdentifier = "Body"`, `RequiredPartType = PartType.Body`
    -   키 "Arm_Left": `SlotIdentifier = "Arm_Left"`, `RequiredPartType = PartType.Arm`
    -   키 "Arm_Right": `SlotIdentifier = "Arm_Right"`, `RequiredPartType = PartType.Arm`
    -   키 "Legs": `SlotIdentifier = "Legs"`, `RequiredPartType = PartType.Legs`
    -   키 "Backpack": `SlotIdentifier = "Backpack"`, `RequiredPartType = PartType.Backpack`
-   **`StandardFrame.cs`**: `FrameType.Standard`로 지정된 프레임입니다. `Frame` 클래스를 상속받아 `GetPartSlots()` 메서드를 `override`하여 미리 정의된 `static readonly Dictionary<string, PartSlotDefinition>`를 반환함으로써 표준적인 파츠 슬롯 구성을 제공합니다. 제공되는 슬롯은 다음과 같습니다:
    -   키 "Head": `SlotIdentifier = "HeadSlot"`, `RequiredPartType = PartType.Head`
    -   키 "Body": `SlotIdentifier = "BodySlot"`, `RequiredPartType = PartType.Body`
    -   키 "Arm_Left": `SlotIdentifier = "Arm_Left"`, `RequiredPartType = PartType.Arm`
    -   키 "Arm_Right": `SlotIdentifier = "Arm_Right"`, `RequiredPartType = PartType.Arm`
    -   키 "Legs": `SlotIdentifier = "Legs"`, `RequiredPartType = PartType.Legs`
    -   키 "Backpack": `SlotIdentifier = "Backpack"`, `RequiredPartType = PartType.Backpack`
-   **`HeavyFrame.cs`**: `FrameType.Heavy`로 지정된 프레임. `GetPartSlots()`를 구현하여 중량 프레임의 파츠 슬롯 구성을 정의합니다. 현재 코드는 `StandardFrame` 및 `LightFrame`과 동일하게 모든 슬롯(백팩 포함)을 제공하며, 주석에 따르면 백팩 슬롯이 필수일 수도 있습니다. 제공되는 슬롯은 다음과 같습니다 (SlotIdentifier가 StandardFrame보다 간결함):
    -   키 "Head": `SlotIdentifier = "Head"`, `RequiredPartType = PartType.Head`
    -   키 "Body": `SlotIdentifier = "Body"`, `RequiredPartType = PartType.Body`
    -   키 "Arm_Left": `SlotIdentifier = "Arm_Left"`, `RequiredPartType = PartType.Arm`
    -   키 "Arm_Right": `SlotIdentifier = "Arm_Right"`, `RequiredPartType = PartType.Arm`
    -   키 "Legs": `SlotIdentifier = "Legs"`, `RequiredPartType = PartType.Legs`
    -   키 "Backpack": `SlotIdentifier = "Backpack"`, `RequiredPartType = PartType.Backpack`
-   **공통**: 모든 구체 프레임 클래스는 `Frame`의 생성자를 호출하고, `GetPartSlots()` 추상 메서드를 `static readonly Dictionary<string, PartSlotDefinition>`를 반환하도록 구현합니다. 필요시 `CanEquipPart` 등을 재정의하여 타입별 특별 규칙을 적용할 수 있습니다.

### 4. `Weapon.cs`

-   **역할**: AF에 장착 가능한 무기를 나타내는 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_type`, `_damageType`: 무기 이름, 타입(`WeaponType`), 데미지 타입(`DamageType`)
    -   `_damage`, `_accuracy`, `_minRange`, `_maxRange`, `_attackSpeed`: 기본 데미지, 정확도, 최소/최대 사거리, 공격 속도. **행동 트리 노드 등에서 무기의 사거리를 참조할 때는 주로 `_maxRange` (또는 `MaxRange` 프로퍼티)가 사용된다.**
    -   `_overheatPerShot`, `_currentHeat`: 발사 당 과열 증가량, 현재 과열도
    -   `_baseAPCost`: 기본 공격 AP 소모량 (코드 내 기본값: 2.0f). 생성 시 최소 0.1f로 보정.
    -   `_maxAmmo`: 최대 탄약 수. 생성 시 0 이상으로 보정.
    -   `_currentAmmo`: 현재 탄약 수. `_maxAmmo`가 0 이하이면 999(무한)로 초기화, 아니면 `_maxAmmo` 값으로 초기화.
    -   `_reloadAPCost`: 재장전 AP 소모량 (코드 내 기본값: 1.5f). 생성 시 최소 0.1f로 보정.
    -   `_reloadTurns`: 재장전 필요 턴 수 (0이면 즉시). 생성 시 0 이상으로 보정.
    -   `_isReloading`, `_reloadStartTurn`: 재장전 상태 및 재장전 시작 턴.
    -   `_attackFlavorKey`, `_reloadFlavorKey`: 공격/재장전 시 사용할 Flavor Text 키. 생성자나 `InitializeFromSO`에서 설정되며, null일 경우 빈 문자열로 초기화.
    -   `_isOperational`: 무기 작동 가능 여부
    -   `_specialEffects`: 특수 효과 목록 (`List<string>`)
    -   `_weight`: 무기 무게 (코드 내 기본값: 1f). 생성자나 `InitializeFromSO`에서 설정.
-   **주요 기능**:
    -   **생성자**: `name`, `type`, `damageType`, `damage`, `accuracy`, `minRange`, `maxRange`, `attackSpeed`, `overheatPerShot`, `baseAPCost`, `maxAmmo`, `reloadAPCost`, `reloadTurns`, `attackFlavorKey`, `reloadFlavorKey`, `weight`를 파라미터로 받아 초기화합니다.
    -   ScriptableObject로부터 초기화 (`InitializeFromSO(WeaponSO weaponSO)`): `WeaponSO` 에셋의 값으로 무기 속성(Flavor Key, 무게 포함)을 설정하고, 런타임 상태를 초기화합니다.
    -   발사 (`Fire`), 냉각 (`Cooldown`)
    -   탄약 관리 (`HasAmmo`, `ConsumeAmmo`): `HasAmmo`는 `_maxAmmo <= 0` (무한 탄약) 조건을 명시적으로 확인. `ConsumeAmmo`는 무한 탄약 시 항상 true 반환.
    -   재장전 관리 (`StartReload`, `FinishReload`, `CheckReloadCompletion`)
    -   데미지 계산 (`CalculateDamage`)
    -   상태 관리 (`Repair`, `DamageWeapon`)
    -   복제 (`Clone`)
-   **특징**: 무기의 상세 스펙과 발사, 재장전, 상태 관리 등 관련 로직을 포함합니다.

### 5. `Pilot.cs`

-   **역할**: AF를 조종하는 파일럿을 나타내는 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_baseStats`: 파일럿 이름 및 기본 스탯 (`Stats` 객체)
    -   `_level`, `_experience`, `_experienceToNextLevel`: 레벨 및 경험치 정보. 기본 생성자 또는 상세 생성자에서 Lv 1, Exp 0, NextExp 100으로 초기화됩니다.
    -   `_specialization`: 파일럿 전문화 타입 (`SpecializationType` enum). 기본 생성자에서는 `StandardCombat`으로 초기화.
    -   `_skills`: 보유 스킬 목록 (`List<string>`)
    -   `_specializationBonus`: 전문화에 따른 스탯 보너스 (`Stats` 객체). `CalculateSpecializationBonus()`를 통해 계산됩니다.
-   **주요 기능**:
    -   **생성자**: 매개변수 없는 기본 생성자(`public Pilot()`)가 추가되어 기본값으로 파일럿을 초기화합니다. 기존의 상세 생성자(`name`, `baseStats`, `specialization` 파라미터)도 존재합니다.
    -   경험치 획득 및 레벨업 처리 (`GainExperience`, `LevelUp`):
        -   `LevelUp()` 시 `_baseStats`가 모든 항목에 대해 10% 증가 (`_baseStats * 1.1f`).
        -   다음 레벨업 필요 경험치는 `(int)(_experienceToNextLevel * 1.5f)`로 계산.
        -   3레벨마다 (`_level % 3 == 0`) `AddRandomSkill()`을 호출하여 새 스킬 획득.
    -   전문화 보너스 계산 (`CalculateSpecializationBonus`): 각 `SpecializationType`에 따라 정해진 `Stats` 보너스를 반환합니다. (예: `StandardCombat`는 공격력 +0.2, 정확도 +0.15 등의 보너스. `Evasion` 타입 추가됨).
    -   스킬 추가 (`AddSkill`)
    -   랜덤 스킬 추가 (`AddRandomSkill`): 현재 `StandardCombat`, `Defense`, `Support`, `Engineering` 전문화에 대해 레벨에 따른 스킬명으로 추가합니다.
    -   총 스탯 계산 (`GetTotalStats`): `_baseStats + _specializationBonus`를 반환합니다.
-   **특징**: 파일럿의 성장 요소(레벨, 경험치, 스킬)와 전문화에 따른 스탯 보너스를 관리합니다.

### 6. `Stats.cs`

-   **역할**: AF, 파츠, 파일럿 등이 공유하는 기본 스탯 세트를 정의하는 클래스입니다.
-   **주요 속성**: `AttackPower`, `Defense`, `Speed`, `Accuracy`, `Evasion`, `Durability`, `EnergyEfficiency`, `MaxAP`, `APRecovery`. 각 스탯 필드는 기본 생성자에서 0f로 초기화되며, `EnergyEfficiency`만 1f로 초기화됩니다.
-   **주요 기능**:
    -   **생성자**: 기본 생성자(`Stats()`)는 모든 스탯을 0f로 (`EnergyEfficiency`는 1f), 상세 생성자(`Stats(...)`)는 모든 스탯 값을 파라미터로 받아 초기화합니다.
    -   스탯 덧셈 (`operator+`)
    -   스탯 곱셈 (`operator*`): AP 관련 스탯(`MaxAP`, `APRecovery`)은 곱셈에서 제외되어 기존 값을 유지합니다.
    -   스탯 수정 적용 (`ApplyModifier`): `StatType.None`일 경우 무시. AP 관련 스탯 수정 로직이 추가되었으며, 수정 후 `MaxAP`는 최소 1 이상을 보장합니다.
    -   음수 스탯 방지 (모든 스탯에 적용, `MaxAP`는 최소 1)
    -   문자열 변환 (`ToString`)
    -   스탯 초기화 (`Clear()`): 모든 스탯을 기본 생성자와 동일한 기본값으로 초기화합니다. (신규 추가)
    -   스탯 합산 (`Add(Stats other)`): 다른 `Stats` 객체의 값들을 현재 객체에 더합니다. (신규 추가)
-   **특징**: 게임 내 대부분의 객체가 가질 수 있는 핵심 능력치를 구조화하고 관련 연산을 제공합니다. **현재 총 내구도(Current HP)는 이 클래스에서 직접 관리하지 않고, `ArmoredFrame` 레벨에서 파츠들의 `CurrentDurability`를 합산하여 계산합니다.**

## 7. 행동 트리 관련 모델 (`Assets/AF/Scripts/AI/BehaviorTree/`)

이 섹션은 행동 트리(BT) 기반 AI 시스템을 구성하는 주요 모델 클래스들을 설명합니다. 이들은 `ArmoredFrame`의 AI 행동 로직을 정의하고 실행하는 데 사용됩니다. (상세 내용은 `Docs/AI 리팩토링/BehaviorTree.md` 참조)

-   **`BTNode.cs` (추상 클래스)**: 모든 행동 트리 노드의 기본이 되는 추상 클래스입니다. `Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)`라는 핵심 추상 메서드를 정의하며, 이 메서드는 각 노드의 로직을 실행하고 `NodeStatus` (Success, Failure, Running)를 반환합니다.
-   **`CompositeNode.cs` (추상 클래스)**: 여러 자식 노드를 가질 수 있는 복합 노드의 기본 클래스입니다. `SelectorNode` (자식 중 하나라도 Success면 Success 반환)와 `SequenceNode` (모든 자식이 Success여야 Success 반환) 등이 이를 상속합니다.
-   **`DecoratorNode.cs` (추상 클래스)**: 단일 자식 노드를 가지고 그 자식 노드의 실행 결과나 조건을 변경하는 역할을 하는 장식 노드의 기본 클래스입니다.
-   **`ConditionNode.cs` (추상 클래스)**: 특정 조건을 검사하여 `Success` 또는 `Failure`를 반환하는 잎새 노드의 기본 클래스입니다. 예: `IsTargetInRangeNode`, `HasEnoughAPNode`.
-   **`ActionNode.cs` (추상 클래스)**: 특정 행동을 결정하거나 실행하는 잎새 노드의 기본 클래스입니다. 예: `AttackTargetNode`, `MoveToTargetNode`, `ReloadWeaponNode`.
-   **`Blackboard.cs` (클래스)**: 행동 트리 내에서 노드 간 데이터를 공유하고, AI가 최종적으로 결정한 행동과 관련된 정보(예: `DecidedActionType`, `CurrentTarget`, `SelectedWeapon`, `WeaponToReload`, `IntendedMovePosition`, `ImmediateReloadWeapon`)를 저장하는 데 사용되는 데이터 컨테이너 클래스입니다. 각 `ArmoredFrame`은 `AICtxBlackboard`라는 이름으로 자신만의 `Blackboard` 인스턴스를 가집니다. 추가적으로 `AttackPosition` (공격 목표 위치), `HasReachedTarget` (목표 도달 여부), `IsAlerted` (경계 상태 여부) 등의 명시적 프로퍼티도 포함합니다. `ClearAllData()` 메서드는 제네릭 데이터와 모든 명시적 프로퍼티를 초기화합니다.

## 보조 모델 및 열거형

-   **`ArmoredFrameSnapshot.cs`**: 특정 시점의 `ArmoredFrame` 상태를 저장하는 구조체입니다. 이름(`Callsign`), 위치, 팀, AP, 내구도, 스탯, 파츠 및 무기 상태 스냅샷(`Dictionary<string, PartSnapshot>`, `List<WeaponSnapshot>`)을 포함합니다. 파츠 스냅샷은 슬롯 ID를 키로 사용하여 접근할 수 있어, `PartDestroyedEvent`의 `PartDestroyed_SlotId`와 연계하여 특정 파츠의 상태를 파악하는 데 용이합니다. 주로 전투 로그 재생 시 사용됩니다.
-   **`PartSnapshot.cs`**: 파츠의 상태 스냅샷 구조체 (이름, 현재/최대 내구도, 작동 여부, 파츠 타입). 슬롯 ID는 `ArmoredFrameSnapshot`의 딕셔너리 키를 통해 알 수 있습니다.
-   **`WeaponSnapshot.cs`**: 무기의 상태 스냅샷 구조체 (이름, 현재/최대 탄약, 재장전 상태, 작동 여부).
-   **`StatusEffect.cs`**: 상태 효과(버프/디버프) 정보를 나타내는 클래스입니다. 효과 이름, 지속 턴, 효과 타입(`StatusEffectEvents.StatusEffectType`), 스탯 변경 정보(`StatToModify`, `ModificationType`, `ModificationValue`), 틱 효과 정보(`TickEffectType`, `TickValue`)를 포함합니다. 모든 생성자에 `EffectType` 파라미터가 추가되었으며, 이를 저장하는 `EffectType` 프로퍼티가 새로 생겼습니다.
-   **`PartSlotDefinition.cs`**: `Frame`에서 사용하는 클래스로, 파츠 슬롯의 식별자와 해당 슬롯에 장착 가능한 파츠 타입(`RequiredPartType`)을 정의합니다.

-   **열거형 (Enums)**:
    -   `PartType.cs`: 파츠 종류 (Frame, Body, Head, Arm, Legs, Backpack). `Backpack` 타입은 존재하나, `BackpackPart.cs` 구현체는 현재 프로젝트에 없을 수 있습니다(확인 필요).
    -   `FrameType.cs`: 프레임 종류 (Light, Standard, Heavy)
    -   `WeaponType.cs`: 무기 종류 (Melee, MidRange, LongRange)
    -   `DamageType.cs`: 데미지 종류 (Physical, Energy, Explosive, Piercing, Electric)
    -   `SpecializationType.cs`: 파일럿 전문화 종류 (StandardCombat, MeleeCombat, RangedCombat, Defense, **Support**, Engineering, Evasion)
    -   `StatType.cs`: 스탯 종류 (AttackPower, Defense, ... MaxAP, APRecovery)
    -   `ModificationType.cs`: 스탯 변경 방식 (None, Additive, Multiplicative)
    -   `TickEffectType.cs`: 턴 기반 효과 종류 (None, DamageOverTime, HealOverTime)

## 시스템 구조 요약

1.  게임의 핵심 유닛은 `ArmoredFrame` 객체로 표현됩니다. 각 유닛은 고유 `Callsign`으로 식별됩니다.
2.  `ArmoredFrame`은 하나의 `Frame`을 기반으로 하며, `Frame`은 기본 스탯과 파츠 장착 슬롯 규칙을 정의합니다.
3.  `Frame`의 슬롯에는 해당 타입의 `Part` 객체를 장착할 수 있습니다.
4.  `ArmoredFrame`에는 여러 `Weapon` 객체를 장착할 수 있습니다.
5.  `ArmoredFrame`은 `Pilot` 객체에 의해 조종됩니다.
6.  **AI 유닛의 경우, `ArmoredFrame`은 `BehaviorTreeRoot` (행동 트리의 루트 노드)와 `AICtxBlackboard` (행동 트리용 데이터 저장소, `ImmediateReloadWeapon` 등 포함)를 가집니다. 이들을 통해 AI 행동이 결정됩니다.**
7.  `Stats` 클래스는 다양한 객체의 능력치를 표현하며, `CombatContext` **클래스**는 전투 관련 정보를 묶어 전달합니다.
8.  전투 중 `ArmoredFrame`의 상태는 `StatusEffect`에 의해 변경될 수 있으며, `ArmoredFrameSnapshot`을 통해 상태를 기록할 수 있습니다. (파츠 스냅샷은 슬롯 ID로 관리)
9.  각종 열거형은 모델들의 종류와 특성을 구분하는 데 사용됩니다.

## 결론

`Assets/AF/Scripts/Models` 디렉토리는 Armored Frame 게임의 핵심 데이터 구조를 정의합니다. `ArmoredFrame` 클래스를 중심으로 다양한 구성 요소를 조합하여 게임 유닛을 표현하며(`Callsign` 도입, `IsOperational` 로직 변경점 등 반영), **이제 행동 트리 시스템을 위한 `BTNode` 및 `Blackboard`와 같은 AI 관련 모델도 이 생태계의 중요한 부분을 차지합니다(`ImmediateReloadWeapon` 추가 등).** `Stats` 클래스는 능력치 시스템의 기반을 제공하며, 관련 열거형들은 게임 내 다양한 요소들을 분류하고 정의하는 데 중요한 역할을 합니다. 스냅샷 구조 또한 상세화되어 로그 분석 및 재생에 더욱 유용해졌습니다. 이 모델들은 게임 로직의 다른 부분(전투, UI, 데이터 관리 등)에서 사용될 데이터의 청사진을 제공합니다. 