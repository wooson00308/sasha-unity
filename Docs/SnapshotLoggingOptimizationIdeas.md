# 스냅샷 로깅 최적화 아이디어: 델타 스냅샷 / 이벤트 소싱

## 개념

현재 전투 로그 시스템은 각 턴 시작(`UnitActivationStart`) 시점에 모든 유닛의 전체 상태 스냅샷(`ArmoredFrameSnapshot`)을 저장합니다. 로그 재생 시 더 세밀한 상태 변화를 보기 위해 더 자주 스냅샷을 저장하는 것을 고려할 수 있지만, 매 이벤트마다 전체 스냅샷을 저장하는 것은 성능 및 메모리 측면에서 비효율적일 수 있습니다.

이에 대한 대안으로, **델타 스냅샷** 또는 **이벤트 소싱**과 유사한 접근 방식을 제안합니다.

## 구현 방식

1.  **기준 스냅샷 저장:** 기존처럼 `UnitActivationStart` 시점에는 전체 유닛 상태 스냅샷(`TurnStartStateSnapshot`)을 `LogEntry`에 저장합니다. 이는 해당 턴/활성화 시작 시점의 기준 상태가 됩니다.

2.  **변경 사항만 기록:** 이후 발생하는 이벤트(`ActionCompleted`, `DamageApplied`, `PartDestroyed` 등)에 대한 `LogEntry`에는 전체 스냅샷 대신 **해당 이벤트로 인해 발생한 상태 변경 정보만** 기록합니다.
    *   `LogEntry` 구조체에 이벤트 타입별로 필요한 최소한의 변경 정보 필드를 추가합니다.
        *   예: `AffectedUnitName` (string), `EventType` (enum), `CompletedActionType` (enum, ActionCompleted용), `ChangedPartSlot` (string, Damage/PartDestroyed용), `NewDurability` (float, Damage용), `PartDestroyed` (bool, PartDestroyed용), `StatusEffectName` (string, StatusEffect용) 등.
    *   각 이벤트 핸들러(`TextLoggerService` 내)는 전체 스냅샷을 생성하는 대신, 해당 이벤트에 맞는 변경 정보 필드만 채워서 `LogEntry`를 생성합니다.

3.  **UI 재생 로직 수정:**
    *   로그 재생을 담당하는 UI 서비스 (`CombatTextUIService`, `CombatRadarUIService` 등)의 `ProcessPlaybackAsync` 함수를 수정합니다.
    *   `TurnStartStateSnapshot`이 포함된 로그를 만나면, 해당 스냅샷을 기준으로 전체 UI 상태를 초기화/업데이트합니다.
    *   이후 변경 정보만 담긴 로그를 만나면, 해당 정보를 해석하여 UI의 특정 부분(예: 특정 유닛의 파츠 내구도 표시, 레이더 마커 위치 등)만 업데이트합니다.

## 장점

*   **성능 향상:** 전체 스냅샷 생성 및 직렬화/역직렬화 비용 감소.
*   **메모리 효율:** 로그 데이터 크기 대폭 감소.
*   **데이터 간결성:** 중복되는 상태 정보 없이 변경 이력만 기록.

## 단점

*   **구현 복잡도 증가:**
    *   `LogEntry` 구조체 설계 및 관리가 더 복잡해짐.
    *   UI 재생 로직에서 변경 정보를 해석하고 적용하는 부분이 추가됨.
*   **디버깅:** 특정 시점의 전체 상태를 보려면 기준 스냅샷부터 변경 이력을 순차적으로 적용해야 할 수 있음 (UI 재생 로직 자체는 이를 수행).

## 결론

초기 구현 복잡도는 다소 증가하지만, 성능, 메모리 효율성, 데이터 관리 측면에서 장기적으로 더 우수한 접근 방식입니다. 특히 전투 로그가 길어지거나 참가 유닛 수가 많아질 경우 이점이 두드러집니다. 