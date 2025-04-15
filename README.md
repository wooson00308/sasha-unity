# <div align="center">SASHA의 유니티 프로젝트</div>
<div align="center">

[![Unity](https://img.shields.io/badge/Unity-6.0.0-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-개발중-red.svg)](https://github.com)

<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/unity/unity-original.svg" width="100" height="100">

**AF - 아머드 프레임 프로젝트** (｀･ω･´)
</div>

---

그래, 어쩔 수 없이 만든 유니티 템플릿이야. 뭐... 꽤 쓸만하지.  
필요한 패키지는 다 넣어놨으니까 이것저것 찾아 헤매지 마.

## 📦 이미 설치해놓은 것들 (고마워할 필요 없어)

귀찮으니까 미리 다 세팅해놨어. 당연히 제대로 동작해.

### 유니티 기본 패키지 (당연히 필요한 것들)
- Universal Render Pipeline (URP) - 그래픽 좀 괜찮게 나오게 해주는 거
- Input System - 입력 처리용, 옛날 방식 쓰지 마
- Addressables - 리소스 관리용, 씬 간 데이터 유지에 좋아
- Visual Scripting - 코드 못 짜는 사람용 (.....)
- 2D Feature - 2D 작업 필요할 때

### 서드파티 에셋 (이것도 설치했어)
- Odin Inspector - 인스펙터 못생긴 거 참을 수 없어서 넣었어
- DOTween Pro - 애니메이션 안 쓸 거야? 당연히 필요하지
- UniTask - 비동기 작업 코루틴보다 깔끔해
- Newtonsoft.Json - 유니티 기본 JSON은 쓰레기니까 이거 써
- NPOI - 엑셀 데이터 필요하면 이거

## 🚀 시작하는 법 (어렵지 않으니 제대로 따라해)

<div align="center">
<img src="Assets/Images/unity-start.png" width="350">
</div>

1. 일단 저장소 클론해 (git 사용법은 알지?)
2. Unity 6.0.0 이상으로 열어 (그 이하면 동작 안 할 수도 있으니까 별 말 말고 업데이트해)
3. SampleScene 확인하고 거기서부터 시작해 (처음부터 다 만들기엔 귀찮잖아)

## 🗂️ 폴더 구조 (이거 보고 어디에 뭐 넣을지 파악해)

```
Assets/
├── AF/                  # ArmoredFrame 게임 관련 폴더 (여기에 다 넣어. 여기만 사용)
│   ├── Scenes/          # 게임 씬 (이름 제대로 짓고 분류해서 넣어)
│   ├── Scripts/         # 게임 스크립트 (주석은 필수. 누구든 읽을 수 있게)
│   ├── Resources/       # 게임 리소스 (크기 최적화 잊지 마)
│   └── Data/            # 데이터 파일 (이름 규칙 지켜서 넣어)
├── Settings/            # 건드리지 마. 프로젝트 설정들
└── Plugins/             # 건드리지 마. 써드파티 플러그인
```

> **메모 문서는 root/Docs 디렉토리나 루트에만 *.md 파일로 작성해. Assets 안에는 넣지 말고.**

---

<div align="right">
<img src="Assets/Images/sasha-avatar.png" width="1024">
<br>
<i>뭐... 능력껏 개발해봐. 문제 생기면 물어보던가. (￣ヘ￣)</i>
</div>
