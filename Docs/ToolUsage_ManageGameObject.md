# GameObject 관리 도구 사용 예시 (manage_gameobject)

이 문서는 `manage_gameobject` 도구를 사용하여 Unity 씬의 게임 오브젝트를 생성, 수정하는 작업, 특히 UI 요소 및 `RectTransform` 관련 작업의 성공적인 예시와 주의점을 기록합니다. 과거 실패 사례를 바탕으로 안정적인 방법을 제시하는 것을 목표로 합니다.

## 1. 기존 오브젝트에 UI 컴포넌트 추가 (RectTransform 확보)

Canvas 하위의 일반 게임 오브젝트가 `RectTransform` 대신 `Transform`을 가지고 있을 수 있습니다. UI 레이아웃 작업을 하려면 `RectTransform`이 필요합니다.

이 경우, 해당 오브젝트에 `modify` 액션으로 `UnityEngine.UI.Image` 같은 **UI 컴포넌트를 추가**하면 유니티가 자동으로 `RectTransform`을 생성하거나 변환해줍니다.

**주의:**
*   오브젝트 탐색 시 `search_method="by_path"`를 사용하고, **하이어라키 경로를 정확히(띄어쓰기 포함)** 입력하는 것이 ID 검색보다 안정적이었습니다.
*   이미 해당 UI 컴포넌트가 있다면 에러가 발생할 수 있습니다.

**성공 예시:**

```json
// MechInfoPanel에 Image 컴포넌트 추가 (RectTransform 확보 목적)
{
  "action": "modify",
  "components_to_add": ["UnityEngine.UI.Image"],
  "search_method": "by_path",
  "target": "@UI/MainCanvas /HangarPanel/MechInfoPanel"
}
```

## 2. RectTransform 속성 수정 (중첩된 UI 요소)

UI 컴포넌트 추가 등으로 `RectTransform`이 확보된 오브젝트의 속성(앵커, 피벗, 오프셋 등)을 수정할 수 있습니다.

**주의:**
*   마찬가지로 `search_method="by_path"`와 정확한 경로 사용을 권장합니다. ID 검색(`search_method="by_id"`)은 불안정한 결과를 보였습니다.
*   `component_properties` 내부에 `"RectTransform"` 키를 사용하고, 그 값으로 속성명과 값을 담은 딕셔너리를 전달합니다.

**성공 예시:**

```json
// MechInfoPanel의 pivot 수정
{
  "action": "modify",
  "component_properties": {
    "RectTransform": {
      "pivot": [0.5, 0.5]
    }
  },
  "search_method": "by_path",
  "target": "@UI/MainCanvas /HangarPanel/MechInfoPanel"
}
```

```json
// MechInfoPanel을 HangarPanel 왼쪽 절반에 맞춤
{
  "action": "modify",
  "component_properties": {
    "RectTransform": {
      "anchorMin": [0, 0],
      "anchorMax": [0.5, 1],
      "offsetMin": [0, 0],
      "offsetMax": [0, 0],
      "pivot": [0.5, 0.5] // pivot도 명시적으로 설정하는 것이 좋을 수 있음
    }
  },
  "search_method": "by_path",
  "target": "@UI/MainCanvas /HangarPanel/MechInfoPanel"
}
```

## 3. 새 UI 오브젝트 생성 (RectTransform 포함)

새 게임 오브젝트를 만들 때 `components_to_add`에 `UnityEngine.UI.Image` 같은 UI 컴포넌트를 포함시키면, 생성된 오브젝트는 자동으로 `RectTransform`을 가지게 됩니다.

**성공 예시:**

```json
// HangarPanel 아래에 Image 컴포넌트를 가진 새 오브젝트 생성
{
  "action": "create",
  "components_to_add": ["UnityEngine.UI.Image"],
  "name": "TestChildObjectWithImage",
  "parent": "@UI/MainCanvas /HangarPanel"
}
```
*주의: 이렇게 생성된 오브젝트의 RectTransform 속성을 바로 수정하려고 할 때 ID 검색이 실패하는 경우가 있었습니다. 경로 검색을 사용하세요.*

## 4. 상위 레벨 UI RectTransform 수정

Canvas 바로 아래에 있는 패널(`TopBarPanel`, `MainMenuPanel` 등)의 `RectTransform` 속성은 비교적 안정적으로 수정되었습니다.

**성공 예시:**

```json
// TopBarPanel을 상단에 고정하고 높이 설정
{
  "action": "modify",
  "component_properties": {
    "RectTransform": {
      "anchorMin": [0, 1],
      "anchorMax": [1, 1],
      "pivot": [0.5, 1],
      "anchoredPosition": [0, 0],
      "sizeDelta": [0, 100] // 높이 100으로 설정 (너비는 앵커에 따라 자동 조절됨)
    }
  },
  "search_method": "by_path",
  "target": "@UI/MainCanvas /TopBarPanel"
}
```

## 5. 중요 참고사항

*   **경로 검색 우선:** 오브젝트 식별 시 ID(`by_id`)보다는 경로(`by_path`) 검색이 더 안정적인 경향을 보였습니다. 경로는 하이어라키 창에 보이는 그대로, 띄어쓰기까지 정확히 입력해야 합니다.
*   **RectTransform 직접 추가 불가:** `RectTransform`은 `components_to_add` 목록에 직접 넣어 추가할 수 없습니다. UI 컴포넌트(Image, Text 등)를 추가하면 유니티가 자동으로 처리합니다.
*   **도구 불안정성:** 특정 상황(특히 중첩된 객체 ID 검색, 속성 수정)에서 도구가 예상과 다르게 동작하거나 실패하는 경우가 있었습니다. 문제가 지속되면 다른 접근 방식을 고려하거나 직접 에디터에서 작업하는 것이 필요할 수 있습니다. 