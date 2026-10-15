using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 수색 보상표의 한 행
    // 어느 단계에서 어떤 주사위 눈이 나오면 얼마를 받는지 정의
    public sealed class SearchRewardDefinition
    {
        public int MinStage { get; }
        public int MaxStage { get; }

        public int MinDice { get; }
        public int MaxDice { get; }

        public IReadOnlyList<SearchRewardItemDefinition> Rewards { get; }

        public SearchRewardDefinition(int minStage , int maxStage , int minDice , int maxDice , params SearchRewardItemDefinition[] rewards)
        {
            if ( minStage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(minStage));
            }

            if ( maxStage < minStage )
            {
                throw new ArgumentOutOfRangeException(nameof(maxStage));
            }

            if ( minDice < 1 || minDice > 20 )
            {
                throw new ArgumentOutOfRangeException(nameof(minDice));
            }

            if ( maxDice < minDice || maxDice > 20 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxDice));
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
                    throw new ArgumentException("수색 보상 항목에 null이 있습니다." , nameof(rewards));
                }
            }

            MinStage = minStage;
            MaxStage = maxStage;
            MinDice = minDice;
            MaxDice = maxDice;
            Rewards = Array.AsReadOnly(copiedRewards);
        }

        public bool Contains(int stage , int dice)
        {
            return stage >= MinStage && stage <= MaxStage && dice >= MinDice && dice <= MaxDice;
        }
    }
}