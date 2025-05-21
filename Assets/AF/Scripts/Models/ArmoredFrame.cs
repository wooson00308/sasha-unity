using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus; // 이벤트 버스 사용 가정
using AF.Services; // 서비스 로케이터 사용 가정
using AF.Combat; // 네임스페이스 추가
using AF.Combat.Handlers; // 핸들러 접근을 위해 추가
using UnityEngine;
using AF.AI.BehaviorTree; // <<< 행동 트리 네임스페이스 추가

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

        /// <summary>
        /// 현재 남은 수리 횟수
        /// </summary>
        private int _currentRepairUses;

        // ──────────────────────────────────────────────────────────────
        // 실드(흡수 HP) 관련
        // ──────────────────────────────────────────────────────────────
        [NonSerialized]
        private float _currentShieldHP; // EnergyShield 등 실드용 임시 HP

        /// <summary>
        /// 현재 실드(흡수 HP) 양을 반환합니다.
        /// </summary>
        public float CurrentShieldHP => _currentShieldHP;

        /// <summary>
        /// 실드를 추가합니다. (음수면 무시)
        /// </summary>
        public void AddShield(float amount)
        {
            if (amount <= 0f) return;
            _currentShieldHP += amount;
        }

        /// <summary>
        /// 실드를 모두 제거합니다.
        /// </summary>
        public void ClearShield() => _currentShieldHP = 0f;

        // 이벤트 버스 (생성자나 메서드에서 주입받거나 서비스 로케이터로 가져옴)
        private EventBus.EventBus _eventBus;

        TextLoggerService textLoggerService;

        // AI Behavior Tree를 위한 현재 타겟
        public ArmoredFrame CurrentTarget { get; set; }

        // +++ 행동 트리 및 블랙보드 참조 +++
        public BTNode BehaviorTreeRoot { get; set; } // BTNode 정의 후 주석 해제. AF.AI.BehaviorTree.BTNode로 사용 예정
        public Blackboard AICtxBlackboard { get; private set; }
        // +++ 행동 트리 및 블랙보드 참조 끝 +++

        // +++ 현재 전투 컨텍스트 참조 필드 추가 +++
        public CombatContext CurrentCombatContext { get; set; } // 외부(CombatSimulatorService)에서 설정

        /// <summary>
        /// 기체가 파괴되었는지 여부를 반환합니다.
        /// </summary>
        public bool IsDestroyed => !_isOperational;

        /// <summary>
        /// 기체의 현재 위치입니다.
        /// </summary>
        public Vector3 Position => _position;

        // 공개 프로퍼티
        public string Name => _name;
        public Frame FrameBase => _frameBase;
        public Pilot Pilot => _pilot;
        public int TeamId => _teamId;
        public IReadOnlyDictionary<string, Part> Parts => _parts;
        public IReadOnlyList<Weapon> EquippedWeapons => _equippedWeapons.AsReadOnly();
        public Stats CombinedStats => _combinedStats;
        public bool IsOperational => _isOperational;
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects.AsReadOnly();
        public float CurrentAP => _currentAP;
        public float TotalWeight => _totalWeight;

        /// <summary>
        /// 현재 모든 파츠의 내구도 총합 대비 현재 내구도 총합의 비율을 반환합니다.
        /// </summary>
        public float CurrentDurabilityRatio
        {
            get
            {
                if (CombinedStats.Durability <= 0f) return 0f; 
                return GetCurrentAggregatedHP() / CombinedStats.Durability;
            }
        }

        public Vector3? IntendedMovePosition { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public ArmoredFrame(string name, Frame frameBase, Vector3 initialPosition, int teamId)
        {
            _name = name;
            _frameBase = frameBase ?? throw new ArgumentNullException(nameof(frameBase));
            _position = initialPosition;
            _teamId = teamId;

            // +++ 블랙보드 초기화 +++
            AICtxBlackboard = new Blackboard();
            // +++ 블랙보드 초기화 끝 +++

            // 이벤트 버스 인스턴스 가져오기 (서비스 로케이터 사용 예시)
            if (ServiceLocator.Instance != null)
            {
                if (ServiceLocator.Instance.HasService<EventBusService>())
                _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus;
                if (ServiceLocator.Instance.HasService<TextLoggerService>())
                    textLoggerService = ServiceLocator.Instance.GetService<TextLoggerService>();
            }
            else
            {
                Debug.LogError($"ArmoredFrame({Name}): ServiceLocator.Instance is null!");
            }

            _activeStatusEffects = new List<StatusEffect>(); // 상태 효과 리스트 초기화
            
            // AP는 RecalculateStats에서 Clamp될 것이므로 여기서의 초기화는 기본값으로 충분할 수 있음
            // _currentAP = _combinedStats.MaxAP; // 스탯 계산 전에 기본값으로라도 초기화 (RecalculateStats에서 처리 예정)

            RecalculateStats(); // 프레임 기본 스탯으로 초기화 (이때 _currentRepairUses와 _currentAP도 설정됨)
            CheckOperationalStatus();
            
            _currentAP = _combinedStats.MaxAP; // RecalculateStats 이후 MaxAP로 현재 AP 설정

            textLoggerService?.TextLogger?.Log($"[ArmoredFrame Ctor] {Name} initialized. CombinedMaxRepairUses: {_combinedStats.MaxRepairUses}, CurrentRepairUses: {_currentRepairUses}", LogLevel.Debug);
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
                CheckOperationalStatus(); // <<< 기체 작동 상태 재확인
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
            if (string.IsNullOrEmpty(slotIdentifier)) return null;
            if (_parts.TryGetValue(slotIdentifier, out Part part))
            {
                _parts.Remove(slotIdentifier);
                RecalculateStats(); 
                CheckOperationalStatus(); 
                return part;
            }
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
            bool newEffectAdded = false;
            StatusEffect effectToProcess = null;

            if (existingEffect != null)
            {
                existingEffect.DurationTurns = Math.Max(existingEffect.DurationTurns, effect.DurationTurns); 
                // 재적용 시에도 OnApply를 호출할지 여부는 기획에 따라 결정 (일단 호출한다고 가정)
                effectToProcess = existingEffect;
            }
            else
            {
                _activeStatusEffects.Add(effect);
                newEffectAdded = true;
                effectToProcess = effect;
            }
            
            // OnApply 핸들러 즉시 호출
            if (effectToProcess != null)
            {
                if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<IStatusEffectProcessor>())
                {
                    var statusEffectProcessor = ServiceLocator.Instance.GetService<IStatusEffectProcessor>();
                    var handler = statusEffectProcessor.GetHandler(effectToProcess.EffectType);
                    if (handler != null)
                    {
                        // CurrentCombatContext가 설정되어 있다는 가정 하에 사용
                        // 전투 상황이 아닐 때(예: 테스트 설정 중) CurrentCombatContext가 null일 수 있으므로 방어 코드 추가
                        if (CurrentCombatContext != null) 
                        {
                           handler.OnApply(CurrentCombatContext, this, effectToProcess);
                        }
                        else
                        {
                            // CombatContext 없이 호출하거나, 필수적인 경우 경고/에러 처리
                            // 이 경우 OnApply 핸들러가 CombatContext 없이도 최소한의 동작을 하거나,
                            // CombatContext가 반드시 필요한 핸들러라면 문제가 될 수 있음.
                            // 지금은 일단 CombatContext가 있는 경우에만 호출하도록 함.
                            Debug.LogWarning($"ArmoredFrame({Name}): CurrentCombatContext is null. OnApply for {effectToProcess.EffectName} might not work as expected outside of active combat simulation.");
                            // 또는 빈 컨텍스트로라도 호출? handler.OnApply(new CombatContext(...minimal args...), this, effectToProcess);
                            // 아니면, OnApply 시그니처를 CombatContext ctx = null 로 변경? -> 핸들러 구현체들이 모두 수정되어야 함
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"ArmoredFrame({Name}): IStatusEffectProcessor service not found. Cannot call OnApply for {effectToProcess.EffectName}.");
                }
            }

            RecalculateStats(); 
            CheckOperationalStatus();

            // StatusEffectAppliedEvent 발행은 핸들러의 OnApply 내부에서 처리하는 것을 권장
            // 또는 여기서 발행한다면, 핸들러 호출 이후에 하는 것이 적절
            if (newEffectAdded || effectToProcess != null) // 효과가 실제로 추가/갱신되었을 때
            {
                 _eventBus?.Publish(new StatusEffectEvents.StatusEffectAppliedEvent(this, null /*source?*/, effectToProcess.EffectType, effectToProcess.DurationTurns, effectToProcess.ModificationValue, effectToProcess.EffectName));
            }
        }

        /// <summary>
        /// 특정 이름의 상태 효과를 제거합니다.
        /// </summary>
        public void RemoveStatusEffect(string effectName)
        {
            StatusEffect effectToRemove = _activeStatusEffects.FirstOrDefault(e => e.EffectName == effectName);

            if (effectToRemove != null)
            {
                if (ServiceLocator.Instance != null && ServiceLocator.Instance.HasService<IStatusEffectProcessor>())
                {
                    var statusEffectProcessor = ServiceLocator.Instance.GetService<IStatusEffectProcessor>();
                    var handler = statusEffectProcessor.GetHandler(effectToRemove.EffectType);
                    if (handler != null)
                    {
                        if (CurrentCombatContext != null)
            {
                            handler.OnRemove(CurrentCombatContext, this, effectToRemove); 
                        }
                        else
                        {
                            Debug.LogWarning($"ArmoredFrame({Name}): CurrentCombatContext is null. OnRemove for {effectToRemove.EffectName} might not work as expected outside of active combat simulation.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"ArmoredFrame({Name}): IStatusEffectProcessor service not found. Cannot call OnRemove for {effectName}.");
                }

                _activeStatusEffects.Remove(effectToRemove); 
                 
                 RecalculateStats(); 
                 CheckOperationalStatus();
                _eventBus?.Publish(new StatusEffectEvents.StatusEffectExpiredEvent(this, effectToRemove.EffectType, effectToRemove.EffectName, true));
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
        public void TickStatusEffects() // 이 메서드는 StatusEffectProcessor로 옮겨졌습니다.
        {
            // 이 로직은 StatusEffectProcessor로 옮겨졌습니다.
            // ArmoredFrame은 더 이상 자체적으로 상태 효과를 Tick하지 않습니다.
            // 만약 CombatSimulatorService 등 외부에서 이 메서드를 직접 호출하고 있다면, 
            // 해당 호출부를 ServiceLocator.Instance.GetService<IStatusEffectProcessor>().Tick(ctx, this)로 변경해야 합니다.
            Debug.LogWarning($"ArmoredFrame.TickStatusEffects() is deprecated and was called on {Name}. Use IStatusEffectProcessor.Tick() instead.");
        }

        /// <summary>
        /// 모든 파츠, 프레임, 파일럿의 스탯을 합산하여 _combinedStats를 업데이트합니다.
        /// Evasion 스탯은 무게/속도 기반 계산 + 파일럿 보너스로 특별 계산됩니다.
        /// </summary>
        public void RecalculateStats()
        {
            Stats newStats = new Stats(); // 기본값으로 초기화

            // 1. 프레임 기본 스탯
            if (_frameBase != null)
            {
                newStats.Add(_frameBase.BaseStats); // Stats.Add() 사용
                _totalWeight = _frameBase.Weight; // 프레임 무게부터 시작
            }
            else
            {
                _totalWeight = 0f;
            }

            // 2. 장착된 모든 파츠의 스탯 합산
            foreach (var partEntry in _parts.Values)
            {
                if (partEntry != null && partEntry.IsOperational)
                {
                    newStats.Add(partEntry.PartStats); // partEntry.Stats 대신 partEntry.PartStats 사용
                    _totalWeight += partEntry.Weight;
                }
            }

            // 3. 파일럿 스탯 (TODO: 파일럿의 GetTotalStats() 결과를 newStats에 합산해야 함)
            if (_pilot != null)
            {
                newStats.Add(_pilot.GetTotalStats()); // Stats.Add() 사용 가정
            }
            
            // 4. 상태 효과에 의한 스탯 변동 적용
            if (_activeStatusEffects != null && _activeStatusEffects.Count > 0)
            {
                foreach (var eff in _activeStatusEffects)
                {
                    if (eff == null) continue;
                    if (eff.StatToModify == StatType.None || eff.ModificationType == ModificationType.None) continue;
                    newStats.ApplyModifier(eff.StatToModify, eff.ModificationType, eff.ModificationValue);
                }
            }

            // 5. 무기 스탯 (선택적 - 현재는 무기 자체 스탯이 기체 스탯에 직접 영향주지 않음)
            // foreach (var weapon in _equippedWeapons) { ... }

            // 최종 계산된 스탯을 _combinedStats에 할당
            _combinedStats = newStats; // 이 시점에서 _combinedStats.MaxRepairUses는 프레임 및 모든 파츠의 합산된 값임

            // 기체 AP도 현재 스탯에 맞춰 갱신 (최대치를 넘지 않도록 Clamp)
            _currentAP = Mathf.Clamp(_currentAP, 0, _combinedStats.MaxAP);

            // <<< _currentRepairUses 업데이트 추가 >>>
            _currentRepairUses = (int)_combinedStats.MaxRepairUses;

            // TODO: 스탯 변경 이벤트 발행 고려 (예: StatsChangedEvent)
            // _eventBus?.Publish(new CombatEvents.StatsChangedEvent(this, _combinedStats));

            textLoggerService?.TextLogger?.Log($"[RecalculateStats] {Name} - Recalculated. CombinedMaxRepairUses: {_combinedStats.MaxRepairUses}, CurrentRepairUses: {_currentRepairUses}", LogLevel.Debug);
            // 확인용 로그 제거 (위 로그에 CurrentRepairUses 포함됨)
            // textLoggerService?.TextLogger?.Log($"[RecalculateStats] {Name} - AFTER UPDATE. CurrentRepairUses: {_currentRepairUses}", LogLevel.Debug);
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
        /// <param name="source">공격자 (없을 경우 null)</param>
        /// <param name="isCritical">치명타 여부</param>
        /// <param name="isCounterAttack">반격 여부</param>
        /// <returns>해당 파츠가 이 공격으로 파괴되었는지 여부</returns>
        public bool ApplyDamage(string targetSlotIdentifier, float damageAmount, int currentTurn, ArmoredFrame source = null, bool isCritical = false, bool isCounterAttack = false, float armorPiercing = 0f, float shieldPiercing = 0f)
        {
            // DamageResult 구조체 사용 제거, bool 반환 (partWasDestroyedThisHit)을 기본으로 사용
            bool partWasDestroyedThisHit = false;
            bool frameWasUltimatelyDestroyed = false; // 프레임 파괴 여부 추적용

            if (string.IsNullOrEmpty(targetSlotIdentifier) || !_parts.ContainsKey(targetSlotIdentifier))
            {
                targetSlotIdentifier = _parts.FirstOrDefault(p => p.Value.Type == PartType.Body).Key ?? _parts.Keys.FirstOrDefault();
                if (string.IsNullOrEmpty(targetSlotIdentifier))
                {
                    Debug.LogError($"{Name}: 유효한 타겟 슬롯을 찾을 수 없어 데미지 적용 불가.");
                    return false; 
                }
            }

            Part targetPart = _parts[targetSlotIdentifier];
            if (targetPart == null || targetPart.IsDestroyed)
            {
                if (targetPart != null && targetPart.Type != PartType.Body)
                {
                    Debug.LogWarning($"{Name}: 이미 파괴된 파츠({targetPart.Name})에 대한 공격 시도. 데미지 무효.");
                    return false; 
                }
            }

            // PreDamageApplicationEvent 생성자 인자 수정 (9개 -> 4개)
            var preDamageEvent = new CombatActionEvents.PreDamageApplicationEvent(this, targetPart, damageAmount, currentTurn /*, source, isCritical, isCounterAttack, armorPiercing, shieldPiercing*/);
            _eventBus?.Publish(preDamageEvent);
            float finalDamage = preDamageEvent.ModifiedDamage;

            // ──────────────────────────────────────────────────────────────
            // 1단계: 실드로 데미지 흡수
            // ──────────────────────────────────────────────────────────────
            if (_currentShieldHP > 0f && finalDamage > 0f)
            {
                float absorbed = Mathf.Min(finalDamage, _currentShieldHP);
                _currentShieldHP -= absorbed;
                finalDamage -= absorbed;

                // 실드가 전부 소모되면 상태 효과 제거 (효과 이름이 EnergyShield 고정 가정)
                if (_currentShieldHP <= 0.001f)
                {
                    _currentShieldHP = 0f;
                    if (HasStatusEffect("EnergyShield"))
                    {
                        RemoveStatusEffect("EnergyShield");
                    }
                }

                // 실드가 데미지를 완전히 흡수했다면 파츠 데미지 처리 생략
                if (finalDamage <= 0.001f)
                {
                    return false;
                }
            }

            if (targetPart != null && !targetPart.IsDestroyed) 
            {
                partWasDestroyedThisHit = targetPart.ApplyDamage(finalDamage);
                // DamageAppliedEvent 생성자 인자 확인 및 사용 (기존 10개 인자 유지)
                _eventBus?.Publish(new CombatActionEvents.DamageAppliedEvent(source, this, targetPart, finalDamage, isCritical, partWasDestroyedThisHit, currentTurn, targetPart.CurrentDurability, targetPart.MaxDurability, isCounterAttack));
            }
            else if (targetPart != null && targetPart.IsDestroyed)
            {
                partWasDestroyedThisHit = true; // 이미 파괴된 상태였음
            }

            CheckOperationalStatus();
            frameWasUltimatelyDestroyed = !_isOperational; // CheckOperationalStatus 이후 상태 반영

            if (partWasDestroyedThisHit && (targetPart == null || targetPart.IsOperational == false)) 
            {
                RecalculateStats();
                Debug.Log($"{Name}의 {targetPart?.Name ?? targetSlotIdentifier} 파츠가 파괴되어 스탯 재계산됨.");
            }

            if (frameWasUltimatelyDestroyed)
            {
                Debug.Log($"{Name}이(가) 파괴되었습니다!");
                _eventBus?.Publish(new CombatSessionEvents.UnitDefeatedEvent(this, currentTurn, source)); 
            }
            return partWasDestroyedThisHit; // 파츠가 이번 공격으로 파괴되었는지 여부 반환
        }

        /// <summary>
        /// 턴 시작 시 AP를 회복합니다.
        /// </summary>
        public void RecoverAPOnTurnStart()
        {
            if (!_isOperational) return;
            float recoveredAP = _combinedStats.APRecovery;
            _currentAP = Mathf.Min(_currentAP + recoveredAP, _combinedStats.MaxAP);
        }

        /// <summary>
        /// 지정된 양만큼 AP를 소모합니다.
        /// </summary>
        /// <param name="amount">소모할 AP 양</param>
        /// <returns>AP 소모 성공 여부</returns>
        public bool ConsumeAP(float amount)
        {
            if (amount <= 0) return true; 
            if (_currentAP >= amount)
            {
                _currentAP -= amount;
                return true;
            }
            else
            {
                Debug.LogWarning($"ArmoredFrame({Name}): AP 부족! (현재: {_currentAP}, 필요: {amount})");
                return false; 
            }
        }

        /// <summary>
        /// 특정 행동에 필요한 AP가 충분한지 확인합니다.
        /// </summary>
        /// <param name="requiredAmount">필요한 AP 양</param>
        /// <returns>AP 충분 여부</returns>
        public bool HasEnoughAP(float requiredAmount)
        {
            if (requiredAmount <= 0) { return true; }
            return _currentAP > requiredAmount - 0.001f; 
        }

        /// <summary>
        /// 지정된 파츠에 수리를 적용합니다.
        /// </summary>
        /// <param name="targetSlotIdentifier">수리할 파츠의 슬롯 ID</param>
        /// <param name="repairAmount">수리량</param>
        /// <returns>실제로 수리된 양. 수리할 필요가 없거나 파츠가 없으면 0을 반환합니다.</returns>
        public float ApplyRepair(string targetSlotIdentifier, float repairAmount)
        {
            if (string.IsNullOrEmpty(targetSlotIdentifier) || repairAmount <= 0) { return 0f; }
            Part partToRepair = GetPart(targetSlotIdentifier);
            if (partToRepair == null || !partToRepair.IsOperational || partToRepair.CurrentDurability >= partToRepair.MaxDurability) { return 0f; }
            float maxDurability = partToRepair.MaxDurability;
            float currentDurability = partToRepair.CurrentDurability;
            float actualRepairAmount = Mathf.Min(repairAmount, maxDurability - currentDurability);
            bool? statusChanged = partToRepair.SetDurability(currentDurability + actualRepairAmount); 
            if (statusChanged.HasValue && statusChanged.Value)
            {
                RecalculateStats();
                CheckOperationalStatus();
                Debug.Log($"ArmoredFrame({Name}): {targetSlotIdentifier} 파츠 수리 완료. 작동 가능 상태로 복구됨.");
            }
            return actualRepairAmount; 
        }

        /// <summary>
        /// 기체의 위치를 설정합니다. (CombatTestRunner 등 외부 설정용)
        /// </summary>
        public void SetPosition(Vector3 newPosition) { _position = newPosition; }

        public void EndTurn() { }

        public Weapon GetPrimaryWeapon()
        {
            if (EquippedWeapons == null || EquippedWeapons.Count == 0) { return null; }
            return EquippedWeapons.FirstOrDefault(weapon => weapon != null && weapon.IsOperational);
        }

        /// <summary>
        /// 현재 장착된 모든 파츠의 현재 내구도 총합을 반환합니다.
        /// </summary>
        /// <returns>모든 파츠의 현재 내구도 합계</returns>
        public float GetCurrentAggregatedHP()
        {
            float currentHP = 0f;
            if (_parts != null) 
            {
                foreach (var partKvp in _parts) { if (partKvp.Value != null) { currentHP += partKvp.Value.CurrentDurability; } }
            }
            return currentHP;
        }

        /// <summary>
        /// 현재 남은 수리 횟수를 반환합니다.
        /// </summary>
        public int GetCurrentRepairUses() { return _currentRepairUses; }

        /// <summary>
        /// 수리 횟수를 1 감소시킵니다.
        /// </summary>
        public void DecrementRepairUses() { if (_currentRepairUses > 0) { _currentRepairUses--; } }
    }
} 