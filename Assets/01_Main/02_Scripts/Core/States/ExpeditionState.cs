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

        // 현재 단계에서 발생한 사건 해결한 뒤에도 유지
        public string StageEventId { get; }

        // 바로 이전 단계에서 발생한 사건
        public string PreviousStageEventId { get; }

        // 귀환을 시작할 때 확정한 위험 정보
        public ReturnRiskDefinition ReturnRisk { get; }

        public ExpeditionState(string survivorId , string regionId , int stage , int noise , ResourceAmount loot , string stageEventId = null , string previousStageEventId = null , ReturnRiskDefinition returnRisk = null)
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

            if ( stageEventId != null && string.IsNullOrWhiteSpace(stageEventId) )
            {
                throw new ArgumentException("사건 없음은 null로 지정합니다." , nameof(stageEventId));
            }

            if ( previousStageEventId != null && string.IsNullOrWhiteSpace(previousStageEventId) )
            {
                throw new ArgumentException("직전 단계의 사건 없음은 null로 지정합니다." , nameof(previousStageEventId));
            }

            if ( returnRisk != null && ( noise < returnRisk.MinNoise || noise > returnRisk.MaxNoise ) )
            {
                throw new ArgumentException("귀환 위험 정보가 현재 소음과 일치하지 않습니다." , nameof(returnRisk));
            }

            SurvivorId = survivorId;
            RegionId = regionId;
            Stage = stage;
            Noise = noise;
            Loot = loot;
            StageEventId = stageEventId;
            PreviousStageEventId = previousStageEventId;
            ReturnRisk = returnRisk;
        }
    }
}