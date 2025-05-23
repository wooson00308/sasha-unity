# Services 스크립트 (AF/Scripts/Services)

> SASHA-Unity 프로젝트의 다양한 서비스 및 유틸리티 관련 C# 스크립트(.cs 파일) 문서입니다. 서비스 로케이터 패턴을 기반으로 서비스들을 관리합니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/Services`
  - 게임 내 다양한 기능을 제공하는 서비스 클래스 및 서비스 관리 로직 파일들이 있습니다.
  - `IService.cs`: 모든 서비스가 구현해야 할 기본 인터페이스입니다. `Initialize()`와 `Shutdown()` 두 메서드를 정의하여 서비스의 초기화 및 종료 계약을 명시합니다.
  - `ServiceLocator.cs`: 서비스 인스턴스를 등록하고 제공하는 서비스 로케이터 패턴의 핵심 클래스입니다. 싱글톤으로 구현되어 있으며, `RegisterService`, `GetService`, `RemoveService`, `ClearAllServices` 등의 메서드를 통해 서비스의 등록, 조회, 제거 기능을 제공합니다. 서비스 간 의존성 관리를 중앙 집중화하고 느슨한 결합을 가능하게 합니다.
  - `ServiceManager.cs`: 유니티 게임 오브젝트로 동작하는 서비스 관리자입니다. 게임 시작 시 미리 설정된 서비스 오브젝트 및 핵심 서비스(EventBusService, TextLoggerService, CombatSimulatorService, StatusEffectProcessor 등)를 `ServiceLocator`에 등록하고 초기화하는 역할을 합니다. 게임 종료 시 서비스 종료 순서(`ShutdownOrderItem`)에 따라 서비스들을 안전하게 종료하고 제거합니다.
  - `ShutdownOrderItem.cs`: 서비스 종료 순서 지정을 위한 간단한 직렬화 가능 클래스입니다. `ServiceManager`에서 `serviceType`과 `shutdownPriority`를 사용하여 서비스 종료 순서를 관리할 때 사용됩니다. 