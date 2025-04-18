# ArmoredFrame (AF) 아키텍처 개요

이 문서는 ArmoredFrame(AF) 게임의 주요 아키텍처 구성과 시스템 간 상호작용을 설명합니다. Mermaid 그래프를 사용하여 시스템 구조를 시각화합니다.

## 아키텍처 구성 요소

### 코어 시스템

```mermaid
graph TD
    A[GameManager] --> B[ServiceLocator]
    B --> C[ServiceManager]
    C --> D1[EventBusService]
    C --> D2[DataManagerService]
    C --> D3[CombatSimulatorService]
    C --> D4[UIManagerService]
    C --> D5[SaveSystemService]

    style A fill:#f96,stroke:#333,stroke-width:2px
    style B fill:#69f,stroke:#333,stroke-width:2px
    style C fill:#69f,stroke:#333,stroke-width:2px
    style D1 fill:#9cf,stroke:#333,stroke-width:2px
    style D2 fill:#9cf,stroke:#333,stroke-width:2px
    style D3 fill:#9cf,stroke:#333,stroke-width:2px
    style D4 fill:#9cf,stroke:#333,stroke-width:2px
    style D5 fill:#9cf,stroke:#333,stroke-width:2px
```

### 서비스 의존성 관계

```mermaid
graph LR
    A[GameManager] --> B[ServiceLocator]
    B --- C[ServiceManager]
    
    C --> D1[EventBusService]
    C --> D2[DataManagerService]
    
    D3[CombatSimulatorService] --> D1
    D3 --> D2
    
    D4[UIManagerService] --> D1
    D4 --> D2
    
    D5[SaveSystemService] --> D2
    
    style A fill:#f96,stroke:#333,stroke-width:2px
    style B fill:#69f,stroke:#333,stroke-width:2px
    style C fill:#69f,stroke:#333,stroke-width:2px
    style D1 fill:#9cf,stroke:#333,stroke-width:2px
    style D2 fill:#9cf,stroke:#333,stroke-width:2px
    style D3 fill:#9cf,stroke:#333,stroke-width:2px
    style D4 fill:#9cf,stroke:#333,stroke-width:2px
    style D5 fill:#9cf,stroke:#333,stroke-width:2px
```

## AF 게임 데이터 모델

```mermaid
classDiagram
    class ArmoredFrame {
        +Frame frameBase
        +Body body
        +Head head
        +Arm leftArm
        +Arm rightArm
        +Legs legs
        +Backpack backpack
        +Pilot pilot
        +CalculateStats()
        +CheckPartCompatibility()
    }
    
    class Frame {
        +FrameType type
        +BaseStats stats
        +PartCompatibility compatibility
    }
    
    class Part {
        +PartType type
        +Stats stats
        +int durability
        +List~SpecialAbility~ abilities
    }
    
    class Weapon {
        +WeaponType type
        +DamageType damageType
        +float damage
        +float accuracy
        +float range
        +float attackSpeed
        +SpecialEffects effects
    }
    
    class Pilot {
        +Stats baseStats
        +List~Skill~ skills
        +int level
        +int experience
        +SpecializationType specialization
    }
    
    ArmoredFrame *-- Frame
    ArmoredFrame *-- Part
    ArmoredFrame *-- Pilot
    Part <|-- Body
    Part <|-- Head
    Part <|-- Arm
    Part <|-- Legs
    Part <|-- Backpack
    Arm *-- Weapon
```

## 전투 시스템 흐름

```mermaid
sequenceDiagram
    participant Player
    participant BattleManager
    participant ArmoredFrame
    participant TextLogger
    
    Player->>BattleManager: 전투 시작
    BattleManager->>BattleManager: 전투 초기화
    BattleManager->>ArmoredFrame: AF 초기 설정 로드
    BattleManager->>TextLogger: 전투 시작 로그
    
    loop 턴 진행
        BattleManager->>ArmoredFrame: 행동 결정 요청
        ArmoredFrame->>BattleManager: 행동 결정 반환
        BattleManager->>BattleManager: 행동 수행
        BattleManager->>TextLogger: 행동 결과 로그
        
        opt 파츠 파괴 시
            BattleManager->>ArmoredFrame: 파츠 상태 업데이트
            BattleManager->>TextLogger: 파츠 파괴 로그
        end
        
        alt AF 전투 불능 시
            BattleManager->>TextLogger: 전투 불능 로그
        end
    end
    
    BattleManager->>Player: 전투 결과
    BattleManager->>TextLogger: 전투 종료 로그
```

## 이벤트 시스템

```mermaid
flowchart LR
    A[이벤트 발생 소스] -->|이벤트 발행| B[EventBus]
    B -->|구독| C1[구독자 1]
    B -->|구독| C2[구독자 2]
    B -->|구독| C3[구독자 3]
    
    subgraph 주요 이벤트
    E1[전투 이벤트]
    E2[파츠 상태 변경 이벤트]
    E3[UI 업데이트 이벤트]
    E4[게임 상태 변경 이벤트]
    end
    
    style B fill:#f96,stroke:#333,stroke-width:2px
    style E1 fill:#fc9,stroke:#333,stroke-width:1px
    style E2 fill:#fc9,stroke:#333,stroke-width:1px
    style E3 fill:#fc9,stroke:#333,stroke-width:1px
    style E4 fill:#fc9,stroke:#333,stroke-width:1px
```

## 서비스 로케이터 패턴 구현

```mermaid
classDiagram
    class IService {
        <<interface>>
        +Initialize()
        +Shutdown()
    }
    
    class ServiceLocator {
        -Dictionary~Type, IService~ services
        +GetService<T>()
        +RegisterService<T>()
        +UnregisterService<T>()
    }
    
    class ServiceManager {
        -List~IService~ registeredServices
        +InitializeServices()
        +ShutdownServices()
        +RegisterCoreServices()
    }
    
    class ConcreteService {
        +Initialize()
        +Shutdown()
        +ServiceSpecificMethods()
    }
    
    ServiceLocator o-- IService
    ServiceManager --> ServiceLocator
    IService <|.. ConcreteService
```

## 향후 확장 계획

1. **리소스 로더 서비스**: 어드레서블 기반 리소스 관리 시스템
2. **파츠 시스템 확장**: 다양한 파츠와 프레임의 확장성 구현
3. **전투 로그 시스템 고도화**: 텍스트 기반 전투 로그 기능 향상
4. **데이터 저장 및 관리 체계화**: SO(ScriptableObject) 기반 데이터 관리 시스템

## 파일 구조

현재 프로젝트는 다음과 같은 모듈식 구조로 구성되어 있습니다:

- **Assets/AF/Scripts/**
  - **Services/**: 서비스 로케이터 패턴 구현
    - ServiceLocator.cs: 서비스 관리 핵심 클래스
    - ServiceManager.cs: 서비스 실행 관리
    - IService.cs: 서비스 인터페이스
    - ServiceExtensions.cs: 확장 메서드 제공
  - **EventBus/**: 이벤트 버스 시스템
    - EventBus.cs: 이벤트 발행-구독 관리
    - IEvent.cs: 이벤트 인터페이스
    - EventBusService.cs: 서비스로 등록된 이벤트 버스
  - **Examples/**: 샘플 구현 코드
    - ExampleTimer.cs: 타이머 샘플

향후 다음 모듈이 추가될 예정입니다:
- **Models/**: 게임 데이터 모델 클래스
- **Combat/**: 전투 시뮬레이션 시스템
- **UI/**: 사용자 인터페이스 관련 코드
- **Data/**: 데이터 관리 및 저장 시스템 