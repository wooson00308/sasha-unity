using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus; // 이벤트 버스 사용 가정
using AF.Services; // 서비스 로케이터 사용 가정
using AF.Combat; // 네임스페이스 추가
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 게임의 주요 유닛인 ArmoredFrame을 구현한 클래스입니다.
    /// 선택된 Frame을 기반으로 다양한 파츠를 슬롯에 장착하여 구성합니다.
    /// </summary>
    [Serializable]
    public class ArmoredFrame
    {
        /// <summary>
        /// ArmoredFrame의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 기본 프레임
        /// </summary>
        [SerializeField] private Frame _frameBase;

        /// <summary>
        /// 파츠 참조 (현재는 Part 클래스의 하위 클래스들이 구현되지 않았으므로 Part로 선언)
        /// </summary>
        private Dictionary<string, Part> _parts = new Dictionary<string, Part>();

        /// <summary>
        /// 통합 스탯 (모든 파츠의 스탯 합산)
        /// </summary>
        private Stats _combinedStats = new Stats();

        /// <summary>
        /// ArmoredFrame의 현재 상태
        /// </summary>
        private bool _isOperational = true;

        /// <summary>
        /// ArmoredFrame의 현재 위치
        /// </summary>
        [SerializeField] private Vector3 _position;

        /// <summary>
        /// ArmoredFrame의 소속 팀 ID
        /// </summary>
        [SerializeField] private int _teamId;

        /// <summary>
        /// 이 ArmoredFrame을 조종하는 파일럿
        /// </summary>
        private Pilot _pilot;

        /// <summary>
        /// 장착된 무기 목록
        /// </summary>
        private List<Weapon> _equippedWeapons = new List<Weapon>();

        /// <summary>
        /// 현재 적용 중인 상태 효과 목록
        /// </summary>
        private List<StatusEffect> _activeStatusEffects = new List<StatusEffect>();

        /// <summary>
        /// 현재 행동력 (AP)
        /// </summary>
        private float _currentAP;

        /// <summary>
        /// 프레임과 모든 파츠 무게의 합
        /// </summary>
        private float _totalWeight;

        // 이벤트 버스 (생성자나 메서드에서 주입받거나 서비스 로케이터로 가져옴)
        private EventBus.EventBus _eventBus;

        // 공개 프로퍼티
        public string Name => _name;
        public Frame FrameBase => _frameBase;
        public Pilot Pilot => _pilot;
        public Vector3 Position { get => _position; set => _position = value; }
        public int TeamId => _teamId;
        public IReadOnlyDictionary<string, Part> Parts => _parts;
        public IReadOnlyList<Weapon> EquippedWeapons => _equippedWeapons.AsReadOnly();
        public Stats CombinedStats => _combinedStats;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects.AsReadOnly();
        public float CurrentAP => _currentAP;
        public float TotalWeight => _totalWeight;

        /// <summary>
        /// 생성자
        /// </summary>
        public ArmoredFrame(string name, Frame frameBase, Vector3 initialPosition, int teamId)
        {
            _name = name;
            _frameBase = frameBase ?? throw new ArgumentNullException(nameof(frameBase));
            _position = initialPosition;
            _teamId = teamId;

            // 이벤트 버스 인스턴스 가져오기 (서비스 로케이터 사용 예시)
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<EventBusService>())
            {
                _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus;
            }
            else
            {
                Debug.LogError($"ArmoredFrame({Name}): EventBusService를 찾을 수 없습니다!");
                // _eventBus = new EventBus.EventBus(); // 임시 폴백 또는 예외 처리
            }

            _activeStatusEffects = new List<StatusEffect>(); // 상태 효과 리스트 초기화
            
            // <<< AP 초기화 추가 시작 >>>
            _currentAP = _combinedStats.MaxAP; // 스탯 계산 전에 기본값으로라도 초기화
            // <<< AP 초기화 추가 끝 >>>

            RecalculateStats(); // 프레임 기본 스탯으로 초기화
            CheckOperationalStatus();
            
            // <<< RecalculateStats 이후 AP 재설정 시작 >>>
            _currentAP = _combinedStats.MaxAP; // 재계산된 MaxAP로 설정
            // <<< RecalculateStats 이후 AP 재설정 끝 >>>
        }

        /// <summary>
        /// 파일럿을 ArmoredFrame에 할당합니다.
        /// </summary>
        public void AssignPilot(Pilot pilot)
        {
            _pilot = pilot;
            // TODO: 파일럿 스탯을 CombinedStats에 반영하는 로직 필요 (RecalculateStats 수정)
            RecalculateStats(); // 파일럿 변경 시 스탯 재계산
        }

        /// <summary>
        /// 지정된 슬롯에 파츠를 장착합니다.
        /// </summary>
        /// <returns>장착 성공 여부</returns>
        public bool AttachPart(Part part, string slotIdentifier)
        {
            if (part == null || string.IsNullOrEmpty(slotIdentifier))
            {
                Debug.LogError($"ArmoredFrame({Name}): 유효하지 않은 파츠 또는 슬롯 식별자로 AttachPart 호출됨.");
                return false;
            }

            if (_frameBase.CanEquipPart(part, slotIdentifier)) // 프레임에게 장착 가능 여부 확인
            {
                // 기존 파츠가 있다면 제거 (선택적: 덮어쓰기 또는 실패 처리)
                if (_parts.ContainsKey(slotIdentifier))
                {
                    Debug.LogWarning($"ArmoredFrame({Name}): 슬롯 '{slotIdentifier}'에 이미 파츠({_parts[slotIdentifier].Name})가 있어 교체합니다.");
                    DetachPart(slotIdentifier); // 기존 파츠 제거 (스탯 재계산 포함)
                }

                _parts[slotIdentifier] = part;
                RecalculateStats(); // 새 파츠 포함하여 스탯 재계산
                Debug.Log($"ArmoredFrame({Name}): 파츠 '{part.Name}'을(를) 슬롯 '{slotIdentifier}'에 성공적으로 장착했습니다.");
                // TODO: PartAttachedEvent 발행 고려
                return true;
            }
            else
            {
                Debug.LogWarning($"ArmoredFrame({Name}): 슬롯 '{slotIdentifier}'에 파츠 '{part.Name}'(타입: {part.Type}) 장착 불가 (프레임 호환성 또는 규칙 위반).");
                return false;
            }
        }

        /// <summary>
        /// 지정된 슬롯의 파츠를 제거합니다.
        /// </summary>
        /// <returns>제거된 파츠 또는 null</returns>
        public Part DetachPart(string slotIdentifier)
        {
            if (_parts.TryGetValue(slotIdentifier, out Part part))
            {
                _parts.Remove(slotIdentifier);
                RecalculateStats(); // 파츠 제거 후 스탯 재계산
                Debug.Log($"ArmoredFrame({Name}): 슬롯 '{slotIdentifier}'에서 파츠 '{part.Name}'을(를) 성공적으로 제거했습니다.");
                // TODO: PartDetachedEvent 발행 고려
                return part;
            }
            // Debug.LogWarning($"ArmoredFrame({Name}): 제거할 파츠가 슬롯 '{slotIdentifier}'에 없습니다.");
            return null;
        }

        /// <summary>
        /// 지정된 슬롯의 파츠를 반환합니다.
        /// </summary>
        public Part GetPart(string slotIdentifier)
        {
            _parts.TryGetValue(slotIdentifier, out Part part);
            return part;
        }
        
        /// <summary>
        /// 장착된 모든 작동 가능한 파츠 슬롯 식별자 목록을 반환합니다.
        /// </summary>
        public List<string> GetAllOperationalPartSlots()
        {
            return _parts.Where(kvp => kvp.Value.IsOperational).Select(kvp => kvp.Key).ToList();
        }

        /// <summary>
        /// 무기를 장착합니다. (단순 리스트 추가 방식 유지 - 향후 슬롯 기반으로 변경 가능)
        /// </summary>
        public void AttachWeapon(Weapon weapon)
        {
            if (weapon != null && !_equippedWeapons.Contains(weapon))
            {
                _equippedWeapons.Add(weapon);
                // TODO: 무기 장착 슬롯 시스템 구현 시 이 로직 변경 필요
                // TODO: 무기 스탯을 CombinedStats에 반영해야 할 수도 있음 (RecalculateStats 수정)
                RecalculateStats(); // 임시: 무기 변경 시 스탯 재계산 (필요시)
                Debug.Log($"ArmoredFrame({Name}): 무기 '{weapon.Name}' 장착.");
            }
        }

        /// <summary>
        /// 무기를 제거합니다.
        /// </summary>
        public bool DetachWeapon(Weapon weapon)
        {
            if (weapon != null && _equippedWeapons.Remove(weapon))
            {
                 RecalculateStats(); // 임시: 무기 변경 시 스탯 재계산 (필요시)
                 Debug.Log($"ArmoredFrame({Name}): 무기 '{weapon.Name}' 제거.");
                 return true;
            }
            return false;
        }

        /// <summary>
        /// 장착된 모든 무기 목록을 반환합니다. (기존 GetEquippedWeapons 역할)
        /// </summary>
        public List<Weapon> GetAllWeapons() // 이름 변경 GetAllWeapons로 통일성
        {
            return _equippedWeapons;
        }

        /// <summary>
        /// 새로운 상태 효과를 추가합니다. 동일한 이름의 효과가 이미 있다면 지속 시간을 갱신합니다. (간단한 중첩 처리)
        /// </summary>
        public void AddStatusEffect(StatusEffect effect)
        {
            if (effect == null) return;

            var existingEffect = _activeStatusEffects.FirstOrDefault(e => e.EffectName == effect.EffectName);
            if (existingEffect != null)
            {
                // 이미 같은 효과가 있으면 지속 시간만 갱신 (또는 가장 긴 시간으로 설정 등 정책 결정 필요)
                existingEffect.DurationTurns = Math.Max(existingEffect.DurationTurns, effect.DurationTurns); 
                Debug.Log($"ArmoredFrame({Name}): 상태 효과 '{effect.EffectName}' 지속 시간 갱신 ({existingEffect.DurationTurns} 턴 남음).");
            }
            else
            {
                _activeStatusEffects.Add(effect);
                Debug.Log($"ArmoredFrame({Name}): 상태 효과 '{effect.EffectName}' 적용 ({effect.DurationTurns} 턴 동안).");
                // TODO: StatusEffectAppliedEvent 발행 고려
            }
            
            // 상태 효과 적용 후 스탯/상태 재계산 필요 시 호출
            RecalculateStats(); 
            CheckOperationalStatus();
        }

        /// <summary>
        /// 특정 이름의 상태 효과를 제거합니다.
        /// </summary>
        public void RemoveStatusEffect(string effectName)
        {
            int removedCount = _activeStatusEffects.RemoveAll(e => e.EffectName == effectName);
            if (removedCount > 0)
            {
                 Debug.Log($"ArmoredFrame({Name}): 상태 효과 '{effectName}' 제거됨.");
                 // TODO: StatusEffectRemovedEvent 발행 고려
                 
                 // 상태 효과 제거 후 스탯/상태 재계산 필요 시 호출
                 RecalculateStats(); 
                 CheckOperationalStatus();
            }
        }

        /// <summary>
        /// 특정 이름의 상태 효과가 현재 적용 중인지 확인합니다.
        /// </summary>
        public bool HasStatusEffect(string effectName)
        {
            return _activeStatusEffects.Any(e => e.EffectName == effectName);
        }

        /// <summary>
        /// 상태 효과의 남은 턴 수를 감소시키고, 만료된 효과를 제거하며, 틱 효과 이벤트를 발행합니다.
        /// </summary>
        public void TickStatusEffects()
        {
            List<StatusEffect> expiredEffects = new List<StatusEffect>();
            List<StatusEffect> effectsToTick = new List<StatusEffect>(_activeStatusEffects); // 반복 중 컬렉션 변경 방지 위해 복사

            foreach (var effect in effectsToTick)
            {
                // 1. 틱 효과 처리 (지속 시간 감소 전에 처리해야 0턴 남은 효과도 마지막 틱 적용 가능)
                if (effect.TickEffectType != TickEffectType.None && effect.TickValue != 0f)
                {
                    // 이벤트 버스를 통해 StatusEffectTickEvent 발행 (정식 클래스 이름 사용)
                    _eventBus?.Publish(new StatusEffectEvents.StatusEffectTickEvent(this, effect)); 
                    Debug.Log($"ArmoredFrame({Name}): 상태 효과 '{effect.EffectName}' 틱 발생! (Type: {effect.TickEffectType}, Value: {effect.TickValue}). 이벤트 발행됨.");
                }
                
                // 2. 지속 시간 감소 (영구 지속(-1) 효과 제외)
                if (effect.DurationTurns > 0)
                {
                    effect.DurationTurns--;
                }

                // 3. 만료된 효과 처리
                if (effect.DurationTurns == 0) // 0이 되면 이번 턴까지만 유효하고 다음 턴 전에 제거
                {
                    expiredEffects.Add(effect);
                }
            }

            // 만료된 효과 실제 제거
            bool requiresRecalculation = false;
            foreach (var expired in expiredEffects)
            {
                if (_activeStatusEffects.Remove(expired))
                {
                    Debug.Log($"ArmoredFrame({Name}): 상태 효과 '{expired.EffectName}' 만료됨.");
                    requiresRecalculation = true;
                    // TODO: StatusEffectExpiredEvent 발행 고려
                    // _eventBus?.Publish(new StatusEffectExpiredEvent(this, expired.EffectType...));
                }
            }

            // 상태 효과 만료로 스탯/상태 변경 가능성 있을 시 재계산
            if (requiresRecalculation)
            {
                RecalculateStats();
                CheckOperationalStatus();
            }
        }

        /// <summary>
        /// 모든 파츠, 프레임, 파일럿의 스탯을 합산하여 _combinedStats를 업데이트합니다.
        /// Evasion 스탯은 무게/속도 기반 계산 + 파일럿 보너스로 특별 계산됩니다.
        /// </summary>
        private void RecalculateStats()
        {
            Debug.Log($"--- RecalculateStats START ({Name}) --- Current Combined: {_combinedStats}"); // 시작 로그 (유닛 이름 추가)
            // Stats 객체를 새로 생성하여 초기화
            _combinedStats = new Stats();
            Debug.Log($"Initialized Combined: {_combinedStats}"); // 초기화 후 로그

            // 1. 프레임 기본 스탯 추가
            if (_frameBase != null)
            {
                Debug.Log($"Adding Frame ({_frameBase.Name}) Stats: {_frameBase.BaseStats}"); // 더할 값 로그
                _combinedStats += _frameBase.BaseStats;
                Debug.Log($"After Frame: {_combinedStats}"); // 더한 후 로그
            }

            // 2. 모든 장착된 파츠 스탯 합산 (Evasion 제외)
            float totalPartsWeight = 0f;
            foreach (var kvp in _parts) // key-value pair로 순회 (로그에 슬롯 ID 포함 위함)
            {
                string slotId = kvp.Key; // 슬롯 ID 가져오기
                Part part = kvp.Value;   // 파츠 가져오기
                if (part != null)
                {
                    // Evasion을 제외한 나머지 스탯만 합산
                    Stats statsToAdd = new Stats(
                        part.PartStats.AttackPower,
                        part.PartStats.Defense,
                        part.PartStats.Speed,
                        part.PartStats.Accuracy,
                        0, // Evasion은 나중에 계산
                        part.PartStats.Durability,
                        part.PartStats.EnergyEfficiency,
                        part.PartStats.MaxAP,
                        part.PartStats.APRecovery
                    );
                    Debug.Log($"Adding Part ({part.Name} in {slotId}) Stats: {statsToAdd}"); // 더할 값 로그 (슬롯 ID 추가)
                    _combinedStats += statsToAdd;
                    Debug.Log($"After Part ({part.Name}): {_combinedStats}"); // 더한 후 로그
                    totalPartsWeight += part.Weight;
                }
            }

            // 3. 파일럿 스탯 합산 (Evasion 제외)
            if (_pilot != null)
            {
                 // Stats pilotTotalStats = _pilot.GetTotalStats(); // 이전 코드: 파일럿 총 스탯 사용
                 // Stats pilotStatsToAdd = new Stats( ... ); // 이전 코드: 총 스탯 기반 객체 생성
                 // _combinedStats += pilotStatsToAdd; // 이전 코드: 총 스탯 객체 더하기
                 
                 // 수정: 파일럿 기본 스탯 (Evasion 제외) 직접 더하기
                 Stats pilotBaseStatsNoEvasion = new Stats(
                    _pilot.BaseStats.AttackPower,
                    _pilot.BaseStats.Defense,
                    _pilot.BaseStats.Speed,
                    _pilot.BaseStats.Accuracy,
                    0, // Evasion 제외
                    _pilot.BaseStats.Durability,
                    _pilot.BaseStats.EnergyEfficiency,
                    _pilot.BaseStats.MaxAP,
                    _pilot.BaseStats.APRecovery
                 );
                 Debug.Log($"Adding Pilot ({_pilot.Name}) Base Stats: {pilotBaseStatsNoEvasion}"); // 더할 값 로그
                 _combinedStats += pilotBaseStatsNoEvasion;
                 Debug.Log($"After Pilot Base: {_combinedStats}"); // 더한 후 로그
                 
                 // 수정: 파일럿 전문화 보너스 (Evasion 제외) 직접 더하기
                 Stats pilotBonusStatsNoEvasion = new Stats(
                     _pilot.SpecializationBonus.AttackPower,
                     _pilot.SpecializationBonus.Defense,
                     _pilot.SpecializationBonus.Speed,
                     _pilot.SpecializationBonus.Accuracy,
                     0, // Evasion 제외
                     _pilot.SpecializationBonus.Durability,
                     _pilot.SpecializationBonus.EnergyEfficiency,
                     _pilot.SpecializationBonus.MaxAP,
                     _pilot.SpecializationBonus.APRecovery
                 );
                 Debug.Log($"Adding Pilot ({_pilot.Name}) Bonus Stats: {pilotBonusStatsNoEvasion}"); // 더할 값 로그
                 _combinedStats += pilotBonusStatsNoEvasion;
                 Debug.Log($"After Pilot Bonus: {_combinedStats}"); // 더한 후 로그
            }

            // 4. 총 무게 계산 (프레임 + 파츠)
            _totalWeight = (_frameBase?.Weight ?? 0f) + totalPartsWeight;

            // 5. Evasion 스탯 특별 계산
            float baseEvasion = 0f;
            // 속도 기반 회피율 계산 (예시 공식: 속도의 5%)
            float evasionFromSpeed = _combinedStats.Speed * 0.05f;
            // 무게 기반 회피율 페널티 계산 (예시 공식: 무게 10당 0.01 감소)
            float evasionPenaltyFromWeight = _totalWeight * 0.001f;

            baseEvasion = Mathf.Max(0f, evasionFromSpeed - evasionPenaltyFromWeight);

            // 파일럿의 Evasion 스탯 추가
            // float pilotEvasionBonus = _pilot?.GetTotalStats().Evasion ?? 0f; // 이전 코드: 총 스탯 Evasion 사용
            // 수정: 파일럿 기본 Evasion + 전문화 보너스 Evasion 사용
            float pilotBaseEvasion = _pilot?.BaseStats.Evasion ?? 0f;
            float pilotBonusEvasion = _pilot?.SpecializationBonus.Evasion ?? 0f;
            float pilotEvasionBonus = pilotBaseEvasion + pilotBonusEvasion;

            // (선택적) 특정 파츠(예: Legs)의 Evasion 합산
            float partsEvasionBonus = 0f;
            Part legsPart = GetPart("Legs"); // "Legs" 슬롯 가정
            if (legsPart != null) // Legs 파츠 Evasion 로그 추가
            {
                 Debug.Log($"Adding Legs Part ({legsPart.Name}) Evasion Bonus: {legsPart.PartStats.Evasion}");
                 partsEvasionBonus += legsPart.PartStats.Evasion;
            }
            // TODO: 다른 슬롯의 Legs 파츠가 있다면 추가 (예: "Leg_Left", "Leg_Right")
            // Part leftLeg = GetPart("Leg_Left");
            // if (leftLeg != null) { ... }
            // Part rightLeg = GetPart("Leg_Right");
            // if (rightLeg != null) { ... }

            // 최종 Evasion 계산 및 CombinedStats에 반영
            float finalEvasion = baseEvasion + pilotEvasionBonus + partsEvasionBonus;
            Debug.Log($"Calculated Evasion: Base({baseEvasion:F2}) + Pilot({pilotEvasionBonus:F2}) + Parts({partsEvasionBonus:F2}) = Final({finalEvasion:F2})"); // Evasion 계산 로그
            
            // 최종 CombinedStats 생성 전, 현재 값 로그 (Evasion 제외된 값)
            Debug.Log($"Pre-Final Combined Stats (before Evasion overwrite): {_combinedStats}");
            
            _combinedStats = new Stats(
                 _combinedStats.AttackPower,
                 _combinedStats.Defense,
                 _combinedStats.Speed,
                 _combinedStats.Accuracy,
                 finalEvasion, // 최종 계산된 Evasion 값 사용
                 _combinedStats.Durability,
                 _combinedStats.EnergyEfficiency,
                 _combinedStats.MaxAP,
                 _combinedStats.APRecovery
            );

            // 6. 활성 상태 효과 적용
            foreach (var effect in _activeStatusEffects)
            {
                if (effect.StatToModify != StatType.None && effect.ModificationType != ModificationType.None)
                {
                    // Stats 클래스의 ApplyModifier 메소드를 사용하여 _combinedStats 직접 수정
                    _combinedStats.ApplyModifier(effect.StatToModify, effect.ModificationType, effect.ModificationValue);
                    Debug.Log($"Applying Status Effect ({effect.EffectName}): Modified {_combinedStats}"); // 상태 효과 적용 후 로그
                }
            }

            // 최종 스탯 재계산 후 AP 값 재설정 (최대치 초과 방지)
            _currentAP = Mathf.Clamp(_currentAP, 0, _combinedStats.MaxAP);

            // 작동 상태 재확인 (내구도 등)
            CheckOperationalStatus();
            Debug.Log($"--- RecalculateStats END ({Name}) --- Final Combined: {_combinedStats}"); // 종료 로그 (유닛 이름 추가)
        }

        /// <summary>
        /// ArmoredFrame이 작동 가능한 상태인지 확인하고 _isOperational 상태를 업데이트합니다.
        /// 주로 Body 파츠가 파괴되면 작동 불능이 됩니다.
        /// </summary>
        private void CheckOperationalStatus()
        {
            // 프레임 자체가 없으면 작동 불가
            if (_frameBase == null)
            {
                _isOperational = false;
                return;
            }

            // Body 슬롯 정의 확인
            var slots = _frameBase.GetPartSlots();
            string bodySlotKey = slots.FirstOrDefault(kvp => kvp.Value.RequiredPartType == PartType.Body).Key;

            if (!string.IsNullOrEmpty(bodySlotKey))
            {
                // Body 파츠가 없거나 파괴되었는지 확인
                if (!_parts.TryGetValue(bodySlotKey, out Part bodyPart) || !bodyPart.IsOperational)
                {
                    _isOperational = false;
                    // Debug.Log($"ArmoredFrame({Name}): Body 파츠 부재 또는 파괴로 작동 불능 상태.");
                    return; // Body 없으면 바로 작동 불능
                }
            }
            else
            {
                 Debug.LogWarning($"ArmoredFrame({Name}): 프레임({_frameBase.Name})에 Body 슬롯 정의가 없습니다! 작동 상태 판정 불가.");
                 // Body 슬롯 정의가 없는 경우의 처리 방식 결정 필요 (일단 작동 가능으로 둘까?)
            }
            
            // TODO: 추가적인 작동 불능 조건 (예: 에너지 부족, 특정 중요 파츠 파괴 등) 구현 가능
            
            // 모든 필수 조건 통과 시 작동 가능
            _isOperational = true;
        }

        /// <summary>
        /// 특정 슬롯의 파츠에 데미지를 적용합니다.
        /// </summary>
        /// <param name="targetSlotIdentifier">데미지를 받을 파츠의 슬롯 식별자</param>
        /// <param name="damageAmount">적용할 데미지 양</param>
        /// <param name="currentTurn">현재 턴 번호 (이벤트 발행용)</param>
        /// <returns>해당 파츠가 이 공격으로 파괴되었는지 여부</returns>
        public bool ApplyDamage(string targetSlotIdentifier, float damageAmount, int currentTurn)
        {
            if (!_isOperational) return false; 

            if (_parts.TryGetValue(targetSlotIdentifier, out Part targetPart))
            {
                if (!targetPart.IsOperational) return false; 

                float previousDurability = targetPart.CurrentDurability;
                bool wasDestroyedByThisHit = targetPart.ApplyDamage(damageAmount);

                if (wasDestroyedByThisHit)
                {
                    Debug.LogWarning($"ArmoredFrame({Name}): 슬롯 \'{targetSlotIdentifier}\'의 파츠 \'{targetPart.Name}\' 파괴됨!");
                    targetPart.OnDestroyed(this); 

                    // <<< Body 파괴 시 즉시 격파 처리 추가 >>>
                    if (targetSlotIdentifier == "Body") 
                    {
                        _isOperational = false;
                        Debug.LogError($"ArmoredFrame({Name}): Body 파츠 파괴로 인해 작동 불능!");
                    }
                    // <<< Body 파괴 시 즉시 격파 처리 추가 끝 >>>

                    // 스탯 재계산 및 최종 상태 확인 (이벤트 발행 전에 수행)
                    RecalculateStats();
                    CheckOperationalStatus(); 

                    // <<< 이벤트 메시지 동적 설정 시작 >>>
                    string eventEffectDescription = _isOperational 
                        ? $"[{Name}]의 {targetSlotIdentifier} 파괴 (성능 저하 예상)" 
                        : $"[{Name}] 격파됨! ({targetSlotIdentifier} 파괴됨)";
                    // <<< 이벤트 메시지 동적 설정 끝 >>>

                    // 파츠 파괴 이벤트 발행 (주석 처리 - CombatSimulatorService에서 발행)
                    /* 
                    _eventBus?.Publish(new PartEvents.PartDestroyedEvent(
                        this, 
                        targetPart.Type, // PartType 전달
                        null, // 공격자 정보 (TODO)
                        // params string[] effects 부분:
                        $"[{Name}] {targetSlotIdentifier} 파괴됨", // 첫번째 메시지: 어떤 파츠가 파괴되었는지 명시
                        eventEffectDescription // 두번째 메시지: 최종 결과 (성능저하 or 격파)
                    ));
                    */
                    
                    // AF가 작동 불능이 되었는지 로그만 남김 (이벤트와 별개)
                    if (!_isOperational)
                    {
                        Debug.LogError($"ArmoredFrame({Name}) 최종 작동 불능 확인 (Body 파괴 또는 기타 조건 충족)");
                    }

                    return true; 
                }
            }
            else
            {
                Debug.LogWarning($"ArmoredFrame({Name}): 존재하지 않는 슬롯 '{targetSlotIdentifier}'에 데미지 적용 시도됨.");
            }
            return false; 
        }

        /// <summary>
        /// 턴 시작 시 AP를 회복합니다.
        /// </summary>
        public void RecoverAPOnTurnStart()
        {
            if (!_isOperational) return; // 작동 불능 상태면 회복 안 함
            
            float recoveredAP = _combinedStats.APRecovery;
            _currentAP = Mathf.Min(_currentAP + recoveredAP, _combinedStats.MaxAP); // MaxAP를 넘지 않도록
            
            // TODO: AP 회복 관련 이벤트 발행 고려 (APChangedEvent 등)
            Debug.Log($"ArmoredFrame({Name}): AP 회복 +{recoveredAP}, 현재 AP: {_currentAP}/{_combinedStats.MaxAP}");
        }

        /// <summary>
        /// 지정된 양만큼 AP를 소모합니다.
        /// </summary>
        /// <param name="amount">소모할 AP 양</param>
        /// <returns>AP 소모 성공 여부</returns>
        public bool ConsumeAP(float amount)
        {
            if (amount <= 0) return true; // 0 이하 소모는 항상 성공
            
            if (_currentAP >= amount)
            {
                _currentAP -= amount;
                // TODO: AP 소모 관련 이벤트 발행 고려
                Debug.Log($"ArmoredFrame({Name}): AP 소모 -{amount}, 현재 AP: {_currentAP}/{_combinedStats.MaxAP}");
                return true;
            }
            else
            {
                Debug.LogWarning($"ArmoredFrame({Name}): AP 부족! (현재: {_currentAP}, 필요: {amount})");
                return false; // AP 부족으로 소모 실패
            }
        }

        /// <summary>
        /// 특정 행동에 필요한 AP가 충분한지 확인합니다.
        /// </summary>
        /// <param name="requiredAmount">필요한 AP 양</param>
        /// <returns>AP 충분 여부</returns>
        public bool HasEnoughAP(float requiredAmount)
        {
            // 추가: AP 소모량이 0 또는 음수이면 항상 가능
            if (requiredAmount <= 0) 
            {
                return true;
            }
            // 부동 소수점 오차 감안하여 비교
            return _currentAP > requiredAmount - 0.001f; 
        }

        // TODO: 수리 메서드 (Repair)도 슬롯 기반으로 수정 필요
        // public void RepairPart(string slotIdentifier, float amount) { ... }
        // public void RepairAllParts(float amount) { ... }
    }
} 