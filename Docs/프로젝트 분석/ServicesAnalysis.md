# Services 디렉토리 분석 (`Assets/AF/Scripts/Services`)

이 문서는 `Assets/AF/Scripts/Services` 디렉토리에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임의 핵심 서비스들을 관리하고 접근하기 위한 기반 시스템을 제공합니다.

## 주요 컴포넌트

### 1. `IService.cs`

-   **역할**: 모든 서비스 클래스가 구현해야 하는 기본 인터페이스입니다.
-   **주요 메서드**:
    -   `Initialize()`: 서비스가 `ServiceLocator`에 등록될 때 호출되어 초기 설정을 수행합니다.
    -   `Shutdown()`: 서비스가 `ServiceLocator`에서 제거되거나 게임이 종료될 때 호출되어 리소스 해제 등의 마무리 작업을 수행합니다.
-   **목적**: 서비스의 생명주기(초기화 및 종료)를 표준화하여 `ServiceLocator`가 일관된 방식으로 서비스를 관리할 수 있도록 합니다.

### 2. `ServiceLocator.cs`

-   **역할**: 서비스 로케이터 패턴을 구현한 싱글톤 클래스입니다. 게임 내 다양한 서비스에 대한 중앙 집중식 접근 지점을 제공합니다.
-   **주요 기능**:
    -   **서비스 등록 (`RegisterService<T>`)**: `IService`를 구현한 서비스 인스턴스를 타입별로 등록합니다. 등록 시 `Initialize()` 메서드를 호출합니다. 이미 해당 타입의 서비스가 등록되어 있으면 기존 서비스를 `Shutdown()`하고 교체합니다.
    -   **서비스 검색 (`GetService<T>`)**: 등록된 서비스 인스턴스를 타입으로 검색하여 반환합니다. 서비스가 등록되어 있지 않으면 예외를 발생시킵니다.
    -   **서비스 확인 (`HasService<T>`)**: 특정 타입의 서비스가 등록되어 있는지 확인합니다.
    -   **서비스 제거 (`RemoveService<T>`)**: 특정 타입의 서비스를 제거합니다. 제거 시 `Shutdown()` 메서드를 호출합니다.
    -   **전체 서비스 제거 (`ClearAllServices`)**: 등록된 모든 서비스를 종료(`Shutdown()`)하고 제거합니다.
-   **특징**:
    -   싱글톤 패턴을 사용하여 어디서든 `ServiceLocator.Instance`를 통해 접근 가능합니다.
    -   `Dictionary<Type, IService>`를 사용하여 서비스를 관리합니다.
    -   서비스 등록 및 제거 시 초기화/종료 로직을 자동으로 처리합니다.

### 3. `ServiceManager.cs`

-   **역할**: Unity `MonoBehaviour` 컴포넌트로, 게임 시작 시 필요한 서비스들을 `ServiceLocator`에 자동으로 등록하고 관리하는 역할을 합니다.
-   **주요 기능**:
    -   **자동 서비스 등록**: `Awake()` 시점에 `RegisterServices()`를 호출하여 핵심 서비스 및 인스펙터에 설정된 서비스 객체들을 `ServiceLocator`에 등록합니다.
        -   **핵심 서비스 등록 (`RegisterCoreServices`)**: `EventBusService`, `TextLoggerService`, `CombatSimulatorService`와 같은 필수적인 서비스들을 코드에서 직접 등록합니다.
        -   **인스펙터 기반 등록**: `_serviceObjects` 리스트에 할당된 `MonoBehaviour` 객체 중 `IService`를 구현한 객체들을 리플렉션을 사용하여 적절한 인터페이스 타입으로 `ServiceLocator`에 등록합니다.
    -   **종료 순서 관리**: `_useShutdownOrder` 플래그가 활성화된 경우, `_shutdownOrder` 리스트에 정의된 순서(`ShutdownOrderItem`)에 따라 `OnDestroy()` 시점에 서비스를 순차적으로 종료(`RemoveService<T>`)합니다. 순서가 지정되지 않으면 `ClearAllServices()`를 호출하여 한 번에 모든 서비스를 종료합니다.
    -   **DontDestroyOnLoad**: `_dontDestroyOnLoad` 옵션을 통해 씬 전환 시 파괴되지 않도록 설정할 수 있습니다.
-   **특징**:
    -   Unity 라이프사이클(`Awake`, `OnDestroy`)과 연동됩니다.
    -   인스펙터 설정을 통해 게임에 필요한 서비스를 유연하게 구성할 수 있습니다.
    -   리플렉션을 사용하여 서비스 인터페이스 타입을 동적으로 찾아 등록하는 기능을 포함합니다.
    -   서비스 간의 종료 의존성이 있을 경우, 명시적인 순서를 지정하여 관리할 수 있습니다.

### 4. `ShutdownOrderItem.cs`

-   **역할**: 서비스의 종료 순서를 지정하기 위한 데이터를 담는 간단한 직렬화 가능 클래스입니다.
-   **주요 속성**:
    -   `serviceType`: 종료 순서를 지정할 서비스의 `System.Type`입니다.
    -   `shutdownPriority`: 종료 우선순위 값 (낮은 숫자가 먼저 종료됨).
-   **사용**: `ServiceManager`의 `_shutdownOrder` 리스트에서 사용되어 서비스 종료 순서를 정의합니다.

## 시스템 흐름 요약

1.  게임이 시작되면 `ServiceManager`(`MonoBehaviour`)가 `Awake()`에서 활성화됩니다.
2.  `ServiceManager`는 `ServiceLocator.Instance`를 얻어옵니다.
3.  `RegisterServices()` 메서드가 호출됩니다.
    -   `RegisterCoreServices()`를 통해 필수 서비스(`EventBusService`, `TextLoggerService`, `CombatSimulatorService` 등)가 `ServiceLocator`에 등록됩니다. 각 서비스의 `Initialize()`가 호출됩니다.
    -   인스펙터의 `_serviceObjects`에 할당된 `MonoBehaviour` 중 `IService`를 구현한 객체들이 리플렉션을 통해 검색되고 적절한 인터페이스 타입으로 `ServiceLocator`에 등록됩니다. 각 서비스의 `Initialize()`가 호출됩니다.
4.  게임 플레이 중 다른 시스템들은 `ServiceLocator.Instance.GetService<T>()`를 사용하여 필요한 서비스에 접근합니다.
5.  게임이 종료되거나 `ServiceManager` 오브젝트가 파괴될 때 `OnDestroy()`가 호출됩니다.
    -   `_useShutdownOrder`가 활성화되어 있으면 `_shutdownOrder`에 정의된 순서대로 `ServiceLocator.RemoveService<T>()`를 호출하여 서비스를 순차적으로 종료합니다. 각 서비스의 `Shutdown()`이 호출됩니다.
    -   그렇지 않으면 `ServiceLocator.ClearAllServices()`를 호출하여 모든 서비스를 한 번에 종료합니다. 모든 서비스의 `Shutdown()`이 호출됩니다.

## 결론

`Assets/AF/Scripts/Services` 디렉토리의 스크립트들은 서비스 로케이터 패턴을 기반으로 게임의 핵심 기능들을 모듈화하고 관리하기 위한 강력한 기반을 제공합니다. `IService` 인터페이스는 서비스의 표준 계약을 정의하고, `ServiceLocator`는 중앙 집중식 접근 및 관리 지점을 제공하며, `ServiceManager`는 Unity 환경과의 통합 및 서비스 생명주기 관리를 담당합니다. 이를 통해 코드의 결합도를 낮추고 유지보수성을 높일 수 있습니다. 