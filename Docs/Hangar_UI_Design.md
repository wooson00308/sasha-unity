# 행거(Hangar) UI 디자인 기획서

**문서 버전:** 1.0
**작성일:** 2024-MM-DD (실제 작성일로 변경)
**작성자:** SASHA (AI Assistant)

## 1. 문서 개요

본 문서는 ArmoredFrame(AF) 프로젝트의 핵심 기능 중 하나인 '행거(Hangar)' 시스템의 사용자 인터페이스(UI) 디자인 및 기능을 정의하는 것을 목적으로 한다. 행거 시스템은 플레이어가 소유한 프레임, 파츠, 무기, 파일럿을 관리하고, 이를 조합하여 자신만의 AF 구성을 생성, 수정, 저장하는 공간이다.

## 2. 목표

*   플레이어가 직관적으로 자신의 AF 구성 목록을 확인하고 관리할 수 있도록 한다.
*   AF 조립 과정을 쉽고 명확하게 제공한다.
*   조립 중인 AF의 주요 스탯 정보를 실시간으로 제공하여 의사결정을 돕는다.
*   향후 확장성(AF 미리보기, 부품 필터링/정렬 등)을 고려한 구조를 설계한다.

## 3. 주요 화면 구성

행거 시스템은 크게 2개의 주요 화면(패널)으로 구성된다.

1.  **행거 메인 패널 (`HangarMainPanel`):** 저장된 AF 구성 목록 표시 및 관리
2.  **AF 조립/수정 패널 (`AssemblyPanel`):** 신규 AF 생성 또는 기존 AF 수정

## 4. 화면 상세 설계

### 4.1. 행거 메인 패널 (`HangarMainPanel`)

**목표:** 플레이어가 저장한 AF 구성들을 한눈에 보고 관리할 수 있도록 한다.

**레이아웃:**

*   **상단 영역 (Header Area):** 패널 제목 및 플레이어 정보 표시
*   **중앙 영역 (Content Area):** 스크롤 가능한 AF 구성 목록 표시
*   **하단 영역 (Footer Area):** 주요 액션 버튼 배치

**UI 요소 및 기능:**

| 영역     | 요소 타입              | 표시 내용 / 기능 설명                                                                                             | 데이터 연동 / 참고 서비스                   |
| :------- | :--------------------- | :---------------------------------------------------------------------------------------------------------------- | :------------------------------------------ |
| 상단     | 텍스트 (Title)         | "내 격납고" (고정 텍스트)                                                                                           | -                                           |
| 상단     | 텍스트 (Player Info)   | "보유 크레딧: [크레딧 값]"                                                                                       | `HangarService.GetPlayerData().Credits`     |
| 중앙     | 스크롤 뷰 (ScrollRect) | 저장된 AF 목록을 담는 컨테이너. 세로 스크롤 가능.                                                                   | -                                           |
| 중앙     | - 리스트 아이템 (Prefab) | `AFListItemPrefab` 사용. 각 아이템은 아래 요소 포함.                                                             | `HangarService.GetSavedConfigurations()`    |
|          |   - 텍스트 (AF Name)   | AF 구성 이름 (`PlayerAFConfiguration.ConfigurationName`)                                                          | `PlayerAFConfiguration`                     |
|          |   - 버튼 (Select)      | "선택" 또는 "수정". 클릭 시 해당 AF 구성 정보(`ConfigurationID`)를 가지고 `AssemblyPanel`로 전환.            | `PlayerAFConfiguration.ConfigurationID`     |
|          |   - 버튼 (Delete)      | "삭제". 클릭 시 확인 팝업 표시 후 `HangarService.DeleteConfiguration(ConfigurationID)` 호출. 리스트 갱신. | `PlayerAFConfiguration.ConfigurationID`     |
|          |   - (선택) 이미지        | AF 미리보기 썸네일 (향후 추가 기능)                                                                               | -                                           |
| 하단     | 버튼 (New AF)        | "새 AF 만들기". 클릭 시 비어있는 `AssemblyPanel`로 전환.                                                         | -                                           |
| 하단     | 버튼 (Back)          | "뒤로가기" 또는 "메인 메뉴". 클릭 시 이전 화면(예: 메인 메뉴)으로 전환.                                        | `UIPanelNavigationService` (가정)           |

**인터랙션:**

*   리스트 아이템의 "선택" 버튼 클릭 시 해당 AF 구성 정보가 `AssemblyPanel`로 전달되어 로드된다.
*   "삭제" 버튼 클릭 시 사용자 확인을 거친 후 해당 구성이 `PlayerData`에서 제거되고 UI 리스트가 갱신된다.

### 4.2. AF 조립/수정 패널 (`AssemblyPanel`)

**목표:** 플레이어가 프레임, 파츠, 무기, 파일럿을 선택하여 AF를 조립하고 저장/수정할 수 있도록 한다.

**레이아웃:**

*   **상단 영역 (Header Area):** AF 이름 입력 및 저장/취소 액션
*   **좌측 영역 (Component Selection Area):** 각 부품 카테고리별 선택 UI (드롭다운 등)
*   **우측 영역 (Information Area):** 현재 조합된 AF의 스탯 및 미리보기(선택) 표시

**UI 요소 및 기능:**

| 영역   | 요소 타입                     | 표시 내용 / 기능 설명                                                                                                                               | 데이터 연동 / 참고 서비스                                                                                    |
| :----- | :---------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------- |
| 상단   | 입력 필드 (InputField)        | AF 구성 이름 (`PlayerAFConfiguration.ConfigurationName`) 입력/수정.                                                                                | `PlayerAFConfiguration`                                                                                      |
| 상단   | 버튼 (Save)                 | "저장". 현재 조합 정보를 `PlayerAFConfiguration` 객체로 만들어 `HangarService.SaveNewConfiguration()` 또는 `UpdateConfiguration()` 호출 후 `HangarMainPanel`로 전환. | `HangarService`                                                                                              |
| 상단   | 버튼 (Cancel)               | "취소". 변경사항 저장 없이 `HangarMainPanel`로 전환.                                                                                             | -                                                                                                            |
| 좌측   | **프레임 섹션**               |                                                                                                                                                     |                                                                                                              |
|        | - 텍스트                      | "프레임"                                                                                                                                          | -                                                                                                            |
|        | - 드롭다운 (Dropdown)         | 플레이어가 소유한 프레임 목록 (`PlayerData.OwnedFrameIDs`) 표시. 선택 시 우측 스탯 정보 업데이트 및 프레임 기반 파츠 슬롯 활성화/비활성화.                  | `HangarService.GetPlayerData().OwnedFrameIDs`, `Frame.GetPartSlots()`                                        |
| 좌측   | **파일럿 섹션**               |                                                                                                                                                     |                                                                                                              |
|        | - 텍스트                      | "파일럿"                                                                                                                                          | -                                                                                                            |
|        | - 드롭다운 (Dropdown)         | 플레이어가 소유한 파일럿 목록 (`PlayerData.OwnedPilotIDs`) 표시. 선택 시 우측 스탯 정보 업데이트.                                                      | `HangarService.GetPlayerData().OwnedPilotIDs`                                                                |
| 좌측   | **파츠 섹션 (슬롯별)**      | 프레임에 정의된 슬롯(`Frame.GetPartSlots()`) 만큼 동적 생성 또는 고정 배치.                                                                             | `Frame.GetPartSlots()`                                                                                       |
|        | - 텍스트 (Slot Name)        | 슬롯 이름 (예: "머리", "몸통", "왼팔"...)                                                                                                     | `PartSlotDefinition.SlotIdentifier`                                                                          |
|        | - 드롭다운 (Dropdown)         | 해당 슬롯 타입(`RequiredPartType`)과 일치하는 플레이어 소유 파츠 목록(`PlayerData.OwnedPartIDs`) 표시. 선택 시 우측 스탯 정보 업데이트.                    | `HangarService.GetPlayerData().OwnedPartIDs`, `PartSlotDefinition.RequiredPartType`                            |
| 좌측   | **무기 섹션 (슬롯별)**      | 프레임에 정의된 무기 슬롯(현재는 고정 2개 가정) 만큼 배치.                                                                                             | `PlayerData.OwnedWeaponIDs`                                                                                  |
|        | - 텍스트 (Weapon Slot Name) | "무기 1", "무기 2" 등                                                                                                                               | -                                                                                                            |
|        | - 드롭다운 (Dropdown)         | 플레이어가 소유한 무기 목록 (`PlayerData.OwnedWeaponIDs`) 표시. 선택 시 우측 스탯 정보 업데이트.                                                        | `HangarService.GetPlayerData().OwnedWeaponIDs`                                                               |
| 우측   | **정보 표시 영역**            |                                                                                                                                                     |                                                                                                              |
|        | - (선택) 이미지 영역          | 현재 조합된 AF의 3D 또는 2D 미리보기 (향후 추가 기능)                                                                                             | -                                                                                                            |
|        | - 텍스트 영역 (`StatsDisplay`)  | 현재 조합(프레임+파일럿+파츠+무기)의 **예상** 종합 스탯 표시.<br/>- 주요 스탯 (공격력, 방어력, 속도, 정확도, 회피, 내구도, 최대 AP, AP 회복량, 총 무게 등)<br/>- 좌측 드롭다운 변경 시 실시간 업데이트 필요. | 임시 `ArmoredFrame` 객체 생성 및 `RecalculateStats()` 호출 결과 또는 별도 계산 로직 통해 표시. `HangarService`? |

**인터랙션:**

*   각 드롭다운에서 부품을 선택하면 우측의 스탯 정보가 실시간으로 갱신된다. (성능 고려 필요. Debounce 등 활용 가능)
*   프레임 선택 시 해당 프레임이 지원하는 파츠 슬롯 UI만 활성화/표시된다.
*   "저장" 시 유효성 검사(필수 부품 장착 여부 등) 후 `PlayerData`에 저장/업데이트된다.

## 5. 공통 UI 요소 (선택)

*   **확인 팝업 (`ConfirmationPopup`):** 삭제 등 중요한 액션 전에 사용자 확인을 받는 용도.
*   **툴팁 (`Tooltip`):** 각 스탯이나 부품 이름 위에 마우스 오버 시 상세 설명을 보여주는 기능 (향후 추가).

## 6. 향후 고려 사항

*   **AF 미리보기:** 조립 중인 AF의 3D 모델 또는 2D 이미지를 실시간으로 보여주는 기능.
*   **부품 필터링/정렬:** 드롭다운 목록이 길어질 경우, 이름, 타입, 성능 등으로 필터링하거나 정렬하는 기능.
*   **부품 비교:** 선택한 부품과 현재 장착된 부품의 스탯을 비교하여 보여주는 기능.
*   **자원 소모:** AF 생성/수정 시 크레딧 등 자원 소모 로직 연동.
*   **부품 구매/획득:** 행거 내에서 직접 부품을 구매하거나 획득하는 상점/보상 연계.

---
*본 문서는 초기 기획 단계의 내용이며, 실제 구현 과정에서 세부 사항은 변경될 수 있습니다.* 