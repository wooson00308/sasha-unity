using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 파일럿이 사용하는 스킬 정보를 나타내는 클래스입니다.
    /// </summary>
    [System.Serializable] // Inspector에서 Pilot과 함께 보이도록 설정 (선택적)
    public class Skill
    {
        [SerializeField] private string _name = "Default Skill";
        [SerializeField] private string _description = "Default skill description.";
        [SerializeField] private float _apCost = 1.0f;
        [SerializeField] private int _cooldownTurns = 0; // 쿨다운 턴 수 (0이면 쿨다운 없음)
        [SerializeField] private Sprite _icon; // UI 표시용 아이콘

        private int _remainingCooldown = 0; // 현재 남은 쿨다운 턴 수

        // 공개 프로퍼티
        public string Name => _name;
        public string Description => _description;
        public float ApCost => _apCost;
        public int CooldownTurns => _cooldownTurns;
        public Sprite Icon => _icon;
        public int RemainingCooldown => _remainingCooldown;

        /// <summary>
        /// 스킬이 현재 사용 가능한지 확인합니다 (쿨다운 고려).
        /// </summary>
        public bool IsReady()
        {
            return _remainingCooldown <= 0;
        }

        /// <summary>
        /// 스킬 사용 시 쿨다운을 시작합니다.
        /// </summary>
        public void StartCooldown()
        {
            _remainingCooldown = _cooldownTurns;
        }

        /// <summary>
        /// 턴 시작 시 쿨다운을 1 감소시킵니다.
        /// </summary>
        public void TickCooldown()
        {
            if (_remainingCooldown > 0)
            {
                _remainingCooldown--;
            }
        }

        // 기본 생성자 (필요시)
        public Skill() { }

        // 상세 생성자 (필요시)
        public Skill(string name, string description, float apCost, int cooldownTurns, Sprite icon)
        {
            _name = name;
            _description = description;
            _apCost = apCost;
            _cooldownTurns = cooldownTurns;
            _icon = icon;
            _remainingCooldown = 0;
        }
    }
} 