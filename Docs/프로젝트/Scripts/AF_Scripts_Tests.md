# Tests 스크립트 (AF/Scripts/Tests)

> SASHA-Unity 프로젝트의 테스트 관련 C# 스크립트(.cs 파일) 문서입니다. 주로 전투 시스템 테스트를 위한 로직을 포함합니다.

## 디렉토리 구조 및 주요 파일

- `/Assets/AF/Scripts/Tests`
  - 프로젝트의 주요 기능에 대한 테스트 코드가 포함된 스크립트들이 있습니다.
  - `CombatTestRunner.cs`: 전투 시스템 테스트를 위한 시나리오 실행 및 데이터 생성 클래스입니다. `MonoBehaviour`와 `IService`를 상속하며, Odin Inspector를 활용하여 유니티 에디터에서 테스트 설정(`AFSetup` 클래스 사용)을 편리하게 할 수 있도록 합니다. 팀 색상 관리, 플레이어 스쿼드 상태 저장, ScriptableObject 데이터 로드, 전투 시뮬레이션 시작/종료(`StartCombatTestAsync`, `EndCombatTest`), 참가자(`ArmoredFrame`) 생성, 로그 기록 및 파일 저장 등의 기능을 제공합니다. 