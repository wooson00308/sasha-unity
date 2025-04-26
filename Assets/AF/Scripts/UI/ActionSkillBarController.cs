using AF.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    public class ActionSkillBarController : MonoBehaviour
    {
        [SerializeField] private List<Button> _skillButtons;
        [SerializeField] private List<TextMeshProUGUI> _apCostTexts; // 각 버튼에 대응하는 AP 코스트 텍스트 (선택적)
        // [SerializeField] private List<Image> _skillIcons; // 각 버튼의 스킬 아이콘 (선택적)

        private ArmoredFrame _trackedUnit;

        /// <summary>
        /// 액션/스킬 바 업데이트
        /// </summary>
        public void UpdateBar(ArmoredFrame unit)
        {
            if (unit == null)
            {
                // 비활성화 또는 기본 상태 표시
                DisableAllButtons();
                return;
            }
            _trackedUnit = unit;

            // 사용 가능한 액션 가져오기 (ArmoredFrame에 구현된 GetAvailableActions 사용 가정)
            var availableActions = _trackedUnit.GetAvailableActions(); // 반환 타입 List<CombatAction> 가정

            for (int i = 0; i < _skillButtons.Count; i++)
            {
                var button = _skillButtons[i];
                if (button == null) continue;

                if (i < availableActions.Count)
                {
                    // CombatAction 구조체/클래스가 이름, AP 비용, 아이콘 정보 등을 포함한다고 가정
                    // var action = availableActions[i];
                    button.gameObject.SetActive(true);

                    // 버튼 텍스트/아이콘 설정
                    // var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                    // if (buttonText != null) buttonText.text = action.Name;
                    // if (i < _skillIcons.Count && _skillIcons[i] != null) _skillIcons[i].sprite = action.IconSprite;

                    // AP 비용 표시 및 상호작용 가능 여부 설정
                    // bool canAfford = _trackedUnit.CurrentAP >= action.ApCost;
                    // button.interactable = canAfford;
                    // if (i < _apCostTexts.Count && _apCostTexts[i] != null)
                    // {
                    //     _apCostTexts[i].text = action.ApCost.ToString();
                    //     _apCostTexts[i].color = canAfford ? Color.white : Color.red; // 예시
                    // }

                    // 버튼 클릭 이벤트 설정 (CombatVisualUIService 또는 CombatManager 등으로 액션 실행 요청)
                    // button.onClick.RemoveAllListeners();
                    // button.onClick.AddListener(() => RequestActionExecution(action));
                }
                else
                {
                    // 사용 가능한 액션 수보다 버튼이 많으면 비활성화
                    button.gameObject.SetActive(false);
                }
            }
            Debug.LogWarning("UpdateBar requires CombatAction definition and ArmoredFrame.GetAvailableActions() to be fully implemented.");
        }

        private void DisableAllButtons()
        {
            if (_skillButtons == null) return;
            foreach (var button in _skillButtons)
            {
                if (button != null) button.gameObject.SetActive(false);
            }
        }

        // private void RequestActionExecution(CombatAction action)
        // {
        //     // TODO: EventBus 등을 통해 CombatSimulatorService에 액션 실행 요청
        //     Debug.Log($"Requesting action: {action.Name}");
        //     // ServiceLocator.Instance.GetService<EventBusService>().Publish(new RequestActionEvent(action, _trackedUnit));
        // }
    }

    // 필요한 경우 CombatAction 구조체/클래스 정의 (별도 파일 권장)
    /*
    public struct CombatAction
    {
        public string Name;
        public float ApCost;
        public ActionType Type; // enum ActionType { WeaponAttack, PilotSkill, PartAbility, Movement, Defense } 등
        public object Source; // Weapon, Skill, AbilityName 등 액션 출처
        public Sprite IconSprite; // UI용 아이콘
        // 기타 필요한 정보 (쿨다운, 대상 지정 방식 등)

        public CombatAction(string name, float apCost, ActionType type, object source = null, Sprite icon = null)
        {
            Name = name; ApCost = apCost; Type = type; Source = source; IconSprite = icon;
        }
    }
    */
} 