using System;
using System.Collections.Generic;
using UnityEngine;
using AF.Services;
using AF.Combat;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame에 장착 가능한 무기 클래스입니다.
    /// </summary>
    [Serializable]
    public class Weapon
    {
        /// <summary>
        /// 무기의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 무기의 타입 (근접, 중거리, 원거리)
        /// </summary>
        [SerializeField] private WeaponType _type;

        /// <summary>
        /// 데미지 타입 (물리, 에너지, 폭발, 관통, 전기)
        /// </summary>
        [SerializeField] private DamageType _damageType;

        /// <summary>
        /// 기본 데미지
        /// </summary>
        [SerializeField] private float _damage;

        /// <summary>
        /// 정확도 (0.0 ~ 1.0)
        /// </summary>
        [SerializeField] private float _accuracy;

        /// <summary>
        /// 사거리 (유닛 단위)
        /// </summary>
        [SerializeField] private float _range;

        /// <summary>
        /// 공격 속도 (초당 공격 횟수)
        /// </summary>
        [SerializeField] private float _attackSpeed;

        /// <summary>
        /// 연사 시 과열도
        /// </summary>
        [SerializeField] private float _overheatPerShot;

        /// <summary>
        /// 이 무기를 사용하는 데 드는 기본 AP 소모량
        /// </summary>
        [SerializeField] private float _baseAPCost = 2.0f; // 기본 공격 AP 소모 2로 설정

        /// <summary>
        /// 최대 탄약 수 (0 이하는 무한 탄약으로 간주)
        /// </summary>
        [SerializeField] private int _maxAmmo = 0;

        /// <summary>
        /// 현재 탄약 수
        /// </summary>
        private int _currentAmmo;

        /// <summary>
        /// 재장전에 필요한 AP 비용
        /// </summary>
        [SerializeField] private float _reloadAPCost = 1.5f; // 기본 재장전 AP 소모 1.5로 설정

        /// <summary>
        /// 재장전에 필요한 턴 수 (0이면 즉시, 1 이상이면 해당 턴 동안 재장전 상태)
        /// </summary>
        [SerializeField] private int _reloadTurns = 0; // 기본값: 즉시 재장전

        /// <summary>
        /// 현재 재장전 중인지 여부
        /// </summary>
        private bool _isReloading = false;

        /// <summary>
        /// 재장전 시작 턴
        /// </summary>
        private int _reloadStartTurn = -1;

        /// <summary>
        /// 현재 과열도
        /// </summary>
        private float _currentHeat;

        /// <summary>
        /// 특수 효과 목록
        /// </summary>
        private List<string> _specialEffects;

        /// <summary>
        /// 무기가 현재 작동 가능한지 여부
        /// </summary>
        private bool _isOperational;

        // 공개 프로퍼티
        public string Name => _name;
        public WeaponType Type => _type;
        public DamageType DamageType => _damageType;
        public float Damage => _damage;
        public float Accuracy => _accuracy;
        public float Range => _range;
        public float AttackSpeed => _attackSpeed;
        public float BaseAPCost => _baseAPCost;
        public float CurrentHeat => _currentHeat;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<string> SpecialEffects => _specialEffects;
        public int MaxAmmo => _maxAmmo;
        public int CurrentAmmo => _currentAmmo;
        public float ReloadAPCost => _reloadAPCost;
        public int ReloadTurns => _reloadTurns;
        public bool IsReloading => _isReloading;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Weapon()
        {
            _name = "Default Weapon";
            _type = WeaponType.MidRange;
            _damageType = DamageType.Physical;
            _damage = 10.0f;
            _accuracy = 0.7f;
            _range = 5.0f;
            _attackSpeed = 1.0f;
            _overheatPerShot = 0.1f;
            _baseAPCost = 2.0f; // 기본값 명시

            // <<< 탄약 시스템 초기화 추가 >>>
            _maxAmmo = 0; // 기본 무한 탄약
            _currentAmmo = _maxAmmo > 0 ? _maxAmmo : 999; // 무한 탄약 시 큰 값 할당 (내부 로직 단순화 위해)
            _reloadAPCost = 1.5f;
            _reloadTurns = 0;
            _isReloading = false;
            _reloadStartTurn = -1;
            // <<< 탄약 시스템 초기화 추가 끝 >>>

            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 상세 정보를 지정하는 생성자
        /// </summary>
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, float range, float attackSpeed, float overheatPerShot)
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _range = Mathf.Max(1.0f, range);
            _attackSpeed = Mathf.Max(0.1f, attackSpeed);
            _overheatPerShot = Mathf.Max(0.0f, overheatPerShot);
            // <<< 탄약 시스템 초기화 (기본값 사용) >>>
            _maxAmmo = 0;
            _currentAmmo = 999;
            _reloadAPCost = 1.5f;
            _reloadTurns = 0;
            _isReloading = false;
            _reloadStartTurn = -1;
            // <<< 탄약 시스템 초기화 끝 >>>
            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 상세 정보를 지정하는 생성자
        /// </summary>
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, float range, float attackSpeed, float overheatPerShot, float baseAPCost)
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _range = Mathf.Max(1.0f, range);
            _attackSpeed = Mathf.Max(0.1f, attackSpeed);
            _overheatPerShot = Mathf.Max(0.0f, overheatPerShot);
            _baseAPCost = Mathf.Max(0.1f, baseAPCost); // 최소 AP 소모량 제한
            // <<< 탄약 시스템 초기화 (기본값 사용) >>>
            _maxAmmo = 0;
            _currentAmmo = 999;
            _reloadAPCost = 1.5f;
            _reloadTurns = 0;
            _isReloading = false;
            _reloadStartTurn = -1;
            // <<< 탄약 시스템 초기화 끝 >>>
            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 상세 정보를 지정하는 생성자 (탄약/재장전 포함)
        /// </summary>
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, float range, float attackSpeed, float overheatPerShot, float baseAPCost,
                      int maxAmmo, float reloadAPCost, int reloadTurns) // 탄약/재장전 파라미터 추가
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _range = Mathf.Max(1.0f, range);
            _attackSpeed = Mathf.Max(0.1f, attackSpeed);
            _overheatPerShot = Mathf.Max(0.0f, overheatPerShot);
            _baseAPCost = Mathf.Max(0.1f, baseAPCost);

            // <<< 탄약 시스템 초기화 >>>
            _maxAmmo = Mathf.Max(0, maxAmmo); // 0 이상 보장
            _currentAmmo = _maxAmmo > 0 ? _maxAmmo : 999;
            _reloadAPCost = Mathf.Max(0.1f, reloadAPCost); // 최소 비용 보장
            _reloadTurns = Mathf.Max(0, reloadTurns); // 0 이상 보장
            _isReloading = false;
            _reloadStartTurn = -1;
            // <<< 탄약 시스템 초기화 끝 >>>

            _currentHeat = 0.0f;
            _specialEffects = new List<string>();
            _isOperational = true;
        }

        /// <summary>
        /// 무기를 발사합니다.
        /// </summary>
        /// <returns>과열로 인한 발사 실패시 false, 성공시 true</returns>
        public bool Fire()
        {
            if (!_isOperational)
                return false;

            // 과열 체크
            if (_currentHeat >= 1.0f)
                return false;

            // 과열도 증가
            _currentHeat += _overheatPerShot;
            if (_currentHeat > 1.0f)
                _currentHeat = 1.0f;

            return true;
        }

        /// <summary>
        /// 무기의 과열도를 냉각합니다.
        /// </summary>
        /// <param name="cooldownAmount">냉각량</param>
        public void Cooldown(float cooldownAmount)
        {
            _currentHeat = Mathf.Max(0.0f, _currentHeat - cooldownAmount);
        }

        /// <summary>
        /// 특수 효과를 추가합니다.
        /// </summary>
        public void AddSpecialEffect(string effect)
        {
            if (!_specialEffects.Contains(effect))
            {
                _specialEffects.Add(effect);
            }
        }

        /// <summary>
        /// 무기를 수리합니다.
        /// </summary>
        public void Repair()
        {
            _isOperational = true;
            _currentHeat = 0.0f;
        }

        /// <summary>
        /// 무기를 손상시킵니다.
        /// </summary>
        public void DamageWeapon()
        {
            _isOperational = false;
        }

        /// <summary>
        /// 실제 데미지를 계산합니다. (명중률 고려)
        /// </summary>
        /// <returns>명중 시 데미지 값, 빗나갈 경우 0</returns>
        public float CalculateDamage(float attackerAccuracyMod, float targetEvasionMod)
        {
            // 실제 명중률 계산 (무기 정확도 + 공격자 정확도 보정 - 타겟 회피 보정)
            float finalAccuracy = Mathf.Clamp01(_accuracy + attackerAccuracyMod - targetEvasionMod);
            
            // 명중 여부 판정
            if (UnityEngine.Random.value <= finalAccuracy)
            {
                return _damage;
            }
            return 0.0f; // 빗나감
        }

        /// <summary>
        /// 발사 가능한 탄약이 있는지 확인합니다. (무한 탄약 제외)
        /// </summary>
        public bool HasAmmo()
        {
            // MaxAmmo가 0 이하면 무한 탄약으로 간주
            return _maxAmmo <= 0 || _currentAmmo > 0;
        }

        /// <summary>
        /// 탄약을 1 소모합니다. (무한 탄약 제외)
        /// </summary>
        /// <returns>탄약 소모 성공 여부</returns>
        public bool ConsumeAmmo()
        {
            if (_maxAmmo <= 0) return true; // 무한 탄약이면 항상 성공

            if (_currentAmmo > 0)
            {
                _currentAmmo--;
                try
                {
                    ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=18> [{_name}] 탄약 소모. 남은 탄약: {_currentAmmo}/{_maxAmmo}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
                return true;
            }
            try
            {
                ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=20> [{_name}] 탄약 부족으로 소모 실패.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
            }
            return false; // 탄약 부족
        }

        /// <summary>
        /// 재장전을 시작합니다.
        /// </summary>
        /// <param name="currentTurn">현재 게임 턴</param>
        /// <returns>재장전 시작 성공 여부 (이미 재장전 중이거나 탄약이 가득 차 있으면 실패)</returns>
        public bool StartReload(int currentTurn)
        {
            if (_isReloading)
            {
                try
                {
                    ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=20> [{_name}] 이미 재장전 중입니다.", LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
                return false; // 이미 재장전 중
            }
             if (_maxAmmo > 0 && _currentAmmo >= _maxAmmo)
            {
                try
                {
                    ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=20> [{_name}] 탄약이 이미 가득 찼습니다.", LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
                return false; // 탄약 가득 참
            }
             if (_maxAmmo <= 0)
            {
                try
                {
                    ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=20> [{_name}]은(는) 무한 탄약 무기라 재장전할 수 없습니다.", LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
                return false; // 무한 탄약 무기
            }


            if (_reloadTurns > 0) // 재장전 시간이 필요한 경우
            {
                _isReloading = true;
                _reloadStartTurn = currentTurn;
                try
                {
                    int turnsRemaining = _reloadTurns - (currentTurn - _reloadStartTurn);
                    ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=13> [{_name}] 재장전 시작. 완료까지 {turnsRemaining}턴 필요 (현재 턴 포함).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
                return true;
            }
            else // 즉시 재장전 (_reloadTurns == 0)
            {
                FinishReload(); // 바로 완료 처리
                return true;
            }
        }

        /// <summary>
        /// 재장전을 완료합니다. (주로 CombatSimulatorService에서 턴 경과 후 호출)
        /// </summary>
        public void FinishReload()
        {
            if (_maxAmmo > 0) // 무한 탄약이 아닐 경우에만
            {
                _currentAmmo = _maxAmmo;
            }
            _isReloading = false;
            _reloadStartTurn = -1;
            try
            {
                ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=14> [{_name}] 재장전 완료! (탄약: {_currentAmmo}/{(_maxAmmo > 0 ? _maxAmmo.ToString() : "∞")})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 턴을 기준으로 재장전이 완료되었는지 확인합니다.
        /// </summary>
        /// <param name="currentTurn">현재 게임 턴</param>
        /// <returns>재장전 완료 여부</returns>
        public bool CheckReloadCompletion(int currentTurn)
        {
            if (!_isReloading) return false;

            bool completed = currentTurn - _reloadStartTurn >= _reloadTurns;
            
            if (!completed && _reloadTurns > 0)
            {
                try
                {
                    int turnsRemaining = _reloadTurns - (currentTurn - _reloadStartTurn);
                    if (turnsRemaining > 0)
                    {
                        ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=22> [{_name}] 재장전 진행 중... {turnsRemaining}턴 남음.", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
            }
            
            return completed;
        }
    }
} 