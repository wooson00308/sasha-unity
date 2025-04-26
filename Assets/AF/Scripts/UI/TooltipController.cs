using UnityEngine;
using TMPro; // TextMeshProUGUI 사용
using AF.Models; // StatusEffect 사용

namespace AF.UI
{
    public class TooltipController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tooltipText; // Inspector에서 할당 필요

        private void Awake()
        {
            // 시작 시 비활성화
            gameObject.SetActive(false);
            if (_tooltipText == null)
            {
                // 하위 오브젝트에서 자동으로 찾아 할당 시도
                _tooltipText = GetComponentInChildren<TextMeshProUGUI>();
                if (_tooltipText == null)
                {
                    Debug.LogError("TooltipController: TextMeshProUGUI component not found or assigned!");
                }
            }
        }

        /// <summary>
        /// 툴팁의 내용을 설정합니다.
        /// </summary>
        /// <param name="effect">표시할 상태 효과 정보</param>
        public void SetTooltip(StatusEffect effect)
        {
            if (_tooltipText == null || effect == null) return;

            // 툴팁 내용 구성 (예시)
            string durationString = effect.DurationTurns < 0 ? "영구" : $"{effect.DurationTurns} 턴 남음";
            _tooltipText.text = $"<b>{effect.EffectName}</b>\n{effect.Description}\n<i>({durationString})</i>";
            
            // TODO: 필요에 따라 더 많은 정보 추가 (스탯 변경량, 틱 효과 등)
        }

        /// <summary>
        /// 툴팁을 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 툴팁을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // TODO: 툴팁 위치 조정 로직 추가 (아이콘 위치 기반)
    }
} 