using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 외부에서 읽을 현재 상태의 사본
    public sealed class GameSnapshot
    {
        public int Day { get; }
        public GAME_PHASE Phase { get; }
        public long Revision { get; }
        public ResourceAmount Resources { get; }
        public IReadOnlyList<SurvivorState> Survivors { get; }
        public ExpeditionState Expedition { get; }
        public ActionReport PendingReport { get; }
        public DaySummary DaySummary { get; }
        public RUN_END_REASON EndReason { get; }

        internal GameSnapshot(int day , GAME_PHASE phase , long revision , ResourceAmount resources ,
            SurvivorState[] survivors , ExpeditionState expedition , ActionReport pendingReport,
            DaySummary daySummary = null, RUN_END_REASON endReason = RUN_END_REASON.NONE)
        {
            Day = day;
            Phase = phase;
            Revision = revision;
            Resources = resources;
            Survivors = Array.AsReadOnly((SurvivorState[])survivors.Clone());
            Expedition = expedition;
            PendingReport = pendingReport;
            DaySummary = daySummary;
            EndReason = endReason;
        }
    }
}
