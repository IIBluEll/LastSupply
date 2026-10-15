using System;

namespace LastSupply.Core
{
    // 단계 진입 시 사용할 사건 범주 확률. 실제 추첨은 Session에서 수행합니다.
    public sealed class StageEventChanceDefinition
    {
        public int Stage { get; }
        public int NoEventChance { get; }
        public int OpportunityChance { get; }
        public int DangerChance { get; }

        public StageEventChanceDefinition(int stage , int noEventChance , int opportunityChance , int dangerChance)
        {
            if ( stage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            if ( noEventChance < 0 || noEventChance > 100 )
            {
                throw new ArgumentOutOfRangeException(nameof(noEventChance));
            }

            if ( opportunityChance < 0 || opportunityChance > 100 )
            {
                throw new ArgumentOutOfRangeException(nameof(opportunityChance));
            }

            if ( dangerChance < 0 || dangerChance > 100 )
            {
                throw new ArgumentOutOfRangeException(nameof(dangerChance));
            }

            if ( noEventChance + opportunityChance + dangerChance != 100 )
            {
                throw new ArgumentException("사건 범주 확률의 합은 100이어야 합니다.");
            }

            Stage = stage;
            NoEventChance = noEventChance;
            OpportunityChance = opportunityChance;
            DangerChance = dangerChance;
        }
    }
}
