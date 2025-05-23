# EventBus 스크립트 (AF/Scripts/EventBus)

> SASHA-Unity 프로젝트의 이벤트/메시징 시스템 관련 C# 스크립트(.cs 파일) 문서입니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/EventBus`
  - 게임 내 다양한 시스템 간의 통신을 위한 이벤트 버스 로직 파일들이 있습니다.
  - `EventBus.cs`: 이벤트 발행 및 구독을 관리하는 핵심 이벤트 버스 클래스입니다. `Subscribe`, `Unsubscribe` 메서드를 통해 리스너를 등록/해제하고, `Publish` 메서드로 이벤트를 발생시킵니다. 각 이벤트 타입(`IEvent` 구현체)별로 리스너 목록을 관리하며, 로깅 기능과 모든 구독을 일괄 해제하는 `Clear` 메서드를 포함합니다.
  - `EventBusService.cs`: `EventBus` 인스턴스를 서비스로 제공하는 클래스입니다. `IService` 인터페이스를 구현하여 `ServiceLocator`를 통해 접근 가능합니다. 서비스 초기화(`Initialize`) 시 `EventBus` 인스턴스를 생성하고, 종료(`Shutdown`) 시 `EventBus`를 클리어하고 인스턴스를 해제합니다.
  - `IEvent.cs`: 이벤트 버스 시스템에서 사용되는 모든 이벤트의 기본 인터페이스입니다. 모든 커스텀 이벤트는 이 인터페이스를 구현해야 합니다. 현재는 마커 인터페이스 역할을 하며, 이벤트 타입임을 명시하는 데 사용됩니다.
  - `CombatLogEvents.cs`: 전투 로그 관련 이벤트 정의 파일입니다. `CombatLogPlaybackUpdateEvent` 이벤트가 정의되어 있으며, 이 이벤트는 전투 로그 재생 중 `CombatTextUIService`에 의해 발생하여 다른 UI 요소들의 동기화를 위해 사용됩니다. 이벤트는 현재 유닛 상태 스냅샷, 활성 유닛 이름, 관련 `LogEntry` 데이터를 포함합니다. 