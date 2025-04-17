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
} 