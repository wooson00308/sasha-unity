using System.Collections.Generic;
using System.Linq;
using AF.Models;

namespace AF.Combat
{
    public interface IBattleResultEvaluator
    {
        CombatSessionEvents.CombatEndEvent.ResultType Evaluate(
            IList<ArmoredFrame> participants,
            IDictionary<ArmoredFrame,int> teamAssignments);
    }

    public sealed class BattleResultEvaluator : IBattleResultEvaluator
    {
        public CombatSessionEvents.CombatEndEvent.ResultType Evaluate(
            IList<ArmoredFrame> participants,
            IDictionary<ArmoredFrame,int> teamAssignments)
        {
            if (participants == null || participants.Count == 0)
                return CombatSessionEvents.CombatEndEvent.ResultType.Aborted;

            var aliveTeams = participants
                .Where(p => p != null && p.IsOperational && teamAssignments.ContainsKey(p))
                .GroupBy(p => teamAssignments[p])
                .Select(g => g.Key)
                .ToList();

            switch (aliveTeams.Count)
            {
                case 0:  return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
                case 1:  return aliveTeams[0] == 0
                          ? CombatSessionEvents.CombatEndEvent.ResultType.Victory
                          : CombatSessionEvents.CombatEndEvent.ResultType.Defeat;
                default: return CombatSessionEvents.CombatEndEvent.ResultType.Draw;
            }
        }
    }
}
