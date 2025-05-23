# Combat 스크립트 (AF/Scripts/Combat)

> SASHA-Unity 프로젝트의 전투 시스템 관련 C# 스크립트(.cs 파일) 문서입니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/Combat`
  - 전투 시뮬레이션, 액션 실행, 로그 처리 등 핵심 전투 로직 파일들이 있습니다.
  - `BattleResultEvaluator.cs`: 전투 참가자 목록과 팀 배정 정보를 바탕으로 전투가 종료되었는지 (`Victory`, `Defeat`, `Draw`, `Aborted`) 여부와 결과를 평가하는 로직을 포함합니다.
  - `CombatActionEvents.cs`: 전투 시스템 내에서 발생하는 다양한 행동(공격, 이동, 방어, 재장전 등)과 관련된 이벤트를 정의하는 파일입니다. `ActionType` enum 등을 포함합니다. 이 파일은 전투 행동의 시작(`ActionStartEvent`), 완료(`ActionCompletedEvent`), 무기 발사(`WeaponFiredEvent`), 수리 시도(`RepairAttemptEvent`), 수리 적용(`RepairAppliedEvent`), 카운터 공격 알림(`CounterAttackAnnouncedEvent`), 데미지 적용 전/후(`PreDamageApplicationEvent`, `DamageAppliedEvent`) 등 주요 전투 이벤트 클래스들을 포함하고 있어. 각 이벤트 발생 시 필요한 데이터를 담아 관련 시스템에 전달하는 역할을 합니다.
  - `CombatActionExecutor.cs`: AI 또는 플레이어의 행동 결정(`ActionType`)을 받아 실제 게임 로직(데미지 적용, 이동 처리, AP 소모 등)을 실행하는 핵심 서비스입니다. `ICombatActionExecutor` 인터페이스를 구현합니다. 이 서비스는 AP 계산, 이동 가능 여부 확인 등 전투 규칙을 적용하고, 각 행동 타입(공격, 이동, 방어, 재장전, 수리, 어빌리티 사용 등)에 따른 구체적인 실행 로직을 처리합니다. 실행 결과는 이벤트로 발행됩니다.
  - `CombatContext.cs`: 현재 진행 중인 전투의 상태와 관련 서비스(로거, 액션 실행기 등)에 대한 참조를 담고 있는 컨텍스트 객체입니다. Behavior Tree 노드나 다른 전투 관련 로직에서 현재 전투 정보를 얻을 때 사용됩니다. 이벤트 버스, 로거, 액션 실행기 외에 현재 턴/사이클, 참가자 목록, 팀 정보, 턴별 방어/이동 유닛 목록 등 전투 전반의 정보를 포함하여 여러 컴포넌트가 공유하는 데이터 역할을 합니다.
  - `CombatSessionEvents.cs`: 전투 세션 시작, 종료, 라운드 시작/종료, 유닛 활성화 시작/종료 등 전투의 전체 흐름과 관련된 이벤트를 정의하는 파일입니다. `CombatStartEvent`, `CombatEndEvent`, `UnitActivationStartEvent`, `UnitActivationEndEvent`, `RoundStartEvent`, `RoundEndEvent`, `UnitDefeatedEvent` 같은 이벤트 클래스들을 포함하여 전투의 주요 단계 변화를 외부에 알리는 역할을 합니다.
  - `CombatSimulatorService.cs`: 전투의 전체 시뮬레이션 흐름을 관리하는 메인 서비스입니다. 참가자 관리, 턴/사이클 진행, AI 행동 트리 실행, 전투 종료 조건 확인 등의 역할을 담당하며 `ICombatSimulatorService` 인터페이스를 구현합니다. 액션 실행, 상태 효과 처리, 결과 평가 등의 서브 서비스들을 조율하는 전투 오케스트레이터 역할을 합니다.
  - `DamageEvents.cs`: 데미지 발생, 적용, 회복 등 데미지 처리와 관련된 이벤트를 정의하는 파일입니다. 데미지가 계산된 후 적용 전에 발생하는 `DamageCalculatedEvent`와 데미지가 회피되었을 때 발생하는 `DamageAvoidedEvent` 클래스를 포함하여 데미지 처리 과정을 추적하고 반응할 수 있도록 합니다.
  - `ICombatActionExecutor.cs`: `CombatActionExecutor` 서비스의 인터페이스 정의입니다. 전투 행동 실행 및 AP 비용 계산 기능에 대한 계약을 정의하여 실제 구현체와 분리된 접근을 제공합니다.
  - `ICombatEvent.cs`: 모든 전투 관련 이벤트가 상속받는 기본 인터페이스입니다. 이벤트 버스 시스템(`IEvent`)과 함께 사용되어 전투 관련 이벤트를 식별하는 마커 인터페이스 역할을 합니다.
  - `ICombatSimulatorService.cs`: `CombatSimulatorService`의 인터페이스 정의입니다. 전투의 시작, 종료, 턴 진행, 행동 수행, 유닛 전투 불능 상태 확인, 참가자/팀 목록 조회 등 전투 시뮬레이션 및 관리의 핵심 기능들을 정의합니다.
  - `ITextLogger.cs`: 텍스트 로깅 기능에 대한 인터페이스 정의입니다. 로그 메시지 기록, 이벤트 로깅, 로그 조회/검색/필터링, 파일 저장, 전투 요약 생성 등 로거 시스템이 제공해야 할 기능들을 정의합니다.
  - `LogLevel.cs`: 로그 메시지의 중요도를 나타내는 레벨(Debug, Info, Warning, Error 등)을 정의하는 enum 파일입니다. 로그 메시지의 심각도나 종류를 구분하는 데 사용됩니다.
  - `LogLevelFlags.cs`: 여러 `LogLevel`을 비트 플래그로 조합하여 사용할 때 필요한 플래그를 정의하는 파일입니다. 로그 필터링 등 여러 로그 레벨을 동시에 다룰 때 사용됩니다.
  - `LogEventType.cs`: 로그 메시지의 유형(SystemMessage, CombatEvent 등)을 정의하는 enum 파일입니다. 전투의 특정 이벤트(행동 완료, 데미지 적용, 상태 효과 적용 등)와 로그를 연결하여 상세 분류 및 처리에 사용됩니다.
  - `PartEvents.cs`: 아머드 프레임의 파츠 파괴, 수리 등 파츠 상태 변화와 관련된 이벤트를 정의하는 파일입니다. `PartDestroyedEvent`, `PartStatusChangedEvent`, `SystemCriticalFailureEvent` 등의 이벤트 클래스를 포함하여 기체 부품의 손상, 수리, 시스템 오류 등을 외부에 알립니다.
  - `StatusEffectEvents.cs`: 전투 중 발생하는 상태 이상 효과와 관련된 이벤트 클래스들을 정의하는 파일입니다. `StatusEffectType` enum으로 효과 종류를 나누고, 적용(`StatusEffectAppliedEvent`), 만료(`StatusEffectExpiredEvent`), 주기적 발동(`StatusEffectTickEvent`), 환경 효과 시작/종료(`EnvironmentalEffectStartEvent`, `EnvironmentalEffectEndEvent`), 저항(`StatusEffectResistEvent`) 등 상태 효과의 전 생애주기와 관련된 이벤트를 정의합니다.
  - `StatusEffectProcessor.cs`: 전투 중 상태 효과의 적용, 해제, 주기적인 효과 발동 (`Tick`) 등을 관리하고 처리하는 로직을 포함합니다. `IStatusEffectProcessor` 인터페이스를 구현하며, 각 상태 효과 타입에 대한 핸들러(`IStatusEffectHandler`)를 등록하고 관리하여 해당 효과의 구체적인 로직을 실행합니다.
  - `TextLogger.cs`: 실제 텍스트 로깅 기능을 구현하는 클래스입니다. `ITextLogger` 인터페이스를 구현합니다. `LogEntry` 객체에 메시지, 시간, 턴/사이클, 관련 유닛 및 상세 델타 정보(행동, 데미지, 상태 효과 등)를 저장하며 로그를 관리합니다. 저장된 로그를 다양한 포맷으로 조회, 필터링, 파일 저장하는 기능을 제공합니다.
  - `TextLoggerService.cs`: 게임 전반에 걸쳐 텍스트 로깅 기능을 제공하는 서비스입니다. `ITextLogger` 구현체를 감싸서 사용합니다. 이벤트 버스를 통해 다양한 전투 이벤트를 수신하고, 플레이버 텍스트 및 팀 색상을 활용하여 로그 메시지를 생성한 뒤 `TextLogger`에 전달하는 역할을 합니다. 로거 포맷팅 설정 기능도 포함합니다.

  - `/Handlers`
    - 다양한 상태 효과(Status Effect)를 처리하는 핸들러 스크립트들이 있습니다.
    - `AccuracyBoostHandler.cs`: 명중률 증가 상태 효과 핸들러입니다. `Buff_AccuracyBoost` 상태 효과 발생 시 로깅을 통해 효과 적용, 만료, 해제를 알립니다. 실제 명중률 스탯 변경 로직은 다른 곳에서 처리되는 것으로 보이며, 이 핸들러는 효과 적용 여부 표시 용도로 사용됩니다.
    - `APRecoveryBoostHandler.cs`: AP 회복 증가 상태 효과 핸들러입니다. `Buff_APRecoveryBoost` 상태 효과 적용 시 `target.CombinedStats.ApplyModifier`를 호출하여 AP 회복량 스탯을 증가시키고, 해제 시 이를 되돌리는 로직을 포함합니다. 효과 적용, 만료, 해제 시 로깅도 수행합니다.
    - `DamageOverTimeHandler.cs`: 지속 데미지 상태 효과 핸들러입니다. `Debuff_DamageOverTime` 상태 효과 적용 시 로깅하고, `OnTick` 시에는 `target.ApplyDamage`를 호출하여 대상에게 주기적으로 데미지를 입히는 로직을 수행합니다. 본체 또는 첫 번째 파츠에 데미지를 입히며, `StatusEffectTickEvent`를 발행합니다. 만료 및 해제 시 로깅합니다.
    - `DefenseBoostHandler.cs`: 방어력 증가 상태 효과 핸들러입니다. `Buff_DefenseBoost` 상태 효과 적용 시 `target.CombinedStats.ApplyModifier`를 호출하여 방어력 스탯을 증가시키고, 해제 시 이를 되돌리는 로직을 포함합니다. 주기적인 효과는 없으며, 효과 적용, 만료, 해제 시 로깅을 수행합니다.
    - `DefenseReducedHandler.cs`: 방어력 감소 상태 효과 핸들러입니다. `Debuff_DefenseReduced` 상태 효과 적용 시 방어력 스탯을 감소시키고, 해제 시 이를 되돌리는 로직을 포함합니다. 방어력 증가 핸들러와 기능적으로 유사하며, 효과 적용, 만료, 해제 시 로깅을 수행합니다.
    - `EvasionBoostHandler.cs`: 회피율 증가 상태 효과 핸들러입니다. `Buff_EvasionBoost` 상태 효과 적용 시 회피율 스탯을 증가시키고, 해제 시 이를 되돌리는 로직을 포함합니다. 주기적인 효과는 없으며, 효과 적용, 만료, 해제 시 로깅을 수행합니다.
    - `IStatusEffectHandler.cs`: 모든 상태 효과 핸들러가 구현해야 할 인터페이스입니다. `OnApply`, `OnTick`, `OnExpire`, `OnRemove` 네 가지 메소드를 정의하여 상태 효과의 생애주기 동안 필요한 로직 실행을 위한 계약을 제공합니다.
    - `MaxAPBoostHandler.cs`: 최대 AP 증가 상태 효과 핸들러입니다. `Buff_MaxAPBoost` 상태 효과 적용 시 대상의 `MaxAP` 스탯을 증가시키고, 해제 시 감소시켜 원래대로 되돌립니다. 주기적인 효과는 없으며, 효과 적용, 만료, 해제 시 로깅을 수행합니다.
    - `RepairOverTimeHandler.cs`: 지속 수리 상태 효과 핸들러입니다. `Buff_RepairOverTime` 상태 효과 적용 시 로깅하고, `OnTick` 시 `target.ApplyRepair`를 호출하여 대상의 가장 손상된 작동 가능한 파츠를 주기적으로 수리하는 로직을 수행합니다. 수리가 발생했을 때 `StatusEffectTickEvent`를 발행하고 로깅합니다. 만료 및 해제 시 로깅합니다.
    - `ShieldGeneratorHandler.cs`: 쉴드 생성 상태 효과 핸들러입니다. `Buff_ShieldGenerator` 상태 효과 적용 시 `target.AddShield`를 호출하여 대상에게 지정된 양만큼 쉴드를 추가하고, 만료 또는 해제 시 `target.ClearShield`를 호출하여 쉴드를 제거합니다. 주기적인 효과는 없으며, 효과 적용, 만료, 해제 시 로깅을 수행합니다.
    - `UtilityBuffHandler.cs`: 범용 유틸리티/마커용 상태 효과 핸들러입니다. `Buff_Utility` 상태 효과 적용 시 특정 스탯 변경이나 주기적인 효과 없이, 단순히 효과가 적용, 만료, 해제되었음을 로깅하는 역할을 합니다. `effect.EffectName`을 로그에 포함하여 어떤 효과인지 식별할 수 있도록 합니다. 