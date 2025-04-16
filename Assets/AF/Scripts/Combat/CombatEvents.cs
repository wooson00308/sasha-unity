using UnityEngine;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 전투 시작, 종료와 관련된 이벤트
    /// </summary>
    public class CombatSessionEvents
    {
        /// <summary>
        /// 전투 시작 이벤트
        /// </summary>
        public class CombatStartEvent : ICombatEvent
        {
            public ArmoredFrame[] Participants { get; private set; }
            public string BattleId { get; private set; }
            public string BattleName { get; private set; }
            public Vector3 BattleLocation { get; private set; }

            public CombatStartEvent(ArmoredFrame[] participants, string battleId, string battleName, Vector3 battleLocation)
            {
                Participants = participants;
                BattleId = battleId;
                BattleName = battleName;
                BattleLocation = battleLocation;
            }
        }

        /// <summary>
        /// 전투 종료 이벤트
        /// </summary>
        public class CombatEndEvent : ICombatEvent
        {
            public enum ResultType { Victory, Defeat, Draw, Aborted }

            public ArmoredFrame[] Survivors { get; private set; }
            public ResultType Result { get; private set; }
            public string BattleId { get; private set; }
            public float Duration { get; private set; }

            public CombatEndEvent(ArmoredFrame[] survivors, ResultType result, string battleId, float duration)
            {
                Survivors = survivors;
                Result = result;
                BattleId = battleId;
                Duration = duration;
            }
        }

        /// <summary>
        /// 전투 턴 시작 이벤트
        /// </summary>
        public class TurnStartEvent : ICombatEvent
        {
            public int TurnNumber { get; private set; }
            public ArmoredFrame ActiveUnit { get; private set; }
            public string BattleId { get; private set; }

            public TurnStartEvent(int turnNumber, ArmoredFrame activeUnit, string battleId)
            {
                TurnNumber = turnNumber;
                ActiveUnit = activeUnit;
                BattleId = battleId;
            }
        }

        /// <summary>
        /// 전투 턴 종료 이벤트
        /// </summary>
        public class TurnEndEvent : ICombatEvent
        {
            public int TurnNumber { get; private set; }
            public ArmoredFrame ActiveUnit { get; private set; }
            public string BattleId { get; private set; }

            public TurnEndEvent(int turnNumber, ArmoredFrame activeUnit, string battleId)
            {
                TurnNumber = turnNumber;
                ActiveUnit = activeUnit;
                BattleId = battleId;
            }
        }
    }

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

            public ActionCompletedEvent(ArmoredFrame actor, ActionType action, bool success, string resultDescription, int turnNumber)
            {
                Actor = actor;
                Action = action;
                Success = success;
                ResultDescription = resultDescription;
                TurnNumber = turnNumber;
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

            public WeaponFiredEvent(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon, bool hit, float accuracyRoll)
            {
                Attacker = attacker;
                Target = target;
                Weapon = weapon;
                Hit = hit;
                AccuracyRoll = accuracyRoll;
            }
        }
    }

    /// <summary>
    /// 데미지 처리와 관련된 이벤트
    /// </summary>
    public class DamageEvents
    {
        /// <summary>
        /// 데미지 발생 이벤트 - 데미지가 적용되기 전에 발생
        /// </summary>
        public class DamageCalculatedEvent : ICombatEvent
        {
            public ArmoredFrame Source { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public Weapon Weapon { get; private set; }
            public float RawDamage { get; private set; }
            public float CalculatedDamage { get; private set; }
            public DamageType DamageType { get; private set; }
            public PartType TargetPart { get; private set; }

            public DamageCalculatedEvent(ArmoredFrame source, ArmoredFrame target, Weapon weapon, 
                                         float rawDamage, float calculatedDamage, 
                                         DamageType damageType, PartType targetPart)
            {
                Source = source;
                Target = target;
                Weapon = weapon;
                RawDamage = rawDamage;
                CalculatedDamage = calculatedDamage;
                DamageType = damageType;
                TargetPart = targetPart;
            }
        }

        /// <summary>
        /// 데미지 적용 결과 이벤트 - 데미지가 적용된 후 발생
        /// </summary>
        public class DamageAppliedEvent : ICombatEvent
        {
            public ArmoredFrame Source { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public float DamageDealt { get; private set; }
            public PartType DamagedPart { get; private set; }
            public bool IsCritical { get; private set; }
            public float PartCurrentDurability { get; private set; }
            public float PartMaxDurability { get; private set; }

            public DamageAppliedEvent(ArmoredFrame source, ArmoredFrame target, 
                                    float damageDealt, PartType damagedPart, bool isCritical,
                                    float partCurrentDurability, float partMaxDurability)
            {
                Source = source;
                Target = target;
                DamageDealt = damageDealt;
                DamagedPart = damagedPart;
                IsCritical = isCritical;
                PartCurrentDurability = partCurrentDurability;
                PartMaxDurability = partMaxDurability;
            }
        }

        /// <summary>
        /// 데미지 회피 이벤트
        /// </summary>
        public class DamageAvoidedEvent : ICombatEvent
        {
            public enum AvoidanceType { Dodge, Deflect, Intercept, Shield }

            public ArmoredFrame Source { get; private set; }
            public ArmoredFrame Target { get; private set; }
            public float DamageAvoided { get; private set; }
            public AvoidanceType Type { get; private set; }
            public string Description { get; private set; }

            public DamageAvoidedEvent(ArmoredFrame source, ArmoredFrame target,
                                     float damageAvoided, AvoidanceType type, string description)
            {
                Source = source;
                Target = target;
                DamageAvoided = damageAvoided;
                Type = type;
                Description = description;
            }
        }
    }

    /// <summary>
    /// 파츠 상태 변화와 관련된 이벤트
    /// </summary>
    public class PartEvents
    {
        /// <summary>
        /// 파츠 파괴 이벤트
        /// </summary>
        public class PartDestroyedEvent : ICombatEvent
        {
            public ArmoredFrame Frame { get; private set; }
            public PartType DestroyedPartType { get; private set; }
            public ArmoredFrame Destroyer { get; private set; }
            public string[] Effects { get; private set; }

            public PartDestroyedEvent(ArmoredFrame frame, PartType destroyedPartType, 
                                     ArmoredFrame destroyer, params string[] effects)
            {
                Frame = frame;
                DestroyedPartType = destroyedPartType;
                Destroyer = destroyer;
                Effects = effects;
            }
        }

        /// <summary>
        /// 파츠 상태 변화 이벤트
        /// </summary>
        public class PartStatusChangedEvent : ICombatEvent
        {
            public enum StatusChangeType { Damaged, Overheated, Malfunctioning, Disabled, Repaired }

            public ArmoredFrame Frame { get; private set; }
            public PartType PartType { get; private set; }
            public StatusChangeType ChangeType { get; private set; }
            public float Severity { get; private set; } // 0.0f ~ 1.0f
            public string Description { get; private set; }

            public PartStatusChangedEvent(ArmoredFrame frame, PartType partType, 
                                         StatusChangeType changeType, float severity,
                                         string description)
            {
                Frame = frame;
                PartType = partType;
                ChangeType = changeType;
                Severity = severity;
                Description = description;
            }
        }

        /// <summary>
        /// 시스템 치명적 오류 이벤트
        /// </summary>
        public class SystemCriticalFailureEvent : ICombatEvent
        {
            public ArmoredFrame Frame { get; private set; }
            public string SystemName { get; private set; }
            public string FailureDescription { get; private set; }
            public int TurnDuration { get; private set; }
            public bool IsPermanent { get; private set; }

            public SystemCriticalFailureEvent(ArmoredFrame frame, string systemName, 
                                             string failureDescription, int turnDuration, 
                                             bool isPermanent)
            {
                Frame = frame;
                SystemName = systemName;
                FailureDescription = failureDescription;
                TurnDuration = turnDuration;
                IsPermanent = isPermanent;
            }
        }
    }
} 