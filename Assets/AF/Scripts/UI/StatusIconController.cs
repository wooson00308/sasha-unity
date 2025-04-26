using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 사용
using TMPro; // TextMeshPro 사용 (선택적)
using AF.Models; // StatusEffect 사용 위해 추가
using UnityEngine.EventSystems; // IPointerEnterHandler, IPointerExitHandler 사용
using AF.Services; // ServiceLocator 사용

namespace AF.UI
{
    [RequireComponent(typeof(Image))] // Image 컴포넌트 강제
    // <<< IPointerEnterHandler, IPointerExitHandler 인터페이스 구현 추가 >>>
    public class StatusIconController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _durationText;

        // <<< StatusEffect 객체 저장 필드 추가 >>>
        private StatusEffect _statusEffect;
        // EffectName 프로퍼티는 _statusEffect에서 가져오도록 변경 가능
        public string EffectName => _statusEffect?.EffectName;

        // CombatVisualUIService 참조 (툴팁 요청용)
        private CombatVisualUIService _combatVisualUIService;

        private void Awake()
        {
            // Image 컴포넌트 자동 할당 시도
            if (_iconImage == null)
            {
                _iconImage = GetComponent<Image>();
            }
            // TextMeshProUGUI 컴포넌트 자동 할당 시도 (선택적)
            if (_durationText == null)
            {
                _durationText = GetComponentInChildren<TextMeshProUGUI>();
                if (_durationText != null) _durationText.gameObject.SetActive(false); // 초기에는 숨김
            }

            // 서비스 로케이터를 통해 CombatVisualUIService 인스턴스 찾기
            // 주의: CombatVisualUIService가 ServiceLocator에 등록되어 있어야 함
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<CombatVisualUIService>())
            {
                 _combatVisualUIService = ServiceLocator.Instance.GetService<CombatVisualUIService>();
            }
            else
            {
                Debug.LogError("StatusIconController: CombatVisualUIService not found via ServiceLocator!");
            }
        }

        /// <summary>
        /// 상태 아이콘을 초기화합니다.
        /// </summary>
        // <<< 파라미터를 StatusEffect 객체로 변경 >>>
        public void Initialize(StatusEffect statusEffect)
        {
            _statusEffect = statusEffect; // StatusEffect 객체 저장

            if (_statusEffect == null) 
            {
                Debug.LogError("StatusIconController Initialize: statusEffect is null!");
                gameObject.SetActive(false); // 문제가 있으면 아이콘 비활성화
                return;
            }

            // 아이콘 로드 로직 (EffectName 사용)
            string iconPath = $"Icons/StatusEffects/{_statusEffect.EffectName}"; 
            Sprite loadedIcon = Resources.Load<Sprite>(iconPath);
            if (loadedIcon != null)
            {
                _iconImage.sprite = loadedIcon;
            }
            else
            {
                Debug.LogWarning($"StatusIconController: 아이콘 로드 실패! 경로 확인 필요: {iconPath}");
            }

            // 남은 턴 수 표시 로직 (DurationTurns 사용)
            UpdateDuration(_statusEffect.DurationTurns);
        }

        /// <summary>
        /// 표시되는 남은 턴 수를 업데이트합니다.
        /// </summary>
        public void UpdateDuration(int remainingDuration)
        {
            if (_durationText != null)
            {
                if (remainingDuration < 0) 
                {
                    _durationText.text = "∞";
                    _durationText.gameObject.SetActive(true);
                }
                else if (remainingDuration > 0) 
                {
                    _durationText.text = remainingDuration.ToString();
                    _durationText.gameObject.SetActive(true);
                }
                else 
                {
                    _durationText.gameObject.SetActive(false);
                }
            }
        }

        // <<< IPointerEnterHandler 구현 >>>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_statusEffect != null && _combatVisualUIService != null)
            {
                // CombatVisualUIService에 툴팁 표시 요청
                _combatVisualUIService.ShowTooltip(_statusEffect, transform.position);
            }
        }

        // <<< IPointerExitHandler 구현 >>>
        public void OnPointerExit(PointerEventData eventData)
        {
             if (_combatVisualUIService != null)
            {
                // CombatVisualUIService에 툴팁 숨김 요청
                _combatVisualUIService.HideTooltip();
            }
        }
    }
} 