using System.Collections.Generic;
using AF.EventBus;
using AF.Services;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 전투 공용 데이터 묶음. 모든 하위 서비스가 읽는다.
    /// </summary>
    public readonly struct CombatContext
    {
        public CombatContext(
            EventBus.EventBus bus,
            ICombatSimulatorService simulator,
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
            Simulator        = simulator;
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
        public ICombatSimulatorService    Simulator         { get; }
        public TextLoggerService          Logger            { get; }
        public string                     BattleId          { get; }
        public int                        CurrentTurn       { get; }
        public int                        CurrentCycle      { get; }
        public HashSet<ArmoredFrame>      DefendedThisTurn  { get; }
        public HashSet<ArmoredFrame>      MovedThisActivation { get; }
        public IList<ArmoredFrame>        Participants      { get; }
        public IDictionary<ArmoredFrame,int> TeamAssignments { get; }
    }
}
