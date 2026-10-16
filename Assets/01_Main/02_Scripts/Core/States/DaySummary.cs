using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 정산은 이미 끝난 상태입니다. 확인 버튼은 아래 값을 다시 적용하지 않습니다.
    public sealed class DaySummary
    {
        public long Id { get; }
        public int CompletedDay { get; }
        public int? NextDay { get; }
        public RUN_END_REASON EndReason { get; }
        public ExpeditionState Expedition { get; }
        public ResourceAmount RecoveredLoot { get; }
        public ResourceAmount LostLoot { get; }
        public ResourceAmount ResourcesBefore { get; }
        public ResourceAmount ResourcesAfter { get; }
        public int FoodConsumed { get; }
        public bool FoodShortage { get; }
        public int StarvationDamage { get; }
        public IReadOnlyList<SurvivorState> SurvivorsBefore { get; }
        public IReadOnlyList<SurvivorState> SurvivorsAfter { get; }
        public IReadOnlyList<string> DiedSurvivorIds { get; }

        internal DaySummary(long id, int completedDay, int? nextDay, RUN_END_REASON endReason,
            ExpeditionState expedition, ResourceAmount recoveredLoot, ResourceAmount lostLoot,
            ResourceAmount resourcesBefore, ResourceAmount resourcesAfter,
            SurvivorState[] survivorsBefore, SurvivorState[] survivorsAfter,
            int foodConsumed, bool foodShortage, int starvationDamage)
        {
            Id = id;
            CompletedDay = completedDay;
            NextDay = nextDay;
            EndReason = endReason;
            Expedition = expedition;
            RecoveredLoot = recoveredLoot;
            LostLoot = lostLoot;
            ResourcesBefore = resourcesBefore;
            ResourcesAfter = resourcesAfter;
            SurvivorsBefore = Array.AsReadOnly((SurvivorState[])survivorsBefore.Clone());
            SurvivorsAfter = Array.AsReadOnly((SurvivorState[])survivorsAfter.Clone());
            FoodConsumed = foodConsumed;
            FoodShortage = foodShortage;
            StarvationDamage = starvationDamage;

            var diedIds = new List<string>();
            for ( int i = 0; i < survivorsAfter.Length; i++ )
            {
                if ( !survivorsAfter[i].IsAlive &&
                     (survivorsBefore[i].IsAlive || survivorsAfter[i].SurvivorId == expedition.SurvivorId) )
                {
                    diedIds.Add(survivorsAfter[i].SurvivorId);
                }
            }

            DiedSurvivorIds = diedIds.AsReadOnly();
        }
    }
}
