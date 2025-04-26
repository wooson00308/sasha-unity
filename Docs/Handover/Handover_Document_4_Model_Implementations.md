# 인수인계 문서 4: 모델 구현체

이 문서는 AF 프로젝트의 기본 모델 클래스(`Frame`, `Part`)를 상속받아 구현된 구체적인 프레임 및 파츠 클래스들을 요약합니다.

## 1. 프레임 구현체 (Frame Derivatives)

모든 프레임 구현체는 `Frame` 추상 클래스를 상속받아 특정 `FrameType`을 정의하고, 해당 프레임 타입이 가지는 파츠 슬롯 구성을 `GetPartSlots()` 메서드를 통해 반환합니다. 생성자에서는 이름, 기본 스탯 (및 무게)을 받아 부모 클래스를 초기화합니다.

### 1.1. LightFrame.cs

- **경로**: `Assets/AF/Scripts/Models/Frames/LightFrame.cs`
- **타입**: `FrameType.Light`
- **슬롯 구성**: Head, Body, Arm_Left, Arm_Right, Legs (Backpack 슬롯 없음)
- **특징**: 경량 프레임의 특성을 정의하며, 필요시 `CanEquipPart` 또는 `GetCompatibilityFactor`를 재정의하여 경량 파츠와의 호환성 규칙을 추가할 수 있습니다.

### 1.2. StandardFrame.cs

- **경로**: `Assets/AF/Scripts/Models/Frames/StandardFrame.cs`
- **타입**: `FrameType.Standard`
- **슬롯 구성**: Head, Body, Arm_Left, Arm_Right, Legs (Backpack 슬롯 없음, 일부 슬롯 식별자가 다름: HeadSlot, BodySlot)
- **특징**: 표준형 프레임의 특성을 정의합니다.

### 1.3. HeavyFrame.cs

- **경로**: `Assets/AF/Scripts/Models/Frames/HeavyFrame.cs`
- **타입**: `FrameType.Heavy`
- **슬롯 구성**: Head, Body, Arm_Left, Arm_Right, Legs, Backpack (모든 슬롯 포함)
- **특징**: 중량 프레임의 특성을 정의하며, 필요시 `CanEquipPart` 또는 `GetCompatibilityFactor`를 재정의하여 중량 파츠와의 호환성 규칙을 추가할 수 있습니다.

## 2. 파츠 구현체 (Part Derivatives)

모든 파츠 구현체는 `Part` 추상 클래스를 상속받아 특정 `PartType`을 정의하고, 해당 파츠가 파괴되었을 때(`OnDestroyed`) 발생하는 게임 로직(주로 로그 출력 및 TODO 주석으로 명시된 패널티 적용 로직)을 구현합니다. 생성자는 이름, 스탯, 내구도 (및 무게)를 받아 부모 클래스를 초기화합니다.

### 2.1. HeadPart.cs

- **경로**: `Assets/AF/Scripts/Models/Parts/HeadPart.cs`
- **타입**: `PartType.Head`
- **OnDestroyed()**: 헤드 파괴 로그 출력. 명중률 관련 패널티 적용 로직 추가 예정.

### 2.2. BodyPart.cs

- **경로**: `Assets/AF/Scripts/Models/Parts/BodyPart.cs`
- **타입**: `PartType.Body`
- **OnDestroyed()**: 바디 파괴 로그 출력. AF 유닛 작동 불능 처리 로직(`ArmoredFrame.CheckOperationalStatus()`)과 연동됨.

### 2.3. ArmsPart.cs

- **경로**: `Assets/AF/Scripts/Models/Parts/ArmsPart.cs`
- **타입**: `PartType.Arm`
- **OnDestroyed()**: 팔 파괴 로그 출력. 무기 명중률 감소 또는 사용 불가 등의 패널티 적용 로직 추가 예정.

### 2.4. LegsPart.cs

- **경로**: `Assets/AF/Scripts/Models/Parts/LegsPart.cs`
- **타입**: `PartType.Legs`
- **OnDestroyed()**: 다리 파괴 로그 출력. 이동 속도 및 회피율 감소 등의 패널티 적용 로직 추가 예정.

---

*이 문서는 각 구현 클래스의 핵심적인 역할과 특징을 요약한 것으로, 상세한 로직 및 TODO 사항은 해당 스크립트 코드를 직접 참조해야 합니다.* 