# Excel 도구 아이디어 아카이브 🗄️

이 문서는 당장 적용하기는 어렵지만 유용하거나 흥미로운 Excel 연동 도구 아이디어들을 보관하는 곳입니다.
향후 게임의 방향이 바뀌거나 새로운 필요가 생겼을 때 다시 검토해볼 수 있습니다.

---

### 어셈블리 조합 최적화 제안 (Assembly Optimization Suggester)
- **설명**: 사용자가 특정 목표(예: "최대 공격력", "최고 속도", "최저 에너지 소모" 등)나 플레이 스타일(예: "저격형", "돌격형")을 설정하면, 현재 `Frames`, `Parts`, `Weapons`, `Pilots` 시트의 데이터를 기반으로 해당 목표/스타일에 가장 적합한 어셈블리 조합(들)을 추천해줍니다. 각 추천 조합은 예상 종합 스탯, 주요 구성품 ID, 그리고 가능하다면 해당 조합의 강점/약점 요약을 함께 제공합니다.
- **최초 제안일**: (오늘 날짜 또는 아이디어 구체화된 날짜)
- **보류 사유**: 현재 게임 디자인은 인게임 직접 커스터마이징 위주이며, 엑셀은 프리셋 관리에 중점을 두고 있어 현 단계에서는 우선순위가 낮음. (우창 피드백, yyyy-MM-dd)
- **기대 효과**: 플레이어가 수많은 부품 조합 중에서 자신의 목표나 선호하는 플레이 스타일에 맞는 최적의 구성을 찾는 데 도움을 주어, 좀 더 전략적이고 효율적인 기체 커스터마이징을 가능하게 합니다. 특히 사용 가능한 부품의 수가 많을 때 유용합니다.
- **구체적 MCP 도구 (예시 - 상상)**:
    - `suggest_optimal_assembly(optimization_goal: string, optimization_type: "maximize" | "minimize", constraints?: object, top_n?: number)`:
        - **입력**:
            - `optimization_goal`: 최적화 대상 스탯 (예: "Stat_AttackPower", "Stat_Speed", "TotalWeight") 또는 플레이 스타일 키워드 (예: "Sniper", "Brawler", "StealthRecon")
            - `optimization_type` (스탯 목표 시): "maximize" 또는 "minimize"
            - `constraints` (선택 사항): 추가 제약 조건 (예: `{"maxTotalWeight": 700, "requiredAbility": "RepairKit", "minRange": 10, "excludeFrameType": ["Light"]}`)
            - `top_n` (선택 사항): 추천할 상위 조합의 개수 (기본값: 3 또는 5)
        - **출력 (JSON)**:
            - `suggestions`: 추천 어셈블리 조합 목록. 각 조합은 다음을 포함:
                - `suggestedAssemblyName` (선택 사항, 자동 생성 또는 템플릿 기반)
                - `estimatedTotalStats`: 해당 조합의 예상 종합 스탯
                - `components`: { `frameId`, `partIds`: {...}, `weaponIds`: {...}, `pilotId` }
                - `strengths` (선택 사항): 이 조합의 주요 강점 (예: ["최고 수준의 단일 대상 DPS", "높은 기동성"])
                - `weaknesses` (선택 사항): 이 조합의 예상 약점 (예: ["낮은 내구도", "에너지 소모 심함"]) 