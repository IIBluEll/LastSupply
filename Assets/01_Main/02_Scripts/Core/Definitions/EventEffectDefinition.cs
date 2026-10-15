using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 이벤트로 적용할 자원 보상, HP 변화, 소음 증가
    public sealed class EventEffectDefinition
    {
        public int HpChange { get; }
        public int NoiseIncrease { get; }

        public IReadOnlyList<SearchRewardItemDefinition> Rewards { get; }

        public EventEffectDefinition(int hpChange, int noiseIncrease, params SearchRewardItemDefinition[] rewards)
        {
            if ( noiseIncrease < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(noiseIncrease));
            }

            if ( rewards == null )
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            var copiedRewards = (SearchRewardItemDefinition[])rewards.Clone();

            for ( int i = 0; i < copiedRewards.Length; i++ )
            {
                if ( copiedRewards[ i ] == null )
                {
                    throw new ArgumentException("사건 보상 항목에 null이 있습니다." , nameof(rewards));
                }
            }

            HpChange = hpChange;
            NoiseIncrease = noiseIncrease;
            Rewards = Array.AsReadOnly(copiedRewards);
        }
    }
}