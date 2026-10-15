using System;

namespace LastSupply.Core
{
    // 소음 구간별 귀환 사건 발생 확률과 실패 피해
    public sealed class ReturnRiskDefinition
    {
        public int MinNoise { get; }
        public int MaxNoise { get; }
        public string Name { get; }
        public int EventChance { get; }
        public int FailureDamage { get; }

        public ReturnRiskDefinition(int minNoise , int maxNoise , string name , int eventChance , int failureDamage)
        {
            if ( minNoise < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(minNoise));
            }

            if ( maxNoise < minNoise )
            {
                throw new ArgumentOutOfRangeException(nameof(maxNoise));
            }

            if ( string.IsNullOrWhiteSpace(name) )
            {
                throw new ArgumentException("귀환 위험 표시 이름이 필요합니다." , nameof(name));
            }

            if ( eventChance < 0 || eventChance > 100 )
            {
                throw new ArgumentOutOfRangeException(nameof(eventChance));
            }

            if ( failureDamage < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(failureDamage));
            }

            MinNoise = minNoise;
            MaxNoise = maxNoise;
            Name = name;
            EventChance = eventChance;
            FailureDamage = failureDamage;
        }
    }
}
