using System;
using System.Collections.Generic;
using UnityEngine;
using AF.Services;
using AF.Combat;

namespace AF.Models
{
    /// <summary>
    /// 무기의 사거리 정보를 나타내는 구조체
    /// </summary>
    [Serializable]
    public struct RangeData
    {
        [Tooltip("무기의 최대 유효 사거리")]
        public float MaxRange;
        [Tooltip("무기의 최적 교전 사거리")]
        public float OptimalRange;

        public RangeData(float maxRange, float optimalRange)
        {
            MaxRange = Mathf.Max(0.1f, maxRange); // 최소 사거리 보장
            OptimalRange = Mathf.Clamp(optimalRange, 0.1f, MaxRange); // 최적 사거리는 0.1 ~ 최대 사거리 사이
        }

        // 기본값 설정 (예: 최대 5, 최적 3)
        public static RangeData Default => new RangeData(5.0f, 3.0f);
    }

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
        /// 사거리 (유닛 단위) - 이제 RangeData 구조체 사용
        /// </summary>
        [SerializeField] private RangeData _rangeData = RangeData.Default;

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
        /// 재장전에 필요한 사이클 수 (0이면 즉시, 1 이상이면 해당 사이클 동안 재장전 상태)
        /// </summary>
        [SerializeField] private int _reloadTurns = 0; // 기본값: 즉시 재장전

        /// <summary>
        /// 현재 재장전 중인지 여부
        /// </summary>
        private bool _isReloading = false;

        /// <summary>
        /// 재장전 시작 사이클 -> 턴으로 변경
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
        /// 공격 시 사용할 Flavor Text 키
        /// </summary>
        [SerializeField] private string _attackFlavorKey = ""; // 추가 및 기본값 설정

        /// <summary>
        /// 재장전 시 사용할 Flavor Text 키
        /// </summary>
        [SerializeField] private string _reloadFlavorKey = ""; // 추가 및 기본값 설정

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
        public RangeData Range => _rangeData;
        public float AttackSpeed => _attackSpeed;
        public float BaseAPCost => _baseAPCost;
        public float CurrentHeat => _currentHeat;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<string> SpecialEffects => _specialEffects;
        public string AttackFlavorKey => _attackFlavorKey;
        public string ReloadFlavorKey => _reloadFlavorKey;
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
            _rangeData = RangeData.Default;
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
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, RangeData rangeData, float attackSpeed, float overheatPerShot)
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _rangeData = rangeData;
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
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, RangeData rangeData, float attackSpeed, float overheatPerShot, float baseAPCost)
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _rangeData = rangeData;
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
        /// 상세 정보를 지정하는 생성자 (탄약/재장전/FlavorKey 포함)
        /// </summary>
        public Weapon(string name, WeaponType type, DamageType damageType, float damage, float accuracy, RangeData rangeData, float attackSpeed, float overheatPerShot, float baseAPCost,
                      int maxAmmo, float reloadAPCost, int reloadTurns,
                      string attackFlavorKey, string reloadFlavorKey) // FlavorKey 파라미터 추가
        {
            _name = name;
            _type = type;
            _damageType = damageType;
            _damage = damage;
            _accuracy = Mathf.Clamp01(accuracy);
            _rangeData = rangeData;
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

            // <<< Flavor Key 할당 추가 >>>
            _attackFlavorKey = attackFlavorKey ?? "";
            _reloadFlavorKey = reloadFlavorKey ?? "";
            // <<< Flavor Key 할당 추가 끝 >>>
        }

        /// <summary>
        /// WeaponSO 데이터로 Weapon 인스턴스를 초기화합니다.
        /// </summary>
        public void InitializeFromSO(Data.WeaponSO weaponSO)
        {
            if (weaponSO == null) 
            {
                Debug.LogError("Cannot initialize Weapon from null WeaponSO.");
                // 기본값으로 두거나 예외 처리가 필요할 수 있음
                return; 
            }

            _name = weaponSO.WeaponName;
            _type = weaponSO.WeaponType;
            _damageType = weaponSO.DamageType;
            _damage = weaponSO.BaseDamage;
            _accuracy = weaponSO.Accuracy;
            // _rangeData = weaponSO.RangeData; // <<< 기존 직접 할당 주석 처리

            // --- RangeData 초기화 (WeaponSO에 RangeData 필드 또는 기존 Range 필드 사용) ---
            // TODO: WeaponSO에 RangeData 필드를 추가하는 것이 장기적으로 더 좋음.
            // 임시 방편: WeaponSO에 RangeData 필드가 없으면 Range(float) 사용
            if (weaponSO.Range > 0) // 기존 Range(float) 값이 유효하면 사용
            {
                 // RangeData가 없으므로 추정. 최적 사거리는 최대 사거리의 70%로 가정 (조정 가능)
                 _rangeData = new RangeData(weaponSO.Range, weaponSO.Range * 0.7f); 
                 Debug.LogWarning($"WeaponSO '{weaponSO.WeaponName}' is using legacy Range(float). Estimating OptimalRange ({_rangeData.OptimalRange:F1}) based on MaxRange ({_rangeData.MaxRange:F1}) using 70% rule. Consider adding RangeData field to WeaponSO.");
            }
            else
            {
                 // 기존 Range 값도 없으면 기본값 사용
                 _rangeData = RangeData.Default;
                 Debug.LogWarning($"WeaponSO '{weaponSO.WeaponName}' has no valid Range data. Using default RangeData (Max: {_rangeData.MaxRange}, Optimal: {_rangeData.OptimalRange}).");
            }
            // --- RangeData 초기화 끝 ---

            _attackSpeed = weaponSO.AttackSpeed;
            _overheatPerShot = weaponSO.OverheatPerShot;
            _baseAPCost = weaponSO.BaseAPCost;
            _maxAmmo = weaponSO.AmmoCapacity;
            _reloadAPCost = weaponSO.ReloadAPCost;
            _reloadTurns = weaponSO.ReloadTurns;
            _specialEffects = new List<string>(weaponSO.SpecialEffects ?? new List<string>());
            _attackFlavorKey = weaponSO.AttackFlavorKey ?? ""; // SO에서 키 가져오기
            _reloadFlavorKey = weaponSO.ReloadFlavorKey ?? ""; // SO에서 키 가져오기

            // Runtime 상태 초기화
            _currentAmmo = _maxAmmo > 0 ? _maxAmmo : 999; // 무한 탄약 처리
            _isReloading = false;
            _reloadStartTurn = -1;
            _currentHeat = 0.0f;
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
                    //ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=18> [{_name}] 탄약 소모. 남은 탄약: {_currentAmmo}/{_maxAmmo}", LogLevel.Debug);
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
        /// <param name="currentTurn">현재 전투 턴</param>
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

            // 로그 서비스를 안전하게 가져옵니다.
            TextLoggerService loggerService = null;
            try { loggerService = ServiceLocator.Instance.GetService<TextLoggerService>(); } 
            catch (Exception ex) { Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}"); }

            if (_reloadTurns > 0) // 재장전 시간이 필요한 경우
            {
                _isReloading = true;
                _reloadStartTurn = currentTurn;
                
                // <<< Flavor Text 기반 로그 >>>
                if (loggerService != null && !string.IsNullOrEmpty(_reloadFlavorKey)) 
                {
                    string templateKey = $"{_reloadFlavorKey}_Start"; // 예: Reload_Shotgun_Start
                    string flavorText = loggerService.GetRandomFlavorText(templateKey);
                    if (!string.IsNullOrEmpty(flavorText))
                    {
                        int turnsRemaining = Mathf.Max(0, _reloadTurns - (currentTurn - _reloadStartTurn));
                        var parameters = new Dictionary<string, string>
                        {
                            { "weaponName", _name },
                            { "turnsRemaining", turnsRemaining.ToString() }
                        };
                        string formattedLog = loggerService.FormatFlavorText(flavorText, parameters);
                        loggerService.TextLogger?.Log($"<sprite index=13> {formattedLog}"); // 아이콘 추가
                    }
                    else
                    { 
                        // 템플릿 못 찾으면 기본 로그
                        int turnsRemaining = Mathf.Max(0, _reloadTurns - (currentTurn - _reloadStartTurn));
                        loggerService.TextLogger?.Log($"<sprite index=13> [{_name}] 재장전 시작. 완료까지 {turnsRemaining} 턴 필요.");
                    }
                }
                else // loggerService가 없거나 키가 비었으면 기존 로그
                {
                    int turnsRemaining = Mathf.Max(0, _reloadTurns - (currentTurn - _reloadStartTurn));
                    Debug.LogWarning($"<sprite index=13> [{_name}] 재장전 시작 (로그 시스템 문제 또는 FlavorKey 없음). 완료까지 {turnsRemaining} 턴 필요.");
                }
                // <<< Flavor Text 기반 로그 끝 >>>
                
                return true;
            }
            else // 즉시 재장전 (_reloadTurns == 0)
            {
                FinishReload(); // 바로 완료 처리 (FinishReload에서 로그 처리)
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
            bool wasReloading = _isReloading; // Store previous state for logging
            _isReloading = false;
            _reloadStartTurn = -1;
            
            // <<< Flavor Text 기반 로그 >>>
            TextLoggerService loggerService = null;
            try { loggerService = ServiceLocator.Instance.GetService<TextLoggerService>(); } 
            catch (Exception ex) { Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}"); }

            if (loggerService != null && !string.IsNullOrEmpty(_reloadFlavorKey))
            {
                string templateKey = $"{_reloadFlavorKey}_Finish"; // 예: Reload_Shotgun_Finish
                string flavorText = loggerService.GetRandomFlavorText(templateKey);
                if (!string.IsNullOrEmpty(flavorText))
                {
                    var parameters = new Dictionary<string, string>
                    {
                        { "weaponName", _name },
                        { "currentAmmo", _currentAmmo.ToString() },
                        { "maxAmmo", (_maxAmmo > 0 ? _maxAmmo.ToString() : "∞") }
                    };
                    string formattedLog = loggerService.FormatFlavorText(flavorText, parameters);
                    loggerService.TextLogger?.Log($"<sprite index=14> {formattedLog}"); // 아이콘 추가
                }
                else
                { 
                     // 템플릿 못 찾으면 기본 로그
                    loggerService.TextLogger?.Log($"<sprite index=14> [{_name}] 재장전 완료! (탄약: {_currentAmmo}/{(_maxAmmo > 0 ? _maxAmmo.ToString() : "∞")})");
                }
            }
            else // loggerService가 없거나 키가 비었으면 기존 로그
            {
                 Debug.LogWarning($"<sprite index=14> [{_name}] 재장전 완료! (로그 시스템 문제 또는 FlavorKey 없음)");
            }
            // <<< Flavor Text 기반 로그 끝 >>>
        }

        /// <summary>
        /// 현재 턴을 기준으로 재장전이 완료되었는지 확인합니다.
        /// </summary>
        /// <param name="currentTurn">현재 전투 턴</param>
        /// <returns>재장전 완료 여부</returns>
        public bool CheckReloadCompletion(int currentTurn)
        {
            if (!_isReloading || _reloadStartTurn < 0) 
            {
                return false;
            }

            bool completed = currentTurn >= _reloadStartTurn + _reloadTurns;
            
            if (!completed && _reloadTurns > 0)
            {
                try
                {
                    int turnsRemaining = Mathf.Max(0, (_reloadStartTurn + _reloadTurns) - currentTurn);
                    if (turnsRemaining > 0)
                    {
                        ServiceLocator.Instance.GetService<TextLoggerService>().TextLogger.Log($"<sprite index=22> [{_name}] 재장전 완료까지 {turnsRemaining} 턴 남음.", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TextLoggerService 접근 오류: {ex.Message}");
                }
            }
            
            if (completed)
            {
                FinishReload();
            }

            return completed;
        }

        /// <summary>
        /// 무기를 복사합니다. (상태 초기화 포함)
        /// </summary>
        public Weapon Clone()
        {
            Weapon clone = new Weapon(
                this._name,
                this._type,
                this._damageType,
                this._damage,
                this._accuracy,
                this._rangeData,
                this._attackSpeed,
                this._overheatPerShot,
                this._baseAPCost,
                this._maxAmmo,
                this._reloadAPCost,
                this._reloadTurns,
                this._attackFlavorKey,
                this._reloadFlavorKey
            );

            // 상태 변수 초기화
            clone._currentAmmo = (_maxAmmo <= 0) ? 0 : _maxAmmo; // 무한 탄약이 아니면 최대 탄약으로 시작
            clone._currentHeat = 0f;
            clone._isOperational = true; // 복사 시 작동 상태로 시작
            clone._isReloading = false;
            clone._reloadStartTurn = -1;
            if (this._specialEffects != null)
            {
                clone._specialEffects = new List<string>(this._specialEffects);
            }
            else
            {
                clone._specialEffects = new List<string>();
            }
            return clone;
        }
    }
} 