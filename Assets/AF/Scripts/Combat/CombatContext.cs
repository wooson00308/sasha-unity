using System.Collections.Generic;
using AF.EventBus;
using AF.Services;
using AF.Models;
using System.Linq;

namespace AF.Combat
{
    /// <summary>
    /// 전투 공용 데이터 묶음. 모든 하위 서비스가 읽는다.
    /// </summary>
    public readonly struct CombatContext
    {
        public CombatContext(
            EventBus.EventBus bus,
            TextLoggerService logger,
            string battleId,
            int currentTurn,
            int currentCycle,
            HashSet<ArmoredFrame> defended,
            IList<ArmoredFrame> participants,
            IDictionary<ArmoredFrame, int> teamAssignments,
            HashSet<ArmoredFrame> movedThisActivation)
        {
            Bus              = bus;
            Logger           = logger;
            BattleId         = battleId;
            CurrentTurn      = currentTurn;
            CurrentCycle     = currentCycle;
            DefendedThisTurn = defended;
            Participants     = participants;
            TeamAssignments  = teamAssignments;
            MovedThisActivation = movedThisActivation;
        }

        public EventBus.EventBus          Bus               { get; }
        public TextLoggerService          Logger            { get; }
        public string                     BattleId          { get; }
        public int                        CurrentTurn       { get; }
        public int                        CurrentCycle      { get; }
        public HashSet<ArmoredFrame>      DefendedThisTurn  { get; }
        public HashSet<ArmoredFrame>      MovedThisActivation { get; }
        public IList<ArmoredFrame>        Participants      { get; }
        public IDictionary<ArmoredFrame,int> TeamAssignments { get; }

        public List<ArmoredFrame> GetEnemies(ArmoredFrame self)
        {
            if (self == null || !TeamAssignments.TryGetValue(self, out int selfTeam))
            {
                return new List<ArmoredFrame>(); // 자신 또는 팀 정보를 찾을 수 없으면 빈 리스트 반환
            }

            var enemies = new List<ArmoredFrame>();
            var localParticipants = Participants; 
            var localTeamAssignments = TeamAssignments;
            if (localParticipants != null)
            {
                foreach (var participant in localParticipants)
                {
                    if (participant != null && participant != self && participant.IsOperational &&
                        localTeamAssignments.TryGetValue(participant, out int participantTeam) &&
                        participantTeam != selfTeam)
                    {
                        enemies.Add(participant);
                    }
                }
            }
            return enemies;
        }

        public IEnumerable<ArmoredFrame> GetAllies(ArmoredFrame actor)
        {
            if (actor == null || !TeamAssignments.TryGetValue(actor, out int actorTeam))
            {
                return Enumerable.Empty<ArmoredFrame>(); // 자신 또는 팀 정보 없으면 빈 목록 반환
            }

            var allies = new List<ArmoredFrame>();
            var localParticipants = Participants;
            var localTeamAssignments = TeamAssignments;
            if (localParticipants != null)
            {
                foreach (var participant in localParticipants)
                {
                    if (participant != null && participant != actor && participant.IsOperational &&
                        localTeamAssignments.TryGetValue(participant, out int participantTeam) &&
                        participantTeam == actorTeam)
                    {
                        allies.Add(participant);
                    }
                }
            }
            return allies;
        }
    }
}
