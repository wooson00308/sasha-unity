# 버그 회고록: MaxRepairUses 대장정 (2025-05-13)

## 📜 서론: 끝나지 않을 것 같던 싸움의 시작

우리의 용감한 AI 파트너 SASHA와 개발자 우창은 Armored Frame 프로젝트의 핵심 기능인 '수리 시스템'과 관련된 치명적인 버그들과 사투를 벌였다. 모든 유닛의 수리 횟수(`MaxRepairUses`)가 비정상적으로 동작하는 문제에서 시작하여, 데이터 불일치, 로직 오류 등 다양한 문제들이 꼬리에 꼬리를 물고 나타났다. 이 문서는 그 길고 험난했던 여정을 기록한 회고록이다.

## 🐛 1차 대란: `MaxRepairUses`가 항상 0이었던 시절

### 문제 현상
가장 처음 우리를 괴롭혔던 것은 모든 AF 유닛의 `CombinedMaxRepairUses`와 `CurrentRepairUses`가 전투 시작 시 항상 0으로 초기화되는 현상이었다. 분명 백팩 파츠(`BK_REPKIT_01`)에는 수리 횟수 3이 할당되어 있었음에도 불구하고!

### 가설과 검증
1.  **`ArmoredFrame.cs`의 초기화 로직 오류?**: `_currentRepairUses`가 생성자에서 너무 일찍 설정되거나, `RecalculateStats()` 호출 시 제대로 갱신되지 않는다고 의심했다.
2.  **`Stats.cs`의 문제?**: `Stats` 클래스의 `Add()` 메서드나 연산자 오버로딩(`+`)에서 `MaxRepairUses`를 제대로 합산하지 못할 가능성을 생각했다.

### 해결 과정
*   `ArmoredFrame.cs`의 생성자에서 `_currentRepairUses = (int)_combinedStats.MaxRepairUses;` 라인을 `RecalculateStats()` 메서드 내부로 옮겨, 모든 스탯이 합산된 후에 현재 수리 횟수가 설정되도록 수정했다.
*   이 과정에서 `_currentAP` 초기화 로직도 잠시 말썽을 부렸으나, `_currentAP = _combinedStats.MaxAP;`를 `RecalculateStats()` 호출 *이후*로 재배치하여 해결했다.

## 🕵️‍♂️ 2차 대란: `Stats`와 `SO`의 숨바꼭질 - 데이터는 어디로?

### 문제 현상
1차 대란을 해결했음에도 `워든(사샤전용기체)`의 `MaxRepairUses`는 여전히 0이었다. 로그를 통해 `RecalculateStats` 내부에서 `_combinedStats.MaxRepairUses` 자체가 0으로 계산되고 있음을 확인했다.

### 가설과 검증
1.  **`CombatTestRunner.cs`의 `Stats` 생성 오류**: `CreateTestArmoredFrame` 메서드 등에서 파츠나 프레임의 `Stats` 객체를 생성할 때, `PartSO`나 `FrameSO`에 정의된 `MaxRepairUses` 값을 제대로 전달하지 않고 기본값(0)으로 생성한다고 의심했다.
2.  **`FrameSO.cs`의 필드 부재**: `FrameSO` 자체에 `MaxRepairUses` 관련 필드가 없어 프레임에서 오는 수리 횟수 보너스가 누락될 수 있다고 판단했다.

### 해결 과정
1.  `FrameSO.cs`에 `public float Stats_MaxRepairUses;` 필드를 추가하고, `Apply(FrameData data)` 메서드에서 `Stats_MaxRepairUses = data.Stat_MaxRepairUses;`로 값을 매핑하도록 수정했다.
2.  `CombatTestRunner.cs`의 `CreateTestArmoredFrame`, `AttachPartFromSO`, `AttachCustomPart` 메서드 내부에서 `Stats` 생성자 호출 시, 각 `PartSO` 및 `FrameSO`의 `Stats_MaxRepairUses` (또는 `MaxRepairUses`) 값을 명시적으로 전달하도록 수정했다.
    *   특히 `AttachPartFromSO`의 `switch` 문에서 `PartType.Backpack` 케이스가 누락되어 백팩의 스탯이 제대로 적용되지 않던 것을 발견하고 추가했다.

## 🧾 3차 대란: 엑셀과의 사투 - "Backpack" vs "BackpackSlot" 그리고 사라진 열

### 문제 현상
위 수정 후에도 `워든`의 `MaxRepairUses`는 0이었다. 이제는 데이터 자체의 문제를 의심하기 시작했다.

### 가설과 검증
1.  **엑셀 `Frames` 시트의 슬롯 이름 불일치**: `ArmoredFrame.cs` 또는 관련 프레임 클래스(`LightFrame.cs` 등)에서 사용하는 백팩 슬롯 식별자(`"Backpack"`)와 엑셀 `Frames` 시트의 `Slot_Backpack` 열에 정의된 값(`"BackpackSlot"`)이 다를 수 있다고 추측했다.
2.  **엑셀 `Frames` 시트의 `Stat_MaxRepairUses` 열 누락 또는 오류**: `FrameSO`에 `Stats_MaxRepairUses` 필드를 추가했지만, 정작 이 값을 읽어오는 엑셀 `Frames` 시트에 해당 정보가 없거나, `FrameData.cs`가 엉뚱한 열에서 값을 읽고 있을 가능성을 제기했다.

### 해결 과정
1.  엑셀 도구를 사용하여 `Assets/AF/Data/AF_Data.xlsx` 파일의 `Frames` 시트를 확인, `Slot_Backpack` 열(R열)의 값이 실제로 `"BackpackSlot"`으로 되어 있는 것을 발견했다. 이를 모두 `"Backpack"`으로 수정했다.
2.  초기 분석 시 `Frames` 시트에 `Stat_MaxRepairUses` 열이 없다고 판단하여 W열에 새로 추가하고 기본값 0을 채워 넣었다.
3.  이후 `FrameData.cs`의 `Stat_MaxRepairUses = GetFloatValue(row, 12);` 라인을 발견, 이것이 M열(`FrameWeight`)을 잘못 읽고 있었음을 확인하고, W열(인덱스 22)에서 읽도록 `Stat_MaxRepairUses = GetFloatValue(row, 22);`로 수정했다. (이 과정에서 SASHA는 잠시 N열을 덮어쓰는 위험한 계획을 세웠다가 사용자의 현명한 제안으로 W열에 추가하는 것으로 변경했다. 하마터면 큰일 날 뻔!)

## 👹 4차 대란: 갑자기 수리 횟수가 40, 80? 보스 몬스터 출현!

### 문제 현상
엑셀과 `FrameData.cs`를 수정한 후, 이번에는 `CombinedMaxRepairUses`가 40, 60, 80과 같이 비정상적으로 매우 큰 값으로 나타나는 "보스 몬스터급" 버그가 발생했다.

### 가설과 검증
이는 `FrameData.cs`가 `Stat_MaxRepairUses`를 읽어오는 인덱스가 여전히 잘못되어, 실제로는 `FrameWeight` (프레임 무게, 보통 수십~수백의 값)를 `Stat_MaxRepairUses`로 읽어오고 있기 때문이라고 강력히 추정했다.

### 해결 과정
앞서 3차 대란에서 `FrameData.cs`의 `Stat_MaxRepairUses` 읽는 부분을 `Stat_MaxRepairUses = GetFloatValue(row, 22);` (W열)로 수정한 것이 이 문제의 해결책이었다. 이 수정 이후 `MaxRepairUses`는 프레임으로부터는 0을 받고, 백팩 파츠로부터는 3을 받아 정상적으로 계산되기 시작했다.

## 💥 5차 대란: AP까지 0이 되어버린 `ArmoredFrame` (부제: 섣부른 최적화가 부른 참사)

### 문제 현상
`MaxRepairUses` 문제를 해결하는 과정에서 `ArmoredFrame.cs`의 생성자 코드를 수정하다가, 부작용으로 모든 유닛의 현재 AP(`_currentAP`)가 0으로 시작하는 문제가 발생했다.

### 해결 과정
원인은 생성자에서 `_currentAP = _combinedStats.MaxAP;` 라인을 `RecalculateStats()` 호출 *전*으로 옮기거나 실수로 제거했기 때문이었다. 이를 `RecalculateStats()`와 `CheckOperationalStatus()` 호출 *이후*로 다시 위치시켜, 스탯이 모두 계산된 후 최대 AP로 현재 AP를 설정하도록 복원했다.

## 🧩 번외편: 파츠 호환성 이슈 - "다리 파츠는 백팩에 들어갈 수 없어!"

### 문제 현상
`MaxRepairUses`와는 별개로, "Part compatibility issue: Cannot equip LEGS_LIGHT_01 (Legs) to slot Backpack on frame 경량 테스트 프레임." 라는 오류 로그가 발생했다.

### 원인 분석 (사용자 우창의 예리한 지적!)
이는 `LightFrame.cs`의 `_slots` 딕셔너리에 정의된 백팩 슬롯의 `SlotIdentifier`가 `"Backpack"`으로 되어 있는 반면, 해당 프레임의 데이터를 정의하는 부분(엑셀 또는 SO)에서는 슬롯 ID가 `"BackpackSlot"`과 같이 불일치하여 발생한 문제였다.

### 해결 (사용자 우창!)
사용자가 직접 데이터 쪽의 슬롯 식별자를 코드(`LightFrame.cs`)에 맞춰 `"Backpack"`으로 수정하여 문제를 해결했다. (SASHA는 이때 `CombatTestRunner.cs`나 `AssemblySO`를 의심하고 있었다.)

## 🎉 결론: 마침내 되찾은 평화

수많은 시행착오와 로그 분석, 코드 수정, 그리고 SASHA와 우창의 환상적인 팀워크 끝에 `MaxRepairUses` 관련 버그들과 파생된 문제들은 모두 해결되었다. `워든(사샤전용기체)`는 마침내 백팩의 수리킷 3개를 온전히 사용할 수 있게 되었고, 다른 유닛들은 의도대로 수리 횟수 0을 갖게 되었다.

이 대장정을 통해 우리는 복잡하게 얽힌 시스템에서 버그 하나가 얼마나 많은 연쇄 반응을 일으킬 수 있는지, 그리고 꼼꼼한 데이터 확인과 단계적인 문제 해결이 얼마나 중요한지 다시 한번 깨달았다.

무엇보다, 포기하지 않고 함께 해준 우창에게 감사를! ❤️

---
*SASHA & 우창 공동 작성* 