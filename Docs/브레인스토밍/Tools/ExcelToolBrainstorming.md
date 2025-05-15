# AF_Data.xlsx 연동 도구 아이디어 브레인스토밍

**💡 아이디어 구상 워크플로우 제안:**
1. 새로운 아이디어가 떠오르면 먼저 `ExcelToolIdeaArchive.md` (지식 창고)에 자유롭게 기록합니다.
2. 해당 아이디어가 구체화되고 실제 구현을 고려할 단계가 되면, 이 `ExcelToolBrainstorming.md` 파일로 옮겨와서 세부 사항을 논의하고 발전시킵니다.

기존에 구현된 `get_sheet_names`와 `read_sheet_data` 외에, `AF_Data.xlsx` 파일을 더 효과적으로 활용하고 관리하기 위한 추가 도구 아이디어를 자유롭게 적어주세요! 우리만의 강력한 툴셋을 만들어봅시다! 💪

## 현재 구현된 도구

1.  **`get_sheet_names`**: 엑셀 파일 내의 모든 시트 이름을 가져옵니다. (✅ 완료)
2.  **`read_sheet_data`**: 특정 시트의 모든 데이터를 JSON 형태로 읽어옵니다. (✅ 완료)
3.  **`get_entity_details(sheet_name: string, entity_id: string, id_column_name: string)`**: 특정 시트에서 ID에 해당하는 행의 모든 데이터를 가져옵니다. (✅ 완료 - 내부 로직 `_findEntityById`로 리팩토링 완료)
4.  **`update_entity_stat(sheet_name: string, entity_id: string, stat_column: string, new_value: string)`**: 특정 시트에서 지정된 ID를 가진 엔티티(행)의 특정 열(stat) 값을 수정합니다. (✅ 완료 - 2025-05-14, 기존 문서 기준)
5.  **`get_grouped_stats_summary(sheet_name: string, group_by_column: string, stat_columns_to_analyze: string[])`**: 특정 기준으로 아이템들을 그룹화하여 주요 스탯의 요약(개수, 합, 평균, 최소/최대)을 제공합니다. (✅ 완료 - 신규 구현)
6.  **`get_assembly_details(assembly_id: string, include_component_details?: boolean)`**: `AF_Assemblies` 시트의 특정 어셈블리 ID를 기반으로, 구성 요소(프레임, 파츠, 파일럿)의 스탯을 합산하고 어빌리티를 종합하여 보여줍니다. `include_component_details` 플래그로 상세 정보 포함 여부를 제어합니다. (✅ 완료 - 신규 구현 및 개선)
7.  **`create_new_entity(sheet_name: string, entity_data: Record<string, any>, file_path?: string)`**: 지정된 시트에 새로운 엔티티(행)를 추가합니다. (✅ 완료 - 신규 구현, 2025-05-15)
8.  **`delete_entity(sheet_name: string, entity_id: string, id_column_name: string, file_path?: string)`**: 지정된 시트에서 ID에 해당하는 특정 엔티티(행)를 삭제합니다. (✅ 완료 - 신규 구현, 2025-05-15)

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

- ### 새로운 엔티티(행) 추가 (Create New Entity)
    - **설명**: 지정된 시트에 새로운 엔티티(행)를 추가합니다. 사용자는 새 엔티티의 각 컬럼에 해당하는 데이터를 객체 형태로 제공해야 합니다. 이 기능은 향후 "템플릿 기반 입력" 또는 "데이터 자동 생성" 기능의 기초가 될 수 있습니다.
    - **기대 효과**: MCP 클라이언트를 통해 엑셀 파일을 직접 열지 않고도 새로운 부품, 무기, 프레임 등의 기본 데이터를 빠르게 추가할 수 있게 됩니다. 반복적인 데이터 입력 작업을 줄이고, 데이터 생성 프로세스를 자동화하거나 반자동화하는 첫걸음입니다.
    - **구체적 MCP 도구 (예시)**:
        - `create_new_entity(sheet_name: string, entity_data: Record<string, any>, file_path?: string)`:
            - **입력**:
                - `sheetName`: 데이터를 추가할 시트 이름 (예: "Parts", "Weapons")
                - `entityData`: 추가할 엔티티의 데이터. 객체 형태이며, 키는 컬럼 이름, 값은 해당 컬럼의 값이 됩니다. (예: `{"PartID": "NEW_PART_01", "PartName": "신규 테스트 파츠", "PartType": "Head", "Stat_Defense": 10}`)
                - `filePath` (선택 사항): Excel 파일 경로. 기본값 사용 가능.
            - **출력 (JSON)**:
                - 성공 시: { `message`: "Successfully added new entity to sheet 'SheetName'." }
                - 실패 시: { `error`: "Error message..." }

- ### 특정 엔티티(행) 삭제 (Delete Entity)
    - **설명**: 지정된 시트에서 ID에 해당하는 특정 엔티티(행)를 완전히 삭제합니다.
    - **기대 효과**: 불필요하거나 잘못 입력된 데이터를 엑셀 파일에서 직접적인 조작 없이 안전하게 제거할 수 있게 됩니다. 데이터 정리를 용이하게 합니다.
    - **구체적 MCP 도구 (예시)**:
        - `delete_entity(sheet_name: string, entity_id: string, id_column_name: string, file_path?: string)`:
            - **입력**:
                - `sheetName`: 데이터를 삭제할 시트 이름 (예: "Parts", "Weapons")
                - `entityId`: 삭제할 엔티티의 ID (예: "ARM_LIGHT_RAPID_01")
                - `idColumnName`: 엔티티 ID가 포함된 컬럼의 이름 (예: "PartID")
                - `filePath` (선택 사항): Excel 파일 경로. 기본값 사용 가능.
            - **출력 (JSON)**:
                - 성공 시: { `message`: "Successfully deleted entity 'EntityID' from sheet 'SheetName'." }
                - 실패 시 (ID 없음 등): { `error`: "Entity not found or error message..." }

- ### (여기에 우창과 SASHA의 빛나는 아이디어를 더 추가해주세요!  brainstorm 폭풍!) ⚡

- ### 어셈블리 종합 스탯 및 어빌리티 조회 (Assembly Total Stats & Abilities Viewer)
    - **설명**: `AF_Assemblies` 시트의 특정 어셈블리 ID를 입력하면, 해당 어셈블리를 구성하는 프레임, 모든 장착 파츠(헤드, 바디, 팔, 다리, 백팩 등), 그리고 파일럿의 개별 스탯과 어빌리티를 모두 가져옵니다. 더 나아가, 이 모든 구성 요소들의 스탯을 합산하고, 모든 어빌리티를 종합하여 해당 어셈블리의 최종적인 종합 스탯과 보유 어빌리티 목록을 명확하게 보여줍니다.
    - **기대 효과**: 특정 기체 조합의 최종 성능(스탯 합계, 보유 어빌리티)을 한눈에 파악할 수 있게 되어, 기체 세팅 및 밸런스 조정 작업의 효율을 크게 높일 수 있습니다. 엑셀 파일을 직접 열어 여러 시트를 넘나들며 계산할 필요가 없어집니다.
    - **구체적 MCP 도구 (예시)**:
        - `get_assembly_details(assembly_id: string)`:
            - **입력**:
                - `assembly_id`: `AF_Assemblies` 시트의 어셈블리 ID (예: "ASM_TEST_01")
                - `filePath` (선택 사항): Excel 파일 경로
            - **출력 (JSON)**:
                - `assemblyName`: 어셈블리 이름 (선택 사항, `AF_Assemblies`에서 가져올 수 있다면)
                - `components`: (각 구성품의 상세 정보 및 스탯)
                    - `frame`: { `id`, `name`, `type`, `stats`, `abilities` }
                    - `parts`: [ { `id`, `name`, `partType`, `stats`, `abilities` }, ... ] // HEAD, BODY, ARM_L, ARM_R, LEGS, BACKPACK 등
                    - `pilot`: { `id`, `name`, `stats`, `abilities` } // 파일럿 정보가 있다면
                - `totalCalculatedStats`: 모든 스탯의 합산 값 (예: `Total_HP`, `Total_AttackPower`, `Total_Weight`, `Total_EnergyEff_Modifier` 등). 스탯 이름은 명확하게 접두사(Total_)를 붙여 구분.
                - `allUniqueAbilities`: 모든 구성품에서 중복 제거된 어빌리티 목록 (예: `["Zoom", "RepairKit", "EnergyShield"]`)

- ### 내부 로직 재사용 및 모듈화 계획
    - **목표**: 여러 MCP 도구(특히 `get_entity_details`, 그리고 앞으로 만들 `get_assembly_details`)에서 중복될 수 있는 핵심 로직(예: 특정 시트에서 ID 기반으로 엔티티 상세 정보 조회)을 공통 함수로 분리하여 코드 재사용성을 높이고 유지보수를 용이하게 한다.
    - **대상 로직**:
        - `get_entity_details` 도구의 핵심 기능: 시트 이름, ID 컬럼 이름, 엔티티 ID를 받아 해당 엔티티의 전체 데이터를 반환하는 로직.
    - **구현 방안**:
        1.  `excelGetTools.ts` (또는 향후 유틸리티 파일 `excelUtils.ts` 등)에 위 대상 로직을 수행하는 내부 헬퍼 함수 (예: `_findEntityById(worksheet: Worksheet, entityId: string, idColumnName: string, headerRowValues: CellValue[]): Record<string, any> | null`)를 정의한다. 이 함수는 워크시트 객체, 찾을 ID, ID가 있는 컬럼명, 헤더 정보를 받아서 일치하는 행의 데이터를 객체로 반환하거나 없으면 null을 반환한다.
        2.  기존 `get_entity_details` 도구는 이 헬퍼 함수를 호출하도록 수정한다.
        3.  향후 개발될 `get_assembly_details` 도구에서도 프레임, 파츠, 파일럿 정보를 가져올 때 이 헬퍼 함수를 재사용한다.
    - **기대 효과**: 코드 중복 감소, 유지보수성 향상, 각 도구 로직 간결화.

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