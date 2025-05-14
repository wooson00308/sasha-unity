# AF_Data.xlsx 연동 도구 아이디어 브레인스토밍

기존에 구현된 `get_sheet_names`와 `read_sheet_data` 외에, `AF_Data.xlsx` 파일을 더 효과적으로 활용하고 관리하기 위한 추가 도구 아이디어를 자유롭게 적어주세요! 우리만의 강력한 툴셋을 만들어봅시다! 💪

## 현재 구현된 도구

1.  **`get_sheet_names`**: 엑셀 파일 내의 모든 시트 이름을 가져옵니다. (✅ 완료)
2.  **`read_sheet_data`**: 특정 시트의 모든 데이터를 JSON 형태로 읽어옵니다. (✅ 완료)
3.  **`get_entity_details(sheet_name: string, entity_id: string, id_column_name: string)`**: 특정 시트에서 ID에 해당하는 행의 모든 데이터를 가져옵니다. (✅ 완료 - 2025-05-14)
4.  **`update_entity_stat(sheet_name: string, entity_id: string, stat_column: string, new_value: string)`**: 특정 시트에서 지정된 ID를 가진 엔티티(행)의 특정 열(stat) 값을 수정합니다. (✅ 완료 - 2025-05-14)

## 새로운 도구 아이디어 제안 (밸런싱 및 데이터 관리 중심) ✨

- ### ID 기반 상세 정보 조회 및 수정 (Targeted Data Viewer/Editor)
    - **설명**: 특정 ID (`FrameID`, `PartID`, `WeaponID` 등)를 입력하면, 해당 아이템의 모든 스탯 정보를 보기 좋게 보여주고, **특정 스탯 값을 바로 수정하여 엑셀 파일에 반영**할 수 있는 기능을 제공합니다. (엑셀 쓰기 기능 필요)
    - **기대 효과**: 엑셀 파일을 직접 열지 않고도 특정 아이템의 정보를 빠르게 확인하고, 필요한 스탯만 정확하게 수정하여 밸런싱 작업 효율 증대 및 실수 감소.
    - **구체적 MCP 도구 (예시)**:
        - `get_entity_details(sheet_name: string, entity_id: string, id_column_name: string)`: (✅ 완료 - 위 목록 참고)
        - `update_entity_stat(sheet_name: string, entity_id: string, stat_column: string, new_value: string)`: 특정 시트, 특정 ID의 특정 스탯(컬럼) 값을 새로운 값으로 업데이트. (⚠️ 다음 목표?)

- ### 타입별/티어별 주요 스탯 비교 분석 (Comparative Stat Analyzer)
    - **설명**: 특정 기준(예: `FrameType`, `WeaponTier`)으로 아이템들을 그룹화하여 주요 스탯(예: `Stat_Speed`, `Stat_AttackPower`)의 평균, 최소/최대값, 분포 등을 비교 분석하여 보여줍니다.
    - **기대 효과**: 전체적인 밸런스 흐름 파악 용이, 특정 그룹 아이템의 성능 적정성 판단 지원.
    - **구체적 MCP 도구 (예시)**:
        - `get_grouped_stats_summary(sheet_name: string, group_by_column: string, stat_columns_to_analyze: string[])`: 그룹별 주요 스탯 요약 정보 제공.

- ### 고아 데이터 탐지 (Orphan Data Detection)
    - **설명**: `Parts`, `Weapons` 등의 아이템 시트에서 정의되었지만, 실제로 `AF_Assemblies`나 다른 주요 데이터에서 참조되지 않는 (사용되지 않는) 아이템들을 찾아내는 기능입니다.
    - **기대 효과**: 불필요한 데이터 정리, 데이터 무결성 향상, 리소스 최적화.

- ### 데이터 유효성 검사 (Data Validation Tool)
    - **설명**: 각 시트의 특정 컬럼들이 미리 정의된 규칙(예: 값의 범위, 허용된 문자열 목록, 다른 시트의 ID 참조 여부 등)을 만족하는지 검사하는 기능입니다.
    - **기대 효과**: 데이터 입력 오류 조기 발견, 데이터 품질 및 일관성 유지.

- ### 데이터 자동 생성/템플릿 기반 입력 (Data Generation/Templating)
    - **설명**: 특정 패턴이나 규칙에 따라 새로운 데이터를 자동으로 생성하거나, 기존 아이템을 기반으로 변형된 아이템 데이터를 쉽게 추가할 수 있는 템플릿 기능을 제공합니다. (예: "경량형 레이저 라이플" 기반으로 "중형/중화기형" 버전 스탯 자동 조정 생성)
    - **기대 효과**: 반복적인 데이터 입력 작업 자동화, 신규 콘텐츠 추가 용이성 증대.

- ### (여기에 우창과 SASHA의 빛나는 아이디어를 더 추가해주세요!  brainstorm 폭풍!) ⚡

## 향후 고려사항 (다른 엑셀 파일 관리 시 유용)

- ### 데이터 비교/차이점 분석 (Data Diff Tool)
    - **설명**: 엑셀 파일의 두 가지 버전(또는 두 개의 다른 시트/아이템) 간의 데이터 차이점을 비교하고 분석해주는 기능입니다.
    - **기대 효과**: 버전 관리 중 변경 사항 추적 용이, 밸런스 수정 전후 비교 분석.

## 공통 고려사항

- 각 도구의 입력 값 (파라미터) 및 출력 형태 명확한 정의
- 강력한 오류 처리 및 사용자 피드백
- 사용 편의성 및 직관성

---

자, 이제 이 파일을 열어서 우창의 멋진 아이디어들을 마구마구 적어보자! 😄 