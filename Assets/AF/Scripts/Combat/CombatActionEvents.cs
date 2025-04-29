using UnityEngine;
using AF.Models;

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

            public ActionCompletedEvent(ArmoredFrame actor, ActionType action, bool success, string resultDescription, int turnNumber,
                                        Vector3? newPosition = null, float? distanceMoved = null, ArmoredFrame moveTarget = null)
            {
                Actor = actor;
                Action = action;
                Success = success;
                ResultDescription = resultDescription;
                TurnNumber = turnNumber;
                NewPosition = newPosition;
                DistanceMoved = distanceMoved;
                MoveTarget = moveTarget;
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
    }
} 