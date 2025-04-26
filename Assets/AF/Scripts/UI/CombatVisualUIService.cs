using AF.Services;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI; // UGUI 관련 네임스페이스 추가
using AF.Combat; // Combat 이벤트 네임스페이스 추가
using AF.EventBus;
using System.Collections.Generic; // Dictionary 사용 위해 추가 -> List 사용으로 변경
using TMPro; // TextMeshPro 사용
using AF.Models; // ArmoredFrame 참조 위해 추가

namespace AF.UI
{
    public class CombatVisualUIService : MonoBehaviour, IService
    {
        [Title("UI Prefab References")] // 필드 이름 변경
        [Required]
        [SerializeField] private GameObject unitInfoPanelPlayerPrefab;
        [Required]
        [SerializeField] private GameObject unitInfoPanelTargetPrefab;
        [Required]
        [SerializeField] private GameObject actionSkillBarPrefab;
        [Required]
        [SerializeField] private GameObject condensedLogPanelPrefab;
        [Required]
        [SerializeField] private GameObject tooltipPanelPrefab;
        [Required]
        [SerializeField] private Transform uiCanvasTransform;

        // --- UI Component References (Prefabs need these components with specified names/paths) ---
        // WARNING: Directly referencing components here couples the Service to specific UI implementations.
        // Consider using separate Controller classes for each panel/bar for better separation of concerns.
        [Header("Player Panel Components")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Slider _playerHpSlider;
        [SerializeField] private TextMeshProUGUI _playerHpText;
        [SerializeField] private Slider _playerApSlider;
        [SerializeField] private TextMeshProUGUI _playerApText;
        [SerializeField] private Transform _playerStatusEffectContainer; // Status Icon Prefab 인스턴스화 위치

        [Header("Target Panel Components")]
        [SerializeField] private TextMeshProUGUI _targetNameText;
        [SerializeField] private Slider _targetHpSlider;
        [SerializeField] private TextMeshProUGUI _targetHpText;
        [SerializeField] private Slider _targetApSlider;
        [SerializeField] private TextMeshProUGUI _targetApText;
        [SerializeField] private Transform _targetStatusEffectContainer;

        [Header("Action Skill Bar Components")]
        [SerializeField] private List<Button> _actionSkillButtons; // 스킬 버튼 리스트 (프리팹에 맞게 구성 필요)
        [SerializeField] private List<TextMeshProUGUI> _actionSkillApCostTexts; // 스킬 AP 소모량 텍스트

        [Header("Condensed Log Panel Components")]
        [SerializeField] private TextMeshProUGUI _condensedLogText;
        private Queue<string> _logQueue = new Queue<string>();
        private const int MAX_LOG_LINES = 3; // const로 변경

        private AF.EventBus.EventBus _eventBus;
        private ArmoredFrame _currentPlayerUnit; // 편의상 플레이어 유닛 참조 저장
        private ArmoredFrame _currentTargetUnit; // 편의상 현재 타겟 유닛 참조 저장

        // 인스턴스화된 UI 컨트롤러 참조
        private UnitInfoPanelController _playerPanelController;
        private UnitInfoPanelController _targetPanelController;
        private ActionSkillBarController _actionSkillBarController;
        private CondensedLogPanelController _condensedLogPanelController;
        private TooltipController _tooltipController;

        // 유닛과 UI 컨트롤러 매핑 (다대다 전투 시 필요)
        private Dictionary<ArmoredFrame, UnitInfoPanelController> _unitPanelMap = new Dictionary<ArmoredFrame, UnitInfoPanelController>();

        public void Initialize()
        {
            Debug.Log("CombatVisualUIService Initialized.");
            if (uiCanvasTransform == null) {
                Debug.LogError("UI Canvas Transform is not assigned!");
                uiCanvasTransform = FindObjectOfType<Canvas>()?.transform;
                 if (uiCanvasTransform == null) {
                     Debug.LogError("Cannot find Canvas!");
                     return;
                 }
            }

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus;
            SubscribeToEvents();

            // UI 프리팹 인스턴스화 및 컨트롤러 참조 얻기
            _playerPanelController = InstantiateAndGetController<UnitInfoPanelController>(unitInfoPanelPlayerPrefab, uiCanvasTransform);
            _targetPanelController = InstantiateAndGetController<UnitInfoPanelController>(unitInfoPanelTargetPrefab, uiCanvasTransform);
            _actionSkillBarController = InstantiateAndGetController<ActionSkillBarController>(actionSkillBarPrefab, uiCanvasTransform);
            _condensedLogPanelController = InstantiateAndGetController<CondensedLogPanelController>(condensedLogPanelPrefab, uiCanvasTransform);
            _tooltipController = InstantiateAndGetController<TooltipController>(tooltipPanelPrefab, uiCanvasTransform);

            // 다대다 전투 대비 초기화 (예시, 현재는 1v1 기준)
            _unitPanelMap.Clear();
            if (_playerPanelController != null) _playerPanelController.gameObject.SetActive(false); // 시작 시 비활성화
            if (_targetPanelController != null) _targetPanelController.gameObject.SetActive(false);
            if (_actionSkillBarController != null) _actionSkillBarController.gameObject.SetActive(false);
            if (_condensedLogPanelController != null) _condensedLogPanelController.gameObject.SetActive(false);
            if (_tooltipController != null) _tooltipController.Hide();
        }

        public void Shutdown()
        {
            Debug.Log("CombatVisualUIService Shutdown.");
            UnsubscribeFromEvents();
            // 컨트롤러 오브젝트 제거
            DestroyControllerObject(_playerPanelController);
            DestroyControllerObject(_targetPanelController);
            DestroyControllerObject(_actionSkillBarController);
            DestroyControllerObject(_condensedLogPanelController);
            DestroyControllerObject(_tooltipController);
             _unitPanelMap.Clear();
        }

        private void ValidateComponentReferences()
        {
             // 각 SerializeField 컴포넌트가 할당되었는지 확인하는 코드
             if (_playerNameText == null) Debug.LogError("Player Name Text not assigned!");
             if (_playerHpSlider == null) Debug.LogError("Player HP Slider not assigned!");
             if (_playerHpText == null) Debug.LogError("Player HP Text not assigned!");
             if (_playerApSlider == null) Debug.LogError("Player AP Slider not assigned!");
             if (_playerApText == null) Debug.LogError("Player AP Text not assigned!");
             if (_playerStatusEffectContainer == null) Debug.LogError("Player Status Effect Container not assigned!");
             // ... Target, Action Bar, Log Panel 컴포넌트 검사 추가 ...
             if (_condensedLogText == null) Debug.LogError("Condensed Log Text not assigned!");
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStart);
            _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);
            _eventBus.Subscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
        }

        private void UnsubscribeFromEvents()
        {
             if (_eventBus == null) return;

            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStart);
            _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompleted);
            _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageApplied);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectAppliedEvent>(HandleStatusEffectApplied);
            _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyed);
            _eventBus.Unsubscribe<StatusEffectEvents.StatusEffectTickEvent>(HandleStatusEffectTick);
        }

        // --- Event Handlers ---

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent ev)
        {
            Debug.Log("CombatVisualUIService: Combat Started");
             _unitPanelMap.Clear(); // 새 전투 시작 시 맵 클리어

            // 모든 참가자에 대해 패널 설정 시도 (다대다 확장 고려)
            // 현재는 플레이어(teamID 0)와 첫 번째 적(teamID 1)만 처리
            ArmoredFrame playerUnit = null;
            ArmoredFrame targetUnit = null;

             foreach(var unit in ev.Participants)
             {
                 // TeamId 속성 직접 사용
                 if (unit.TeamId == 0 && _playerPanelController != null)
                 {
                     playerUnit = unit;
                     _playerPanelController.InitializePanel(unit);
                     _unitPanelMap[unit] = _playerPanelController; // 맵에 추가
                 }
                 else if (unit.TeamId == 1 && _targetPanelController != null && targetUnit == null) // 첫 번째 적만 타겟 패널 사용
                 {
                      targetUnit = unit;
                     _targetPanelController.InitializePanel(unit);
                     _unitPanelMap[unit] = _targetPanelController; // 맵에 추가
                 }
                 // else { // 다른 팀 ID 또는 추가 적에 대한 처리 (패널 동적 생성 등) }
             }

            if (_actionSkillBarController != null && playerUnit != null)
            {
                _actionSkillBarController.UpdateBar(playerUnit);
                _actionSkillBarController.gameObject.SetActive(true);
            }
             if (_condensedLogPanelController != null)
             {
                  _condensedLogPanelController.ClearLog(); // 이전 로그 지우기
                  _condensedLogPanelController.AddLog("Combat Started!");
                  _condensedLogPanelController.gameObject.SetActive(true);
             }
        }

        private void HandleTurnStart(CombatSessionEvents.TurnStartEvent ev)
        {
            if (ev.ActiveUnit == null) return;
            Debug.Log($"CombatVisualUIService: Turn {ev.TurnNumber} Started for Unit {ev.ActiveUnit.Name}");

            if (_unitPanelMap.TryGetValue(ev.ActiveUnit, out var panel))
            {
                 panel.UpdateAp();
                 // panel.SetHighlight(true); // 턴 표시 하이라이트 (컨트롤러에 구현 필요)
            }
            // 이전 턴 유닛 하이라이트 제거 로직 필요

            // 플레이어 턴일 때만 스킬 바 업데이트
            if (ev.ActiveUnit.TeamId == 0 && _actionSkillBarController != null)
            {
                 _actionSkillBarController.UpdateBar(ev.ActiveUnit);
            }

             if (_condensedLogPanelController != null)
             {
                  _condensedLogPanelController.AddLog($"Turn {ev.TurnNumber}: {ev.ActiveUnit.Name}");
             }
        }

        private void HandleActionCompleted(CombatActionEvents.ActionCompletedEvent ev)
        {
             if (ev.Actor == null) return;
             Debug.Log($"CombatVisualUIService: Action {ev.Action} executed by {ev.Actor.Name}");

             if (_unitPanelMap.TryGetValue(ev.Actor, out var executorPanel))
             {
                  executorPanel.UpdateAp();
             }

            if (ev.Actor.TeamId == 0 && _actionSkillBarController != null)
            {
                 _actionSkillBarController.UpdateBar(ev.Actor); // 행동 후 스킬 바 업데이트
            }

             if (_condensedLogPanelController != null)
             {
                 // TODO: actionType을 실제 액션 이름으로 변환하는 로직 필요
                 string actionName = ev.Action.ToString(); // 임시
                 string logMsg = $"{ev.Actor.Name} used {actionName}";
                 logMsg += $". Result: {ev.ResultDescription}"; // 결과 설명 추가
                  _condensedLogPanelController.AddLog(logMsg);
             }
        }

        private void HandleDamageApplied(DamageEvents.DamageAppliedEvent ev)
        {
             if (ev.Target == null) return;
             Debug.Log($"CombatVisualUIService: Damage {ev.DamageDealt} applied to {ev.Target.Name}");

             if (_unitPanelMap.TryGetValue(ev.Target, out var targetPanel))
             {
                 targetPanel.UpdateHp();
                 // targetPanel.ShowDamageEffect(ev.DamageDealt); // 데미지 시각 효과 호출 (컨트롤러에 구현 필요)
             }

             if (_condensedLogPanelController != null)
             {
                  _condensedLogPanelController.AddLog($"{ev.Target.Name} takes {ev.DamageDealt} damage!");
             }
        }

        private void HandleStatusEffectApplied(StatusEffectEvents.StatusEffectAppliedEvent ev)
        {
             if (ev.Target == null) return;
             Debug.Log($"CombatVisualUIService: Status effect {ev.EffectType} applied to {ev.Target.Name}");

             if (_unitPanelMap.TryGetValue(ev.Target, out var targetPanel))
             {
                 // TODO: StatusEffectAppliedEvent는 StatusEffect 객체를 직접 전달하지 않으므로,
                 //       아이콘 업데이트를 위해서는 StatusEffectType과 Target만으로 처리하거나,
                 //       별도의 메커니즘(예: Target의 ActiveStatusEffects 재확인) 필요.
                 //       StatusIconController 구현 확인 및 이 핸들러 로직 재설계 필요.
                 // targetPanel.UpdateStatusEffectIcon(ev.Effect, ev.IsApplied); 
             }

             if (_condensedLogPanelController != null)
             {
                 string logMsg = $"{ev.Target.Name} gains {ev.EffectType} effect.";
                  _condensedLogPanelController.AddLog(logMsg);
             }
        }

         private void HandlePartDestroyed(PartEvents.PartDestroyedEvent ev)
        {
             if (ev.Frame == null) return;
             Debug.Log($"CombatVisualUIService: Part {ev.DestroyedPartType} destroyed on {ev.Frame.Name}");

             if(_unitPanelMap.TryGetValue(ev.Frame, out var targetPanel))
             {
                 targetPanel.ShowPartDestroyedIndicator(ev.DestroyedPartType);
             }

             if (_condensedLogPanelController != null)
             {
                  _condensedLogPanelController.AddLog($"{ev.Frame.Name}'s {ev.DestroyedPartType} is destroyed!");
             }
        }

        private void HandleStatusEffectTick(StatusEffectEvents.StatusEffectTickEvent ev)
        {
            if (ev.Target == null || ev.Effect == null) return;

            if (_unitPanelMap.TryGetValue(ev.Target, out var targetPanel))
            {
                targetPanel.UpdateStatusEffectDuration(ev.Effect);
            }
        }

        // --- Helper Methods for UI Update ---
        // WARNING: These methods directly manipulate assumed UI components. Refactor to Controllers recommended.

        private void UpdatePlayerPanel(ArmoredFrame unit)
        {
            if (unit == null || _playerNameText == null) return;
            _playerNameText.text = unit.Name;
            UpdatePlayerHp(unit);
            UpdatePlayerAp(unit);
            // TODO: Implement UpdatePlayerStatusIcons(unit); // Clear and update status icons
        }

        private void UpdateTargetPanel(ArmoredFrame unit)
        {
             if (unit == null || _targetNameText == null) return;
            _targetNameText.text = unit.Name;
            UpdateTargetHp(unit);
            UpdateTargetAp(unit);
            // TODO: Implement UpdateTargetStatusIcons(unit);
        }

        private void UpdatePlayerHp(ArmoredFrame unit)
        {
            if (unit == null || _playerHpSlider == null || _playerHpText == null) return;
            // TODO: Replace GetMaxDurability/GetCurrentDurability with actual methods/properties if they exist
            float maxHp = unit.GetMaxDurability(); // Placeholder - ArmoredFrame needs this method/property
            float currentHp = unit.GetCurrentDurability(); // Placeholder - ArmoredFrame needs this method/property
            _playerHpSlider.maxValue = maxHp;
            _playerHpSlider.value = currentHp;
            _playerHpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }
         private void UpdateTargetHp(ArmoredFrame unit)
        {
             if (unit == null || _targetHpSlider == null || _targetHpText == null) return;
            float maxHp = unit.GetMaxDurability(); // Placeholder
            float currentHp = unit.GetCurrentDurability(); // Placeholder
            _targetHpSlider.maxValue = maxHp;
            _targetHpSlider.value = currentHp;
            _targetHpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }

        private void UpdatePlayerAp(ArmoredFrame unit)
        {
             if (unit == null || _playerApSlider == null || _playerApText == null) return;
             int maxAp = Mathf.FloorToInt(unit.CombinedStats.MaxAP);
             int currentAp = Mathf.FloorToInt(unit.CurrentAP);
             _playerApSlider.maxValue = maxAp;
             _playerApSlider.value = currentAp;
             _playerApText.text = $"{currentAp} / {maxAp}";
        }
        private void UpdateTargetAp(ArmoredFrame unit)
        {
             if (unit == null || _targetApSlider == null || _targetApText == null) return;
             int maxAp = Mathf.FloorToInt(unit.CombinedStats.MaxAP);
             int currentAp = Mathf.FloorToInt(unit.CurrentAP);
             _targetApSlider.maxValue = maxAp;
             _targetApSlider.value = currentAp;
             _targetApText.text = $"{currentAp} / {maxAp}";
        }

        private void UpdateActionSkillBar(ArmoredFrame unit)
        {
            if (unit == null || _actionSkillButtons == null || _actionSkillButtons.Count == 0) return;
            // TODO: Implement logic to get available actions/skills from the unit
            // Example: List<CombatAction> actions = unit.GetAvailableActions();
            // Then iterate through actions and update corresponding buttons (_actionSkillButtons[i])
            // Update button interactability based on AP cost (actions[i].ApCost) and unit.CurrentAP
            // Update AP cost text (_actionSkillApCostTexts[i])
            // Set button icons/text based on action/skill data
            Debug.LogWarning("Action Skill Bar update logic requires ArmoredFrame.GetAvailableActions() or similar implementation.");
        }

        private void AddLogToCondensedPanel(string message)
        {
            if (_condensedLogText == null) return;
             if (_logQueue.Count >= MAX_LOG_LINES)
             {
                 _logQueue.Dequeue();
             }
             _logQueue.Enqueue(message);
             _condensedLogText.text = string.Join("\n", _logQueue);
        }

        // Helper to instantiate prefab and get controller component
        private T InstantiateAndGetController<T>(GameObject prefab, Transform parent) where T : MonoBehaviour
        {
            if (prefab == null) return null;
            var instance = Instantiate(prefab, parent);
            var controller = instance.GetComponentInChildren<T>(); // 하위 오브젝트에서 컨트롤러 검색
            if (controller == null)
            {
                 Debug.LogError($"{typeof(T).Name} not found in prefab '{prefab.name}' or its children!");
                 Destroy(instance); // 컨트롤러 없으면 인스턴스 파괴
                 return null;
            }
            return controller;
        }

        // Helper to destroy the root object of the controller
        private void DestroyControllerObject<T>(T controller) where T : MonoBehaviour
        {
            if (controller != null && controller.gameObject != null)
            {
                // 일반적으로 컨트롤러는 프리팹의 루트에 있거나, 루트의 자식에 있음
                // 루트를 파괴해야 프리팹 인스턴스 전체가 사라짐
                Destroy(controller.transform.root.gameObject);
            }
        }

        // <<< 툴팁 표시 메서드 추가 >>>
        public void ShowTooltip(StatusEffect effect, Vector3 iconPosition)
        {
            if (_tooltipController != null && effect != null)
            {
                _tooltipController.SetTooltip(effect);
                // TODO: 아이콘 위치(iconPosition)를 기반으로 툴팁 위치 조정 로직 추가
                // 예: _tooltipController.transform.position = iconPosition + new Vector3(offsetX, offsetY, 0);
                _tooltipController.Show();
            }
        }

        // <<< 툴팁 숨김 메서드 추가 >>>
        public void HideTooltip()
        {
            if (_tooltipController != null)
            {
                _tooltipController.Hide();
            }
        }
    }
} 