# 시스템 밸런스 및 레벨 디자인 기초 문서

## 1. 서론

이 문서는 Armored Frame (AF) 프로젝트의 게임 밸런싱 및 레벨 디자인 작업을 위한 핵심 시스템 구조와 데이터 흐름을 파악하고, 주요 조절 포인트를 식별하는 것을 목적으로 한다. 

## 2. 핵심 데이터 구조

### 2.1. Stats (`Stats.cs`)

모든 유닛, 파츠, 파일럿의 기본 스탯을 정의한다. 주요 스탯은 다음과 같다:

*   `AttackPower`: 공격력 (무기 데미지에 곱해지는 계수)
*   `Defense`: 방어력 (받는 데미지 감소)
*   `Speed`: 속도 (이동 거리 및 행동 주기에 영향)
*   `Accuracy`: 정확도 (공격 명중률에 영향)
*   `Evasion`: 회피율 (적 공격 회피 확률에 영향)
*   `Durability`: 내구도 (최대 체력에 영향, 파츠의 경우 해당 파츠의 최대 내구도)
*   `EnergyEfficiency`: 에너지 효율 (AP 소모량 등에 영향)
*   `MaxAP`: 최대 행동력
*   `APRecovery`: 턴당 AP 회복량
*   `MaxRepairUses`: 최대 수리 횟수 (주로 백팩 등 특정 파츠에서 제공)

**주요 기능:**

*   생성자: 기본값 또는 특정 값으로 스탯 객체 초기화.
*   연산자 오버로딩 (`+`, `*`): 스탯 객체 간 덧셈 및 스탯 객체와 계수 간 곱셈 지원.
*   `ApplyModifier(StatType, ModificationType, float)`: 특정 스탯을 덧셈 또는 곱셈 방식으로 수정. (음수 방지 및 최소/최대값 보정 포함)
*   `Clear()`: 모든 스탯을 기본값으로 초기화.
*   `Add(Stats other)`: 다른 스탯 객체의 값을 현재 객체에 더함.

**밸런싱 포인트:** 각 스탯의 기본값, 증가/감소폭, 다른 스탯과의 연관성, 스탯 한계치 등이 주요 조절 대상이다.

### 2.2. 파츠 (`Part.cs` 및 `PartType` Enum)

ArmoredFrame을 구성하는 개별 부품. 추상 클래스 `Part`를 상속받아 `HeadPart`, `BodyPart`, `ArmPart`, `LegPart`, `BackpackPart` 등이 존재할 것으로 예상된다. (현재 코드에는 구체적인 하위 클래스 대신 `PartType` Enum으로 구분)

*   **`PartType` Enum:** `Head`, `Body`, `Arm_Left`, `Arm_Right`, `Legs`, `Backpack` 등 (Frame 자신은 제외)

**주요 속성:**

*   `Name`: 파츠 이름
*   `Type`: 파츠 타입 (`PartType` Enum)
*   `PartStats`: 해당 파츠가 제공하는 `Stats` 객체
*   `Weight`: 파츠의 무게
*   `CurrentDurability`: 현재 내구도
*   `MaxDurability`: 최대 내구도
*   `IsOperational`: 현재 작동 가능 여부 (내구도가 0 초과면 `true`)
*   `Abilities`: 특수 능력 목록 (문자열 리스트)

**주요 기능:**

*   `SetDurability(float)`: 내구도 설정 및 작동 상태 업데이트.
*   `ApplyDamage(float)`: 데미지 적용 및 파괴 여부 반환.
*   `AddAbility(string)`: 특수 능력 추가.
*   `OnDestroyed(ArmoredFrame)`: 파츠 파괴 시 호출되는 추상 메서드 (각 파츠 타입별로 구현 필요).

**밸런싱 포인트:** 각 파츠 타입별 기본 스탯 분포, 무게, 내구도, 제공하는 특수 능력, 파괴 시 페널티 등을 조절하여 파츠 간의 역할과 중요도를 설정한다.

### 2.3. 프레임 (`Frame.cs` 및 `FrameType` Enum)

ArmoredFrame의 기본 골격. 파츠를 장착할 슬롯과 기본 스탯, 무게 등을 제공한다. 추상 클래스 `Frame`을 상속받아 구체적인 프레임 타입들이 존재할 것으로 예상된다.

*   **`FrameType` Enum:** `Light`, `Standard`, `Heavy` 등.

**주요 속성:**

*   `Name`: 프레임 이름
*   `Type`: 프레임 타입 (`FrameType` Enum)
*   `BaseStats`: 프레임 자체의 기본 `Stats` 객체
*   `Weight`: 프레임 자체의 무게
*   `PartCompatibility`: (현재는 `Dictionary<PartType, float>`) 파츠 타입별 호환성 계수 (추후 ScriptableObject 등으로 관리 가능성 언급됨)
*   `PartSlotDefinition`: 프레임이 제공하는 파츠 슬롯의 정의 (슬롯 식별자, 요구 파츠 타입 등).

**주요 기능:**

*   `GetPartSlots()`: 프레임의 파츠 슬롯 정의 목록 반환 (추상 메서드, 하위 프레임 클래스에서 구현).
*   `CanEquipPart(Part, string)`: 특정 슬롯에 파츠 장착 가능 여부 확인 (슬롯의 `RequiredPartType`과 파츠의 `Type` 비교).
*   `GetCompatibilityFactor(PartType)`: 특정 파츠 타입에 대한 호환성 계수 반환.

**밸런싱 포인트:** 프레임 타입별 기본 스탯, 무게, 제공 슬롯 수 및 종류, 파츠 호환성 등을 통해 프레임의 기본 성향(경량 고기동, 중장갑 고내구 등)을 결정한다. 

### 2.4. 무기 (`Weapon.cs` 및 관련 Enums)

ArmoredFrame에 장착되어 실제 공격을 수행하는 장비.

*   **`WeaponType` Enum:** `Melee`, `ShortRange`, `MidRange`, `LongRange` 등 (구체적인 분류는 게임 디자인에 따라 정의)
*   **`DamageType` Enum:** `Physical`, `Energy`, `Explosive`, `Pierce`, `Electric` 등 (구체적인 분류는 게임 디자인에 따라 정의)

**주요 속성:**

*   `Name`: 무기 이름
*   `Type`: 무기 타입 (`WeaponType` Enum)
*   `DamageType`: 데미지 타입 (`DamageType` Enum)
*   `Damage`: 기본 데미지
*   `Accuracy`: 무기 자체 정확도 (0.0 ~ 1.0)
*   `MinRange`, `MaxRange`: 최소 및 최대 사거리
*   `AttackSpeed`: 공격 속도 (초당 공격 횟수, 턴제에서는 다른 의미로 사용될 수 있음. 예: 연사 가능 횟수)
*   `BaseAPCost`: 공격 시 기본 AP 소모량
*   `MaxAmmo`: 최대 탄약 수 (0 이하는 무한 탄약)
*   `CurrentAmmo`: 현재 탄약 수
*   `ReloadAPCost`: 재장전 시 AP 소모량
*   `ReloadTurns`: 재장전에 필요한 턴 수 (0이면 즉시)
*   `IsReloading`: 현재 재장전 중인지 여부
*   `Weight`: 무기 무게
*   `SpecialEffects`: 특수 효과 목록 (문자열 리스트, 예: "貫通", "기절 확률 증가")
*   `AttackFlavorKey`, `ReloadFlavorKey`: 공격/재장전 시 사용할 Flavor Text 템플릿 키
*   `IsOperational`: 현재 작동 가능한지 여부 (예: 파괴되거나 특정 조건 만족 못하면 `false`)

**주요 기능:**

*   `InitializeFromSO(WeaponSO)`: `WeaponSO` 데이터로 무기 인스턴스 초기화.
*   `HasAmmo()`: 발사 가능한 탄약 있는지 확인.
*   `ConsumeAmmo()`: 탄약 1 소모.
*   `StartReload(int currentTurn)`: 재장전 시작 (재장전 턴이 필요하면 재장전 상태로 변경).
*   `FinishReload()`: 재장전 완료 (탄약 채우고 재장전 상태 해제).
*   `CheckReloadCompletion(int currentTurn)`: 현재 턴 기준으로 재장전 완료 여부 확인 및 처리.
*   `Clone()`: 무기 복사 (상태 초기화 포함).

**밸런싱 포인트:** 각 무기 타입별 기본 스탯(데미지, 사거리, 정확도, AP소모, 탄약 등) 설정, 특수 효과 부여, 무게 등을 통해 무기 간의 역할과 성능을 차별화한다. 데미지 타입별 저항/약점 시스템과 연계하여 전략성을 부여할 수 있다.

### 2.5. 파일럿 전문화 (`SpecializationType.cs` Enum)

파일럿의 전투 성향 및 보너스를 결정하는 유형. `Pilot` 클래스에 이 Enum 타입의 속성이 있을 것으로 예상된다.

*   **`SpecializationType` Enum:**
    *   `StandardCombat`: 표준 전투
    *   `MeleeCombat`: 근접 전투 특화
    *   `RangedCombat`: 원거리 전투 특화
    *   `Defense`: 방어 특화
    *   `Support`: 지원 특화 (수리, 버프 등)
    *   `Engineering`: 기계공학 (파츠 수리 효율 증가, 특정 장비 사용 가능 등)
    *   `Evasion`: 회피 특화

**밸런싱 포인트:** 각 전문화 타입별로 제공하는 스탯 보너스, 사용 가능한 특수 스킬, AI 행동 패턴 변화 등을 통해 파일럿의 역할을 명확히 하고 성장 방향을 제시한다.

### 2.6. 아머드 프레임 (`ArmoredFrame.cs`)

게임 내 실제 전투 유닛. 프레임, 파츠, 파일럿, 무기를 조합하여 구성된다.

**주요 속성 및 시스템:**

*   `Name`: 유닛 이름
*   `FrameBase`: 장착된 `Frame` 객체
*   `Parts`: (`Dictionary<string, Part>`) 장착된 파츠들 (슬롯 식별자, `Part` 객체)
*   `Pilot`: 배정된 `Pilot` 객체
*   `EquippedWeapons`: (`List<Weapon>`) 장착된 무기들
*   `CombinedStats`: (`Stats`) 프레임, 모든 작동 중인 파츠, (향후) 파일럿 스탯을 합산한 최종 스탯.
*   `TotalWeight`: 프레임과 모든 파츠 무게의 합.
*   `IsOperational`: 현재 유닛이 전투 가능한 상태인지 여부 (주로 `Body` 파츠의 상태에 따라 결정).
*   `Position`: 현재 위치 (Vector3).
*   `TeamId`: 소속 팀 ID.
*   `CurrentAP`: 현재 행동력.
*   `MaxAP`: (`CombinedStats`에서 가져옴) 최대 행동력.
*   `APRecovery`: (`CombinedStats`에서 가져옴) 턴 시작 시 회복되는 AP량.
*   `CurrentRepairUses`: 현재 남은 수리 횟수 (주로 백팩 파츠의 `MaxRepairUses` 스탯에서 초기화).
*   `ActiveStatusEffects`: (`List<StatusEffect>`) 현재 적용 중인 상태 효과 목록.
*   `BehaviorTreeRoot`: (AI용) 행동 트리 루트 노드.
*   `AICtxBlackboard`: (AI용) 행동 트리 컨텍스트 블랙보드.

**주요 기능 및 로직:**

*   **생성자:** 이름, 프레임, 초기 위치, 팀 ID를 받아 유닛 생성. 블랙보드 및 이벤트 버스 초기화. `RecalculateStats()` 호출.
*   `AssignPilot(Pilot)`: 파일럿 할당 및 스탯 재계산.
*   `AttachPart(Part, string)`, `DetachPart(string)`: 파츠 장착/제거 및 스탯 재계산, 작동 상태 확인.
*   `AttachWeapon(Weapon)`, `DetachWeapon(Weapon)`: 무기 장착/제거.
*   `RecalculateStats()`: 프레임, 모든 작동 중인 파츠, (향후) 파일럿의 스탯을 합산하여 `CombinedStats`와 `TotalWeight`를 갱신. `CurrentRepairUses`도 이때 `CombinedStats.MaxRepairUses` 값으로 설정.
*   `CheckOperationalStatus()`: `Body` 파츠 등의 상태를 확인하여 유닛 전체의 작동 가능 여부 판단.
*   `ApplyDamage(string targetSlotIdentifier, float damageAmount, ...)`: 특정 파츠에 데미지 적용. 파츠 파괴 시 스탯 재계산. 유닛 파괴 시 이벤트 발행.
*   `ApplyRepair(string targetSlotIdentifier, float repairAmount)`: 특정 파츠 수리. 수리 후 작동 상태 복구 시 스탯 재계산.
*   `RecoverAPOnTurnStart()`: 턴 시작 시 `CombinedStats.APRecovery`만큼 AP 회복.
*   `ConsumeAP(float)`, `HasEnoughAP(float)`: AP 소모 및 충분 여부 확인.
*   `AddStatusEffect(StatusEffect)`, `RemoveStatusEffect(string)`, `TickStatusEffects()`: 상태 효과 추가, 제거, 턴 경과에 따른 처리.
*   `GetPrimaryWeapon()`: 주 무기 반환 (현재는 첫 번째 작동 가능 무기).
*   `GetCurrentRepairUses()`, `DecrementRepairUses()`: 현재 수리 횟수 확인 및 감소.

**밸런싱 포인트:**

*   프레임, 파츠, 파일럿, 무기 조합을 통한 다양한 유닛 빌드 가능성 제공.
*   `CombinedStats` 계산 로직 (각 요소의 스탯 반영 비율 등)은 핵심적인 밸런스 요소.
*   AP 시스템 (최대 AP, 회복량, 행동별 소모량)은 전투의 템포와 행동 선택의 중요도를 결정.
*   무게(`TotalWeight`)와 이동력(`CombinedStats.Speed` 및 이동 AP 소모) 간의 관계.
*   수리 횟수 및 수리량의 적절성.
*   상태 효과의 종류, 지속 시간, 효과 값.
*   AI 행동 패턴 (`BehaviorTreeRoot` 및 노드 설정)과 연계하여 특정 역할군의 행동 양상 조절. 

## 3. 전투 흐름 및 관리 (`CombatSimulatorService.cs`)

`CombatSimulatorService`는 전투의 시작부터 종료까지 모든 과정을 총괄하는 핵심 서비스이다. 유닛의 행동 순서, 턴 관리, AI 실행, 이벤트 발행 등을 담당하며, 게임의 전반적인 전투 페이스와 전략적 깊이에 직접적인 영향을 미친다.

### 3.1. 주요 관리 상태

*   `_isInCombat` (bool): 현재 전투 진행 중인지 여부.
*   `_currentTurn` (int): 전체적인 턴 또는 라운드 수.
*   `_currentCycle` (int): 현재 턴 내에서 유닛이 활성화되는 순서/단계.
*   `_participants` (List<ArmoredFrame>): 현재 전투에 참여 중인 모든 유닛 리스트.
*   `_teamAssignments` (Dictionary<ArmoredFrame, int>): 각 유닛의 팀 배정 정보.
*   `_currentActiveUnit` (ArmoredFrame): 현재 활성화되어 행동 중인 유닛.
*   `_actedThisCycle` (HashSet<ArmoredFrame>): 현재 사이클(턴)에서 이미 행동을 마친 유닛 집합.
*   `_defendedThisTurn` (HashSet<ArmoredFrame>): 현재 턴에서 방어 행동을 수행한 유닛 집합.
*   `_movedThisActivation` (HashSet<ArmoredFrame>): 현재 유닛 활성화 주기 동안 이동한 유닛 집합.
*   `_defendedThisActivation` (HashSet<ArmoredFrame>): 현재 유닛 활성화 주기 동안 방어한 유닛 집합.

### 3.2. 전투 생명주기

#### 3.2.1. 전투 시작 (`StartCombat`)

1.  기존 전투가 진행 중이었다면 종료 처리.
2.  전투 상태 플래그(`_isInCombat`) 활성화, 턴/사이클 카운터 초기화.
3.  고유 전투 ID (`_currentBattleId`) 생성, 전투 이름 및 시작 시간 기록.
4.  참가자 리스트(`_participants`) 구성 및 `AssignTeams`를 통해 팀 배정.
5.  **핵심: 각 유닛의 파일럿 전문화(`Pilot.Specialization`)에 따라 적절한 행동 트리(`BehaviorTreeRoot`) 인스턴스를 생성하여 할당.**
    *   예: `RangedCombatBT`, `MeleeCombatBT`, `DefenderBT`, `SupportBT`, `BasicAttackBT`.
    *   유닛의 AI 행동 로직의 기반이 된다.
    *   이때 유닛의 `AICtxBlackboard`도 초기화.
6.  `_defendedThisTurn`, `_actedThisCycle` 등 관련 상태 집합 초기화.
7.  `CombatSessionEvents.CombatStartEvent`를 발행하여 전투 시작을 알림.

**밸런싱 포인트:**
*   각 전문화별로 할당되는 기본 Behavior Tree의 구성 및 성능.
*   초기 유닛 배치 및 팀 구성 전략.

#### 3.2.2. 턴 및 활성화 진행 (`ProcessNextTurn`)

이 메소드는 전투의 핵심 루프를 담당하며, 턴과 유닛 활성화를 순차적으로 진행시킨다.

1.  **이전 유닛 활성화 종료:** 만약 이전에 활성화된 유닛(`_currentActiveUnit`)이 있었다면, `CombatSessionEvents.UnitActivationEndEvent` 발행.
2.  **새 턴 시작 여부 결정:**
    *   첫 턴이거나, 모든 활성 유닛이 현재 `_currentCycle`에서 행동을 완료(`_actedThisCycle` 확인)한 경우 새 턴으로 간주.
3.  **새 턴 처리 (해당 시):**
    *   이전 턴에 대한 `CombatSessionEvents.RoundEndEvent` 발행.
    *   `CheckBattleEndCondition()` 호출하여 전투 종료 여부 확인. (종료 시 로직 중단)
    *   `_currentTurn` 증가, `_currentCycle` 0으로 초기화.
    *   `_actedThisCycle`, `_defendedThisTurn` 초기화.
    *   참가 유닛들의 주도권 순서(`initiativeSequence`) 결정 (현재는 단순히 생존 유닛 리스트).
    *   `CombatSessionEvents.RoundStartEvent` 발행.
4.  **다음 활성화 유닛 결정 (`GetNextActiveUnit`):**
    *   `_participants` 중 생존(`IsOperational`)하고 아직 이번 `_currentCycle`에서 행동하지 않은(`!_actedThisCycle.Contains`) 유닛을 순서대로 선택.
    *   활성화할 유닛이 없으면 전투 종료 조건을 다시 확인하고, 전투가 계속된다면 `true` 반환하여 다음 `ProcessNextTurn` 호출 대기.
5.  **선택된 유닛 활성화 준비:**
    *   `_currentActiveUnit`으로 설정.
    *   `_movedThisActivation`, `_defendedThisActivation` 초기화.
    *   `_currentCycle` 증가.
    *   유닛의 `RecoverAPOnTurnStart()` 호출하여 AP 회복.
    *   `CombatSessionEvents.UnitActivationStartEvent` 발행.
    *   장착된 모든 무기의 재장전 상태 업데이트 (`weapon.CheckReloadCompletion(_currentTurn)`).
    *   `_statusProcessor.Tick()` 호출하여 유닛에게 적용된 상태 효과 처리.
6.  **AI 행동 트리 실행 (핵심 로직):**
    *   유닛의 `AICtxBlackboard` 데이터 초기화.
    *   `canContinueActing` 루프 시작 (유닛 생존, AP 보유, 최대 행동 횟수 미만 조건).
        *   유닛의 `BehaviorTreeRoot.Tick()` 호출하여 행동 결정.
            *   실패 시 로그 남기고 `canContinueActing = false`.
        *   블랙보드에서 결정된 액션 타입(`DecidedActionType`) 확인.
        *   액션에 필요한 AP 비용(`_actionExecutor.GetActionAPCost`) 계산 및 확인.
            *   AP 부족 시 로그 남기고 다음 행동 시도 (또는 루프 종료).
        *   이동(`Move`)/방어(`Defend`) 액션은 현재 활성화 주기(`_movedThisActivation`, `_defendedThisActivation`) 내 1회로 제한.
        *   `_actionExecutor.Execute()`를 통해 실제 행동 실행. (타겟, 위치, 무기 정보는 블랙보드에서 가져옴).
        *   행동 실행 후:
            *   이동/방어 성공 시 관련 플래그 업데이트.
            *   공격 후 탄약 소진 시 즉시 재장전(`ImmediateReloadWeapon` 로직).
        *   매 행동 후 `CheckBattleEndCondition()` 호출. (전투 종료 시 모든 처리 중단)
        *   유닛 AP 고갈 시 `canContinueActing = false`.
        *   유닛 파괴 시 `canContinueActing = false`.
    *   루프 종료 후: `_actedThisCycle.Add(_currentActiveUnit)`로 현재 유닛 행동 완료 처리.
7.  `_isInCombat` 상태에 따라 `true`(계속) 또는 `false`(종료) 반환.

**밸런싱 포인트:**
*   턴당 AP 회복량 (`ArmoredFrame.APRecovery` 및 `RecoverAPOnTurnStart` 로직).
*   액션별 AP 소모량 (`CombatActionExecutor.GetActionAPCost`).
*   Behavior Tree 노드들의 실행 우선순위 및 조건 설정.
*   활성화당 최대 행동 횟수 (`MAX_ACTIONS_PER_ACTIVATION`).
*   상태 효과의 지속 시간 및 영향력.
*   유닛의 기본 스탯 (Speed 등)이 행동 순서에 미치는 영향 (현재는 단순 순차 진행).

#### 3.2.3. 전투 종료 (`EndCombat`)

1.  전투 미진행 중이면 반환.
2.  결과 타입(`forceResult`), 전투 지속 시간(`dur`), 생존자(`survivors`) 집계.
3.  모든 참가 유닛의 최종 상태를 `ArmoredFrameSnapshot`으로 기록.
4.  `CombatSessionEvents.CombatEndEvent`를 발행하여 전투 종료 및 결과, 스냅샷 전달.
5.  `_isInCombat = false`로 설정하고 모든 관련 상태 변수 및 리스트 초기화.

**밸런싱 포인트:**
*   승리/패배 조건 (`BattleResultEvaluator.Evaluate`).
*   전투 결과에 따른 보상 또는 다음 단계 연계.

### 3.3. 주요 상호작용 및 서브 시스템

*   **EventBus:** 전투 중 발생하는 주요 상황 (시작, 종료, 턴 변경, 유닛 활성화 등)을 이벤트로 발행하여 다른 시스템(UI, 사운드 등)과 통신.
*   **`ICombatActionExecutor`:** 실제 공격, 이동, 방어 등의 행동 실행 로직 담당. AP 소모량 계산도 포함.
*   **`IStatusEffectProcessor`:** 상태 이상 및 버프/디버프 효과의 적용 및 턴 경과에 따른 처리 담당.
*   **`IBattleResultEvaluator`:** 전투의 승패 조건을 판별.
*   **`TextLoggerService`:** 전투 상황을 텍스트 로그로 기록.
*   **Behavior Trees (BT):** 각 유닛의 AI. `CombatSimulatorService`는 BT를 실행하고, BT는 블랙보드를 통해 결정된 행동을 서비스에 전달.
*   **`CombatContext`:** BT 노드나 `CombatActionExecutor` 등에서 전투의 현재 상황 (참가자, 팀, 턴 정보 등)에 접근할 수 있도록 제공되는 데이터 객체.

### 3.4. 밸런싱 관점에서 고려할 사항

*   **전투 템포:** AP 회복량, 행동 비용, 유닛 속도 등을 조절하여 전투가 너무 빠르거나 느리지 않게 조절.
*   **AI 행동 패턴:** 각 BT의 구성을 통해 유닛들이 얼마나 효과적이고 예측 가능한/불가능한 행동을 하는지 조절.
*   **역할군 균형:** 특정 전문화(탱커, 딜러, 서포터)의 BT가 해당 역할을 잘 수행하는지, 너무 강력하거나 약하지 않은지 검토.
*   **정보 제공:** 전투 중 발생하는 이벤트와 로그를 통해 플레이어가 상황을 명확히 인지하고 전략을 수정할 수 있도록 지원.
*   **행동의 다양성:** 다양한 액션과 상태 효과를 통해 단조로운 전투를 피하고 여러 전략적 선택지를 제공.
*   **성장 곡선:** 플레이어/유닛 성장(스탯, 새 파츠/무기)이 전투 양상에 미치는 영향.

이것으로 `CombatSimulatorService`의 핵심적인 내용을 정리했습니다. 이 정보를 바탕으로 보다 세밀한 밸런스 조정 및 레벨 디자인을 진행할 수 있을 것입니다. 