# Models 스크립트 (AF/Scripts/Models)

> SASHA-Unity 프로젝트의 데이터 모델 관련 핵심 C# 스크립트(.cs 파일) 문서입니다. 아머드 프레임, 파츠, 무기, 어빌리티 및 관련 데이터를 정의합니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/Models`
  - 다양한 데이터 모델의 기본 클래스, 공통 데이터 구조 및 정의 파일들이 있습니다.
  - `ArmoredFrame.cs`: 게임의 주요 유닛인 아머드 프레임의 전체 구조와 로직을 구현한 클래스입니다. 이름, 기본 프레임, 장착된 파츠/무기 목록, 통합 스탯(`Stats`), 현재 상태(`IsOperational`), 위치, 팀 ID, 파일럿(`Pilot`), 활성 상태 효과 목록(`StatusEffect`), 현재 AP, 총 무게, 현재 수리 횟수, 실드(`CurrentShieldHP`) 등의 멤버를 가집니다. 파츠/무기 장착/해제, 스탯 재계산, 데미지 적용, AP 관리, 수리, 상태 효과 관리 등 아머드 프레임의 핵심 기능을 제공합니다. AI 행동 트리 및 블랙보드, 전투 컨텍스트에 대한 참조도 포함합니다.
  - `ArmoredFrameSnapshot.cs`: 전투 로그 재생 시 UI 동기화를 위해 사용되는 아머드 프레임, 파츠(`PartSnapshot`), 무기(`WeaponSnapshot`)의 특정 시점 상태 스냅샷 구조체들을 정의합니다. 각 스냅샷은 이름, 내구도/탄약, 작동 상태 등 핵심 정보를 포함하며, `ArmoredFrameSnapshot`는 파츠/무기 스냅샷 목록과 함께 통합 스탯, AP, 위치, 팀 ID, 작동 상태 등을 담습니다.
  - `Part.cs`: 아머드 프레임을 구성하는 모든 파츠의 기본 추상 클래스입니다. 이름, 타입(`PartType`), 스탯(`Stats`), 무게, 현재/최대 내구도, 작동 가능 여부(`IsOperational`), 특수 능력 목록(`Abilities`) 등의 멤버를 가집니다. 내구도 설정, 데미지 적용, 특수 능력 추가 기능을 제공하며, 파괴 시 로직(`OnDestroyed`)은 하위 클래스에서 구현됩니다.
  - `Frame.cs`: ArmoredFrame의 기본 골격인 프레임의 추상 기본 클래스입니다. `PartSlotDefinition` 클래스를 통해 슬롯 정의(식별자, 요구 파츠 타입)를 포함하며, 이름, 타입(`FrameType`), 기본 스탯(`Stats`), 무게, 파츠 타입별 호환성 정보 등을 가집니다. `GetPartSlots` 추상 메서드로 구체적인 슬롯 정의를 제공하고, `CanEquipPart` 가상 메서드로 파츠 장착 가능 여부를 판단합니다.
  - `Pilot.cs`: ArmoredFrame을 조종하는 파일럿의 데이터 모델 클래스입니다. 이름, 기본 스탯(`Stats`), 레벨, 경험치, 다음 레벨업 필요 경험치, 전문화 타입(`SpecializationType`), 스킬 목록(`Skills`), 전문화에 따른 스탯 보정치 등을 가집니다. 경험치 획득 및 레벨업, 전문화 보너스 계산, 스킬 추가 등의 로직을 포함합니다.
  - `Stats.cs`: ArmoredFrame, Part, Pilot 등의 기본 스탯(공격력, 방어력, 속도, 정확도, 회피율, 내구도, 에너지 효율, 최대 AP, AP 회복량, 최대 수리 횟수)을 정의하는 클래스입니다. 스탯 간 덧셈/곱셈 연산자 오버로딩 및 `ApplyModifier` 메서드를 통해 스탯 값을 수정하는 기능을 제공합니다.
  - `StatusEffect.cs`: 전투 중 발생하는 상태 효과 정보를 나타내는 클래스입니다. 효과 이름, 지속 턴 수, 효과 타입(`StatusEffectEvents.StatusEffectType`), 스탯 변경 정보(`StatType`, `ModificationType`, `ModificationValue`), 틱 효과 정보(`TickEffectType`, `TickValue`) 등을 가집니다.
  - `Weapon.cs`: ArmoredFrame에 장착 가능한 무기의 데이터와 로직을 정의하는 클래스입니다. 이름, 타입(`WeaponType`), 데미지 타입(`DamageType`), 데미지, 정확도, 사거리, 공격 속도, AP 소모량, 탄약, 재장전, 과열, 특수 효과, Flavor Text 키, 작동 상태(`IsOperational`), 무게 등의 멤버를 가집니다. 탄약/재장전 관리, 과열 처리 등의 로직을 포함하며 `WeaponSO` 데이터로 초기화 가능합니다.

  - `AbilityEffectType.cs`: 어빌리티가 발생시키는 주요 효과의 종류(StatModifier, ApplyStatusEffect, DirectDamage, DirectHeal, SpawnObject, SpecialAction, Composite, ControlAbilityUsage)를 정의하는 enum 파일입니다.
  - `AbilityTargetType.cs`: 어빌리티가 영향을 미치는 대상의 유형(None, Self, EnemyUnit, AllyUnit, EnemyPart, AllyPart, Position, AoE_EnemyUnits, AoE_AllyUnits, AoE_AllUnits)을 정의하는 enum 파일입니다.
  - `AbilityType.cs`: 어빌리티의 기본적인 발동 유형(Passive, Active, Triggered)을 정의하는 enum 파일입니다.
  - `DamageType.cs`: 무기의 데미지 타입(Physical, Energy, Explosive, Piercing, Electric)을 정의하는 enum 파일입니다.
  - `FrameType.cs`: ArmoredFrame의 기본 프레임 타입(Light, Standard, Heavy)을 정의하는 enum 파일입니다.
  - `ModificationType.cs`: 스탯 변경 방식(None, Additive, Multiplicative)을 정의하는 enum 파일입니다.
  - `PartType.cs`: ArmoredFrame을 구성하는 파츠의 타입(Frame, Body, Head, Arm, Legs, Backpack)을 정의하는 enum 파일입니다.
  - `SpecializationType.cs`: 파일럿의 전문화 유형(StandardCombat, MeleeCombat, RangedCombat, Defense, Support, Engineering, Evasion)을 정의하는 enum 파일입니다.
  - `StatType.cs`: 상태 효과가 영향을 미치는 스탯의 종류(AttackPower, Defense, Speed, Accuracy, Evasion, Durability, EnergyEfficiency, MaxAP, APRecovery, MaxRepairUses)를 정의하는 enum 파일입니다.
  - `TickEffectType.cs`: 턴마다 발생하는 효과의 종류(None, DamageOverTime, HealOverTime)를 정의하는 enum 파일입니다.
  - `WeaponType.cs`: 무기의 기본 타입(Melee, MidRange, LongRange)을 정의하는 enum 파일입니다.

  - `/Abilities`
    - 특정 어빌리티의 실행 로직 스크립트들이 있습니다.
    - `AbilityDatabase.cs`: 런타임에서 AbilityID를 키로 `AbilitySO` ScriptableObject를 빠르게 조회하기 위한 정적 클래스입니다. `Resources/Abilities` 폴더의 모든 `AbilitySO`를 로드하여 캐싱하고, `TryGetAbility` 메서드로 조회 기능을 제공합니다.
    - `AbilityEffectRegistry.cs`: AbilityID와 해당 어빌리티 효과 실행기(`IAbilityEffectExecutor`) 인스턴스를 매핑하여 관리하는 정적 클래스입니다. `TryGetExecutor` 메서드로 특정 AbilityID에 대한 실행기를 가져올 수 있습니다.
    - `IAbilityEffectExecutor.cs`: 어빌리티 효과 실행기들이 구현해야 하는 인터페이스입니다. 실제 어빌리티 효과를 실행하는 `Execute` 메서드와 실행 가능 여부를 판단하는 `CanExecute` 메서드를 정의합니다.
    - `AnalyzeAbilityExecutor.cs`: "AB_HD_002_Analyze" 어빌리티 실행기입니다. 대상 적 유닛에게 2턴 동안 방어력 20% 감소 디버프(`ArmorBreakDebuff`)를 부여합니다.
    - `APBoostAbilityExecutor.cs`: "AB_BP_003_APBoost" 어빌리티 실행기입니다. 시전자 자신에게 MaxAP +1, APRecovery +2의 영구 버프(`APBoostPassive`, `APBoostRecovery`)를 부여합니다.
    - `EnergyShieldAbilityExecutor.cs`: "AB_BP_002_EnergyShield" 어빌리티 실행기입니다. 시전자 자신에게 3턴 동안 100 HP를 흡수하는 실드 버프(`EnergyShield`)를 부여합니다. (데미지 흡수 로직은 TODO)
    - `EvasiveAbilityExecutor.cs`: "AB_LG_001_Evasive" 어빌리티 실행기입니다. 시전자 자신에게 영구적으로 회피율 +10% 버프(`EvasivePassive`)를 부여합니다.
    - `HoverAbilityExecutor.cs`: "AB_LG_002_Hover" 어빌리티 실행기입니다. 시전자 자신에게 영구적으로 Speed +1 버프(`HoverPassive`)를 부여합니다. (지형 페널티 무시 특성은 TODO)
    - `RepairKitAbilityExecutor.cs`: "AB_BP_004_RepairKit" 어빌리티 실행기입니다. 시전자 자신의 가장 손상된 파츠를 50만큼 수리합니다. 사용 제한(3회)이 있으며, 체력이 50% 미만이고 손상된 파츠가 있을 때 사용 가능합니다.
    - `RepairUnitAbilityExecutor.cs`: "AB_BP_001_RepairUnit" 어빌리티 실행기입니다. 선택한 아군 유닛의 가장 손상된 작동 가능한 파츠를 25만큼 수리합니다. 수리 가능한 파츠가 있을 때 사용 가능합니다.
    - `SelfRepairAbilityExecutor.cs`: "AB_BD_001_SelfRepair" 어빌리티 실행기입니다. 시전자 자신의 Body 파츠 내구도를 5 회복시킵니다. (매 턴 회복 로직은 별도 처리 필요)
    - `ZoomAbilityExecutor.cs`: "AB_HD_001_Zoom" 어빌리티 실행기입니다. 시전자 자신에게 1턴 동안 정확도 30%p 증가 버프(`ZoomBuff`)를 부여합니다. 중복 불가하며, 주무기 사용이 가능하고 AP가 충분할 때 사용 가능합니다.

  - `/Parts`
    - 아머드 프레임을 구성하는 각 파츠(Parts)의 데이터 모델 스크립트들이 있습니다. 이 클래스들은 `Part.cs` 기본 클래스를 상속받아 각 파츠 타입별 특성을 정의합니다.
    - `ArmsPart.cs`: 팔 파츠 클래스입니다. `PartType.Arm`으로 설정됩니다. 파괴 시 무기 명중률 감소 또는 특정 무기 사용 불가 등의 패널티 로직이 추가될 수 있습니다 (`OnDestroyed` TODO).
    - `BackpackPart.cs`: 백팩 파츠 클래스입니다. `PartType.Backpack`으로 설정됩니다. 파괴 시 특수 능력 제거, 폭발 등의 로직이 추가될 수 있습니다 (`OnDestroyed` 구현).
    - `BodyPart.cs`: 몸통 파츠 클래스입니다. `PartType.Body`로 설정됩니다. 파괴 시 기체 작동 불능 처리는 `ArmoredFrame.CheckOperationalStatus`에서 담당합니다 (`OnDestroyed` 오버라이드).
    - `HeadPart.cs`: 머리 파츠 클래스입니다. `PartType.Head`로 설정됩니다. 파괴 시 명중률 감소 등의 패널티 로직이 추가될 수 있습니다 (`OnDestroyed` TODO).
    - `LegsPart.cs`: 다리 파츠 클래스입니다. `PartType.Legs`로 설정됩니다. 파괴 시 이동 속도 감소, 회피율 감소 등의 패널티 로직이 추가될 수 있습니다 (`OnDestroyed` TODO).

  - `/Frames`
    - 아머드 프레임의 종류별 데이터 모델 스크립트들이 있습니다. 이 클래스들은 `Frame.cs` 기본 클래스를 상속받아 각 프레임 타입별 특성과 파츠 슬롯 정의를 구현합니다. 현재는 세 프레임 타입 모두 동일한 파츠 슬롯(`Head`, `Body`, `Arm_Left`, `Arm_Right`, `Legs`, `Backpack`)을 제공합니다.
    - `HeavyFrame.cs`: 중량 프레임 클래스입니다. `FrameType.Heavy`로 설정되며, 중량 프레임의 파츠 슬롯 정의를 제공합니다.
    - `LightFrame.cs`: 경량 프레임 클래스입니다. `FrameType.Light`로 설정되며, 경량 프레임의 파츠 슬롯 정의를 제공합니다.
    - `StandardFrame.cs`: 표준형 프레임 클래스입니다. `FrameType.Standard`로 설정되며, 표준형 프레임의 파츠 슬롯 정의를 제공합니다.


</rewritten_file> 