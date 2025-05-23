# SASHA-Unity 프로젝트 구조

> 우창과 SASHA가 함께 개발하는 유니티 프로젝트의 전체 구조를 간략하게 소개하고, 핵심 C# 스크립트(.cs 파일)의 주요 위치를 안내하는 문서입니다. 각 스크립트 영역의 상세 내용은 개별 링크를 참고하세요.

**주요 특징:** 턴제 전술 게임으로 **멀티액션 시스템**(while 루프와 AP 소모 기반, ActionType별 1회 제한)과 **AI 최적화**(실패 추적을 통한 무한 루프 방지, 순수 데미지 기반 무기 선택, AP-거리 연동 이동 로직, 선택 무기 기반 카이팅)가 구현되어 있어 균형잡힌 전투와 전술적 AI 행동을 제공합니다.

## 핵심 스크립트 파일 위치 (.cs 파일 중심)

- `/Assets`
  - 게임 개발에 사용되는 모든 자산이 위치하는 곳. 프로젝트의 **핵심 C# 스크립트(.cs 파일)는 대부분 이 아래에 있습니다.**
  - `/AF`
    - 게임의 핵심 로직 및 콘텐츠 자산
    - `/Scripts`
      - **게임의 주요 로직, 컴포넌트, 시스템 등을 구현한 핵심 C# 스크립트들이 위치하는 루트 디렉토리입니다.**
      - 이 디렉토리의 상세 구조 및 스크립트 목록은 아래 각 문서에서 확인하세요.
        - [AI BehaviorTree 스크립트 상세](./Scripts/AF_Scripts_AI_BehaviorTree.md)
        - [Combat 스크립트 상세](./Scripts/AF_Scripts_Combat.md)
        - [Models 스크립트 상세](./Scripts/AF_Scripts_Models.md)
        - [Sound 스크립트 상세](./Scripts/AF_Scripts_Sound.md)
        - [Tests 스크립트 상세](./Scripts/AF_Scripts_Tests.md)
        - [UI 스크립트 상세](./Scripts/AF_Scripts_UI.md)
  - `/ExcelToSO`
    - Excel 데이터를 Unity ScriptableObject로 변환하는 툴 관련 자산
    - `/Scripts`
      - **Excel 데이터를 Unity ScriptableObject로 변환하는 데 사용되는 에디터 C# 스크립트들이 있습니다.**
      - 이 디렉토리의 상세 구조 및 스크립트 목록은 아래 문서에서 확인하세요.
      - [ExcelToSO 스크립트 상세](./Scripts/ExcelToSO_Scripts.md)


</rewritten_file> 