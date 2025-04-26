using AF.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    public class UnitInfoPanelController : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Slider _apSlider;
        [SerializeField] private TextMeshProUGUI _apText;

        [Header("Status Effects")]
        [SerializeField] private Transform _statusEffectContainer;
        [SerializeField] private GameObject _statusIconPrefab; // 아이콘 프리팹 (StatusIconController 포함)

        // 표시 대상 유닛 참조 (선택적)
        private ArmoredFrame _trackedUnit;

        /// <summary>
        /// 패널 초기화 (전투 시작 시 호출)
        /// </summary>
        public void InitializePanel(ArmoredFrame unit)
        {
            if (unit == null)
            {
                gameObject.SetActive(false); // 유닛 없으면 비활성화
                return;
            }
            _trackedUnit = unit;
            gameObject.SetActive(true);

            if (_nameText != null) _nameText.text = unit.Name;
            UpdateHp();
            UpdateAp();
            UpdateAllStatusEffectIcons(); // 초기 상태 아이콘 업데이트
        }

        /// <summary>
        /// HP UI 업데이트
        /// </summary>
        public void UpdateHp()
        {
            if (_trackedUnit == null || _hpSlider == null || _hpText == null) return;
            float maxHp = _trackedUnit.GetMaxDurability(); // 추가된 메서드 사용
            float currentHp = _trackedUnit.GetCurrentDurability(); // 추가된 메서드 사용
            _hpSlider.maxValue = maxHp;
            _hpSlider.value = currentHp;
            _hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        }

        /// <summary>
        /// AP UI 업데이트
        /// </summary>
        public void UpdateAp()
        {
            if (_trackedUnit == null || _apSlider == null || _apText == null) return;
            int maxAp = Mathf.FloorToInt(_trackedUnit.CombinedStats.MaxAP); // MaxAP 사용
            int currentAp = Mathf.FloorToInt(_trackedUnit.CurrentAP);
            _apSlider.maxValue = maxAp;
            _apSlider.value = currentAp;
            _apText.text = $"{currentAp} / {maxAp}";
        }

        /// <summary>
        /// 상태 이상 아이콘 추가/제거 (StatusEffect 객체 기반)
        /// </summary>
        public void UpdateStatusEffectIcon(StatusEffect effect, bool isApplied)
        {
            if (_statusEffectContainer == null || _statusIconPrefab == null || effect == null)
            {
                 Debug.LogError("UpdateStatusEffectIcon: 필수 컴포넌트 누락 또는 유효하지 않은 effect 객체"); // 오류 로그 추가
                 return;
            }

            if (isApplied)
            {
                // 이미 존재하는지 EffectName으로 확인
                if (FindStatusIcon(effect.EffectName) != null) return; // 이미 있으면 추가 안함

                var iconInstance = Instantiate(_statusIconPrefab, _statusEffectContainer);
                var iconController = iconInstance.GetComponent<StatusIconController>(); // StatusIconController 참조
                if (iconController != null)
                {
                    // <<< StatusIconController.Initialize 호출 시 StatusEffect 객체 전달하도록 수정 >>>
                    iconController.Initialize(effect); 
                }
                else
                {
                     Debug.LogError("StatusIconController not found on Status Icon Prefab!");
                     Destroy(iconInstance); // 컨트롤러 없으면 파괴
                }
            }
            else // isApplied == false (제거)
            {
                // EffectName으로 제거할 아이콘 찾기
                StatusIconController iconToRemove = FindStatusIcon(effect.EffectName);
                if (iconToRemove != null)
                {
                    Destroy(iconToRemove.gameObject);
                }
                 else
                 {
                     // 제거하려는 효과의 아이콘이 없을 때 로그 (선택적)
                     Debug.LogWarning($"UpdateStatusEffectIcon: 제거할 상태 효과 아이콘 '{effect.EffectName}'을(를) 찾을 수 없습니다.");
                 }
            }
        }

        /// <summary>
        /// 현재 유닛의 모든 상태 이상 아이콘을 업데이트 (초기화 시 또는 상태 변경 시 사용)
        /// </summary>
        private void UpdateAllStatusEffectIcons()
        {
            if (_trackedUnit == null || _statusEffectContainer == null) return;

            // 기존 아이콘 모두 제거
            foreach (Transform child in _statusEffectContainer)
            {
                Destroy(child.gameObject);
            }

            // 현재 활성화된 효과들 아이콘 다시 생성
            if (_trackedUnit.ActiveStatusEffects != null)
            {
                foreach (var effect in _trackedUnit.ActiveStatusEffects)
                {
                    // <<< StatusEffect 객체와 true (적용) 전달 >>>
                    UpdateStatusEffectIcon(effect, true);
                }
            }
        }

        /// <summary>
        /// 특정 이름의 상태 효과 아이콘 컨트롤러 찾기
        /// </summary>
        // <<< 파라미터 effectType -> effectName, 비교 로직 변경 >>>
        private StatusIconController FindStatusIcon(string effectName)
        {
             if (_statusEffectContainer == null || string.IsNullOrEmpty(effectName)) return null;
             foreach (Transform child in _statusEffectContainer)
             {
                 var controller = child.GetComponent<StatusIconController>();
                 // <<< controller.EffectType -> controller.EffectName 으로 비교 >>>
                 if (controller != null && controller.EffectName == effectName)
                 {
                     return controller;
                 }
             }
             return null;
        }

        /// <summary>
        /// 파츠 파괴 시각화 (예: 특정 UI 요소 비활성화 또는 색상 변경)
        /// </summary>
        // <<< 파라미터 타입을 AF.Models.PartType 으로 명시 >>>
        public void ShowPartDestroyedIndicator(AF.Models.PartType partType)
        {
             // TODO: 패널 내부에 파츠 타입별 상태를 표시하는 UI 요소(이미지 등)를 찾아 업데이트
             Debug.LogWarning($"Part Destroyed UI indicator for {partType} not implemented in UnitInfoPanelController.");
        }

        /// <summary>
        /// 특정 상태 효과 아이콘의 남은 턴 수를 업데이트합니다.
        /// </summary>
        /// <param name="effect">업데이트할 상태 효과 정보</param>
        public void UpdateStatusEffectDuration(StatusEffect effect)
        {
            if (effect == null) return;

            StatusIconController iconController = FindStatusIcon(effect.EffectName);
            if (iconController != null)
            {
                iconController.UpdateDuration(effect.DurationTurns);
            }
            else
            {
                // 아이콘이 없는 경우 로그 (틱 이벤트는 계속 발생할 수 있으므로 경고 레벨 낮춤)
                // Debug.LogWarning($"UpdateStatusEffectDuration: 상태 효과 아이콘 '{effect.EffectName}'을(를) 찾을 수 없어 턴 수를 업데이트할 수 없습니다.");
            }
        }

        // 필요시 추가 메서드 (하이라이트 효과 등)
    }
} 