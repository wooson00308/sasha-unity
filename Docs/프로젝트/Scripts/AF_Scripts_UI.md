# UI 스크립트 (AF/Scripts/UI)

> SASHA-Unity 프로젝트의 사용자 인터페이스(UI) 관련 C# 스크립트(.cs 파일) 문서입니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/UI`
  - 게임 화면에 표시되는 UI 요소 및 관련 로직 스크립트들이 있습니다.
  - `CombatTextUIService.cs`: 전투 중 발생하는 **로그(LogEntry)** 및 **유닛 상세 정보**를 UI에 표시하는 서비스입니다. 특히 전투 종료 시 전체 전투 로그를 받아 **일괄적으로 UI에 생성 및 애니메이션과 함께 표시**하는 기능을 담당합니다. 유닛의 상태를 **스냅샷(ArmoredFrameSnapshot)** 형태로 관리하며, 이를 기반으로 유닛의 상세 정보(AP, 내구도, 파츠/무기 상태, 스탯 등)를 UI에 표시합니다. **LogEventType**에 따라 로그 라인의 **표시 딜레이 및 텍스트 타이핑 애니메이션 속도를 개별적으로 설정**할 수 있는 기능이 포함되어 있습니다. `IService`를 상속받아 서비스 로케이터에 등록되며, `EventBusService`를 통해 전투 관련 이벤트를 구독하여 동작합니다.
  - `CombatRadarUIService.cs`: 전투 상황을 **레이더/소나 스타일 UI**로 시각화하는 서비스입니다. 전투에 참여하는 **유닛들의 위치를 레이더 상에 마커로 표시**하고, 유닛 간의 **타겟팅 라인**, 공격/회피 시의 **시각적 애니메이션 효과** 등을 관리합니다. **CombatLogPlaybackUpdateEvent** 이벤트를 구독하여 전투 로그 재생 시점에 맞춰 **실시간으로 레이더 UI(마커 위치, 상태, 라인 등)를 업데이트**하는 기능을 수행합니다. 다양한 애니메이션 설정 필드(`lineDrawDuration`, `flashDuration`, `pulseScale`, `moveAnimDuration` 등)와 레이더 스캔 효과 설정 필드(`radarScanTransform`, `scanArcWidthDegrees` 등)를 통해 시각 효과를 커스터마이징할 수 있습니다. `IService`를 상속받아 서비스 로케이터에 등록됩니다.
  - `CombatSetupUIService.cs`: 전투 시작 전에 **전투 참가 유닛을 설정하는 UI**를 관리하는 서비스입니다. 사용 가능한 어셈블리, 프레임, 파츠, 무기, 파일럿 등의 **SO(ScriptableObject) 데이터를 로드**하고, 이를 바탕으로 **참가자 슬롯 UI(`ParticipantSlotUI`)를 동적으로 생성 및 관리**합니다. 각 참가자 슬롯에서 사용자가 선택한 설정을 읽어와 **ArmoredFrame 인스턴스를 생성**하고, '전투 시작' 버튼 클릭 시 **CombatSimulatorService에 전투 시작을 요청**하는 역할을 수행합니다. 전투 종료(`CombatSessionEvents.CombatEndEvent`) 이벤트를 구독하여 전투가 끝나면 다시 설정 UI를 활성화합니다. `IService`를 상속받아 서비스 로케이터에 등록됩니다.
  - `ParticipantSlotUI.cs`: `CombatSetupUIService`에서 동적으로 생성하여 사용하는 **개별 전투 참가자 설정 슬롯 UI 컴포넌트**입니다. **프리셋 모드와 커스텀 모드**를 지원하며, 각 모드에 맞는 **드롭다운 메뉴(어셈블리, 프레임, 파츠, 무기, 파일럿)**와 **입력 필드(유닛 이름, 팀 ID, 시작 위치)**를 제공합니다. `CombatSetupUIService`로부터 데이터 옵션을 받아 드롭다운을 채우고, 사용자가 UI에서 선택/입력한 설정 값을 관리합니다. '제거' 버튼 클릭 시 **OnRemoveButtonClicked 이벤트**를 발생시켜 자신을 관리하는 `CombatSetupUIService`가 해당 슬롯을 제거하도록 알립니다.
  - `UILineRenderer.cs`: `UnityEngine.UI.Graphic`을 상속받아 UI Canvas에 **직접 선을 그리는 커스텀 컴포넌트**입니다. 여러 개의 `Vector2` 형태의 점(`points`) 리스트를 받아 이 점들을 잇는 **다중 세그먼트 선**을 그립니다. 선의 **두께(`thickness`)**와 각 세그먼트의 시작점과 끝점 **색상(`startColor`, `endColor`)**을 설정하여 그라데이션 효과를 줄 수 있습니다. `OnPopulateMesh` 메서드를 오버라이드하여 메시를 생성하고 UI 업데이트를 관리합니다. `normalizePoints` 옵션으로 부모 RectTransform 크기에 맞춰 좌표를 정규화할 수 있습니다. 주로 `CombatRadarUIService`와 같이 UI 상에 동적으로 선을 표시할 때 사용됩니다.


</rewritten_file> 