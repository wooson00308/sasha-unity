using AF.Combat;
using AF.EventBus;
using AF.Models;
using AF.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List 사용 위해 추가
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System; // TimeSpan 사용 위해 추가
using System.Linq;
using System.Text;
using System.Threading; // <<< CancellationTokenSource 사용 위해 추가

namespace AF.UI
{
    /// <summary>
    /// 전투 로그를 UI에 표시하는 서비스 (전투 종료 시 한 번에 표시)
    /// </summary>
    public class CombatTextUIService : MonoBehaviour, IService
    {
        [Header("UI References")] 
        // 기존 _logTextDisplay는 프리팹 방식으로 대체될 수 있으므로 주석 처리 또는 제거 고려
        // [SerializeField] private TMP_Text _logTextDisplay; 
        [SerializeField] private ScrollRect _mainLogScrollRect; // <<< 이름 변경: _scrollRect -> _mainLogScrollRect
        [SerializeField] private GameObject _logLinePrefab; // 로그 한 줄을 표시할 프리팹
        [SerializeField] private Transform _logContainer; // 프리팹이 생성될 부모 컨테이너 (VerticalLayoutGroup 등)
        [SerializeField] private TMP_Text _unitDetailTextDisplay; // 유닛 상세 정보를 표시할 텍스트 (이제 전체 유닛용)
        [SerializeField] private TMP_Text _eventTargetDetailTextDisplay; // 최근 이벤트 대상 유닛 상세 정보 표시용 (추가)
        [SerializeField] private TMP_Text _damageTargetDetailTextDisplay; // 피격 대상 표시용 추가
        // +++ 상세 뷰 스크롤 렉트 추가 +++
        [SerializeField] private ScrollRect _unitDetailScrollRect;
        [SerializeField] private ScrollRect _eventTargetDetailScrollRect;
        [SerializeField] private ScrollRect _damageTargetDetailScrollRect;

        [Header("Animation Settings")]
        // [SerializeField] private float _fadeInDuration = 0.3f; // 각 라인 페이드인 시간 (이제 사용 안 함)
        [SerializeField] private float _textAnimationDuration = 0.5f; // 텍스트 타이핑 애니메이션 시간
        [SerializeField] private float _lineDelay = 0.1f; // 다음 라인 표시 전 딜레이
        [SerializeField] private float _damageTargetDisplayClearDelay = 1.5f; // <<< 피격 대상 UI 지연 삭제 시간
        [SerializeField] private float _durationPerCharacter = 0.02f; // <<< 글자당 애니메이션 시간 추가
        [SerializeField] private float _minTextAnimationDuration = 0.1f; // <<< 최소 애니메이션 시간 추가

        private EventBus.EventBus _eventBus;
        private bool _isInitialized = false;
        private CancellationTokenSource _clearDamageTargetDisplayCts; // <<< 지연 삭제 취소 토큰
        // StringBuilder 와 라인 카운트는 프리팹 방식에서는 관리 방식 변경 필요
        // private System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();
        // private int _currentLogLines = 0;
        // private int _maxLogLines = 1000; // 최대 라인 수 제한 로직도 변경 필요 (오브젝트 삭제 등)
        // private bool _isInitialized = false;
        // // private ArmoredFrame _currentlyDisplayedUnit = null; // 제거
        // private List<ArmoredFrame> _finalParticipants = null; // 제거: 스냅샷 방식으로 변경
        
        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            if (_eventBus == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: EventBus 참조를 가져올 수 없습니다.");
                return;
            }
            if (_logLinePrefab == null || _logContainer == null || _mainLogScrollRect == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: 필요한 UI 참조(프리팹, 컨테이너, 스크롤렉트)가 설정되지 않았습니다.");
                return;
            }
            if (_unitDetailTextDisplay == null) // 상세 정보 텍스트 필드 null 체크 추가
            {
                Debug.LogWarning("CombatTextUIService 초기화 경고: Unit Detail Text Display가 설정되지 않았습니다.");
                // 상세 정보 표시 기능은 작동하지 않지만, 서비스 자체는 계속 진행하도록 할 수 있음
            }
            if (_damageTargetDetailTextDisplay == null)
            {
                 Debug.LogWarning("CombatTextUIService 초기화 경고: Damage Target Detail Text Display가 설정되지 않았습니다.");
            }
            // +++ 상세 뷰 스크롤 렉트 Null 체크 추가 +++
            if (_unitDetailScrollRect == null) Debug.LogWarning("CombatTextUIService: Unit Detail ScrollRect is not assigned.");
            if (_eventTargetDetailScrollRect == null) Debug.LogWarning("CombatTextUIService: Event Target Detail ScrollRect is not assigned.");
            if (_damageTargetDetailScrollRect == null) Debug.LogWarning("CombatTextUIService: Damage Target Detail ScrollRect is not assigned.");

            SubscribeToEvents();
            ClearLog(); 
            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            UnsubscribeFromEvents();
            _clearDamageTargetDisplayCts?.Cancel(); // <<< 종료 시 지연 작업 취소
            _clearDamageTargetDisplayCts?.Dispose();
            _eventBus = null;
            _isInitialized = false;
        }

        #endregion

        #region Unit Detail Display Logic (New)

        /// <summary>
        /// Updates the primary unit detail display using the provided snapshot data.
        /// </summary>
        private void UpdateAllUnitsDetailDisplay(Dictionary<string, ArmoredFrameSnapshot> snapshot)
        {
            if (_unitDetailTextDisplay == null) return; // UI 없으면 중단
            
            if (snapshot == null || snapshot.Count == 0)
            {
                _unitDetailTextDisplay.text = "No snapshot data available."; // 스냅샷 데이터 없을 때
                return;
            }

            _unitDetailTextDisplay.text = FormatAllUnitDetails(snapshot.Values); // 스냅샷 값들 사용
        }

        /// <summary>
        /// Formats the detailed status of all units from snapshot data.
        /// </summary>
        private string FormatAllUnitDetails(IEnumerable<ArmoredFrameSnapshot> snapshots)
        {
            StringBuilder allDetails = new StringBuilder();
            foreach (var snapshot in snapshots.OrderBy(s => s.Name)) // 이름 순 정렬
            {
                // Format snapshot data using a dedicated or adapted method
                allDetails.AppendLine(FormatUnitDetailsFromSnapshot(snapshot)); // Use snapshot formatter
                allDetails.AppendLine(new string('-', 20)); // 구분선 추가
            }
             // 마지막 구분선 제거
            if (allDetails.Length > 20) 
            {
                allDetails.Length -= (20 + Environment.NewLine.Length);
            }
            return allDetails.ToString();
        }

        /// <summary>
        /// Updates the event target detail display with the latest info for the given unit.
        /// </summary>
        private void UpdateEventTargetDetailDisplay(ArmoredFrame affectedUnit)
        {
            if (affectedUnit == null || _eventTargetDetailTextDisplay == null) { if (_eventTargetDetailTextDisplay != null) _eventTargetDetailTextDisplay.text = ""; return; }
            _eventTargetDetailTextDisplay.text = FormatUnitDetails(affectedUnit);
        }

        /// <summary>
        /// Formats the detailed status from an ArmoredFrameSnapshot into a string.
        /// Similar to FormatUnitDetails but uses snapshot data.
        /// </summary>
        private string FormatUnitDetailsFromSnapshot(ArmoredFrameSnapshot snapshot)
        {
            StringBuilder sb = new StringBuilder();
            string statusTag = snapshot.IsOperational ? "<sprite index=17>" : "<sprite index=2>";
            sb.AppendLine($"<b>{snapshot.Name}</b> {statusTag} | AP: {snapshot.CurrentAP:F1}/{snapshot.MaxAP:F1} | DUR: {snapshot.CurrentTotalDurability:F0}/{snapshot.MaxTotalDurability:F0}");
            
            // Append Stats from snapshot
            var stats = snapshot.CombinedStats;
            sb.AppendLine($"  <i>Stats: Atk:{stats.AttackPower:F1}/Def:{stats.Defense:F1}/Spd:{stats.Speed:F1}/Acc:{stats.Accuracy:F1}/Eva:{stats.Evasion:F1}</i>");

            // Append Parts from snapshot
            sb.AppendLine("  <b>Parts:</b>");
            if (snapshot.PartSnapshots != null && snapshot.PartSnapshots.Count > 0)
            {
                // Assuming PartSnapshots dictionary keys are the slot IDs and they are ordered reasonably
                // Or potentially get slot order from FrameBase if that info is also snapshotted (currently not)
                foreach (var kvp in snapshot.PartSnapshots.OrderBy(p => p.Key)) // Order by slot ID
                {
                    string partLine;
                    var partSnapshot = kvp.Value;
                    float currentDurability = partSnapshot.CurrentDurability;
                    float maxDurability = partSnapshot.MaxDurability;
                    float durabilityPercent = maxDurability > 0 ? currentDurability / maxDurability : 0;
                    string partStatusIcon;
                    string statusText = $"({currentDurability:F0}/{maxDurability:F0})";

                    if (!partSnapshot.IsOperational || currentDurability <= 0)
                    {
                        partStatusIcon = "<sprite index=2>"; // DESTROYED
                        statusText = "(Destroyed)";
                    }
                    else if (durabilityPercent <= 0.3f) partStatusIcon = "<sprite index=19>"; // PART CRIT
                    else if (durabilityPercent <= 0.7f) partStatusIcon = "<sprite index=18>"; // PART DMG
                    else partStatusIcon = "<sprite index=17>"; // PART OK

                    // Use slotId (kvp.Key) and part name from snapshot
                    partLine = $"    {partStatusIcon} {kvp.Key} ({partSnapshot.Name}): {statusText}";
                    sb.AppendLine(partLine);
                }
            }
            else
            {
                sb.AppendLine("    (Part data not available in snapshot)");
            }

            // Append Weapons from snapshot
            sb.AppendLine("  <b>Weapons:</b>");
            if (snapshot.WeaponSnapshots != null && snapshot.WeaponSnapshots.Count > 0)
            {
                foreach (var weaponSnapshot in snapshot.WeaponSnapshots)
                {
                    string weaponStatusTag = weaponSnapshot.IsOperational ? "<sprite index=17>" : "<sprite index=3>"; // PART OK / SYS FAIL
                    string ammoStatus = weaponSnapshot.MaxAmmo <= 0 ? "(∞)" : $"({weaponSnapshot.CurrentAmmo}/{weaponSnapshot.MaxAmmo})";
                    sb.AppendLine($"    {weaponStatusTag} {weaponSnapshot.Name} {ammoStatus}");
                }
            }
            else
            {
                sb.AppendLine("    (Weapon data not available in snapshot)");
            }
            
            // TODO: Add Status Effects display here if needed from snapshot

            return sb.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Updates the unit detail display with the latest info for the given unit.
        /// </summary>
        private void UpdateUnitDetailDisplay(ArmoredFrame unit) // 이제 사용 안 함 -> UpdateAllUnitsDetailDisplay 또는 UpdateEventTargetDetailDisplay 사용
        {
            // if (unit == null || _unitDetailTextDisplay == null) return;

            // // Only update if the provided unit is the one currently being displayed
            // // (or if no unit is currently displayed, e.g., on Turn Start)
            // // We might remove this check later if we always want to show the affected unit.
            // if (_currentlyDisplayedUnit == null || _currentlyDisplayedUnit == unit)
            // {
            //      _unitDetailTextDisplay.text = FormatUnitDetails(unit);
            // }
        }

        /// <summary>
        /// Formats the detailed status of an ArmoredFrame into a string for UI display.
        /// Adapted from TextLoggerService.LogUnitDetailsInternal logic.
        /// </summary>
        private string FormatUnitDetails(ArmoredFrame unit)
        {
             if (unit == null) return "No unit selected.";

            StringBuilder sb = new StringBuilder();

            // Use sprite indices defined in TextLogger or a shared location
            string statusTag = unit.IsOperational ? "<sprite index=17>" : "<sprite index=2>"; // PART OK / DESTROYED
            sb.AppendLine($"<b>{unit.Name}</b> {statusTag} | AP: {unit.CurrentAP:F1} / {unit.CombinedStats.MaxAP:F1}");
            
            var stats = unit.CombinedStats;
            sb.AppendLine($"  <i>Stats: Atk:{stats.AttackPower:F1}/Def:{stats.Defense:F1}/Spd:{stats.Speed:F1}/Acc:{stats.Accuracy:F1}/Eva:{stats.Evasion:F1}</i>");

            sb.AppendLine("  <b>Parts:</b>");
            // Use FrameBase slots if available for consistent order and empty slots
            IReadOnlyDictionary<string, PartSlotDefinition> slots = unit.FrameBase?.GetPartSlots();
            IEnumerable<string> slotOrder = slots?.Keys ?? unit.Parts.Keys.OrderBy(k => k); // Fallback to sorted keys
            
            foreach (var slotId in slotOrder)
            {
                string partLine = "";
                if (unit.Parts.TryGetValue(slotId, out Part part))
                {
                    float currentDurability = part.CurrentDurability;
                    float maxDurability = part.MaxDurability;
                    float durabilityPercent = maxDurability > 0 ? currentDurability / maxDurability : 0;
                    string partStatusIcon;
                    string statusText = $"({currentDurability:F0}/{maxDurability:F0})";

                    if (!part.IsOperational || currentDurability <= 0)
                    {
                        partStatusIcon = "<sprite index=2>"; // DESTROYED
                        statusText = "(Destroyed)";
                    }
                    else if (durabilityPercent <= 0.3f) partStatusIcon = "<sprite index=19>"; // PART CRIT
                    else if (durabilityPercent <= 0.7f) partStatusIcon = "<sprite index=18>"; // PART DMG
                    else partStatusIcon = "<sprite index=17>"; // PART OK

                    partLine = $"    {partStatusIcon} {slotId} ({part.Name}): {statusText}";
                }
                else
                {
                    // Part is missing from this slot
                    partLine = $"    <sprite index=20> {slotId}: (Empty)"; // PART EMPTY
                }
                sb.AppendLine(partLine);
            }

            sb.AppendLine("  <b>Weapons:</b>");
            var weapons = unit.GetAllWeapons();
            if (weapons != null && weapons.Count > 0)
            {
                foreach (var weapon in weapons)
                {
                    string weaponStatusTag = weapon.IsOperational ? "<sprite index=17>" : "<sprite index=3>"; // PART OK / SYS FAIL
                    string ammoStatus = weapon.MaxAmmo <= 0 ? "(∞)" : $"({weapon.CurrentAmmo}/{weapon.MaxAmmo})";
                    sb.AppendLine($"    {weaponStatusTag} {weapon.Name} {ammoStatus}");
                }
            }
            else
            {
                sb.AppendLine("    (None)");
            }
            
            // TODO: Add Status Effects display here if needed

            return sb.ToString().TrimEnd('\r', '\n');
        }

        // --- Implemented UpdateSnapshotWithDelta (Revised with Debug Logs) --- //
        private Dictionary<string, ArmoredFrameSnapshot> UpdateSnapshotWithDelta(
            Dictionary<string, ArmoredFrameSnapshot> currentSnapshots,
            TextLogger.LogEntry deltaEntry)
        {
            if (currentSnapshots == null || deltaEntry == null)
            {
                Debug.LogWarning("[UpdateSnapshotWithDelta] Input snapshots or deltaEntry is null.");
                return currentSnapshots;
            }

            // Create a copy to modify
            var updatedSnapshots = new Dictionary<string, ArmoredFrameSnapshot>(currentSnapshots);
            bool snapshotUpdated = false; // Flag to track if any change was made

            // <<< DEBUG: Log entry and event type >>>
            // Debug.Log($"[UpdateSnapshotWithDelta] Processing Delta Log - Type: {deltaEntry.EventType}");

            switch (deltaEntry.EventType)
            {
                case LogEventType.DamageApplied:
                    // <<< DEBUG: Log DamageApplied processing >>>
                    // Debug.Log($"[UpdateSnapshotWithDelta] Handling DamageApplied for Target: {deltaEntry.Damage_TargetUnitName}, Part: {deltaEntry.Damage_DamagedPartSlot}, Amount: {deltaEntry.Damage_AmountDealt}");
                    if (!string.IsNullOrEmpty(deltaEntry.Damage_TargetUnitName) &&
                        updatedSnapshots.TryGetValue(deltaEntry.Damage_TargetUnitName, out var targetSnapshot_Dmg))
                    {
                        // <<< DEBUG: Target snapshot found >>>
                        // Debug.Log($"[UpdateSnapshotWithDelta] Found target snapshot for {deltaEntry.Damage_TargetUnitName}. Current Total Dur: {targetSnapshot_Dmg.CurrentTotalDurability}");

                        var updatedPartSnapshots_Dmg = new Dictionary<string, PartSnapshot>(targetSnapshot_Dmg.PartSnapshots);
                        if (!string.IsNullOrEmpty(deltaEntry.Damage_DamagedPartSlot) &&
                            updatedPartSnapshots_Dmg.TryGetValue(deltaEntry.Damage_DamagedPartSlot, out var damagedPartSnapshot_Dmg))
                        {
                             // <<< DEBUG: Part snapshot found >>>
                             // Debug.Log($"[UpdateSnapshotWithDelta] Found part snapshot for {deltaEntry.Damage_DamagedPartSlot}. Current Dur: {damagedPartSnapshot_Dmg.CurrentDurability}, New Dur from Delta: {deltaEntry.Damage_NewDurability}");

                            var updatedPart_Dmg = CreateUpdatedPartSnapshot(
                                damagedPartSnapshot_Dmg,
                                newDurability: deltaEntry.Damage_NewDurability // Use delta's calculated new durability
                            );
                            updatedPartSnapshots_Dmg[deltaEntry.Damage_DamagedPartSlot] = updatedPart_Dmg;

                             // <<< DEBUG: Part updated in dictionary >>>
                             // Debug.Log($"[UpdateSnapshotWithDelta] Updated part {deltaEntry.Damage_DamagedPartSlot} durability to {updatedPart_Dmg.CurrentDurability}");

                            var updatedTargetSnapshot_Dmg = CreateUpdatedArmoredFrameSnapshot(
                                targetSnapshot_Dmg,
                                updatedPartSnapshots_Dmg
                            );
                            updatedSnapshots[deltaEntry.Damage_TargetUnitName] = updatedTargetSnapshot_Dmg;
                            snapshotUpdated = true; // Mark as updated

                             // <<< DEBUG: Frame snapshot updated >>>
                             // Debug.Log($"[UpdateSnapshotWithDelta] Updated frame snapshot for {deltaEntry.Damage_TargetUnitName}. New Total Dur: {updatedTargetSnapshot_Dmg.CurrentTotalDurability}");
                        }
                        else { /* Debug.LogWarning($"[UpdateSnapshotWithDelta] Part snapshot not found for slot: {deltaEntry.Damage_DamagedPartSlot}"); */ }
                    }
                    else { /* Debug.LogWarning($"[UpdateSnapshotWithDelta] Target snapshot not found for name: {deltaEntry.Damage_TargetUnitName}"); */ }
                    break;

                case LogEventType.ActionCompleted:
                     // <<< DEBUG: Log ActionCompleted processing >>>
                     // Debug.Log($"[UpdateSnapshotWithDelta] Handling ActionCompleted for Actor: {deltaEntry.Action_ActorName}, Type: {deltaEntry.Action_Type}");
                    if (deltaEntry.Action_Type == CombatActionEvents.ActionType.Move &&
                        deltaEntry.Action_IsSuccess &&
                        !string.IsNullOrEmpty(deltaEntry.Action_ActorName) &&
                        deltaEntry.Action_NewPosition.HasValue &&
                        updatedSnapshots.TryGetValue(deltaEntry.Action_ActorName, out var actorSnapshot_Move))
                    {
                        // <<< DEBUG: Processing Move action >>>
                        // Debug.Log($"[UpdateSnapshotWithDelta] Updating position for {deltaEntry.Action_ActorName} to {deltaEntry.Action_NewPosition.Value}");
                        var updatedActorSnapshot_Move = CreateUpdatedArmoredFrameSnapshot(
                            actorSnapshot_Move,
                            newPosition: deltaEntry.Action_NewPosition.Value
                        );
                        updatedSnapshots[deltaEntry.Action_ActorName] = updatedActorSnapshot_Move;
                        snapshotUpdated = true;
                    }
                    // TODO: Handle AP changes if delta includes it. E.g.:
                    /*
                    if (deltaEntry.Action_APCost.HasValue && !string.IsNullOrEmpty(deltaEntry.Action_ActorName) && updatedSnapshots.TryGetValue(deltaEntry.Action_ActorName, out var actorSnapshot_AP)) {
                         float newAP = actorSnapshot_AP.CurrentAP - deltaEntry.Action_APCost.Value;
                         var updatedActorSnapshot_AP = CreateUpdatedArmoredFrameSnapshot(
                             actorSnapshot_AP,
                             newCurrentAP: newAP
                         );
                         updatedSnapshots[deltaEntry.Action_ActorName] = updatedActorSnapshot_AP;
                         snapshotUpdated = true;
                    }
                    */
                    break;

                case LogEventType.RepairApplied:
                    // <<< DEBUG: Log RepairApplied processing >>>
                    // Debug.Log($"[UpdateSnapshotWithDelta] Handling RepairApplied for Target: {deltaEntry.Repair_TargetName}, Part: {deltaEntry.Repair_PartSlot}, Amount: {deltaEntry.Repair_Amount}");
                    if (!string.IsNullOrEmpty(deltaEntry.Repair_TargetName) &&
                        updatedSnapshots.TryGetValue(deltaEntry.Repair_TargetName, out var targetSnapshot_Rep))
                    {
                        var updatedPartSnapshots_Rep = new Dictionary<string, PartSnapshot>(targetSnapshot_Rep.PartSnapshots);
                        if (!string.IsNullOrEmpty(deltaEntry.Repair_PartSlot) &&
                            updatedPartSnapshots_Rep.TryGetValue(deltaEntry.Repair_PartSlot, out var repairedPartSnapshot_Rep))
                        {
                            // <<< DEBUG: Part found for repair >>>
                            // Debug.Log($"[UpdateSnapshotWithDelta] Found part {deltaEntry.Repair_PartSlot} for repair. Current Dur: {repairedPartSnapshot_Rep.CurrentDurability}");

                            float newDurability = Mathf.Min(repairedPartSnapshot_Rep.CurrentDurability + deltaEntry.Repair_Amount, repairedPartSnapshot_Rep.MaxDurability);
                            var updatedRepairedPart_Rep = CreateUpdatedPartSnapshot(
                                repairedPartSnapshot_Rep,
                                newDurability: newDurability
                            );
                            updatedPartSnapshots_Rep[deltaEntry.Repair_PartSlot] = updatedRepairedPart_Rep;

                            // <<< DEBUG: Part repaired >>>
                            // Debug.Log($"[UpdateSnapshotWithDelta] Repaired part {deltaEntry.Repair_PartSlot} to {newDurability}");

                            var updatedTargetSnapshot_Rep = CreateUpdatedArmoredFrameSnapshot(
                                targetSnapshot_Rep,
                                updatedPartSnapshots_Rep
                            );
                            updatedSnapshots[deltaEntry.Repair_TargetName] = updatedTargetSnapshot_Rep;
                            snapshotUpdated = true;

                             // <<< DEBUG: Frame snapshot updated after repair >>>
                             // Debug.Log($"[UpdateSnapshotWithDelta] Updated frame snapshot for {deltaEntry.Repair_TargetName} after repair. New Total Dur: {updatedTargetSnapshot_Rep.CurrentTotalDurability}");
                        }
                         else { /* Debug.LogWarning($"[UpdateSnapshotWithDelta] Part snapshot not found for repair slot: {deltaEntry.Repair_PartSlot}"); */ }
                    }
                     else { /* Debug.LogWarning($"[UpdateSnapshotWithDelta] Target snapshot not found for repair name: {deltaEntry.Repair_TargetName}"); */ }
                    break;

                case LogEventType.PartDestroyed:
                    // <<< DEBUG: Log PartDestroyed processing >>>
                    // Debug.Log($"[UpdateSnapshotWithDelta] Handling PartDestroyed for Owner: {deltaEntry.PartDestroyed_OwnerName}, PartType: {deltaEntry.PartDestroyed_PartType}");
                    // TODO: This requires the LogEntry to contain the specific SLOT ID, not just PartType.
                    // If Slot ID is added to LogEntry:
                    /*
                    if (!string.IsNullOrEmpty(deltaEntry.PartDestroyed_OwnerName) &&
                        !string.IsNullOrEmpty(deltaEntry.PartDestroyed_SlotId) && // Assuming SlotId is added
                        updatedSnapshots.TryGetValue(deltaEntry.PartDestroyed_OwnerName, out var ownerSnapshot_PD))
                    {
                        var updatedPartSnapshots_PD = new Dictionary<string, PartSnapshot>(ownerSnapshot_PD.PartSnapshots);
                        if (updatedPartSnapshots_PD.TryGetValue(deltaEntry.PartDestroyed_SlotId, out var destroyedPartSnapshot_PD))
                        {
                            var updatedDestroyedPart_PD = CreateUpdatedPartSnapshot(
                                destroyedPartSnapshot_PD,
                                newDurability: 0 // Set durability to 0
                            );
                            updatedPartSnapshots_PD[deltaEntry.PartDestroyed_SlotId] = updatedDestroyedPart_PD;

                            var updatedOwnerSnapshot_PD = CreateUpdatedArmoredFrameSnapshot(
                                ownerSnapshot_PD,
                                updatedPartSnapshots_PD
                            );
                            updatedSnapshots[deltaEntry.PartDestroyed_OwnerName] = updatedOwnerSnapshot_PD;
                            snapshotUpdated = true;
                            Debug.Log($"[UpdateSnapshotWithDelta] Marked part {deltaEntry.PartDestroyed_SlotId} as destroyed for {deltaEntry.PartDestroyed_OwnerName}");
                        }
                    }
                    */
                    break;

                // Add other cases as needed (e.g., Status Effects changing stats, Weapon ammo changes)

                default:
                    break;
            }

             // <<< DEBUG: Log if snapshot was updated >>>
             // if (snapshotUpdated) Debug.Log($"[UpdateSnapshotWithDelta] Snapshot was updated for event type {deltaEntry.EventType}.");
             // else Debug.Log($"[UpdateSnapshotWithDelta] Snapshot NOT updated for event type {deltaEntry.EventType}.");

            // Return the potentially modified dictionary
            return updatedSnapshots;
        }

        // --- Helper methods for creating updated snapshots (struct immutability) ---

        // Helper to create an updated PartSnapshot
        private PartSnapshot CreateUpdatedPartSnapshot(PartSnapshot original, float? newDurability = null)
        {
            float durability = newDurability ?? original.CurrentDurability;
            bool isOperational = durability > 0;
            // Use the new constructor directly
            return new PartSnapshot(original.Name, durability, original.MaxDurability, isOperational);
        }

        // Helper to create an updated ArmoredFrameSnapshot
        private ArmoredFrameSnapshot CreateUpdatedArmoredFrameSnapshot(
            ArmoredFrameSnapshot original,
            Dictionary<string, PartSnapshot> newPartSnapshots = null,
            Vector3? newPosition = null,
            float? newCurrentAP = null // Optional: Add other fields to update as needed
            )
        {
            var partsToUse = newPartSnapshots ?? original.PartSnapshots;
            // Recalculate totals based on the parts collection being used
            float totalCurrentDurability = partsToUse?.Values.Sum(p => p.CurrentDurability) ?? 0;
            float totalMaxDurability = partsToUse?.Values.Sum(p => p.MaxDurability) ?? 0;
            bool isOperational = totalCurrentDurability > 0;

            // Use the new constructor directly
            return new ArmoredFrameSnapshot(
                original.Name,
                newPosition ?? original.Position,
                original.TeamId,
                newCurrentAP ?? original.CurrentAP,
                original.MaxAP, // Assuming MaxAP doesn't change mid-combat from these deltas
                totalCurrentDurability, // Use recalculated value
                totalMaxDurability,   // Use recalculated value
                isOperational,        // Use recalculated value
                original.CombinedStats, // Assuming stats don't change mid-combat from these deltas
                partsToUse ?? new Dictionary<string, PartSnapshot>(),
                original.WeaponSnapshots ?? new List<WeaponSnapshot>() // Assuming weapons don't change from these deltas
            );
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEndUpdateDetails); // 이전 핸들러 제거

            // 실시간 업데이트를 위한 이벤트 구독 추가 -> 제거
            // _eventBus.Subscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStartUpdateDetails); 
            // _eventBus.Subscribe<DamageEvents.DamageAppliedEvent>(HandleDamageAppliedUpdateDetails);
            // _eventBus.Subscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyedUpdateDetails);
            // _eventBus.Subscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompletedUpdateDetails);
            // 필요시 StatusEffect 이벤트 등 추가 구독...
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnEndEvent>(HandleTurnEndUpdateDetails); // 이전 핸들러 제거

            // 실시간 업데이트 이벤트 구독 해제 -> 제거
            // _eventBus.Unsubscribe<CombatSessionEvents.TurnStartEvent>(HandleTurnStartUpdateDetails);
            // _eventBus.Unsubscribe<DamageEvents.DamageAppliedEvent>(HandleDamageAppliedUpdateDetails);
            // _eventBus.Unsubscribe<PartEvents.PartDestroyedEvent>(HandlePartDestroyedUpdateDetails);
            // _eventBus.Unsubscribe<CombatActionEvents.ActionCompletedEvent>(HandleActionCompletedUpdateDetails);
            // 필요시 StatusEffect 이벤트 등 구독 해제...
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            ClearLog();
            // _finalParticipants = null; // 제거
            if (_unitDetailTextDisplay != null) 
                _unitDetailTextDisplay.text = "Waiting for combat data..."; // 초기 메시지 설정
            if (_eventTargetDetailTextDisplay != null) 
                _eventTargetDetailTextDisplay.text = " "; // 대상 창도 초기화
            
            // 전투 시작 시 초기 참가자 정보 표시 시도 -> 제거: ProcessLogsAsync에서 첫 로그 처리 시 업데이트됨
            /*
            UniTask.Void(async () => 
            { 
                await UniTask.DelayFrame(1); // 한 프레임 지연
                UpdateAllUnitsDetailDisplay(); 
            });
            */
        }

        // HandleCombatEnd는 메인 로그 표시에만 집중 -> 참가자 목록 저장 로직 제거
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent evt)
        {
            // 로그 재생 전에 최종 참가자 목록 저장 -> 제거
            /*
            var simulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            if (simulator != null) 
            {
                var currentParticipants = simulator.GetParticipants(); 
                _finalParticipants = currentParticipants?.ToList(); 
            }
            else
            {
                Debug.LogError("CombatTextUIService: Combat Simulator not found at combat end. Cannot store final participants.");
                _finalParticipants = new List<ArmoredFrame>();
            }
            */
            ProcessLogsAsync(evt).Forget();
        }

        // --- 실시간 업데이트 핸들러들 --- -> 제거
        /*
        private void HandleTurnStartUpdateDetails(CombatSessionEvents.TurnStartEvent evt)
        {
            UpdateAllUnitsDetailDisplay();
            UpdateEventTargetDetailDisplay(evt.ActiveUnit); // 이벤트 대상 창에는 현재 턴 유닛 표시
        }

        private void HandleDamageAppliedUpdateDetails(DamageEvents.DamageAppliedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Target); // 이벤트 대상 창에는 피격 유닛 표시
        }

        private void HandlePartDestroyedUpdateDetails(PartEvents.PartDestroyedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Frame); // 이벤트 대상 창에는 파츠 파괴된 유닛 표시
        }

        private void HandleActionCompletedUpdateDetails(CombatActionEvents.ActionCompletedEvent ev)
        {
            UpdateAllUnitsDetailDisplay(); // 항상 전체 업데이트
            UpdateEventTargetDetailDisplay(ev.Actor); // 이벤트 대상 창에는 행동 완료한 유닛 표시
        }
        */

        // +++ 이전 TurnEnd 핸들러 제거 +++
        /*
        private void HandleTurnEndUpdateDetails(CombatSessionEvents.TurnEndEvent evt)
        {
            if (evt.ActiveUnit == null || _unitDetailTextDisplay == null) return; // 유닛이나 UI 없으면 중단
            UpdateUnitDetailDisplay(evt.ActiveUnit); // Turn End 시 해당 유닛 정보로 업데이트 (추후 로직 변경 예정)
        }
        */
        // +++ 핸들러 추가 끝 +++

        // 로그 처리 로직 (로그 재생 및 상세 정보 동기화)
        // ProcessLogsAsync: Updated logic for delta logs
        private async UniTaskVoid ProcessLogsAsync(CombatSessionEvents.CombatEndEvent evt)
        {
            ITextLogger textLogger = ServiceLocator.Instance.GetService<TextLoggerService>()?.TextLogger;
            if (textLogger == null) { await CreateAndAnimateLogLine("오류: 전투 로그를 불러올 수 없습니다."); return; }

            List<TextLogger.LogEntry> logEntries = textLogger.GetLogEntries();
            if (logEntries == null || logEntries.Count == 0) { Debug.LogWarning("표시할 전투 로그가 없습니다."); return; }

            ClearLog(); // Clear UI before playback
            if (_unitDetailTextDisplay != null) _unitDetailTextDisplay.text = " ";
            if (_eventTargetDetailTextDisplay != null) _eventTargetDetailTextDisplay.text = " ";
            if (_damageTargetDetailTextDisplay != null) _damageTargetDetailTextDisplay.text = " ";

            // Use the correct snapshot type from AF.Models
            Dictionary<string, AF.Models.ArmoredFrameSnapshot> currentPlaybackState = null;
            ArmoredFrameSnapshot activeUnitSnapshotForRadar = default; // <<< Store active unit for radar events

            Debug.Log("Starting Combat Log Playback...");

            foreach (var logEntry in logEntries)
            {
                // <<< 루프 시작 시 무조건 취소 로직 제거 >>>
                // _clearDamageTargetDisplayCts?.Cancel();
                // _clearDamageTargetDisplayCts?.Dispose();
                // _clearDamageTargetDisplayCts = null;

                bool isSnapshotUpdate = false;

                if (logEntry.EventType == LogEventType.UnitActivationStart || logEntry.EventType == LogEventType.RoundStart)
                {
                    if (logEntry.TurnStartStateSnapshot != null)
                    {
                        UpdateAllUnitsDetailDisplay(logEntry.TurnStartStateSnapshot);
                        // Copy the dictionary, ensuring values are the correct AF.Models.ArmoredFrameSnapshot type
                        currentPlaybackState = new Dictionary<string, AF.Models.ArmoredFrameSnapshot>(logEntry.TurnStartStateSnapshot);

                        // <<< Determine active unit for radar event >>>
                        if (logEntry.ContextUnit != null && currentPlaybackState.TryGetValue(logEntry.ContextUnit.Name, out var activeSnap))
                        {
                            activeUnitSnapshotForRadar = activeSnap;
                        }
                        else if (currentPlaybackState.Count > 0) // Fallback
                        {
                            activeUnitSnapshotForRadar = currentPlaybackState.Values.FirstOrDefault(s => s.IsOperational);
                            if (activeUnitSnapshotForRadar.Name == null && currentPlaybackState.Count > 0) // If no operational, take first
                                activeUnitSnapshotForRadar = currentPlaybackState.Values.First();
                        }
                        else { activeUnitSnapshotForRadar = default; } // No units?
                        // <<< Active unit determination end >>>

                        isSnapshotUpdate = true; // Mark that a snapshot update occurred
                    }
                }
                else if (currentPlaybackState != null)
                {
                    currentPlaybackState = UpdateSnapshotWithDelta(currentPlaybackState, logEntry);

                    // <<< 전체 유닛 정보 UI를 매번 업데이트 >>>
                    if (currentPlaybackState != null)
                    {
                        UpdateAllUnitsDetailDisplay(currentPlaybackState);
                        // <<< 전체 유닛 상세 뷰 스크롤 맨 위로 >>>
                        if (_unitDetailScrollRect != null) _unitDetailScrollRect.verticalNormalizedPosition = 1f;
                    }
                    // <<< 업데이트 로직 이동 끝 >>>

                    // 이벤트 대상 UI 업데이트 (기존 로직)
                    if (logEntry.ShouldUpdateTargetView && logEntry.ContextUnit != null)
                    {
                        if (currentPlaybackState != null && currentPlaybackState.TryGetValue(logEntry.ContextUnit.Name, out var updatedContextSnapshot))
                        {
                            _eventTargetDetailTextDisplay.text = FormatUnitDetailsFromSnapshot(updatedContextSnapshot);
                            // <<< 이벤트 대상 상세 뷰 스크롤 맨 위로 >>>
                            if (_eventTargetDetailScrollRect != null) _eventTargetDetailScrollRect.verticalNormalizedPosition = 1f;
                        }
                        else { _eventTargetDetailTextDisplay.text = FormatUnitDetails(logEntry.ContextUnit); } // Fallback
                    }
                    else if (!logEntry.ShouldUpdateTargetView && _eventTargetDetailTextDisplay != null) // 대상 업데이트 안 하면 비우기 (선택적)
                    {
                         // _eventTargetDetailTextDisplay.text = "";
                    }

                    // <<< 피격 대상 UI 업데이트 추가 >>>
                    if (_damageTargetDetailTextDisplay != null) // UI 필드가 할당되었는지 확인
                    {
                        if (logEntry.EventType == LogEventType.DamageApplied && !string.IsNullOrEmpty(logEntry.Damage_TargetUnitName))
                        {
                            // <<< 새로운 DamageApplied 로그 처리: 기존 지연 삭제 취소 >>>
                            _clearDamageTargetDisplayCts?.Cancel();
                            _clearDamageTargetDisplayCts?.Dispose();
                            _clearDamageTargetDisplayCts = null;

                            if (currentPlaybackState != null && currentPlaybackState.TryGetValue(logEntry.Damage_TargetUnitName, out var damagedTargetSnapshot))
                            {
                                _damageTargetDetailTextDisplay.text = FormatUnitDetailsFromSnapshot(damagedTargetSnapshot);
                                // <<< 피격 대상 상세 뷰 스크롤 맨 위로 >>>
                                if (_damageTargetDetailScrollRect != null) _damageTargetDetailScrollRect.verticalNormalizedPosition = 1f;
                            }
                            else // 스냅샷에 없으면 Fallback (이론상 발생하면 안됨)
                            {
                                _damageTargetDetailTextDisplay.text = $"피격 대상 정보 없음: {logEntry.Damage_TargetUnitName}";
                            }
                        }
                        else // DamageApplied 아니면 지연 삭제 시작 (이미 예약된 작업이 없으면)
                        {
                            // <<< 조건 추가: 이미 예약된 지연 삭제 작업이 없고, 텍스트가 비어있지 않다면 >>>
                            if (_clearDamageTargetDisplayCts == null && !string.IsNullOrEmpty(_damageTargetDetailTextDisplay.text))
                            {
                                ClearDamageTargetDisplayAfterDelay().Forget();
                            }
                        }
                    }
                    // <<< 피격 대상 UI 업데이트 끝 >>>
                }
                else if (logEntry.ShouldUpdateTargetView && logEntry.ContextUnit != null)
                {
                    UpdateEventTargetDetailDisplay(logEntry.ContextUnit);
                }

                string messageToDisplay = logEntry.Message;
                if (logEntry.Level == LogLevel.System || logEntry.EventType == LogEventType.RoundStart || logEntry.EventType == LogEventType.UnitActivationStart)
                {
                    messageToDisplay = $"<b>{logEntry.Message}</b>";
                }

                // --- Animate Log Line --- 
                await CreateAndAnimateLogLine(messageToDisplay);

                // --- Publish Playback Event AFTER animation --- 
                if (_eventBus != null)
                {
                    if (isSnapshotUpdate && currentPlaybackState != null && activeUnitSnapshotForRadar.Name != null)
                    {
                         _eventBus.Publish(new PlaybackEvents.PlaybackSnapshotUpdateEvent(currentPlaybackState, activeUnitSnapshotForRadar));
                    }
                    else // Process delta events only if not a snapshot update frame
                    {
                        switch(logEntry.EventType)
                        {
                            case LogEventType.ActionCompleted:
                                if (logEntry.Action_Type == CombatActionEvents.ActionType.Move && logEntry.Action_IsSuccess && logEntry.Action_NewPosition.HasValue && !string.IsNullOrEmpty(logEntry.Action_ActorName))
                                {
                                     _eventBus.Publish(new PlaybackEvents.PlaybackUnitMoveEvent(logEntry.Action_ActorName, logEntry.Action_NewPosition.Value));
                                }
                                else if (logEntry.Action_IsSuccess && !string.IsNullOrEmpty(logEntry.Action_ActorName))
                                {
                                    _eventBus.Publish(new PlaybackEvents.PlaybackGenericActionEvent(logEntry.Action_ActorName));
                                }
                                break;
                            case LogEventType.DamageApplied:
                                if (!string.IsNullOrEmpty(logEntry.Damage_TargetUnitName))
                                {
                                     _eventBus.Publish(new PlaybackEvents.PlaybackDamageEvent(logEntry.Damage_TargetUnitName));
                                }
                                break;
                            case LogEventType.PartDestroyed:
                                if (!string.IsNullOrEmpty(logEntry.PartDestroyed_OwnerName))
                                {
                                     _eventBus.Publish(new PlaybackEvents.PlaybackPartDestroyedEvent(logEntry.PartDestroyed_OwnerName));
                                }
                                break;
                            case LogEventType.RepairApplied:
                                 if (!string.IsNullOrEmpty(logEntry.Repair_TargetName))
                                 {
                                     _eventBus.Publish(new PlaybackEvents.PlaybackRepairEvent(logEntry.Repair_TargetName));
                                 }
                                break;
                            case LogEventType.StatusEffectApplied:
                                 if (!string.IsNullOrEmpty(logEntry.StatusApplied_TargetName))
                                 {
                                     _eventBus.Publish(new PlaybackEvents.PlaybackStatusEffectEvent(logEntry.StatusApplied_TargetName));
                                 }
                                break;
                             // Add other relevant event types if needed
                        }
                    }
                }
            }
            Debug.Log("Combat Log Playback Finished.");

            // <<< 피격 대상 UI 클리어 추가 >>>
            if (_damageTargetDetailTextDisplay != null)
            {
                 _damageTargetDetailTextDisplay.text = " ";
            }
            // <<< 클리어 시 지연 작업도 취소 >>>
            _clearDamageTargetDisplayCts?.Cancel();
            _clearDamageTargetDisplayCts?.Dispose();
            _clearDamageTargetDisplayCts = null;

            // --- Publish Playback Complete Event --- 
            _eventBus?.Publish(new PlaybackEvents.PlaybackCompleteEvent());
        }

        #endregion

        #region Logging Methods

        private async UniTask CreateAndAnimateLogLine(string message)
        {
            if (_logLinePrefab == null || _logContainer == null) return;

            GameObject logInstance = Instantiate(_logLinePrefab, _logContainer);
            TMP_Text logText = logInstance.GetComponentInChildren<TMP_Text>();

            // <<< 메인 로그 스크롤 로직 복원 (애니메이션 전) >>>
            if (_mainLogScrollRect != null)
            {
                 // 즉시 맨 아래로 이동 (애니메이션 대신)
                 await UniTask.Yield(PlayerLoopTiming.LastUpdate); // 레이아웃 업데이트 기다릴 수 있도록 Yield 추가
                 _mainLogScrollRect.verticalNormalizedPosition = 0f;
                 // 또는 애니메이션 사용:
                 // await _mainLogScrollRect.DOVerticalNormalizedPos(0f, 0.1f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            }

            if (logText != null)
            {
                logText.text = "";
                // <<< 텍스트 길이에 따른 동적 듀레이션 계산 >>>
                float targetDuration = message.Length * _durationPerCharacter;
                float actualDuration = Mathf.Clamp(targetDuration, _minTextAnimationDuration, _textAnimationDuration);
                // <<< 동적 듀레이션 적용 >>>
                await logText.DOText(message, actualDuration).SetEase(Ease.Linear).AsyncWaitForCompletion();
            }
            else
            {
                 Debug.LogWarning("로그 라인 프리팹에 TMP_Text 컴포넌트를 찾을 수 없습니다.");
            }

            if (_lineDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_lineDelay));
            }
            else {
                await UniTask.Yield(PlayerLoopTiming.LastUpdate); // Wait one frame for rebuild to settle
            }
        }

        public void ClearLog()
        {
            if (_logContainer == null) return;

            foreach (Transform child in _logContainer)
            {
                Destroy(child.gameObject);
            }

            if (_mainLogScrollRect != null)
            {
                 _mainLogScrollRect.verticalNormalizedPosition = 1f;
            }
            if (_unitDetailTextDisplay != null)
            {
                _unitDetailTextDisplay.text = " ";
            }
            if (_damageTargetDetailTextDisplay != null)
            {
                 _damageTargetDetailTextDisplay.text = " ";
            }
        }

        #endregion

        // +++ 피격 대상 UI 지연 삭제 메서드 +++
        private async UniTaskVoid ClearDamageTargetDisplayAfterDelay()
        {
            _clearDamageTargetDisplayCts = new CancellationTokenSource();
            var token = _clearDamageTargetDisplayCts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_damageTargetDisplayClearDelay), cancellationToken: token);

                // Delay가 성공적으로 완료되고 취소되지 않았으면 텍스트 클리어
                if (_damageTargetDetailTextDisplay != null && !token.IsCancellationRequested)
                {
                    _damageTargetDetailTextDisplay.text = " ";
                }
            }
            catch (OperationCanceledException)
            {
                 // 작업이 취소된 경우 (예: 새로운 DamageApplied 로그가 와서)
                 // Debug.Log("Damage target display clear cancelled.");
            }
            finally
            {
                // CTS 정리
                _clearDamageTargetDisplayCts?.Dispose();
                _clearDamageTargetDisplayCts = null;
            }
        }
        // +++ 지연 삭제 메서드 끝 +++
    }
} 