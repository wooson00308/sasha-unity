using UnityEngine;
using AF.Models;
using System.Collections.Generic; // Needed for Dictionary

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
            public Dictionary<string, ArmoredFrameSnapshot> FinalParticipantSnapshots { get; private set; }

            public CombatEndEvent(ArmoredFrame[] survivors, ResultType result, string battleId, float duration, Dictionary<string, ArmoredFrameSnapshot> finalSnapshots)
            {
                Survivors = survivors;
                Result = result;
                BattleId = battleId;
                Duration = duration;
                FinalParticipantSnapshots = finalSnapshots ?? new Dictionary<string, ArmoredFrameSnapshot>();
            }
        }

        /// <summary>
        /// 전투 턴 시작 이벤트 -> 유닛 활성화 시작 이벤트로 변경
        /// </summary>
        public class UnitActivationStartEvent : ICombatEvent
        {
            public int CurrentTurn { get; private set; }
            public int CurrentCycle { get; private set; }
            public ArmoredFrame ActiveUnit { get; private set; }
            public string BattleId { get; private set; }
            public float APBeforeRecovery { get; private set; }

            public UnitActivationStartEvent(int currentTurn, int currentCycle, ArmoredFrame activeUnit, string battleId, float apBeforeRecovery)
            {
                CurrentTurn = currentTurn;
                CurrentCycle = currentCycle;
                ActiveUnit = activeUnit;
                BattleId = battleId;
                APBeforeRecovery = apBeforeRecovery;
            }
        }

        /// <summary>
        /// 전투 턴 종료 이벤트 -> 유닛 활성화 종료 이벤트로 변경
        /// </summary>
        public class UnitActivationEndEvent : ICombatEvent
        {
            public int CurrentTurn { get; private set; }
            public int CurrentCycle { get; private set; }
            public ArmoredFrame ActiveUnit { get; private set; }
            public string BattleId { get; private set; }

            public UnitActivationEndEvent(int currentTurn, int currentCycle, ArmoredFrame activeUnit, string battleId)
            {
                CurrentTurn = currentTurn;
                CurrentCycle = currentCycle;
                ActiveUnit = activeUnit;
                BattleId = battleId;
            }
        }

        /// <summary>
        /// 전투 라운드(사이클) 시작 이벤트
        /// </summary>
        public class RoundStartEvent : ICombatEvent
        {
            public int RoundNumber { get; private set; }
            public string BattleId { get; private set; }
            public List<ArmoredFrame> InitiativeSequence { get; private set; }

            public RoundStartEvent(int roundNumber, string battleId, List<ArmoredFrame> initiativeSequence = null)
            {
                RoundNumber = roundNumber;
                BattleId = battleId;
                InitiativeSequence = initiativeSequence ?? new List<ArmoredFrame>();
            }
        }

        /// <summary>
        /// 전투 라운드(사이클) 종료 이벤트
        /// </summary>
        public class RoundEndEvent : ICombatEvent
        {
            public int RoundNumber { get; private set; }
            public string BattleId { get; private set; }

            public RoundEndEvent(int roundNumber, string battleId)
            {
                RoundNumber = roundNumber;
                BattleId = battleId;
            }
        }
    }
} 