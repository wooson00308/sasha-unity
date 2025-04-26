# 인수인계 문서 3: 기본 모델 클래스

이 문서는 AF 프로젝트의 핵심 데이터 구조와 게임 개체를 정의하는 Models 폴더 내의 기본 클래스들을 요약합니다.

## 1. Stats.cs

- **경로**: `Assets/AF/Scripts/Models/Stats.cs`
- **역할**: 게임 내 다양한 개체(프레임, 파츠, 파일럿 등)의 스탯 정보를 정의하고 관리하는 클래스입니다.
- **주요 내용**:
    - 기본 스탯 필드 (공격력, 방어력, 속도, 정확도, 회피율, 내구도, 에너지 효율)를 정의합니다.
    - AP(행동력) 관련 스탯 (최대 AP, 턴당 AP 회복량) 필드를 포함합니다.
    - 생성자를 통해 스탯 값을 초기화합니다.
    - `+` 및 `*` 연산자 오버로딩을 통해 Stats 객체 간의 덧셈 및 스칼라 곱셈 연산을 지원합니다. (곱셈 시 AP 관련 스탯 제외)
    - `ApplyModifier()`: 특정 스탯을 지정된 방식(덧셈/곱셈)으로 수정하는 메서드를 제공합니다.
- **목적**: 게임 내 스탯 데이터를 일관된 구조로 관리하고 관련 연산을 용이하게 합니다.

## 2. Frame.cs (추상 클래스)

- **경로**: `Assets/AF/Scripts/Models/Frame.cs`
- **역할**: 모든 프레임 타입(Light, Standard, Heavy)의 기본 구조와 공통 기능을 정의하는 추상 기본 클래스입니다.
- **주요 내용**:
    - 프레임 이름, 타입(`FrameType`), 기본 스탯, 무게 필드를 정의합니다.
    - `PartSlotDefinition` 내부 클래스를 통해 파츠 슬롯의 속성(식별자, 요구 파츠 타입)을 정의합니다.
    - `_partCompatibility` 딕셔너리로 파츠 타입별 호환성 계수를 관리합니다.
    - `GetPartSlots()` (추상): 자식 클래스가 구체적인 슬롯 구성을 반환하도록 강제합니다.
    - `CanEquipPart()` (가상): 특정 슬롯에 파츠 장착 가능 여부를 확인합니다.
    - `GetCompatibilityFactor()` (가상): 특정 파츠 타입에 대한 호환성 계수를 반환합니다.
- **목적**: 다양한 프레임 타입의 공통 기반을 제공하고 확장성을 보장합니다.

## 3. Part.cs (추상 클래스)

- **경로**: `Assets/AF/Scripts/Models/Part.cs`
- **역할**: 모든 파츠(Head, Body, Arm, Legs 등)의 기본 속성과 공통 기능을 정의하는 추상 기본 클래스입니다.
- **주요 내용**:
    - 파츠 이름, 타입(`PartType`), 스탯, 무게, 현재/최대 내구도, 작동 가능 여부 필드를 정의합니다.
    - `_abilities` 리스트로 파츠의 특수 능력을 관리합니다.
    - `ApplyDamage()`: 데미지를 적용하고 파괴 여부를 반환합니다.
    - `Repair()`: 내구도를 회복합니다.
    - `AddAbility()`: 특수 능력을 추가합니다.
    - `OnDestroyed()` (추상): 자식 클래스가 파츠 파괴 시 발생하는 효과를 구현하도록 강제합니다.
- **목적**: 다양한 파츠 타입의 공통 기반을 제공하고 일관된 인터페이스를 유지합니다.

## 4. Weapon.cs

- **경로**: `Assets/AF/Scripts/Models/Weapon.cs`
- **역할**: ArmoredFrame에 장착되는 무기의 속성과 기본 동작을 정의하는 클래스입니다.
- **주요 내용**:
    - 무기 이름, 타입(`WeaponType`), 데미지 타입(`DamageType`), 기본 데미지, 정확도, 사거리, 공격 속도, 발사당 과열도, 기본 AP 소모량 필드를 정의합니다.
    - 현재 과열도, 작동 가능 여부, 특수 효과 리스트를 관리합니다.
    - `Fire()`: 발사 시 과열도를 체크하고 증가시킵니다.
    - `Cooldown()`: 과열도를 감소시킵니다.
    - `AddSpecialEffect()`: 특수 효과를 추가합니다.
    - `CalculateDamage()`: 명중/회피를 고려하여 실제 데미지를 계산합니다.
- **목적**: 게임 내 무기 데이터를 구조화하고 발사, 과열 등 기본적인 무기 메커니즘을 구현합니다.

## 5. StatusEffect.cs

- **경로**: `Assets/AF/Scripts/Models/StatusEffect.cs`
- **역할**: 게임 내 상태 이상 효과(버프, 디버프, 지속 효과 등)의 정보를 정의하는 클래스입니다.
- **주요 내용**:
    - 효과 이름, 지속 턴 수를 정의합니다.
    - 스탯 변경 효과 정보 (변경할 스탯, 변경 방식, 변경 값)를 포함합니다.
    - 틱 기반 효과 정보 (틱 효과 타입, 틱당 값)를 포함합니다.
    - 생성자를 통해 스탯 변경, 틱 효과, 또는 둘 다 가지는 효과를 생성할 수 있습니다.
- **목적**: 다양한 종류의 상태 이상 효과를 일관된 구조로 표현하고 관리합니다.

## 6. Pilot.cs

- **경로**: `Assets/AF/Scripts/Models/Pilot.cs`
- **역할**: ArmoredFrame을 조종하는 파일럿의 정보와 성장 시스템을 정의하는 클래스입니다.
- **주요 내용**:
    - 파일럿 이름, 기본 스탯, 레벨, 경험치, 전문화 타입(`SpecializationType`) 필드를 정의합니다.
    - `_skills` 리스트로 습득한 스킬을 관리합니다.
    - `_specializationBonus`: 전문화에 따른 스탯 보너스를 저장합니다 (`CalculateSpecializationBonus()`에서 계산).
    - `GainExperience()` / `LevelUp()`: 경험치 획득 및 레벨업(스탯 상승, 스킬 획득) 로직을 구현합니다.
    - `AddSkill()`: 스킬을 추가합니다.
    - `GetTotalStats()`: 기본 스탯과 전문화 보너스를 합산한 최종 스탯을 반환합니다.
- **목적**: 파일럿 캐릭터의 데이터를 관리하고 성장 메커니즘을 구현합니다.

## 7. ArmoredFrame.cs

- **경로**: `Assets/AF/Scripts/Models/ArmoredFrame.cs`
- **역할**: 게임의 핵심 유닛인 ArmoredFrame을 나타내는 클래스입니다. 프레임, 파츠, 파일럿, 무기 등을 조합하고 유닛의 상태와 행동을 관리합니다.
- **주요 내용**:
    - 이름, 기본 프레임(`Frame`), 파일럿(`Pilot`), 위치, 팀 ID를 정의합니다.
    - `_parts` 딕셔너리로 장착된 파츠를 관리합니다.
    - `_equippedWeapons` 리스트로 장착된 무기를 관리합니다.
    - `_activeStatusEffects` 리스트로 현재 적용 중인 상태 효과를 관리합니다.
    - `_combinedStats`: 모든 구성 요소의 스탯을 합산한 최종 스탯입니다 (`RecalculateStats()`에서 계산).
    - `_currentAP`: 현재 행동력을 관리합니다.
    - 파츠/무기 장착 및 제거 (`AttachPart`, `DetachPart`, `AttachWeapon`, `DetachWeapon`).
    - 상태 효과 적용/제거/틱 처리 (`AddStatusEffect`, `RemoveStatusEffect`, `TickStatusEffects`).
    - 데미지 적용 및 파괴 판정 (`ApplyDamage`).
    - AP 관리 (`RecoverAPOnTurnStart`, `ConsumeAP`, `HasEnoughAP`).
    - 유닛 작동 가능 여부 확인 (`CheckOperationalStatus`).
- **목적**: AF 유닛의 모든 구성 요소를 통합하고, 게임 내 유닛의 상태 변화와 핵심 메커니즘(스탯 계산, 데미지 처리, AP 관리 등)을 총괄합니다.

---

*이 문서는 각 클래스의 핵심적인 역할과 기능을 요약한 것으로, 상세한 구현은 해당 스크립트 코드를 직접 참조해야 합니다.* 