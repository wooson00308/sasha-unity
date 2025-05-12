using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus; // 이벤트 버스 사용 가정
using AF.Services; // 서비스 로케이터 사용 가정
using AF.Combat; // 네임스페이스 추가
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

        // 이벤트 버스 (생성자나 메서드에서 주입받거나 서비스 로케이터로 가져옴)
        private EventBus.EventBus _eventBus;

        // AI Behavior Tree를 위한 현재 타겟
        public ArmoredFrame CurrentTarget { get; set; }

        // +++ 행동 트리 및 블랙보드 참조 +++
        public BTNode BehaviorTreeRoot { get; set; } // BTNode 정의 후 주석 해제. AF.AI.BehaviorTree.BTNode로 사용 예정
        public Blackboard AICtxBlackboard { get; private set; }
        // +++ 행동 트리 및 블랙보드 참조 끝 +++

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

            _currentRepairUses = (int)_combinedStats.MaxRepairUses; // 수리 횟수 초기화

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
            if (_parts.TryGetValue(slotIdentifier, out Part part))
            {
                _parts.Remove(slotIdentifier);
                RecalculateStats(); // 파츠 제거 후 스탯 재계산
                CheckOperationalStatus(); // <<< 기체 작동 상태 재확인
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
            if (existingEffect != null)
            {
                // 이미 같은 효과가 있으면 지속 시간만 갱신 (또는 가장 긴 시간으로 설정 등 정책 결정 필요)
                existingEffect.DurationTurns = Math.Max(existingEffect.DurationTurns, effect.DurationTurns); 
            }
            else
            {
                _activeStatusEffects.Add(effect);
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
                // newStats.Add(_pilot.GetTotalStats()); // Stats.Add() 사용 가정
            }
            
            // 4. 무기 스탯 (선택적 - 현재는 무기 자체 스탯이 기체 스탯에 직접 영향주지 않음)
            // foreach (var weapon in _equippedWeapons) { ... }

            // 최종 계산된 스탯을 _combinedStats에 할당
            _combinedStats = newStats; // 이 시점에서 _combinedStats.MaxRepairUses는 프레임 및 모든 파츠의 합산된 값임

            // 기체 AP도 현재 스탯에 맞춰 갱신 (최대치를 넘지 않도록 Clamp)
            _currentAP = Mathf.Clamp(_currentAP, 0, _combinedStats.MaxAP);

            // TODO: 스탯 변경 이벤트 발행 고려 (예: StatsChangedEvent)
            // _eventBus?.Publish(new CombatEvents.StatsChangedEvent(this, _combinedStats));
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
        public bool ApplyDamage(string targetSlotIdentifier, float damageAmount, int currentTurn, ArmoredFrame source = null, bool isCritical = false, bool isCounterAttack = false)
        {
            if (string.IsNullOrEmpty(targetSlotIdentifier) || !_parts.ContainsKey(targetSlotIdentifier))
            {
                // 슬롯이 없거나 유효하지 않으면 몸통(Body)을 기본 타겟으로 (게임 규칙에 따라 변경 가능)
                targetSlotIdentifier = _parts.FirstOrDefault(p => p.Value.Type == PartType.Body).Key ?? _parts.Keys.FirstOrDefault();
                if (string.IsNullOrEmpty(targetSlotIdentifier))
                {
                    Debug.LogError($"{Name}: 유효한 타겟 슬롯을 찾을 수 없어 데미지 적용 불가.");
                    return false; // 데미지 적용 실패
                }
            }

            Part targetPart = _parts[targetSlotIdentifier];
            if (targetPart == null || targetPart.IsDestroyed)
            {
                // 이미 파괴된 파츠이거나 존재하지 않는 파츠에 대한 처리 (예: 데미지 무시 또는 다른 파츠로 전이)
                // 현재는 몸통이 아니라면 데미지 무효, 몸통이라면 _isOperational 체크로 이어짐
                if (targetPart != null && targetPart.Type != PartType.Body)
                {
                    Debug.LogWarning($"{Name}: 이미 파괴된 파츠({targetPart.Name})에 대한 공격 시도. 데미지 무효.");
                    return false; // 데미지 적용 안 함 (몸통 아니면)
                }
                // 몸통이 이미 파괴되었다면 기체 파괴 로직으로 이어짐
            }

            // 이벤트 발행 준비 (데미지 적용 전)
            var preDamageEvent = new CombatActionEvents.PreDamageApplicationEvent(this, targetPart, damageAmount, currentTurn);
            _eventBus?.Publish(preDamageEvent);

            // 수정된 데미지 (이벤트 핸들러에 의해 변경될 수 있음)
            float finalDamage = preDamageEvent.ModifiedDamage;


            bool wasDestroyedThisHit = false;
            if (targetPart != null && !targetPart.IsDestroyed) // 파괴되지 않은 파츠에만 데미지 적용
            {
                wasDestroyedThisHit = targetPart.ApplyDamage(finalDamage);

                // 이벤트 발행 (데미지 적용 후)
                _eventBus?.Publish(new CombatActionEvents.DamageAppliedEvent(
                    source: source, // 전달받은 source 사용
                    target: this,
                    damagedPart: targetPart,
                    damageDealt: finalDamage,
                    isCritical: isCritical, // 전달받은 isCritical 사용
                    wasDestroyed: wasDestroyedThisHit,
                    turnNumber: currentTurn,
                    partCurrentDurability: targetPart.CurrentDurability,
                    partMaxDurability: targetPart.MaxDurability,
                    isCounterAttack: isCounterAttack // 전달받은 isCounterAttack 사용
                ));
            }


            // 몸통 파츠가 파괴되었는지 또는 모든 파츠가 파괴되었는지 등 기체 운용 상태 확인
            CheckOperationalStatus();


            // 만약 이번 공격으로 파츠가 파괴되었다면, 스탯과 무게를 즉시 재계산
            if (wasDestroyedThisHit)
            {
                RecalculateStats();
                Debug.Log($"{Name}의 {targetPart.Name} 파츠가 파괴되어 스탯 재계산됨.");
            }

            // 기체가 파괴되었다면 추가 처리 (예: 파괴 이벤트 발행)
            if (!_isOperational)
            {
                Debug.Log($"{Name}이(가) 파괴되었습니다!");
                _eventBus?.Publish(new CombatSessionEvents.UnitDefeatedEvent(this, currentTurn, null)); // 패배 이벤트 발행
                return true; // 기체 파괴됨
            }

            return wasDestroyedThisHit; // 파츠가 이 공격으로 파괴되었는지 여부 반환
        }

        /// <summary>
        /// 턴 시작 시 AP를 회복합니다.
        /// </summary>
        public void RecoverAPOnTurnStart()
        {
            if (!_isOperational) return; // 작동 불능 상태면 회복 안 함
            
            float recoveredAP = _combinedStats.APRecovery;
            _currentAP = Mathf.Min(_currentAP + recoveredAP, _combinedStats.MaxAP); // MaxAP를 넘지 않도록
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

        /// <summary>
        /// 지정된 파츠에 수리를 적용합니다.
        /// </summary>
        /// <param name="targetSlotIdentifier">수리할 파츠의 슬롯 ID</param>
        /// <param name="repairAmount">수리량</param>
        /// <returns>실제로 수리된 양. 수리할 필요가 없거나 파츠가 없으면 0을 반환합니다.</returns>
        public float ApplyRepair(string targetSlotIdentifier, float repairAmount)
        {
            if (string.IsNullOrEmpty(targetSlotIdentifier) || repairAmount <= 0)
            {
                return 0f;
            }

            Part partToRepair = GetPart(targetSlotIdentifier);

            if (partToRepair == null || !partToRepair.IsOperational || partToRepair.CurrentDurability >= partToRepair.MaxDurability)
            {
                return 0f; // 수리할 파츠가 없거나, 작동 불능이거나, 이미 최대 내구도인 경우
            }

            float maxDurability = partToRepair.MaxDurability;
            float currentDurability = partToRepair.CurrentDurability;

            float targetDurability = currentDurability + repairAmount; // 목표 내구도 계산
            float actualRepairAmount = Mathf.Min(repairAmount, maxDurability - currentDurability); // 실제 수리될 양 계산

            // SetDurability 호출하고 상태 변경 여부 확인
            bool? statusChanged = partToRepair.SetDurability(targetDurability);

            // 상태가 변경되었고, 그 상태가 '작동 가능(true)'이면 후속 처리
            if (statusChanged.HasValue && statusChanged.Value)
            {
                // 파츠가 수리되어 작동 가능 상태가 되면, 전체 스탯 및 작동 상태 재계산 필요
                RecalculateStats();
                CheckOperationalStatus();
                // TODO: PartRepairedEvent 같은 이벤트 발행 고려 가능
                Debug.Log($"ArmoredFrame({Name}): {targetSlotIdentifier} 파츠 수리 완료. 작동 가능 상태로 복구됨.");
            }

            return actualRepairAmount; // 실제 수리된 양 반환
        }

        // TODO: 수리 메서드 (Repair)도 슬롯 기반으로 수정 필요
        // public void RepairPart(string slotIdentifier, float amount) { ... } // ApplyRepair로 대체 가능
        // public void RepairAllParts(float amount) { ... } // 필요하다면 구현

        /// <summary>
        /// 기체의 위치를 설정합니다. (CombatTestRunner 등 외부 설정용)
        /// </summary>
        public void SetPosition(Vector3 newPosition)
        {
            _position = newPosition;
        }

        public void EndTurn()
        {
            // 현재 턴 종료 로직 (예: AP 초기화 등)
        }

        public Weapon GetPrimaryWeapon()
        {
            if (EquippedWeapons == null || EquippedWeapons.Count == 0)
            {
                return null;
            }

            // Return the first operational weapon found in the list.
            // More sophisticated logic (e.g., designated primary slot, highest damage) could be added later.
            return EquippedWeapons.FirstOrDefault(weapon => weapon != null && weapon.IsOperational);
        }

        /// <summary>
        /// 현재 장착된 모든 파츠의 현재 내구도 총합을 반환합니다.
        /// </summary>
        /// <returns>모든 파츠의 현재 내구도 합계</returns>
        public float GetCurrentAggregatedHP()
        {
            float currentHP = 0f;
            if (_parts != null) // _parts는 Dictionary<string, Part>
            {
                foreach (var partKvp in _parts)
                {
                    Part part = partKvp.Value;
                    if (part != null)
                    {
                        currentHP += part.CurrentDurability;
                    }
                }
            }
            return currentHP;
        }

        /// <summary>
        /// 현재 남은 수리 횟수를 반환합니다.
        /// </summary>
        public int GetCurrentRepairUses()
        {
            return _currentRepairUses;
        }

        /// <summary>
        /// 수리 횟수를 1 감소시킵니다.
        /// </summary>
        public void DecrementRepairUses()
        {
            if (_currentRepairUses > 0)
            {
                _currentRepairUses--;
            }
        }
    }
} 