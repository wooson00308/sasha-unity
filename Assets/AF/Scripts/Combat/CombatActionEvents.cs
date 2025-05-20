using UnityEngine;
using AF.Models;
using AF.Data;

namespace AF.Combat
{
    /// <summary>
    /// 전투 행동과 관련된 이벤트
    /// </summary>
    public class CombatActionEvents
    {
        /// <summary>
        /// 행동 타입 열거형
        /// </summary>
        public enum ActionType
        {
            None = 0,
            Attack,
            Move,
            UseAbility,
            Defend,
            Retreat,
            Overwatch,
            Reload,
            RepairSelf,
            RepairAlly
        }

        /// <summary>
        /// 행동 시작 이벤트
        /// </summary>
        public class ActionStartEvent : ICombatEvent
        {
            public ArmoredFrame Actor { get; private set; }
            public ActionType Action { get; private set; }
            public object[] ActionParameters { get; private set; }
            public int TurnNumber { get; private set; }

            public ActionStartEvent(ArmoredFrame actor, ActionType action, int turnNumber, params object[] actionParameters)
            {
                Actor = actor;
                Action = action;
                TurnNumber = turnNumber;
                ActionParameters = actionParameters;
            }
        }

        /// <summary>
        /// 행동 완료 이벤트
        /// </summary>
        public class ActionCompletedEvent : ICombatEvent
        {
            public ArmoredFrame Actor { get; private set; }
            public ActionType Action { get; private set; }
            public bool Success { get; private set; }
            public string ResultDescription { get; private set; }
            public int TurnNumber { get; private set; }

            // 이동 액션 상세 정보 (Optional)
            public readonly Vector3? NewPosition;
            public readonly float? DistanceMoved;
            public readonly ArmoredFrame MoveTarget; // 이동 목표 대상
            public bool IsCounterAttack { get; private set; }
            public readonly AbilitySO UsedAbilityEffect;

            public ActionCompletedEvent(ArmoredFrame actor, ActionType action, bool success, string resultDescription, int turnNumber,
                                        Vector3? newPosition = null, float? distanceMoved = null, ArmoredFrame moveTarget = null,
                                        bool isCounterAttack = false,
                                        AbilitySO usedAbilityEffect = null)
            {
                Actor = actor;
                Action = action;
                Success = success;
                ResultDescription = resultDescription;
                TurnNumber = turnNumber;
                NewPosition = newPosition;
                DistanceMoved = distanceMoved;
                MoveTarget = moveTarget;
                IsCounterAttack = isCounterAttack;
                UsedAbilityEffect = usedAbilityEffect;
            }
        }

        /// <summary>
        /// 무기 발사 이벤트
        /// </summary>
        public class WeaponFiredEvent : ICombatEvent
        {
            public ArmoredFrame Attacker { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public Weapon Weapon { get; private set; }
            public bool Hit { get; private set; }
            public float AccuracyRoll { get; private set; }
            public bool IsCounterAttack { get; private set; }

            public WeaponFiredEvent(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon, bool hit, float accuracyRoll, bool isCounterAttack)
            {
                Attacker = attacker;
                Target = target;
                Weapon = weapon;
                Hit = hit;
                AccuracyRoll = accuracyRoll;
                IsCounterAttack = isCounterAttack;
            }
        }

        /// <summary>
        /// 수리 행동 시도 이벤트 (실제 적용 전)
        /// </summary>
        public class RepairAttemptEvent : ICombatEvent
        {
            public ArmoredFrame Actor { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public ActionType RepairType { get; private set; } // RepairSelf or RepairAlly
            public float PotentialRepairAmount { get; private set; }
            public int TurnNumber { get; private set; }

            public RepairAttemptEvent(ArmoredFrame actor, ArmoredFrame target, ActionType repairType, float potentialAmount, int turnNumber)
            {
                Actor = actor;
                Target = target;
                RepairType = repairType;
                PotentialRepairAmount = potentialAmount;
                TurnNumber = turnNumber;
            }
        }

        /// <summary>
        /// 수리 적용 완료 이벤트 (수정됨)
        /// </summary>
        public class RepairAppliedEvent : ICombatEvent // 구조체 대신 클래스 유지, ICombatEvent 상속 유지
        {
            public ArmoredFrame Actor { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public ActionType ActionType { get; private set; } // 추가: RepairAlly or RepairSelf 구분용
            public string TargetSlotIdentifier { get; private set; } // 변경: PartType -> string
            public float AmountRepaired { get; private set; } // 변경: ActualRepairAmount -> AmountRepaired (통일성)
            public int TurnNumber { get; private set; }
            // 제거: RepairedPart, PartCurrentDurability, PartMaxDurability

            // 생성자 수정
            public RepairAppliedEvent(ArmoredFrame actor, ArmoredFrame target, ActionType actionType, string targetSlotIdentifier, float amountRepaired, int turnNumber)
            {
                Actor = actor;
                Target = target;
                ActionType = actionType; // 추가된 필드 초기화
                TargetSlotIdentifier = targetSlotIdentifier; // 변경된 필드 초기화
                AmountRepaired = amountRepaired; // 변경된 필드 초기화
                TurnNumber = turnNumber;
            }
        }

        /// <summary>
        /// 카운터 공격 발생 알림 이벤트
        /// </summary>
        public class CounterAttackAnnouncedEvent : ICombatEvent
        {
            public ArmoredFrame Defender { get; private set; } // 카운터 공격자
            public ArmoredFrame Attacker { get; private set; } // 카운터 대상
            public int TurnNumber { get; private set; }

            public CounterAttackAnnouncedEvent(ArmoredFrame defender, ArmoredFrame attacker, int turnNumber)
            {
                Defender = defender;
                Attacker = attacker;
                TurnNumber = turnNumber;
            }
        }

        /// <summary>
        /// 데미지 적용 전 이벤트 (데미지 수정 가능)
        /// </summary>
        public class PreDamageApplicationEvent : ICombatEvent
        {
            public ArmoredFrame TargetAF { get; private set; }
            public Part DamagedPart { get; private set; }
            public float OriginalDamage { get; private set; }
            public int CurrentTurn { get; private set; }
            public float ModifiedDamage { get; set; } // 외부에서 수정 가능하도록 set 추가

            public PreDamageApplicationEvent(ArmoredFrame targetAF, Part damagedPart, float originalDamage, int currentTurn)
            {
                TargetAF = targetAF;
                DamagedPart = damagedPart;
                OriginalDamage = originalDamage;
                CurrentTurn = currentTurn;
                ModifiedDamage = originalDamage; // 초기값은 원본 데미지
            }
        }

        /// <summary>
        /// 데미지 적용 후 이벤트
        /// </summary>
        public class DamageAppliedEvent : ICombatEvent
        {
            public ArmoredFrame Source { get; private set; } // Attacker -> Source
            public ArmoredFrame Target { get; private set; } // TargetAF -> Target
            public Part DamagedPart { get; private set; }
            public float DamageDealt { get; private set; }
            public bool IsCritical { get; private set; } // WasCriticalHit -> IsCritical
            public bool WasDestroyed { get; private set; }
            public int TurnNumber { get; private set; }
            public float PartCurrentDurability { get; private set; } // 추가
            public float PartMaxDurability { get; private set; }   // 추가
            public bool IsCounterAttack { get; private set; }      // 추가

            public DamageAppliedEvent(
                ArmoredFrame source, 
                ArmoredFrame target, 
                Part damagedPart, 
                float damageDealt, 
                bool isCritical, 
                bool wasDestroyed, 
                int turnNumber,
                float partCurrentDurability, // 추가
                float partMaxDurability,   // 추가
                bool isCounterAttack)      // 추가
            {
                Source = source;
                Target = target;
                DamagedPart = damagedPart;
                DamageDealt = damageDealt;
                IsCritical = isCritical;
                WasDestroyed = wasDestroyed;
                TurnNumber = turnNumber;
                PartCurrentDurability = partCurrentDurability; // 추가
                PartMaxDurability = partMaxDurability;     // 추가
                IsCounterAttack = isCounterAttack;        // 추가
            }
        }
    }
} 