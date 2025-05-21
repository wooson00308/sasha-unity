# 리팩토링: 상태 이상 처리 시스템 개선

## 1. 현황 및 문제점

현재 상태 이상 처리 시스템은 `StatusEffectProcessor` 내에서 특정 효과 이름(문자열)을 기반으로 분기하여 개별 로직을 수행하는 방식으로 구현되어 있습니다. 예를 들어, `Tick` 메서드 내에서 `effect.EffectName.Contains("DamageOverTime")`과 같이 문자열 포함 여부를 확인하여 지속 피해 로직을 처리하고 있습니다.

이 방식의 주요 문제점은 다음과 같습니다.

-   **낮은 확장성**: 새로운 종류의 상태 이상 효과(특히 틱 효과나 특수 로직이 필요한 효과)를 추가할 때마다 `StatusEffectProcessor` 클래스를 직접 수정해야 합니다. 이는 클래스의 SRP(단일 책임 원칙)를 위배하고 클래스를 비대하게 만들 수 있습니다.
-   **유지보수 어려움**: 효과 종류가 많아질수록 `StatusEffectProcessor` 내의 분기문이 복잡해지고, 특정 효과의 로직을 찾거나 수정하기 어려워집니다.
-   **문자열 기반 비교의 취약성**: 효과 이름 변경 시 관련 로직이 누락될 수 있으며, 오타 등으로 인해 버그가 발생할 가능성이 있습니다.
-   **`StatusEffect` 클래스의 잠재력 미활용**: `StatusEffect` 클래스에는 `EffectType` (enum) 필드가 이미 존재하여 상태 이상의 종류를 명확히 구분할 수 있지만, 현재 이 정보가 효과 처리 로직과 직접적으로 연동되지 않고 있습니다.

## 2. 개선 목표

-   **확장성 높은 구조**: 새로운 상태 이상 효과를 추가할 때 기존 코드를 최소한으로 수정하거나, 새로운 처리기 클래스 추가만으로 가능하도록 개선합니다.
-   **유지보수 용이성 향상**: 각 상태 이상 효과의 처리 로직을 명확하게 분리하여 코드 가독성 및 수정 용이성을 높입니다.
-   **타입 안전성 확보**: 문자열 비교 대신 `EffectType` 열거형 또는 다른 타입 안전한 방식을 사용하여 효과를 식별하고 처리합니다.
-   **SRP 준수**: `StatusEffectProcessor`는 상태 효과의 생명 주기(지속 시간 관리, 만료 처리) 및 효과 실행 위임에만 집중하고, 개별 효과의 구체적인 로직은 별도의 처리기(Handler) 또는 `StatusEffect` 객체 자체로 분산시킵니다.

## 3. 리팩토링 전략

### 3.1. 상태 이상 효과 처리기(Handler) 도입 (전략 패턴 활용)

-   각 `StatusEffectType`에 대응하는 개별 처리기 클래스를 정의합니다.
-   공통 인터페이스 `IStatusEffectHandler` (가칭)를 정의하고, 각 처리기는 이 인터페이스를 구현합니다.
    ```csharp
    // 예시 인터페이스
    public interface IStatusEffectHandler
    {
        void OnApply(ArmoredFrame target, StatusEffect effect, CombatContext ctx); // 효과 적용 시
        void OnTick(ArmoredFrame target, StatusEffect effect, CombatContext ctx);  // 턴마다 틱 발생 시
        void OnExpire(ArmoredFrame target, StatusEffect effect, CombatContext ctx); // 효과 만료 시
        void OnRemove(ArmoredFrame target, StatusEffect effect, CombatContext ctx); // 효과 (강제) 제거 시
    }
    ```
-   `StatusEffectProcessor`는 `EffectType`에 따라 적절한 핸들러를 호출하거나, `StatusEffect` 객체 자체가 자신의 핸들러를 참조하도록 할 수 있습니다.

### 3.2. `StatusEffect` 데이터 구조 및 역할 강화

-   `StatusEffect` 클래스에 자신의 `EffectType`에 맞는 `IStatusEffectHandler` 인스턴스를 참조할 수 있는 필드를 추가하거나, 생성 시점에 핸들러를 주입받도록 합니다.
-   또는, `StatusEffect`가 효과 실행에 필요한 모든 데이터를 포함하고, `StatusEffectProcessor` 또는 범용 핸들러가 이 데이터를 해석하여 공통 로직을 수행하도록 합니다. (예: `StatToModify`, `ModificationType`, `ModificationValue` 필드를 활용한 스탯 변경)

### 3.3. `StatusEffectProcessor` 역할 재정의

-   `Tick` 메서드에서는 각 `StatusEffect`의 지속 시간을 관리하고, 만료된 효과를 제거하며, `StatusEffectExpiredEvent`를 발행합니다.
-   틱 효과가 있는 경우, 해당 `StatusEffect`에 연결된 `OnTick` 핸들러를 호출하거나, `StatusEffectTickEvent`를 발행하여 다른 시스템(또는 해당 핸들러)이 처리하도록 합니다.
-   새로운 효과 적용/해제 시에는 직접적인 로직 처리보다는, 해당 `StatusEffect`에 연결된 `OnApply`/`OnRemove` 핸들러를 호출하거나 관련 이벤트를 발행하는 데 집중합니다.

### 3.4. 이벤트 기반 연동 강화

-   `StatusEffectAppliedEvent`, `StatusEffectExpiredEvent`, `StatusEffectTickEvent` 등을 더욱 적극적으로 활용합니다.
-   `ArmoredFrame`이나 관련 UI 시스템 등은 이 이벤트들을 구독하여 필요한 상태 변경(스탯 재계산, UI 업데이트 등)을 수행합니다.

## 4. 단계별 리팩토링 계획

1.  **`IStatusEffectHandler` 인터페이스 정의**:
    -   `OnApply`, `OnTick`, `OnExpire`, `OnRemove` 등의 핵심 메서드를 포함합니다. (초기에는 `OnTick`과 `OnApply/OnExpire` 정도만 필요할 수 있음)

2.  **기존 효과에 대한 핸들러 구현**:
    -   `DamageOverTimeHandler`: `OnTick`에서 피해를 적용하고 `DamageAppliedEvent` 등을 발생시킵니다.
    -   `RepairOverTimeHandler`: `OnTick`에서 수리를 적용하고 `RepairAppliedEvent` 등을 발생시킵니다.
    -   `DefenseBoostHandler`:
        -   `OnApply`: 대상 유닛의 방어력 스탯을 수정 (직접 또는 `ArmoredFrame`의 메서드 호출).
        -   `OnExpire`/`OnRemove`: 적용했던 방어력 스탯 수정을 원래대로 되돌립니다.
        -   (틱 효과는 없으므로 `OnTick`은 비워둡니다.)
    -   다른 버프/디버프 효과들도 유사한 방식으로 스탯 변경 로직을 핸들러의 `OnApply`/`OnExpire`로 옮깁니다.

3.  **`StatusEffect` 클래스 수정**:
    -   선택 1: `public IStatusEffectHandler Handler { get; private set; }` 필드 추가. 생성자에서 `EffectType`에 따라 적절한 핸들러 인스턴스를 주입받도록 수정합니다.
    -   선택 2: `EffectType`만 유지하고, `StatusEffectProcessor`가 `EffectType`에 따라 핸들러를 매핑하는 딕셔너리를 갖도록 합니다.

4.  **`StatusEffectProcessor.Tick` 메서드 리팩토링**:
    -   문자열 비교 대신, 각 `StatusEffect` 객체의 `Handler.OnTick(...)`을 호출하거나, `EffectType`에 맞는 핸들러를 찾아 호출합니다.
    -   지속 시간 관리 및 만료된 효과의 `Handler.OnExpire(...)` 호출 로직을 구현합니다.

5.  **`ArmoredFrame.AddStatusEffect` / `ArmoredFrame.RemoveStatusEffect` 수정**:
    -   효과 추가 시: 해당 `StatusEffect`의 `Handler.OnApply(...)`를 호출합니다.
    -   효과 제거 시: 해당 `StatusEffect`의 `Handler.OnRemove(...)`를 호출합니다.
    -   기존의 `RecalculateStats()` 직접 호출은 핸들러 내부 또는 이벤트 구독 방식으로 변경될 수 있습니다.

6.  **`CombatActionExecutor.ApplyDefendStatus`와 같은 직접 효과 적용 로직 변경**:
    -   `StatusEffect` 객체를 생성하고 `target.AddStatusEffect(effect)`를 호출하는 방식으로 통일합니다. 실제 효과 적용 로직은 `DefenseBoostHandler.OnApply` 등으로 위임됩니다.

7.  **테스트 및 검증**:
    -   기존 상태 이상 효과들이 모두 정상적으로 작동하는지 확인합니다.
    -   새로운 상태 이상 효과를 추가하는 시나리오를 통해 확장성이 확보되었는지 검증합니다.

## 5. 기대 효과

-   각 상태 이상의 로직이 분리되어 코드 이해와 관리가 용이해집니다.
-   새로운 상태 이상 효과를 추가할 때, 새로운 핸들러 클래스를 만들고 등록하는 것만으로 확장이 가능해집니다.
-   `StatusEffectProcessor`의 복잡도가 낮아지고 역할이 명확해집니다.
-   전반적인 시스템의 안정성과 유지보수성이 향상됩니다.

## 6. 추가 고려 사항

-   상태 효과 핸들러를 등록하고 관리하는 `StatusEffectHandlerRegistry` 같은 별도 클래스를 도입할 수 있습니다.
-   `StatusEffectSO` (ScriptableObject)를 활용하여 각 `EffectType`에 대한 기본 설정(기본 지속시간, 핸들러 타입 등)을 데이터로 관리하는 방안도 고려할 수 있습니다.

--- 