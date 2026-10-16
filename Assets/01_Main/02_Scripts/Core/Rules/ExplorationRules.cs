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

        // 0~99 표본으로 사건 범주를 결정합니다. null이면 사건 없음입니다.
        public static EVENT_CATEGORY? CalculateEventCategory(StageEventChanceDefinition chance , int sample)
        {
            if ( chance == null )
            {
                throw new ArgumentNullException(nameof(chance));
            }

            if ( sample < 0 || sample >= 100 )
            {
                throw new ArgumentOutOfRangeException(nameof(sample) , "확률 표본은 0~99여야 합니다.");
            }

            if ( sample < chance.NoEventChance )
            {
                return null;
            }

            if ( sample < chance.NoEventChance + chance.OpportunityChance )
            {
                return EVENT_CATEGORY.OPPORTUNITY;
            }

            return EVENT_CATEGORY.DANGER;
        }

        // 지역·단계·범주에 맞는 후보를 구하고 직전 단계의 중복 사건을 제외
        public static IReadOnlyList<EventDefinition> GetEventCandidates(
            RegionDefinition region , IReadOnlyList<EventDefinition> events ,
            int stage , EVENT_CATEGORY category , string previousStageEventId = null)
        {
            if ( region == null )
            {
                throw new ArgumentNullException(nameof(region));
            }

            if ( events == null )
            {
                throw new ArgumentNullException(nameof(events));
            }

            if ( stage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(stage));
            }

            if ( !Enum.IsDefined(typeof(EVENT_CATEGORY) , category) )
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            var regionEventIds = new HashSet<string>(region.EventIds, StringComparer.Ordinal);
            var candidates = new List<EventDefinition>();

            for ( int i = 0; i < events.Count; i++ )
            {
                EventDefinition definition = events[i];

                if ( definition == null )
                {
                    throw new ArgumentException("사건 목록에 null이 있습니다." , nameof(events));
                }

                if ( !regionEventIds.Contains(definition.Id) )
                {
                    continue;
                }

                if ( definition.Category != category )
                {
                    continue;
                }

                if ( stage < definition.MinStage || stage > definition.MaxStage )
                {
                    continue;
                }

                candidates.Add(definition);
            }

            // 다른 후보가 있을 때만 직전 단계와 같은 사건을 제외
            if ( candidates.Count > 1 && !string.IsNullOrEmpty(previousStageEventId) )
            {
                candidates.RemoveAll(definition => definition.Id == previousStageEventId);
            }

            return candidates.AsReadOnly();
        }

        // 현재 소음에 해당하는 귀환 위험 설정을 반환
        public static ReturnRiskDefinition GetReturnRisk(GameRulesDefinition rules , int noise)
        {
            if ( rules == null )
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if ( noise < 0 || noise > rules.MaxNoise )
            {
                throw new ArgumentOutOfRangeException(nameof(noise));
            }

            for ( int i = 0; i < rules.ReturnRisks.Count; i++ )
            {
                ReturnRiskDefinition definition = rules.ReturnRisks[i];

                if ( noise >= definition.MinNoise && noise <= definition.MaxNoise )
                {
                    return definition;
                }
            }

            throw new InvalidOperationException($"소음에 해당하는 귀환 위험 구간이 없습니다. 소음: {noise}");
        }

        // 사건 효과를 반영했을 때의 다음 값을 계산.
        public static EventEffectResult CalculateEventEffect(
            EventEffectDefinition effect , RESOURCE_TYPE regionResource ,
            int currentHp , int maxHp , int currentNoise , int maxNoise , ResourceAmount currentLoot)
        {
            if ( effect == null )
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if ( !Enum.IsDefined(typeof(RESOURCE_TYPE) , regionResource) )
            {
                throw new ArgumentOutOfRangeException(nameof(regionResource));
            }

            if ( maxHp < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            }

            // 사망한 생존자에게 사건 효과를 적용하여 부활시키지 않음
            if ( currentHp < 1 || currentHp > maxHp )
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            if ( maxNoise < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxNoise));
            }

            if ( currentNoise < 0 || currentNoise > maxNoise )
            {
                throw new ArgumentOutOfRangeException(nameof(currentNoise));
            }

            // 합산 중 int 범위를 넘지 않도록 long으로 계산한 뒤 제한
            int nextHp = (int)Math.Max(0L, Math.Min((long)maxHp, (long)currentHp + effect.HpChange));
            int nextNoise = (int)Math.Min((long)maxNoise, (long)currentNoise + effect.NoiseIncrease);

            ResourceAmount nextLoot = default;

            // 사망하면 기존 전리품과 이번 사건 보상을 모두 잃음
            if ( nextHp > 0 )
            {
                nextLoot = currentLoot;

                for ( int i = 0; i < effect.Rewards.Count; i++ )
                {
                    SearchRewardItemDefinition reward = effect.Rewards[i];
                    RESOURCE_TYPE resourceType = reward.ResourceType ?? regionResource;

                    nextLoot = nextLoot.Add(resourceType , reward.Amount);
                }
            }

            return new EventEffectResult(nextHp , nextNoise , nextLoot);
        }

        public static bool IsActionSuccessful(int dice , int statModifier , int difficulty)
        {
            ValidateDice(dice);

            return (long)dice + statModifier >= difficulty;
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
