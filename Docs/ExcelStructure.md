# AF 데이터 엑셀 구조 제안

이 문서는 스크립트 분석 결과를 바탕으로 제안하는 데이터 관리용 엑셀 구조입니다.

**파일:** `AF_Data.xlsx` (예시 파일명)

**시트 구성:**

1.  `Frames`
2.  `Parts`
3.  `Weapons`
4.  `Pilots`
5.  `AF_Assemblies`

---

## 1. `Frames` 시트

AF의 기본 뼈대 정보를 정의합니다.

| 컬럼명           | 타입    | 설명                                               |
| :--------------- | :------ | :------------------------------------------------- |
| `FrameID`        | string  | **(PK)** 프레임 고유 식별자 (예: "FRM_LIGHT_01")   |
| `FrameName`      | string  | 프레임 이름 (예: "경량 테스트 프레임")           |
| `FrameType`      | enum    | 프레임 타입 (Light, Standard, Heavy 등)           |
| `Stat_AttackPower`| float   | 기본 스탯: 공격력                                  |
| `Stat_Defense`   | float   | 기본 스탯: 방어력                                  |
| `Stat_Speed`     | float   | 기본 스탯: 속도                                    |
| `Stat_Accuracy`  | float   | 기본 스탯: 정확도                                  |
| `Stat_Evasion`   | float   | 기본 스탯: 회피율                                  |
| `Stat_Durability`| float   | 기본 스탯: 내구도                                  |
| `Stat_EnergyEff` | float   | 기본 스탯: 에너지 효율 (기본 1)                     |
| `Stat_MaxAP`     | float   | 기본 스탯: 최대 AP                                 |
| `Stat_APRecovery`| float   | 기본 스탯: AP 회복량                             |
| `FrameWeight`    | float   | 프레임 자체 무게                                   |
| `Slot_Head`      | string  | 헤드 슬롯 식별자 (예: "HeadSlot")                |
| `Slot_Body`      | string  | 바디 슬롯 식별자 (예: "BodySlot")                |
| `Slot_Arm_L`     | string  | 좌측 팔 슬롯 식별자 (예: "ArmSlot_L")            |
| `Slot_Arm_R`     | string  | 우측 팔 슬롯 식별자 (예: "ArmSlot_R")            |
| `Slot_Legs`      | string  | 다리 슬롯 식별자 (예: "LegsSlot")                |
| `Slot_Backpack`  | string  | 백팩 슬롯 식별자 (예: "BackpackSlot", 없으면 비워둠)|
| `Slot_Weapon_1`  | string  | 무기 슬롯 1 식별자 (예: "WeaponSlot_R")          |
| `Slot_Weapon_2`  | string  | 무기 슬롯 2 식별자 (예: "WeaponSlot_L", 없으면 비워둠)|
| `Notes`          | string  | 비고 (선택 사항)                                   |

*   **참고:** `PartCompatibility`는 코드 또는 별도 설정 파일 관리를 권장.
*   **참고:** `Slot_` 컬럼들은 `AF_Assemblies` 시트에서 파츠/무기 ID를 매핑할 때 사용될 슬롯의 이름을 정의합니다. 프레임마다 슬롯 구성이 다를 수 있으므로 여기에 명시합니다.

---

## 2. `Parts` 시트

모든 종류의 파츠 정보를 정의합니다.

| 컬럼명           | 타입   | 설명                                                            |
| :--------------- | :----- | :-------------------------------------------------------------- |
| `PartID`         | string | **(PK)** 파츠 고유 식별자 (예: "HEAD_LIGHT_01")                |
| `PartName`       | string | 파츠 이름 (예: "경량 헤드 유닛")                               |
| `PartType`       | enum   | 파츠 타입 (Head, Body, Arm, Legs, Backpack 등)                  |
| `Stat_AttackPower`| float  | 파츠 기여 스탯: 공격력                                          |
| `Stat_Defense`   | float  | 파츠 기여 스탯: 방어력                                          |
| `Stat_Speed`     | float  | 파츠 기여 스탯: 속도                                            |
| `Stat_Accuracy`  | float  | 파츠 기여 스탯: 정확도                                          |
| `Stat_Evasion`   | float  | 파츠 기여 스탯: 회피율                                          |
| `Stat_Durability`| float  | 파츠 기여 스탯: 내구도 (AF 전체 스탯용)                          |
| `Stat_EnergyEff` | float  | 파츠 기여 스탯: 에너지 효율                                     |
| `Stat_MaxAP`     | float  | 파츠 기여 스탯: 최대 AP                                         |
| `Stat_APRecovery`| float  | 파츠 기여 스탯: AP 회복량                                     |
| `MaxDurability`  | float  | 파츠 자체의 최대 내구도 (체력)                                  |
| `PartWeight`     | float  | 파츠 자체 무게                                                  |
| `Abilities`      | string | 특수 능력 목록 (쉼표 구분, 예: "SensorBoost,TargetAnalysis") |
| `Notes`          | string | 비고 (선택 사항)                                                |

---

## 3. `Weapons` 시트

모든 무기 정보를 정의합니다.

| 컬럼명           | 타입   | 설명                                                         |
| :--------------- | :----- | :----------------------------------------------------------- |
| `WeaponID`       | string | **(PK)** 무기 고유 식별자 (예: "WPN_RIFLE_LIGHT_01")     |
| `WeaponName`     | string | 무기 이름 (예: "경량 라이플")                              |
| `WeaponType`     | enum   | 무기 타입 (Melee, MidRange, LongRange)                     |
| `DamageType`     | enum   | 데미지 타입 (Physical, Energy, Explosive, Piercing, Electric)|
| `BaseDamage`     | float  | 기본 데미지                                                |
| `Accuracy`       | float  | 기본 정확도 (0.0 ~ 1.0)                                   |
| `Range`          | float  | 사거리                                                     |
| `AttackSpeed`    | float  | 공격 속도 (초당 공격 횟수)                                 |
| `OverheatPerShot`| float  | 발사당 과열도 증가량 (실탄 무기는 0)                       |
| `AmmoCapacity`   | int    | 탄약 용량 (에너지 무기는 0 또는 비워둠)                      |
| `BaseAPCost`     | float  | 기본 AP 소모량                                             |
| `SpecialEffects` | string | 특수 효과 목록 (쉼표 구분, 예: "Stun,ArmorPiercing")     |
| `Notes`          | string | 비고 (선택 사항)                                             |

---

## 4. `Pilots` 시트

파일럿 정보를 정의합니다.

| 컬럼명           | 타입   | 설명                                                               |
| :--------------- | :----- | :----------------------------------------------------------------- |
| `PilotID`        | string | **(PK)** 파일럿 고유 식별자 (예: "PILOT_ACE_01")                  |
| `PilotName`      | string | 파일럿 이름 (예: "에이스 파일럿")                                  |
| `Stat_AttackPower`| float  | 기본 스탯: 공격력                                                  |
| `Stat_Defense`   | float  | 기본 스탯: 방어력                                                  |
| `Stat_Speed`     | float  | 기본 스탯: 속도                                                    |
| `Stat_Accuracy`  | float  | 기본 스탯: 정확도                                                  |
| `Stat_Evasion`   | float  | 기본 스탯: 회피율                                                  |
| `Stat_Durability`| float  | 기본 스탯: 내구도 (파일럿 자체 스탯이 영향 주는지 확인 필요)       |
| `Stat_EnergyEff` | float  | 기본 스탯: 에너지 효율 (파일럿 자체 스탯이 영향 주는지 확인 필요) |
| `Stat_MaxAP`     | float  | 기본 스탯: 최대 AP                                                 |
| `Stat_APRecovery`| float  | 기본 스탯: AP 회복량                                             |
| `Specialization` | enum   | 전문화 타입 (Combat, Defense, Support, Engineering)                |
| `InitialLevel`   | int    | 초기 레벨 (선택 사항, 기본 1)                                      |
| `InitialSkills`  | string | 초기 보유 스킬 목록 (쉼표 구분, 선택 사항)                          |
| `Notes`          | string | 비고 (선택 사항)                                                   |

*   **참고:** 레벨업 관련 로직(경험치, 스탯 성장률 등)은 코드에서 관리하는 것을 권장합니다.

---

## 5. `AF_Assemblies` 시트

정의된 프레임, 파츠, 무기, 파일럿 ID를 조합하여 완성된 AF 구성을 정의합니다.

| 컬럼명         | 타입   | 설명                                                                     |
| :------------- | :----- | :----------------------------------------------------------------------- |
| `AssemblyID`   | string | **(PK)** 조립 구성 고유 식별자 (예: "AF_TEST_LIGHT_01")             |
| `AFName`       | string | 이 구성으로 생성될 AF 이름 (예: "테스트용 스피드스터 AF")                |
| `TeamID`       | int    | 테스트 또는 게임 내 팀 구분 ID (선택 사항)                             |
| `FrameID`      | string | **(FK)** 사용할 프레임 ID (Frames 시트 참조)                             |
| `PilotID`      | string | **(FK)** 배정할 파일럿 ID (Pilots 시트 참조)                             |
| `HeadPartID`   | string | **(FK)** 헤드 슬롯에 장착할 파츠 ID (Parts 시트 참조)                    |
| `BodyPartID`   | string | **(FK)** 바디 슬롯에 장착할 파츠 ID (Parts 시트 참조)                    |
| `LeftArmPartID`| string | **(FK)** 좌측 팔 슬롯에 장착할 파츠 ID (Parts 시트 참조)                 |
| `RightArmPartID`| string | **(FK)** 우측 팔 슬롯에 장착할 파츠 ID (Parts 시트 참조)                 |
| `LegsPartID`   | string | **(FK)** 다리 슬롯에 장착할 파츠 ID (Parts 시트 참조)                    |
| `BackpackPartID`| string | **(FK)** 백팩 슬롯에 장착할 파츠 ID (Parts 시트 참조, 없으면 비워둠)    |
| `Weapon1ID`    | string | **(FK)** 무기 슬롯 1에 장착할 무기 ID (Weapons 시트 참조)                |
| `Weapon2ID`    | string | **(FK)** 무기 슬롯 2에 장착할 무기 ID (Weapons 시트 참조, 없으면 비워둠) |
| `Notes`        | string | 비고 (선택 사항)                                                         |

*   **참고:** 파츠/무기 ID 컬럼 이름(예: `HeadPartID`)은 `Frames` 시트의 `Slot_` 컬럼에 정의된 슬롯 식별자와 일치하거나, 로더가 매핑할 수 있어야 합니다. 컬럼 수를 줄이고 싶다면, 슬롯ID-PartID 쌍으로 저장하는 다른 구조도 고려할 수 있습니다 (예: `EquippedParts` 컬럼에 "HeadSlot:HEAD_LIGHT_01,BodySlot:BODY_LIGHT_01,..." 형식으로 저장).

---

**구현 제안:**

1.  엑셀 파일을 CSV 또는 JSON 형식으로 변환합니다.
2.  Unity에서 변환된 파일을 파싱하여 각 시트의 데이터를 로드하는 `DataLoader` 클래스를 구현합니다.
3.  로드된 데이터를 `ScriptableObject` 에셋으로 변환하여 Unity 에디터 내에서 관리하거나, 런타임에 직접 사용할 수 있도록 `Dictionary` 등으로 저장합니다.
4.  `CombatTestRunner` 또는 게임 로직에서 `DataLoader`를 통해 `AF_Assemblies` 정보를 읽고, ID를 사용하여 필요한 데이터를 조합하여 `ArmoredFrame` 객체를 생성합니다. 