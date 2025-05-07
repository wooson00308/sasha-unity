# Models 디렉토리 분석 (`Assets/AF/Scripts/Models`)

이 문서는 `Assets/AF/Scripts/Models` 디렉토리 및 그 하위 디렉토리(`Parts`, `Frames`)에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 핵심 데이터 구조인 Armored Frame(AF)과 관련된 모델들을 정의하며, 행동 트리 AI 시스템에서 사용되는 일부 핵심 데이터 구조도 포함합니다.

## 핵심 모델 클래스

### 1. `ArmoredFrame.cs`

-   **역할**: 게임의 주요 유닛인 Armored Frame을 나타내는 핵심 클래스입니다. 프레임, 파츠, 파일럿, 무기 등을 조합하여 하나의 기체를 구성하며, 행동 트리 기반 AI 로직을 포함합니다.
-   **주요 속성**:
    -   `_name`: 기체 이름
    -   `_frameBase`: 기체의 기본 골격 (`Frame` 객체)
    -   `_parts`: 장착된 파츠 목록 (`Dictionary<string, Part>`, 슬롯 식별자 키)
    -   `_pilot`: 기체를 조종하는 파일럿 (`Pilot` 객체)
    -   `_equippedWeapons`: 장착된 무기 목록 (`List<Weapon>`)
    -   `_combinedStats`: 프레임, 모든 파츠, 파일럿의 스탯을 합산한 최종 스탯 (`Stats` 객체). `CombinedStats.Durability`는 기체의 최대 총 내구도를 나타냅니다.
    -   `_currentAP`: 현재 행동력 (Action Points)
    -   `_totalWeight`: 기체 총 무게 (프레임 + 모든 파츠)
    -   `_activeStatusEffects`: 현재 적용 중인 상태 효과 목록 (`List<StatusEffect>`)
    -   `_isOperational`: 기체 작동 가능 여부 (주요 파츠 파괴 시 false). `IsDestroyed` 프로퍼티는 이 플래그를 통해 결정됩니다 (`public bool IsDestroyed => !_isOperational;`).
    -   `_position`, `_teamId`: 위치 및 소속 팀 정보
    -   **`BehaviorTreeRoot` (행동 트리용)**: 이 유닛의 AI 로직을 담당하는 `AF.AI.BehaviorTree.BTNode` 루트 인스턴스. `CombatSimulatorService`에서 `StartCombat` 시 할당됩니다.
    -   **`AICtxBlackboard` (행동 트리용)**: 이 유닛의 행동 트리가 사용하는 `AF.AI.BehaviorTree.Blackboard` 인스턴스. 노드 간 데이터 공유 및 최종 행동 결정 사항을 저장합니다. `CombatSimulatorService`에서 `StartCombat` 시 초기화됩니다.
    -   `CurrentTarget` (행동 트리용, `AICtxBlackboard`를 통해 관리): 현재 AI가 목표로 삼고 있는 `ArmoredFrame`.
    -   `IntendedMovePosition` (행동 트리용, `AICtxBlackboard`를 통해 관리): 이동 노드가 설정하는 목표 이동 위치 (`Vector3?`).
-   **주요 기능**:
    -   파츠 및 무기 장착/해제 (`AttachPart`, `DetachPart`, `AttachWeapon`, `DetachWeapon`)
    -   파일럿 할당 (`AssignPilot`)
    -   스탯 재계산 (`RecalculateStats`)
    -   작동 상태 확인 (`CheckOperationalStatus`)
    -   데미지 및 수리 적용 (`ApplyDamage`, `ApplyRepair`)
    -   AP 관리 (`RecoverAPOnTurnStart`, `ConsumeAP`, `HasEnoughAP`)
    -   상태 효과 관리 (`AddStatusEffect`, `RemoveStatusEffect`, `TickStatusEffects`)
    -   현재 총 체력 조회 (`GetCurrentAggregatedHP`)
    -   상태 스냅샷 생성 (전투 로그용, `ArmoredFrameSnapshot` 구조체 사용)
-   **특징**: 기체의 모든 구성 요소와 상태를 관리하며, 관련 로직 및 행동 트리 AI 실행을 위한 기반을 제공합니다. "현재 총 체력"을 직접 관리하는 필드는 없으며, 파츠별 내구도와 `GetCurrentAggregatedHP()` 메서드를 통해 간접적으로 관리됩니다.

### 2. `Part.cs` (추상 클래스, `Models/Parts/` 하위에 구체 클래스 존재)

-   **역할**: AF를 구성하는 모든 파츠(머리, 몸통, 팔, 다리 등)의 기본 추상 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_type`: 파츠 이름 및 타입 (`PartType` enum)
    -   `_stats`: 파츠 자체의 스탯 (`Stats` 객체)
    -   `_weight`: 파츠 무게
    -   `_maxDurability`, `_currentDurability`: 최대/현재 내구도
    -   `_isOperational`: 파츠 작동 가능 여부 (내구도 0 이하 시 false)
    -   `_abilities`: 파츠 고유 능력 목록 (`List<string>`)
-   **주요 기능**:
    -   내구도 설정 및 상태 변경 확인 (`SetDurability`)
    -   데미지 적용 (`ApplyDamage`)
    -   능력 추가 (`AddAbility`)
    -   파괴 시 효과 정의 (`OnDestroyed`, 추상 메서드)
-   **특징**: 모든 파츠의 공통 속성과 기능을 정의합니다. 구체적인 파츠(예: `HeadPart`, `ArmPart`)는 이 클래스를 상속받아 구현됩니다.

#### 구체적인 파츠 구현 (`Models/Parts/`)

-   **`HeadPart.cs`**: `PartType.Head`로 지정된 파츠. `OnDestroyed` 구현 필요 (예: 명중률 패널티).
-   **`BodyPart.cs`**: `PartType.Body`로 지정된 파츠. 이 파츠가 파괴되면 `ArmoredFrame`의 `CheckOperationalStatus` 로직에 의해 기체 전체가 작동 불능 상태가 될 수 있습니다.
-   **`ArmsPart.cs`**: `PartType.Arm`로 지정된 파츠. `OnDestroyed` 구현 필요 (예: 무기 관련 패널티).
-   **`LegsPart.cs`**: `PartType.Legs`로 지정된 파츠. `OnDestroyed` 구현 필요 (예: 이동/회피 패널티).
-   *참고: `BackpackPart.cs`는 현재 폴더에 없지만, `PartType`에는 정의되어 있습니다.* (확인 필요)

### 3. `Frame.cs` (추상 클래스, `Models/Frames/` 하위에 구체 클래스 존재)

-   **역할**: AF의 기본 골격인 프레임의 추상 기본 클래스입니다. 기체의 기본 성능과 파츠 장착 규칙을 결정합니다.
-   **주요 속성**:
    -   `_name`, `_type`: 프레임 이름 및 타입 (`FrameType` enum)
    -   `_baseStats`: 프레임 자체의 스탯 (`Stats` 객체)
    -   `_weight`: 프레임 무게
    -   `_partCompatibility`: 파츠 타입별 호환성 계수 (`Dictionary<PartType, float>`, 레거시 기능으로 보임)
-   **주요 기능**:
    -   호환성 초기화 및 타입별 조정 (`InitializeDefaultCompatibility`, `AdjustCompatibilityByType`)
    -   프레임의 파츠 슬롯 정의 반환 (`GetPartSlots`, 추상 메서드, `PartSlotDefinition` 사용)
    -   특정 슬롯에 파츠 장착 가능 여부 확인 (`CanEquipPart`)
    -   파츠 타입별 호환성 계수 반환 (`GetCompatibilityFactor`)
-   **특징**: 프레임의 기본 특성과 파츠 장착 규칙의 기반을 정의합니다. 구체적인 프레임(예: `LightFrame`, `HeavyFrame`)은 이 클래스를 상속받아 슬롯 정보 등을 구현합니다.

#### 구체적인 프레임 구현 (`Models/Frames/`)

-   **`LightFrame.cs`**: `FrameType.Light`로 지정된 프레임. `GetPartSlots()`를 구현하여 경량 프레임의 파츠 슬롯 구성을 정의합니다 (예: 백팩 슬롯 제외 가능성).
-   **`StandardFrame.cs`**: `FrameType.Standard`로 지정된 프레임. `GetPartSlots()`를 구현하여 표준 프레임의 파츠 슬롯 구성을 정의합니다.
-   **`HeavyFrame.cs`**: `FrameType.Heavy`로 지정된 프레임. `GetPartSlots()`를 구현하여 중량 프레임의 파츠 슬롯 구성을 정의합니다 (예: 백팩 슬롯 포함 가능성).
-   **공통**: 모든 구체 프레임 클래스는 `Frame`의 생성자를 호출하고, `GetPartSlots()` 추상 메서드를 `static readonly Dictionary<string, PartSlotDefinition>`를 반환하도록 구현합니다. 필요시 `CanEquipPart` 등을 재정의하여 타입별 특별 규칙을 적용할 수 있습니다.

### 4. `Weapon.cs`

-   **역할**: AF에 장착 가능한 무기를 나타내는 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_type`, `_damageType`: 무기 이름, 타입(`WeaponType`), 데미지 타입(`DamageType`)
    -   `_damage`, `_accuracy`, `_minRange`, `_maxRange`, `_attackSpeed`: 기본 데미지, 정확도, 최소 사거리, 최대 사거리, 공격 속도
    -   `_overheatPerShot`, `_currentHeat`: 발사 당 과열 증가량, 현재 과열도
    -   `_baseAPCost`: 기본 공격 AP 소모량
    -   `_maxAmmo`, `_currentAmmo`: 최대/현재 탄약 수 (`CurrentAmmo` 프로퍼티로 접근). 0 이하는 무한 탄약.
    -   `_reloadAPCost`: 재장전 AP 소모량 (`ReloadAPCost` 프로퍼티로 접근).
    -   `_reloadTurns`: 재장전 필요 턴 수
    -   `_isReloading`, `_reloadStartTurn`: 재장전 상태 및 시작 턴
    -   `_attackFlavorKey`, `_reloadFlavorKey`: 공격/재장전 시 사용할 Flavor Text 키
    -   `_isOperational`: 무기 작동 가능 여부
    -   `_specialEffects`: 특수 효과 목록 (`List<string>`)
-   **주요 기능**:
    -   ScriptableObject로부터 초기화 (`InitializeFromSO`)
    -   발사 (`Fire`), 냉각 (`Cooldown`)
    -   탄약 관리 (`HasAmmo`, `ConsumeAmmo`)
    -   재장전 관리 (`StartReload`, `FinishReload`, `CheckReloadCompletion`)
    -   데미지 계산 (`CalculateDamage`)
    -   상태 관리 (`Repair`, `DamageWeapon`)
    -   복제 (`Clone`)
-   **특징**: 무기의 상세 스펙과 발사, 재장전, 상태 관리 등 관련 로직을 포함합니다.

### 5. `Pilot.cs`

-   **역할**: AF를 조종하는 파일럿을 나타내는 클래스입니다.
-   **주요 속성**:
    -   `_name`, `_baseStats`: 파일럿 이름 및 기본 스탯 (`Stats` 객체)
    -   `_level`, `_experience`, `_experienceToNextLevel`: 레벨 및 경험치 정보
    -   `_specialization`: 파일럿 전문화 타입 (`SpecializationType` enum)
    -   `_skills`: 보유 스킬 목록 (`List<string>`)
    -   `_specializationBonus`: 전문화에 따른 스탯 보너스 (`Stats` 객체)
-   **주요 기능**:
    -   경험치 획득 및 레벨업 처리 (`GainExperience`, `LevelUp`)
    -   전문화 보너스 계산 (`CalculateSpecializationBonus`)
    -   스킬 추가 (`AddSkill`, `AddRandomSkill`)
    -   총 스탯 계산 (`GetTotalStats`: BaseStats + SpecializationBonus)
-   **특징**: 파일럿의 성장 요소(레벨, 경험치, 스킬)와 전문화에 따른 스탯 보너스를 관리합니다.

### 6. `Stats.cs`

-   **역할**: AF, 파츠, 파일럿 등이 공유하는 기본 스탯 세트를 정의하는 클래스입니다.
-   **주요 속성**: `AttackPower`, `Defense`, `Speed`, `Accuracy`, `Evasion`, `Durability`, `EnergyEfficiency`, `MaxAP`, `APRecovery`. **`Durability`는 주로 최대 총 내구도(Max HP)의 개념으로 사용됩니다.**
-   **주요 기능**:
    -   스탯 덧셈 (`operator+`)
    -   스탯 곱셈 (`operator*`, AP 관련 제외)
    -   스탯 수정 적용 (`ApplyModifier`: 덧셈 또는 곱셈 방식 지원)
    -   음수 스탯 방지
    -   문자열 변환 (`ToString`)
-   **특징**: 게임 내 대부분의 객체가 가질 수 있는 핵심 능력치를 구조화하고 관련 연산을 제공합니다. **현재 총 내구도(Current HP)는 이 클래스에서 직접 관리하지 않고, `ArmoredFrame` 레벨에서 파츠들의 `CurrentDurability`를 합산하여 계산합니다.**

## 7. 행동 트리 관련 모델 (`Assets/AF/Scripts/AI/BehaviorTree/`)

이 섹션은 행동 트리(BT) 기반 AI 시스템을 구성하는 주요 모델 클래스들을 설명합니다. 이들은 `ArmoredFrame`의 AI 행동 로직을 정의하고 실행하는 데 사용됩니다. (상세 내용은 `Docs/AI 리팩토링/BehaviorTree.md` 참조)

-   **`BTNode.cs` (추상 클래스)**: 모든 행동 트리 노드의 기본이 되는 추상 클래스입니다. `Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)`라는 핵심 추상 메서드를 정의하며, 이 메서드는 각 노드의 로직을 실행하고 `NodeStatus` (Success, Failure, Running)를 반환합니다.
-   **`CompositeNode.cs` (추상 클래스)**: 여러 자식 노드를 가질 수 있는 복합 노드의 기본 클래스입니다. `SelectorNode` (자식 중 하나라도 Success면 Success 반환)와 `SequenceNode` (모든 자식이 Success여야 Success 반환) 등이 이를 상속합니다.
-   **`DecoratorNode.cs` (추상 클래스)**: 단일 자식 노드를 가지고 그 자식 노드의 실행 결과나 조건을 변경하는 역할을 하는 장식 노드의 기본 클래스입니다.
-   **`ConditionNode.cs` (추상 클래스)**: 특정 조건을 검사하여 `Success` 또는 `Failure`를 반환하는 잎새 노드의 기본 클래스입니다. 예: `IsTargetInRangeNode`, `HasEnoughAPNode`.
-   **`ActionNode.cs` (추상 클래스)**: 특정 행동을 결정하거나 실행하는 잎새 노드의 기본 클래스입니다. 예: `AttackTargetNode`, `MoveToTargetNode`, `ReloadWeaponNode`.
-   **`Blackboard.cs` (클래스)**: 행동 트리 내에서 노드 간 데이터를 공유하고, AI가 최종적으로 결정한 행동과 관련된 정보(예: `DecidedActionType`, `CurrentTarget`, `SelectedWeapon`, `WeaponToReload`, `IntendedMovePosition`)를 저장하는 데 사용되는 데이터 컨테이너 클래스입니다. 각 `ArmoredFrame`은 `AICtxBlackboard`라는 이름으로 자신만의 `Blackboard` 인스턴스를 가집니다.

## 보조 모델 및 열거형

-   **`ArmoredFrameSnapshot.cs`**: 특정 시점의 `ArmoredFrame` 상태를 저장하는 구조체입니다. 이름, 위치, 팀, AP, 내구도, 스탯, 파츠 및 무기 상태 스냅샷(`PartSnapshot`, `WeaponSnapshot`)을 포함합니다. 주로 전투 로그 재생 시 사용됩니다.
-   **`PartSnapshot.cs`**: 파츠의 상태 스냅샷 구조체 (이름, 내구도, 작동 여부).
-   **`WeaponSnapshot.cs`**: 무기의 상태 스냅샷 구조체 (이름, 탄약, 작동 여부).
-   **`StatusEffect.cs`**: 상태 효과(버프/디버프) 정보를 나타내는 클래스입니다. 효과 이름, 지속 턴, 효과 타입(`StatusEffectEvents.StatusEffectType`), 스탯 변경 정보(`StatToModify`, `ModificationType`, `ModificationValue`), 틱 효과 정보(`TickEffectType`, `TickValue`)를 포함합니다.
-   **`PartSlotDefinition.cs`**: `Frame`에서 사용하는 클래스로, 파츠 슬롯의 식별자와 해당 슬롯에 장착 가능한 파츠 타입(`RequiredPartType`)을 정의합니다.

-   **열거형 (Enums)**:
    -   `PartType.cs`: 파츠 종류 (Frame, Body, Head, Arm, Legs, Backpack)
    -   `FrameType.cs`: 프레임 종류 (Light, Standard, Heavy)
    -   `WeaponType.cs`: 무기 종류 (Melee, MidRange, LongRange)
    -   `DamageType.cs`: 데미지 종류 (Physical, Energy, Explosive, Piercing, Electric)
    -   `SpecializationType.cs`: 파일럿 전문화 종류 (StandardCombat, MeleeCombat, RangedCombat, Defense, Support, Engineering, Evasion)
    -   `StatType.cs`: 스탯 종류 (AttackPower, Defense, ... MaxAP, APRecovery)
    -   `ModificationType.cs`: 스탯 변경 방식 (None, Additive, Multiplicative)
    -   `TickEffectType.cs`: 턴 기반 효과 종류 (None, DamageOverTime, HealOverTime)

## 시스템 구조 요약

1.  게임의 핵심 유닛은 `ArmoredFrame` 객체로 표현됩니다.
2.  `ArmoredFrame`은 하나의 `Frame`을 기반으로 하며, `Frame`은 기본 스탯과 파츠 장착 슬롯 규칙을 정의합니다.
3.  `Frame`의 슬롯에는 해당 타입의 `Part` 객체를 장착할 수 있습니다.
4.  `ArmoredFrame`에는 여러 `Weapon` 객체를 장착할 수 있습니다.
5.  `ArmoredFrame`은 `Pilot` 객체에 의해 조종됩니다.
6.  **AI 유닛의 경우, `ArmoredFrame`은 `BehaviorTreeRoot` (행동 트리의 루트 노드)와 `AICtxBlackboard` (행동 트리용 데이터 저장소)를 가집니다. 이들을 통해 AI 행동이 결정됩니다.**
7.  `Stats` 클래스는 다양한 객체의 능력치를 표현하며, `CombatContext` **클래스**는 전투 관련 정보를 묶어 전달합니다.
8.  전투 중 `ArmoredFrame`의 상태는 `StatusEffect`에 의해 변경될 수 있으며, `ArmoredFrameSnapshot`을 통해 상태를 기록할 수 있습니다.
9.  각종 열거형은 모델들의 종류와 특성을 구분하는 데 사용됩니다.

## 결론

`Assets/AF/Scripts/Models` 디렉토리는 Armored Frame 게임의 핵심 데이터 구조를 정의합니다. `ArmoredFrame` 클래스를 중심으로 다양한 구성 요소를 조합하여 게임 유닛을 표현하며, **이제 행동 트리 시스템을 위한 `BTNode` 및 `Blackboard`와 같은 AI 관련 모델도 이 생태계의 중요한 부분을 차지합니다.** `Stats` 클래스는 능력치 시스템의 기반을 제공하며, 관련 열거형들은 게임 내 다양한 요소들을 분류하고 정의하는 데 중요한 역할을 합니다. 이 모델들은 게임 로직의 다른 부분(전투, UI, 데이터 관리 등)에서 사용될 데이터의 청사진을 제공합니다. 