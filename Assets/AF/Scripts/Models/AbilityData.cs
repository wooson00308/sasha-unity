using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 파츠가 가진 특수 능력(어빌리티) 정보를 나타내는 클래스입니다.
    /// </summary>
    [System.Serializable]
    public class AbilityData
    {
        [SerializeField] private string _name = "Default Ability";
        [SerializeField] private string _description = "Default ability description.";
        [SerializeField] private float _apCost = 0.0f; // 패시브 능력 등 고려하여 기본 AP 0
        [SerializeField] private int _cooldownTurns = 0; // 필요시 쿨다운 설정
        [SerializeField] private Sprite _icon;

        private int _remainingCooldown = 0;

        // 공개 프로퍼티
        public string Name => _name;
        public string Description => _description;
        public float ApCost => _apCost;
        public int CooldownTurns => _cooldownTurns;
        public Sprite Icon => _icon;
        public int RemainingCooldown => _remainingCooldown;

        /// <summary>
        /// 어빌리티가 현재 사용 가능한지 확인합니다 (쿨다운 고려).
        /// </summary>
        public bool IsReady()
        {
            // TODO: 추가적인 사용 조건 확인 로직 (예: 특정 파츠 상태)
            return _remainingCooldown <= 0;
        }

        /// <summary>
        /// 어빌리티 사용 시 쿨다운을 시작합니다.
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

        // 기본 생성자
        public AbilityData() { }

        // 상세 생성자
        public AbilityData(string name, string description, float apCost, int cooldownTurns, Sprite icon)
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