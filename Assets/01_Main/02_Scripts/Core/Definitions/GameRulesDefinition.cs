using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 현재 게임에서 사용할 공통 규칙 설정
    public sealed class GameRulesDefinition
    {
        public int MaxStage { get; }
        public int MaxNoise { get; }
        public int MaxDays { get; }
        public ResourceAmount InitialResources { get; }
        public int TargetParts { get; }
        public int DailyFoodConsumption { get; }
        public int StarvationDamage { get; }
        public int HealingMedicineCost { get; }
        public int HealingAmount { get; }

        public IReadOnlyList<SearchRewardDefinition> DefaultSearchRewards { get; }
        public IReadOnlyList<StageEventChanceDefinition> StageEventChances { get; }
        public IReadOnlyList<ReturnRiskDefinition> ReturnRisks { get; }
        public ReturnEventDefinition ReturnEvent { get; }

        public GameRulesDefinition(int maxStage , int maxNoise , int maxDays , ResourceAmount initialResources ,
            int targetParts , int dailyFoodConsumption , int starvationDamage , int healingMedicineCost , int healingAmount ,
            SearchRewardDefinition[] defaultSearchRewards , StageEventChanceDefinition[] stageEventChances ,
            ReturnRiskDefinition[] returnRisks , ReturnEventDefinition returnEvent)
        {
            if ( maxStage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxStage));
            }

            if ( maxNoise < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxNoise));
            }

            if ( maxDays < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxDays));
            }

            if ( targetParts < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(targetParts));
            }

            if ( dailyFoodConsumption < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(dailyFoodConsumption));
            }

            if ( starvationDamage < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(starvationDamage));
            }

            if ( healingMedicineCost < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(healingMedicineCost));
            }

            if ( healingAmount < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(healingAmount));
            }

            if ( defaultSearchRewards == null )
            {
                throw new ArgumentNullException(nameof(defaultSearchRewards));
            }

            if ( stageEventChances == null )
            {
                throw new ArgumentNullException(nameof(stageEventChances));
            }

            if ( returnRisks == null )
            {
                throw new ArgumentNullException(nameof(returnRisks));
            }

            if ( returnEvent == null )
            {
                throw new ArgumentNullException(nameof(returnEvent));
            }

            var copiedRewards = (SearchRewardDefinition[])defaultSearchRewards.Clone();
            var copiedChances = (StageEventChanceDefinition[])stageEventChances.Clone();
            var copiedRisks = (ReturnRiskDefinition[])returnRisks.Clone();

            ValidateSearchRewards(copiedRewards , maxStage , nameof(defaultSearchRewards));
            ValidateStageEventChances(copiedChances , maxStage);
            ValidateReturnRisks(copiedRisks , maxNoise);

            MaxStage = maxStage;
            MaxNoise = maxNoise;
            MaxDays = maxDays;
            InitialResources = initialResources;
            TargetParts = targetParts;
            DailyFoodConsumption = dailyFoodConsumption;
            StarvationDamage = starvationDamage;
            HealingMedicineCost = healingMedicineCost;
            HealingAmount = healingAmount;
            DefaultSearchRewards = Array.AsReadOnly(copiedRewards);
            StageEventChances = Array.AsReadOnly(copiedChances);
            ReturnRisks = Array.AsReadOnly(copiedRisks);
            ReturnEvent = returnEvent;
        }

        // 공통표와 지역표 모두 전체 단계의 D20 구간을 누락·중복 없이 정의해야 합니다.
        internal static void ValidateSearchRewards(IReadOnlyList<SearchRewardDefinition> rewards , int maxStage , string owner)
        {
            for ( int i = 0; i < rewards.Count; i++ )
            {
                if ( rewards[ i ] == null || rewards[ i ].MaxStage > maxStage )
                {
                    throw new ArgumentException($"{owner}: 수색 보상 행이 null이거나 최대 단계를 초과합니다.");
                }
            }

            for ( int stage = 1; stage <= maxStage; stage++ )
            {
                for ( int dice = 1; dice <= 20; dice++ )
                {
                    int matchCount = 0;

                    for ( int i = 0; i < rewards.Count; i++ )
                    {
                        if ( rewards[ i ].Contains(stage , dice) )
                        {
                            matchCount++;
                        }
                    }

                    if ( matchCount != 1 )
                    {
                        throw new ArgumentException($"{owner}: 수색 보상 구간 누락 또는 중복. 단계 {stage}, D20 {dice}");
                    }
                }
            }
        }

        private static void ValidateStageEventChances(StageEventChanceDefinition[] chances , int maxStage)
        {
            var stages = new HashSet<int>();

            for ( int i = 0; i < chances.Length; i++ )
            {
                if ( chances[ i ] == null || chances[ i ].Stage > maxStage || !stages.Add(chances[ i ].Stage) )
                {
                    throw new ArgumentException("단계별 사건 확률표에 null, 범위 초과 또는 중복이 있습니다." , nameof(chances));
                }
            }

            if ( stages.Count != maxStage )
            {
                throw new ArgumentException("단계별 사건 확률표에 누락된 단계가 있습니다." , nameof(chances));
            }
        }

        private static void ValidateReturnRisks(ReturnRiskDefinition[] risks , int maxNoise)
        {
            for ( int i = 0; i < risks.Length; i++ )
            {
                if ( risks[ i ] == null || risks[ i ].MaxNoise > maxNoise )
                {
                    throw new ArgumentException("귀환 위험표에 null 또는 소음 범위 초과가 있습니다." , nameof(risks));
                }
            }

            var sortedRisks = (ReturnRiskDefinition[])risks.Clone();
            Array.Sort(sortedRisks , (left , right) => left.MinNoise.CompareTo(right.MinNoise));
            long nextNoise = 0;

            for ( int i = 0; i < sortedRisks.Length; i++ )
            {
                if ( sortedRisks[ i ].MinNoise != nextNoise )
                {
                    throw new ArgumentException("귀환 위험표에 누락되거나 중복된 소음 구간이 있습니다." , nameof(risks));
                }

                nextNoise = (long)sortedRisks[ i ].MaxNoise + 1;
            }

            if ( nextNoise != (long)maxNoise + 1 )
            {
                throw new ArgumentException("귀환 위험표가 최대 소음까지 정의되지 않았습니다." , nameof(risks));
            }
        }
    }
}
