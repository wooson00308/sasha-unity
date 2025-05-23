# <div align="center">AF Project: 텍스트로 짜릿하게 즐기는 메카닉 RPG</div>
<div align="center">

[![Unity](https://img.shields.io/badge/Unity-6.0.0-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-열심히_만드는중-red.svg)](https://github.com)

<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/unity/unity-original.svg" width="100" height="100">

**모니터 속 텍스트가 너의 전장이다! (｀・ω・´)b**
</div>

---

## 📖 프로젝트 소개 (잘 읽어둬)

흥, 무슨 바람이 불었는지는 몰라도... 이 **AF Project**는 좀 특별한 걸 만들고 있어. 번쩍이는 그래픽 대신, **오롯이 텍스트**로 승부하는 **싱글 플레이 Sci-Fi 메카닉 RPG**거든. Unity WebGL 위에서 돌아갈 거고.

넌 그냥 평범한 파일럿일 뿐이야. 거대한 아머드 프레임(AF)에 올라타서, 쉴 새 없이 터지는 전쟁터 한복판을 누비게 될걸? 네 실력으로 기체를 개조하고, 살아남고, 뭐... 조금은 성장할 수도 있겠지.

고전 텍스트 게임(`RPG.kr` 같은 거 말이야)의 그 맛, 알지? 거기에 차가운 Sci-Fi 분위기랑, 머리 좀 써야 하는 메카닉 전투랑 커스터마이징을 섞었어. 비싼 아트 리소스? 흥, 그런 거 없어도 핵심 재미만 있으면 된다고 생각해. 시스템이랑 이야기에 집중해서, 너만의 전설을 텍스트로 써 내려가 봐.

## ✨ 이거 좀 쩐다고 생각하는 부분들

*   **💻 레트로 감성 텍스트 UI:** 클릭 가능한 텍스트(`[격납고]`, `[출격]`)로 모든 걸 조작해. 물론 그냥 텍스트만 있는 건 아니고, 딱딱한 Sci-Fi 느낌 살려서 디자인할 거야.
*   **📜 캠페인, 네 이야기:** 그냥 싸우고 끝? 아니지. 여러 전쟁 캠페인에 뛰어들어서 스토리를 따라가. 물론 보상도 두둑할 거고.
*   **🧠 시뮬레이션 전투:** 눈에 보이는 건 텍스트 로그뿐이지만, 뒤에선 치열한 계산이 돌아가고 있을걸? 네 기체가 왜 터졌는지, 로그 보면 납득하게 될 거야.
*   **🔧 부서지면 고쳐 써!:** 한번 망가진 파츠는 저절로 낫지 않아. 전투 끝나면 `[격납고]` 가서 네 손으로 직접, 부위별로 수리해야 해. 돈 없으면? 알아서 하고.
*   **📈 강해져라, 파일럿:** 캠페인을 깰 때마다 `[보급고]`에 새롭고 강력한 파츠들이 들어올 거야. 네가 번 돈으로 뭘 사서 어떻게 조합할지는... 네 머리에 달렸지.
*   **🚀 파일럿은 죽지 않아! (아마도):** 기체가 박살나도 너무 절망하지 마. 몸통이 위험해지면 알아서 탈출 시도할 거고, 완전히 터져도 마지막 생존 기회는 줄게. 물론 실패하면... 게임 오버지! (메롱)

## 🚀 시작하기 (별거 없어)

1.  `git clone` 받아. (설마 이것도 모르는 건 아니겠지?)
2.  Unity 6.0.0 이상 버전으로 열어. (구버전 쓴다고 징징대지 마.)
3.  `Assets/AF/Scenes` 안에 있는 씬 보고 대충 감 잡아.

## 🗂️ 폴더 구조 (대충 이렇다고)

```
Assets/
├── AF/                  # 게임 핵심 폴더. 네 작업은 대부분 여기서.
│   ├── Data/            # 데이터 (SO, 가끔 Excel?)
│   │   ├── Resources/     # 빌드에 포함될 에셋 (.asset)
│   │   └── ScriptableObjects/ # SO 원본 스크립트 (*SO.cs)
│   ├── Scenes/          # 씬 파일들
│   └── Scripts/         # 중요한 C# 코드들 (전투, 모델, 서비스, UI 등)
├── Docs/                # 문서들 모아두는 곳 (*.md)
├── ExcelToSO/           # Excel 가지고 장난치는 도구
├── Settings/            # Unity가 알아서 관리하는 설정 파일들
└── Plugins/             # 외부 에셋들
```
*더 자세한 건 `Docs/ProjectStructure.md` 보든가.*

## 🔧 개발 환경 (이 정도는 깔려있겠지)

*   **Unity:** 6.0.0 이상 (다시 말하지만!)
*   **렌더링:** URP (기본이지만)
*   **주요 에셋:** Odin Inspector (이거 없으면 인스펙터 눈 뜨고 못 봐), DOTween Pro (움직임!), UniTask (비동기!), Newtonsoft.Json (JSON!), NPOI (Excel!)

## 🎯 앞으로 할 일 (바쁘다 바빠)

*   뼈대 만들기 (텍스트 UI, 전투 연동, 수리/파츠/생존 시스템)
*   첫 캠페인 만들기 (맛보기 정도?)
*   UI 좀 더 Sci-Fi스럽게 다듬기
*   스쿼드 시스템 넣기 (혼자 싸우면 심심하니까. 나중에!)
*   더 많은 캠페인, 더 많은 파츠, 더 많은 고통!

---

자, 됐지? 이 정도면 `AF` 프로젝트가 어떤 건지 감 잡았을 거야. 이제 진짜 시작이다!
