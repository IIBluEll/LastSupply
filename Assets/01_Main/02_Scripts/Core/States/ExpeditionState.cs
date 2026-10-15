using System;

namespace LastSupply.Core
{
    //한 번의 탐색에서 유지할 기본 상태
    public sealed class ExpeditionState
    {
        public string SurvivorId { get; }
        public string RegionId { get; }

        public int Stage { get; }
        public int Noise { get; }

        public ResourceAmount Loot { get; }

        public ExpeditionState(string survivorId , string regionId , int stage , int noise , ResourceAmount loot)
        {
            if ( string.IsNullOrWhiteSpace(survivorId) )
            {
                throw new ArgumentException("생존자 ID가 필요합니다." , nameof(survivorId));
            }

            if ( string.IsNullOrWhiteSpace(regionId) )
            {
                throw new ArgumentException("지역 ID가 필요합니다." , nameof(regionId));
            }

            if ( stage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            if ( noise < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(noise));
            }

            SurvivorId = survivorId;
            RegionId = regionId;
            Stage = stage;
            Noise = noise;
            Loot = loot;
        }
    }
}