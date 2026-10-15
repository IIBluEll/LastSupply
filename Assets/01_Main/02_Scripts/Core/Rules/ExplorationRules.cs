using System.Collections.Generic;
using System;

namespace LastSupply.Core
{
    // 주어진 주사위 눈과 설정으로 결과를 계산
    public static class ExplorationRules
    {
        public static ResourceAmount CalculateSearchReward(int stage , int dice , RESOURCE_TYPE regionResource ,
            IReadOnlyList<SearchRewardDefinition> defaultRewards , IReadOnlyList<SearchRewardDefinition> regionRewards = null)
        {
            ValidateDice(dice);

            if ( stage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            if ( !Enum.IsDefined(typeof(RESOURCE_TYPE) , regionResource) )
            {
                throw new ArgumentOutOfRangeException(nameof(regionResource));
            }

            IReadOnlyList<SearchRewardDefinition> definitions = regionRewards ?? defaultRewards;

            if ( definitions == null )
            {
                throw new ArgumentNullException(nameof(defaultRewards));
            }

            SearchRewardDefinition matchedDefinition = null;

            for ( int i = 0; i < definitions.Count; i++ )
            {
                SearchRewardDefinition definition = definitions[i];

                if ( definition == null )
                {
                    throw new ArgumentException("수색 보상표에 null 행이 있습니다." , nameof(definitions));
                }

                if ( !definition.Contains(stage , dice) )
                {
                    continue;
                }

                if ( matchedDefinition != null )
                {
                    throw new InvalidOperationException($"수색 보상 구간이 중복됩니다. 단계: {stage}, 주사위: {dice}");
                }

                matchedDefinition = definition;
            }

            if ( matchedDefinition == null )
            {
                throw new InvalidOperationException($"수색 보상 구간이 없습니다. 단계: {stage}, 주사위: {dice}");
            }

            ResourceAmount reward = default;

            for ( int i = 0; i < matchedDefinition.Rewards.Count; i++ )
            {
                SearchRewardItemDefinition item = matchedDefinition.Rewards[i];
                RESOURCE_TYPE resourceType = item.ResourceType ?? regionResource;

                reward = reward.Add(resourceType , item.Amount);
            }

            return reward;
        }

        public static bool IsActionSuccessful(int dice , int statModifier , int difficulty)
        {
            ValidateDice(dice);

            return dice + statModifier >= difficulty;
        }

        private static void ValidateDice(int dice)
        {
            if ( dice < 1 || dice > 20 )
            {
                throw new ArgumentOutOfRangeException(nameof(dice) , "D20의 눈은 1~20이어야 함");
            }
        }
    }
}