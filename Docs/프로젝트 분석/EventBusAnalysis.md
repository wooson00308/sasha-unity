# EventBus 디렉토리 분석 (`Assets/AF/Scripts/EventBus`)

이 문서는 `Assets/AF/Scripts/EventBus` 디렉토리에 포함된 스크립트들의 역할과 구조를 분석합니다. 이 디렉토리는 게임 내 시스템 간의 통신을 위한 이벤트 기반 시스템을 제공합니다.

## 주요 컴포넌트

### 1. `IEvent.cs`

-   **역할**: 이벤트 버스 시스템에서 사용되는 모든 이벤트 클래스가 구현해야 하는 기본 **마커 인터페이스**입니다.
-   **목적**: 특정 클래스가 이벤트임을 명시적으로 나타내어 `EventBus`가 제네릭 제약 조건을 통해 이벤트 타입만 처리하도록 보장합니다. 현재는 마커 역할만 하지만, 필요 시 모든 이벤트가 공유해야 하는 공통 속성이나 메서드를 정의할 수 있습니다.

### 2. `EventBus.cs`

-   **역할**: 이벤트 발행/구독(Publish/Subscribe) 패턴을 구현한 핵심 클래스입니다. 시스템 내의 다른 부분들이 서로 직접적인 참조 없이 통신할 수 있도록 중개합니다.
-   **주요 기능**:
    -   **구독 (`Subscribe<T>`)**: 특정 이벤트 타입(`T`, `IEvent` 구현체)에 대한 리스너(콜백 메서드 `Action<T>`)를 등록합니다.
    -   **구독 해제 (`Unsubscribe<T>`)**: 등록된 리스너를 제거합니다.
    -   **발행 (`Publish<T>`)**: 특정 타입의 이벤트를 발생시킵니다. 해당 타입에 등록된 모든 리스너들에게 이벤트 데이터(`eventData`)를 전달하여 호출합니다.
    -   **관리**: 내부적으로 `Dictionary<Type, List<Delegate>>`를 사용하여 이벤트 타입별 리스너 목록을 관리합니다. 리스너가 없는 이벤트 타입은 메모리 관리를 위해 딕셔너리에서 제거됩니다.
    -   **유틸리티**: 모든 리스너 제거(`Clear`), 로깅 활성화/비활성화(`SetLogging`), 등록된 이벤트 목록 조회(`GetRegisteredEvents`) 기능을 제공합니다.
-   **특징**:
    -   타입 기반 구독: 이벤트 타입을 키로 사용하여 관련 리스너만 효율적으로 관리하고 호출합니다.
    -   안전한 발행: 이벤트 발행 중 리스너 목록이 변경되어도 안전하도록 리스너 목록을 복사하여 순회합니다.
    -   오류 처리: 리스너 실행 중 발생하는 예외를 잡아 로깅하고, 다른 리스너 실행에 영향을 주지 않도록 처리합니다.

### 3. `EventBusService.cs`

-   **역할**: `EventBus` 인스턴스를 `IService`로 래핑하여 `ServiceLocator`를 통해 관리될 수 있도록 하는 서비스 클래스입니다.
-   **주요 기능**:
    -   **서비스 통합**: `IService` 인터페이스를 구현하여 `ServiceManager`에 의해 자동으로 등록되고 관리될 수 있습니다.
    -   **EventBus 인스턴스 관리**: `Initialize()`에서 `EventBus` 인스턴스를 생성하고, `Shutdown()`에서 `EventBus.Clear()`를 호출하여 모든 구독을 정리합니다.
    -   **접근자 제공**: `Bus` 속성을 통해 외부에서 관리되는 `EventBus` 인스턴스에 접근할 수 있도록 합니다 (`ServiceLocator.Instance.GetService<EventBusService>().Bus`).
-   **목적**: `EventBus`를 게임의 다른 서비스들과 동일한 생명주기 및 접근 방식으로 통합 관리하기 위함입니다.

### 4. `CombatLogEvents.cs`

-   **역할**: 전투 로그 재생과 관련된 특정 이벤트 타입을 정의하는 파일입니다.
-   **정의된 이벤트**:
    -   `CombatLogPlaybackUpdateEvent`: `CombatTextUIService`가 전투 로그를 재생하면서 특정 로그 항목에 도달했을 때 발행하는 이벤트입니다. 다른 UI 요소(예: `CombatRadarUIService`)가 이 이벤트를 구독하여 현재 로그 시점의 유닛 상태(`CurrentSnapshot`), 활성 유닛(`ActiveUnitName`), 그리고 현재 로그 항목(`CurrentLogEntry`) 정보를 받아 UI를 동기화할 수 있습니다.
-   **목적**: 전투 시스템 내의 특정 상태 변화나 정보 업데이트를 관련 컴포넌트들에게 알리기 위한 구체적인 이벤트 데이터 구조를 제공합니다.

## 시스템 흐름 요약

1.  `ServiceManager`가 초기화될 때 `EventBusService`가 `ServiceLocator`에 등록됩니다. (`EventBusService.Initialize()` 호출, 내부적으로 `new EventBus()` 실행)
2.  이벤트를 구독하고자 하는 컴포넌트(예: `CombatRadarUIService`)는 `ServiceLocator`를 통해 `EventBusService`를 얻어온 후, `Bus` 속성을 통해 `EventBus` 인스턴스에 접근하여 `Subscribe<T>()` 메서드를 호출하여 원하는 이벤트 타입(예: `CombatLogPlaybackUpdateEvent`)에 대한 리스너를 등록합니다.
3.  이벤트를 발생시켜야 하는 컴포넌트(예: `CombatTextUIService`)는 동일한 방식으로 `EventBus` 인스턴스에 접근하여 `Publish<T>()` 메서드를 호출하고 해당 이벤트 데이터(예: `new CombatLogPlaybackUpdateEvent(...)`)를 전달합니다.
4.  `EventBus`는 발행된 이벤트 타입에 등록된 모든 리스너를 찾아 해당 이벤트 데이터를 인자로 전달하여 호출합니다.
5.  컴포넌트가 파괴되거나 비활성화될 때, `Unsubscribe<T>()`를 호출하여 등록했던 리스너를 해제해야 메모리 누수를 방지할 수 있습니다. (주로 `OnDisable` 또는 `OnDestroy`에서 수행)
6.  게임 종료 시 `ServiceManager`가 `EventBusService.Shutdown()`을 호출하고, 이는 내부적으로 `EventBus.Clear()`를 호출하여 남아있는 모든 구독을 정리합니다.

## 결론

`Assets/AF/Scripts/EventBus` 디렉토리의 스크립트들은 유연하고 분리된 컴포넌트 간 통신을 위한 이벤트 버스 시스템을 구축합니다. `IEvent`는 이벤트의 기본 타입을 정의하고, `EventBus`는 구독 및 발행 메커니즘의 핵심 로직을 제공하며, `EventBusService`는 이 시스템을 `ServiceLocator`와 통합합니다. `CombatLogEvents.cs`와 같은 파일들은 애플리케이션의 특정 도메인에 맞는 구체적인 이벤트 타입을 정의하는 예시입니다. 이 시스템은 컴포넌트 간의 직접적인 의존성을 줄여 코드의 모듈성과 확장성을 높이는 데 기여합니다. 