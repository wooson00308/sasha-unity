# 인수인계 문서 5: 열거형 타입 (Enums)

이 문서는 AF 프로젝트의 Models 폴더 내에서 사용되는 주요 열거형(Enum) 타입들을 요약합니다. 이 Enum들은 게임 내 다양한 카테고리와 상태를 명확하게 구분하고 관리하는 데 사용됩니다.

## 1. DamageType.cs

- **경로**: `Assets/AF/Scripts/Models/DamageType.cs`
- **역할**: 무기나 특정 효과가 가하는 데미지의 속성 타입을 정의합니다.
- **멤버**:
    - `Physical`: 표준 물리 데미지
    - `Energy`: 에너지 기반 데미지
    - `Explosive`: 광역 폭발 데미지
    - `Piercing`: 방어력 일부 무시 데미지
    - `Electric`: 전기 데미지 (특수 효과 가능성)

## 2. FrameType.cs

- **경로**: `Assets/AF/Scripts/Models/FrameType.cs`
- **역할**: ArmoredFrame의 기본 골격인 프레임의 종류를 구분합니다.
- **멤버**:
    - `Light`: 경량 프레임 (속도/회피 특화)
    - `Standard`: 표준 프레임 (균형 잡힌 성능)
    - `Heavy`: 중량 프레임 (내구성/화력 특화)

## 3. ModificationType.cs

- **경로**: `Assets/AF/Scripts/Models/ModificationType.cs`
- **역할**: 스탯 값에 변경을 가할 때 사용되는 연산 방식을 정의합니다.
- **멤버**:
    - `None`: 변경 없음
    - `Additive`: 덧셈 방식 (값 더하기)
    - `Multiplicative`: 곱셈 방식 (값 곱하기)

## 4. PartType.cs

- **경로**: `Assets/AF/Scripts/Models/PartType.cs`
- **역할**: ArmoredFrame을 구성하는 개별 파츠의 종류를 구분합니다.
- **멤버**:
    - `Frame`: 기본 골격
    - `Body`: 동체
    - `Head`: 머리
    - `Arm`: 팔
    - `Legs`: 다리
    - `Backpack`: 백팩

## 5. SpecializationType.cs

- **경로**: `Assets/AF/Scripts/Models/SpecializationType.cs`
- **역할**: 파일럿의 전문화된 전투 스타일이나 역할을 정의합니다.
- **멤버**:
    - `StandardCombat`: 표준 전투
    - `MeleeCombat`: 근접 전투
    - `RangedCombat`: 원거리 전투
    - `Defense`: 방어 전문
    - `Support`: 지원 전문
    - `Engineering`: 기계 공학 전문
    - `Evasion`: 회피 전문

## 6. StatType.cs

- **경로**: `Assets/AF/Scripts/Models/StatType.cs`
- **역할**: 상태 효과나 기타 시스템에서 참조하거나 변경할 수 있는 구체적인 스탯의 종류를 정의합니다.
- **멤버**:
    - `None`: 해당 없음
    - `AttackPower`: 공격력
    - `Defense`: 방어력
    - `Speed`: 속도
    - `Accuracy`: 정확도
    - `Evasion`: 회피율
    - `Durability`: 내구도
    - `EnergyEfficiency`: 에너지 효율
    - `MaxAP`: 최대 행동력
    - `APRecovery`: 턴당 행동력 회복량

## 7. TickEffectType.cs

- **경로**: `Assets/AF/Scripts/Models/TickEffectType.cs`
- **역할**: 상태 효과 등에서 매 턴마다 발생하는 지속적인 효과의 종류를 정의합니다.
- **멤버**:
    - `None`: 효과 없음
    - `DamageOverTime`: 지속 데미지
    - `HealOverTime`: 지속 회복

## 8. WeaponType.cs

- **경로**: `Assets/AF/Scripts/Models/WeaponType.cs`
- **역할**: 무기의 기본적인 교전 거리나 작동 방식을 구분합니다.
- **멤버**:
    - `Melee`: 근접 무기
    - `MidRange`: 중거리 무기
    - `LongRange`: 원거리 무기

---

*이 문서는 각 Enum 타입의 목적과 멤버를 요약한 것으로, 실제 사용 방식은 관련 클래스 코드를 참조해야 합니다.* 