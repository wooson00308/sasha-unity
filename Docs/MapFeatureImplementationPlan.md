# Text-Based Game Map Feature Implementation Plan

## 1. 개요

현재 전투 시스템은 좌표 기반으로 텅 빈 공간에서 이루어진다. 게임의 전략성을 높이기 위해 맵 특성(지형, 엄폐, 시야 등)을 도입하는 방안을 논의하였다. 텍스트 기반 게임의 특성상 물리 엔진이나 NavMesh를 사용할 수 없으므로, 추상적인 데이터 모델과 로직을 통해 구현해야 한다.

## 2. 구현 방안

### 2.1. 맵 표현

*   **데이터 구조:** 2D 그리드 또는 좌표계를 사용하여 맵을 표현한다. 각 좌표(칸)는 다음과 같은 속성을 가진다:
    *   `TerrainType`: (Enum: `Open`, `LightCover`, `HeavyCover`, `HighGround`, `Impassable`, etc.)
    *   `MovementCost`: 해당 칸을 지나는 데 필요한 이동력 비용 (기본 1, 이동 불가 시 무한대 등)
    *   `BlocksLineOfSight`: 해당 칸이 시야를 가리는지 여부 (boolean)
*   **데이터 저장:** 맵 데이터는 2차원 배열, 딕셔너리, 텍스트 파일, 또는 엑셀 시트(`MapData.xlsx`?) 등으로 관리하고 로드한다.

### 2.2. 전투 시스템 통합

*   **이동 (`CombatActionExecutor.PerformMoveToPosition`):**
    *   목표 지점까지의 경로 계산 시 각 칸의 `MovementCost`를 고려한다.
    *   `Impassable` 지형은 통과할 수 없도록 한다.
    *   필요시 간단한 길찾기 알고리즘(예: A*)을 도입하여 최단 경로를 탐색한다.
*   **시야 (Line of Sight, LoS):**
    *   공격 액션(`ExecuteAttack`, `HitChanceConsideration`) 수행 전, 공격자와 대상 사이에 `BlocksLineOfSight` 속성을 가진 지형이 있는지 검사한다.
    *   그리드 기반의 간단한 LoS 알고리즘 (예: Bresenham's line algorithm 변형)을 구현한다.
*   **엄폐 (`Cover`):**
    *   피격 대상이 위치한 칸의 `TerrainType` (`LightCover`, `HeavyCover`)에 따라 피격률(`HitChanceConsideration`)이나 방어력(`ExecuteAttack` 데미지 계산 시)에 보너스/페널티를 적용한다.
*   **고지대 (`HighGround`):**
    *   `HighGround` 지형에 위치한 유닛에게 명중률 또는 사거리 보너스를 부여한다 (`HitChanceConsideration`, `ExecuteAttack` 사거리 체크 시).

### 2.3. AI 수정 (`Considerations`)

*   기존 Consideration들을 맵 특성을 고려하도록 수정하거나 새로운 Consideration을 추가한다.
*   **`TargetPositionSafetyConsideration` 수정:** 목표 지점의 엄폐 여부를 안전성 평가에 포함시킨다.
*   **`DistanceToEnemyConsideration` 수정:** 단순 거리 외에 엄폐물이나 고지대로 이동하는 것을 선호하도록 로직을 개선할 수 있다.
*   **신규 Considerations 제안:**
    *   `IsInCoverConsideration`: 현재 유닛이 엄폐 상태인지 평가한다.
    *   `TargetInLineOfSightConsideration`: 목표 대상까지 시야가 확보되는지 평가한다.
    *   `PathExistsConsideration`: 목표 지점까지 이동 가능한 경로가 있는지 평가한다 (이동 비용 고려).
    *   `TerrainAdvantageConsideration`: 특정 지형(고지대 등) 점유의 이점을 평가한다.

## 3. 복잡도 및 단계적 접근

*   맵 특성 구현은 현재 시스템보다 상당히 복잡하며, 맵 데이터 구조 설계, 시야/경로 계산 로직 구현, 다수의 Consideration 수정/추가가 필요하다.
*   구현 시 모든 기능을 한 번에 구현하기보다는 단계적으로 접근하는 것이 권장된다.
    *   **1단계:** 엄폐 효과(`LightCover`, `HeavyCover`) 적용 (피격률/방어력 보정)
    *   **2단계:** 시야(`BlocksLineOfSight`) 개념 도입 및 공격 가능 여부 판단
    *   **3단계:** 이동 비용(`MovementCost`, `Impassable`) 및 경로 탐색 도입
    *   **4단계:** 고지대(`HighGround`) 등 추가 지형 효과 구현
    *   각 단계별로 관련 AI Consideration 수정 및 테스트 진행

## 4. 향후 논의 사항

*   구체적인 맵 데이터 형식 및 저장 방식 결정
*   길찾기 및 시야 계산 알고리즘 선정 및 구현 상세 논의
*   각 지형 타입별 정확한 효과 수치 (이동 비용, 피격률 보정 등) 정의
*   AI Consideration 수정 및 추가 우선순위 결정 