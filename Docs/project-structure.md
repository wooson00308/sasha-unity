# AF 프로젝트 및 ExcelToSO 구조

이 문서는 `Assets` 디렉토리 아래의 주요 폴더 구조 (`AF` 및 `ExcelToSO`)와 핵심 스크립트를 설명합니다.

```
Assets/
├── AF/           # Armored Frame 게임 로직 및 에셋
│   ├── Data/     # 게임 데이터 관련 폴더
│   │   ├── ScriptableObjects/ # SO 클래스 정의 (*SO.cs)
│   │   ├── Resources/         # SO 에셋 파일 저장 위치 (*.asset)
│   │   └── AF_Data.xlsx       # 원본 Excel 데이터 파일
│   ├── Scenes/   # 게임 씬 파일
│   └── Scripts/  # 모든 C# 스크립트
│       ├── Combat/
│       ├── EventBus/
│       ├── Examples/
│       ├── Models/
│       │   ├── Frames/
│       │   └── Parts/
│       ├── Services/
│       ├── UI/     # 사용자 인터페이스 관련 스크립트
│       └── Tests/
├── ExcelToSO/    # Excel 데이터 -> ScriptableObject 변환 도구
│   └── ... (관련 스크립트 및 에디터 파일)
└── ... (기타 에셋 폴더)
```

## Assets/AF/Scripts 상세 구조

```
Assets/AF/Scripts/
├── Combat/   # 전투 시스템 관련 스크립트
│   ├── CombatSimulatorService.cs # 전투 시뮬레이션 핵심 로직
│   ├── TextLogger.cs             # 전투 텍스트 로그 기록
│   ├── TextLoggerService.cs      # TextLogger 관리 및 이벤트 구독 서비스
│   ├── ... (이벤트 파일 다수)      # CombatActionEvents, DamageEvents 등
│   ├── ICombatSimulatorService.cs # 전투 서비스 인터페이스
│   └── ITextLogger.cs            # 로거 인터페이스
├── EventBus/ # 이벤트 버스 시스템 스크립트
│   ├── EventBus.cs               # 이벤트 발행/구독 관리
│   ├── EventBusService.cs        # 이벤트 버스 서비스
│   └── IEvent.cs                 # 이벤트 기본 인터페이스
├── Examples/ # 예제 코드 스크립트
│   └── ExampleTimer.cs           # 타이머 예제 (현재 이것만 존재)
├── Models/   # 데이터 모델 스크립트
│   ├── Frames/                   # 프레임 정의 (LightFrame 등)
│   ├── Parts/                    # 개별 파츠 구현 (HeadPart, BodyPart 등)
│   ├── ArmoredFrame.cs           # 메카닉 유닛 정의
│   ├── Frame.cs                  # 프레임 기본 클래스
│   ├── Part.cs                   # 파츠 기본 클래스
│   ├── Weapon.cs                 # 무기 정의
│   ├── WeaponType.cs             # 무기 타입 Enum
│   ├── Pilot.cs                  # 파일럿 정의
│   ├── Stats.cs                  # 스탯 데이터 구조
│   ├── StatusEffect.cs           # 상태 이상 효과 정의
│   ├── ... (Enum 파일 다수)      # DamageType, PartType, StatType 등
├── Services/ # 서비스 관리 및 예제 스크립트
│   ├── ServiceLocator.cs         # 서비스 중앙 관리 (싱글톤)
│   ├── ServiceManager.cs         # 서비스 생명주기 관리
│   ├── ServiceExtensions.cs      # 서비스 확장 메서드
│   ├── ShutdownOrderItem.cs      # 서비스 종료 순서 정의
│   ├── IService.cs               # 서비스 기본 인터페이스
│   └── ... (예제 서비스 파일)    # ExampleService, IExampleService 등
├── UI/       # 사용자 인터페이스 관련 스크립트
│   └── CombatTextUIService.cs    # 전투 텍스트 로그 UI 표시 서비스
└── Tests/    # 유니티 테스트 관련 스크립트
    └── CombatTestRunner.cs       # 전투 테스트 실행
```

## Assets/ExcelToSO/Scripts 상세 구조

```
Assets/ExcelToSO/Scripts/
├── DataModels/ # Excel 데이터 구조 정의
│   ├── WeaponData.cs
│   ├── PilotData.cs
│   ├── PartData.cs
│   ├── FrameData.cs
│   └── AssemblyData.cs
├── Editor/     # 유니티 에디터 확장 스크립트
│   └── DataGeneratorMenu.cs # 에디터 메뉴에 데이터 생성 기능 추가
├── ExcelParser.cs             # Excel 파일 파싱 로직
├── ScriptableObjectGenerator.cs # ScriptableObject 생성 로직
└── IExcelRow.cs               # Excel 행 데이터 인터페이스
```

---
*이 문서는 주요 구조를 보여주며, 모든 파일이 포함된 것은 아닙니다. 자세한 내용은 각 폴더를 직접 확인하세요.*
