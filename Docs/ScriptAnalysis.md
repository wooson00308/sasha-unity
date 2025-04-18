# 스크립트 분석 노트

이 문서는 AF 프로젝트의 주요 스크립트 분석 결과를 기록합니다.

## AF.Models.Stats

**파일 경로:** `Assets/AF/Scripts/Models/Stats.cs`

**주요 목적:** ArmoredFrame, Part, Pilot 등의 기본 스탯 정의

**정의된 스탯:**

*   `AttackPower`: float - 공격력 계수
*   `Defense`: float - 방어력 (데미지 감소)
*   `Speed`: float - 속도 (이동/행동 주기)
*   `Accuracy`: float - 정확도 (명중률)
*   `Evasion`: float - 회피율 (회피 확률)
*   `Durability`: float - 내구도 (최대 체력)
*   `EnergyEfficiency`: float - 에너지 효율 (사용량/충전)
*   `MaxAP`: float - 최대 행동력
*   `APRecovery`: float - 턴당 행동력 회복량

**주요 특징:**

*   `[Serializable]` 속성으로 인스펙터 편집 가능.
*   기본 생성자(`Stats()`)는 스탯을 0으로 초기화 (EnergyEfficiency는 1).
*   모든 스탯 값을 받는 생성자(`Stats(...)`) 존재.
*   덧셈(`+`), 곱셈(`*`) 연산자 오버로딩 (단, AP 관련 스탯은 곱셈 미적용 - TODO 확인 필요).
*   `ApplyModifier(StatType, ModificationType, float)` 메서드로 스탯 수정 가능 (버프/디버프).
*   음수 스탯 방지 처리 및 최소값 보장 (MaxAP >= 1, APRecovery >= 0).

**엑셀 구성 시사점:**

*   위에 나열된 9가지 스탯을 엑셀 컬럼으로 포함.
*   각 요소(프레임, 파츠 등)가 기여하지 않는 스탯은 0 또는 빈 칸 처리.

## AF.Models.Frame

**파일 경로:** `Assets/AF/Scripts/Models/Frame.cs`

**주요 목적:** ArmoredFrame의 기본 골격인 프레임의 추상 기본 클래스 정의

**관련 클래스:**
*   `PartSlotDefinition`: 프레임의 파츠 장착 슬롯 정의 (SlotIdentifier, RequiredPartType)

**주요 속성 (Frame):**

*   `_name`: string - 프레임 이름
*   `_type`: FrameType enum - 프레임 타입 (Light, Standard, Heavy 등)
*   `_baseStats`: Stats - 프레임 자체 기본 스탯
*   `_weight`: float - 프레임 자체 무게
*   `_partCompatibility`: Dictionary<PartType, float> - 파츠 타입별 호환성 계수 (타입별로 자동 조정됨)

**주요 메서드 (Frame):**

*   `Frame(...)`: 생성자 (이름, 타입, 스탯, 무게 등 초기화)
*   `GetPartSlots()`: **abstract** - 해당 프레임의 파츠 슬롯 목록(Dictionary<string, PartSlotDefinition>) 반환 (자식 클래스 구현 필수)
*   `CanEquipPart(Part, string)`: **virtual** - 특정 슬롯에 파츠 장착 가능 여부 확인 (기본: 타입 일치, 추가 규칙 가능)
*   `GetCompatibilityFactor(PartType)`: **virtual** - 특정 파츠 타입과의 호환성 계수 반환

**엑셀 구성 시사점:**

*   **`Frames` 시트:** `FrameID`, `FrameName`, `FrameType`, 9가지 기본 스탯 컬럼, `FrameWeight` 필요.
*   **호환성(`PartCompatibility`)**: 코드 내 로직(또는 별도 설정 파일)으로 관리될 가능성 높아 엑셀에서는 제외 고려.
*   **슬롯 정보(`GetPartSlots`)**: 프레임별 슬롯 구성이 다르다면, `AF_Assemblies` 시트에서 각 슬롯 ID에 맞는 파츠 ID를 지정하거나, 별도 `FrameSlots` 시트 등으로 관리 필요.

## AF.Models.Part (및 파생 클래스)

**파일 경로:** `Assets/AF/Scripts/Models/Part.cs` (및 `Parts/` 하위 파일들)

**주요 목적:** ArmoredFrame을 구성하는 개별 파츠(머리, 몸통, 팔, 다리 등)의 기본 구조 및 구체적인 타입 정의

**주요 클래스:**
*   `Part`: 모든 파츠의 **추상** 베이스 클래스
*   `HeadPart`, `BodyPart`, `ArmsPart`, `LegsPart`: `Part`를 상속받는 구체적인 파츠 클래스들

**주요 속성 (Part):**

*   `_name`: string - 파츠 이름
*   `_type`: PartType enum - 파츠 종류 (Head, Body, Arm, Legs, Backpack 등)
*   `_stats`: Stats - 파츠가 제공하는 스탯
*   `_weight`: float - 파츠 자체 무게
*   `_currentDurability`: float - 현재 내구도
*   `_maxDurability`: float - 최대 내구도 (파츠 자체 체력)
*   `_isOperational`: bool - 현재 작동 가능 여부 (내구도 0 이하면 false)
*   `_abilities`: List<string> - 특수 능력 목록

**주요 메서드 (Part):**

*   `Part(...)`: 생성자 (이름, 타입, 스탯, 내구도, 무게 등 초기화)
*   `ApplyDamage(float)`: **virtual** - 데미지 적용 및 파괴 시 true 반환
*   `Repair(float)`: **virtual** - 내구도 회복
*   `AddAbility(string)`: 특수 능력 추가
*   `OnDestroyed(ArmoredFrame)`: **abstract** - 파츠 파괴 시 효과 (자식 클래스 구현 필수)

**파생 클래스 특징:**

*   각자 고유한 `PartType`을 생성자에서 지정.
*   `OnDestroyed` 메서드를 구현하여 파괴 시 페널티 로직(주석 또는 로그 형태) 정의 (예: Head 파괴 -> 명중률 저하, Body 파괴 -> 기체 작동 불능).

**엑셀 구성 시사점:**

*   **`Parts` 시트:** `PartID`, `PartName`, `PartType`, 9가지 스탯 컬럼, `MaxDurability`, `PartWeight`, `Abilities` 컬럼 필요.
*   **`AF_Assemblies` 시트:** 각 프레임 슬롯 식별자에 맞춰 `PartID`를 지정하여 조립 구성.

## AF.Models.Weapon

**파일 경로:** `Assets/AF/Scripts/Models/Weapon.cs`

**주요 목적:** ArmoredFrame에 장착 가능한 무기 정의

**주요 속성:**

*   `_name`: string - 무기 이름
*   `_type`: WeaponType enum - 무기 타입 (Melee, MidRange, LongRange)
*   `_damageType`: DamageType enum - 데미지 타입 (Physical, Energy, Explosive, Piercing, Electric)
*   `_damage`: float - 기본 데미지
*   `_accuracy`: float - 기본 정확도 (0.0 ~ 1.0)
*   `_range`: float - 사거리
*   `_attackSpeed`: float - 공격 속도 (초당 공격 횟수)
*   `_overheatPerShot`: float - 발사당 과열도 증가량
*   `_baseAPCost`: float - 기본 AP 소모량
*   `_currentHeat`: float - 현재 과열도 (0.0 ~ 1.0)
*   `_specialEffects`: List<string> - 특수 효과 목록
*   `_isOperational`: bool - 현재 작동 가능 여부

**주요 메서드:**

*   `Weapon(...)`: 생성자 (다양한 속성 초기화, AP 소모량 포함 버전 존재)
*   `Fire()`: 발사 시도 (과열 체크 및 과열도 증가)
*   `Cooldown(float)`: 과열도 감소
*   `AddSpecialEffect(string)`: 특수 효과 추가
*   `Repair()`: 작동 가능 상태로 복구 및 과열도 초기화
*   `DamageWeapon()`: 작동 불능 상태로 변경
*   `CalculateDamage(float, float)`: 명중 여부 판정 (무기/공격자 정확도, 대상 회피 고려) 후 명중 시 기본 데미지 반환, 빗나가면 0 반환.

**엑셀 구성 시사점:**

*   **`Weapons` 시트:** `WeaponID`, `WeaponName`, `WeaponType`, `DamageType`, `BaseDamage`, `Accuracy`, `Range`, `AttackSpeed`, `OverheatPerShot`, `BaseAPCost`, `SpecialEffects` 컬럼 필요.
*   **`AF_Assemblies` 시트:** 무기 슬롯 식별자에 맞춰 `WeaponID` 지정.

## AF.Models.Pilot

**파일 경로:** `Assets/AF/Scripts/Models/Pilot.cs`

**주요 목적:** ArmoredFrame을 조종하는 파일럿 정의

**주요 속성:**

*   `_name`: string - 파일럿 이름
*   `_baseStats`: Stats - 파일럿 기본 스탯
*   `_level`: int - 현재 레벨
*   `_experience`: int - 현재 경험치
*   `_experienceToNextLevel`: int - 다음 레벨 필요 경험치
*   `_specialization`: SpecializationType enum - 전문화 타입 (Combat, Defense, Support, Engineering)
*   `_skills`: List<string> - 습득한 스킬 목록
*   `_specializationBonus`: Stats - 전문화 타입에 따른 스탯 보너스 (자동 계산됨)

**주요 메서드:**

*   `Pilot(...)`: 생성자 (이름, 기본 스탯, 전문화 타입 초기화)
*   `GainExperience(int)`: 경험치 획득 및 레벨업 처리
*   `LevelUp()`: 레벨 증가, 스탯 상승 (기본 스탯 * 1.1), 필요 경험치 재계산, 스킬 추가 (3레벨마다)
*   `CalculateSpecializationBonus()`: 전문화 타입에 따른 스탯 보너스 계산 및 반환
*   `AddSkill(string)` / `AddRandomSkill()`: 스킬 추가
*   `GetTotalStats()`: 기본 스탯 + 전문화 보너스 합산 반환

**엑셀 구성 시사점:**

*   **`Pilots` 시트:** `PilotID`, `PilotName`, 9가지 기본 스탯 컬럼, `Specialization` 컬럼 필요.
*   **추가 가능 컬럼:** `InitialLevel`, `InitialSkills`.
*   **코드 관리:** 레벨업 로직(경험치 요구량, 스탯 증가율, 스킬 획득 조건)은 코드에서 관리하는 것이 적합.
*   **`AF_Assemblies` 시트:** 배정할 `PilotID` 지정 필요. 

## AF.Tests.CombatTestRunner (CreateTestArmoredFrame 분석)

**파일 경로:** `Assets/AF/Scripts/Tests/CombatTestRunner.cs`

**주요 목적:** 테스트용 ArmoredFrame 인스턴스를 하드코딩 방식으로 생성하는 로직 확인

**하드코딩 방식 요약:**

1.  **프레임 생성:** `frameType`(Light/Heavy) 따라 `new Stats()`, `frameWeight` 직접 지정 후 `new StandardFrame()` 호출.
2.  **파일럿 생성:** `frameType` 따라 `new Stats()` 직접 지정 후 `new Pilot()` 호출.
3.  **파츠 스탯/무게 정의:** `frameType` 따라 각 파츠 타입(Head, Body, Arm, Legs)별 `Stats` 객체 및 `weight` 변수 직접 생성/지정.
4.  **파츠 생성/부착:** 프레임의 `GetPartSlots()` 정보 기반, `switch`문으로 파츠 타입 확인 후 앞에서 정의한 스탯/무게 사용하여 `new HeadPart()`, `new BodyPart()` 등 호출 및 `AttachPart()`.
5.  **무기 생성/부착:** `frameType` 따라 `new Weapon()` (모든 속성 직접 지정) 호출 및 `AttachWeapon()`.

**엑셀 구성 시사점:**

*   이 메서드는 **`AF_Assemblies` 시트** 구성의 핵심 참고 자료.
*   `AF_Assemblies` 시트에는 `AssemblyID`, `AFName`, `TeamID` 외에 아래 **ID 참조 컬럼들**이 필요:
    *   `FrameID`
    *   `PilotID`
    *   `HeadPartID`, `BodyPartID`, `LeftArmPartID`, `RightArmPartID`, `LegsPartID` (등 슬롯별 파츠 ID)
    *   `Weapon1ID` (등 슬롯별 무기 ID)
*   엑셀 로더는 `AF_Assemblies`를 읽고, 각 ID로 다른 시트(`Frames`, `Pilots`, `Parts`, `Weapons`)를 조회하여 데이터를 조합, 최종 `ArmoredFrame` 객체를 생성해야 함. 

## ExcelToSO 시스템 분석

**폴더 경로:** `Assets/ExcelToSO/Scripts/`

**주요 목적:** `.xlsx` 엑셀 파일 데이터를 읽어 Unity `ScriptableObject` 에셋으로 자동 생성하는 에디터 유틸리티

**구성 요소:**

1.  **`ExcelParser.cs`**
    *   NPOI 라이브러리를 사용하여 `.xlsx` 파일의 첫 번째 시트를 읽음.
    *   `Parse<T>(excelPath)`: 엑셀 행들을 `IExcelRow`를 구현한 `T` 타입 객체 리스트로 변환하여 반환.
    *   실제 데이터 파싱은 `T` 타입의 `FromExcelRow(IRow row)` 메서드에 위임.

2.  **`IExcelRow.cs`**
    *   `interface IExcelRow`: 엑셀 행 데이터로부터 객체 필드를 채우는 규약을 정의.
    *   `void FromExcelRow(IRow row)`: 구현 클래스는 NPOI의 `IRow`를 받아 파싱 로직을 구현해야 함.

3.  **`ScriptableObjectGenerator.cs`**
    *   `Generate<TModel, TSO>(excelPath, savePath)`: 파싱된 데이터(`TModel` 리스트)를 `TSO` 타입 ScriptableObject 에셋으로 생성.
    *   `TModel`: `IExcelRow` 구현 데이터 모델 클래스.
    *   `TSO`: `ScriptableObject` 상속 클래스.
    *   동작:
        *   `ExcelParser.Parse<TModel>` 호출하여 데이터 로드.
        *   각 데이터(`data`)에 대해 `ScriptableObject.CreateInstance<TSO>()` 호출.
        *   `TSO`의 `Apply(TModel data)` 메서드를 리플렉션으로 찾아 호출 (데이터 -> SO 필드 복사).
        *   `TModel`의 `id` 프로퍼티 값을 파일명으로 사용 (`id.asset`).
        *   `AssetDatabase.CreateAsset()`으로 지정된 경로(`savePath`)에 SO 에셋 생성.
        *   `AssetDatabase.SaveAssets()`, `Refresh()` 호출.
    *   **주의:** `UnityEditor` 네임스페이스를 사용하므로 에디터 전용 기능.

**사용 전제 조건:**

*   각 엑셀 시트 구조에 맞는 **데이터 모델 클래스**(`IExcelRow` 구현, `FromExcelRow` 메서드 구현, `id` 프로퍼티 포함)가 필요함.
*   생성할 **ScriptableObject 클래스**(`ScriptableObject` 상속, `Apply(TModel data)` 메서드 구현)가 필요함.

**결론:** `.xlsx` -> `IExcelRow` 모델 -> `ScriptableObject`(.asset) 변환 자동화 도구. 