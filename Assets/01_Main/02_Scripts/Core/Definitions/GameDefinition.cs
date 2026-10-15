using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 한 판에서 사용할 읽기 전용 콘텐츠. 진행 상태와 난수는 보관하지 않습니다.
    public sealed class GameDefinition
    {
        public GameRulesDefinition Rules { get; }
        public IReadOnlyList<SurvivorDefinition> Survivors { get; }
        public IReadOnlyList<RegionDefinition> Regions { get; }
        public IReadOnlyList<EventDefinition> Events { get; }

        public GameDefinition(GameRulesDefinition rules , SurvivorDefinition[] survivors ,
            RegionDefinition[] regions , EventDefinition[] events)
        {
            if ( rules == null )
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if ( survivors == null )
            {
                throw new ArgumentNullException(nameof(survivors));
            }

            if ( regions == null )
            {
                throw new ArgumentNullException(nameof(regions));
            }

            if ( events == null )
            {
                throw new ArgumentNullException(nameof(events));
            }

            if ( survivors.Length == 0 || regions.Length == 0 )
            {
                throw new ArgumentException("생존자와 지역은 각각 하나 이상 필요합니다.");
            }

            var copiedSurvivors = (SurvivorDefinition[])survivors.Clone();
            var copiedRegions = (RegionDefinition[])regions.Clone();
            var copiedEvents = (EventDefinition[])events.Clone();
            var survivorIds = new HashSet<string>(StringComparer.Ordinal);
            var regionIds = new HashSet<string>(StringComparer.Ordinal);
            var eventsById = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);

            for ( int i = 0; i < copiedSurvivors.Length; i++ )
            {
                SurvivorDefinition survivor = copiedSurvivors[ i ];

                if ( survivor == null || !survivorIds.Add(survivor.ID) )
                {
                    throw new ArgumentException("생존자 정의에 null 또는 중복 ID가 있습니다." , nameof(survivors));
                }
            }

            for ( int i = 0; i < copiedEvents.Length; i++ )
            {
                EventDefinition definition = copiedEvents[ i ];

                if ( definition == null )
                {
                    throw new ArgumentException("사건 정의에 null이 있습니다." , nameof(events));
                }

                if ( definition.MaxStage > rules.MaxStage )
                {
                    throw new ArgumentException($"{definition.Id}: 사건 등장 단계가 최대 단계를 초과합니다." , nameof(events));
                }

                if ( definition.Id == rules.ReturnEvent.Id )
                {
                    throw new ArgumentException($"{definition.Id}: 귀환 사건을 일반 사건 목록에 넣을 수 없습니다." , nameof(events));
                }

                if ( eventsById.ContainsKey(definition.Id) )
                {
                    throw new ArgumentException($"일반 사건 ID가 중복됩니다: {definition.Id}" , nameof(events));
                }

                eventsById.Add(definition.Id , definition);
            }

            for ( int i = 0; i < copiedRegions.Length; i++ )
            {
                RegionDefinition region = copiedRegions[ i ];

                if ( region == null || !regionIds.Add(region.Id) )
                {
                    throw new ArgumentException("지역 정의에 null 또는 중복 ID가 있습니다." , nameof(regions));
                }

                if ( region.SearchRewards != null )
                {
                    GameRulesDefinition.ValidateSearchRewards(region.SearchRewards , rules.MaxStage , region.Id);
                }

                var regionEventIds = new HashSet<string>(StringComparer.Ordinal);

                for ( int j = 0; j < region.EventIds.Count; j++ )
                {
                    string eventId = region.EventIds[ j ];

                    if ( !eventsById.ContainsKey(eventId) || !regionEventIds.Add(eventId) )
                    {
                        throw new ArgumentException($"{region.Id}: 존재하지 않거나 중복된 일반 사건 ID: {eventId}" , nameof(regions));
                    }
                }

                ValidateEventCandidates(region , eventsById , rules.MaxStage);
            }

            Rules = rules;
            Survivors = Array.AsReadOnly(copiedSurvivors);
            Regions = Array.AsReadOnly(copiedRegions);
            Events = Array.AsReadOnly(copiedEvents);
        }

        private static void ValidateEventCandidates(RegionDefinition region , Dictionary<string, EventDefinition> eventsById , int maxStage)
        {
            for ( int stage = 1; stage <= maxStage; stage++ )
            {
                bool hasOpportunity = false;
                bool hasDanger = false;

                for ( int i = 0; i < region.EventIds.Count; i++ )
                {
                    EventDefinition definition = eventsById[region.EventIds[ i ]];

                    if ( stage < definition.MinStage || stage > definition.MaxStage )
                    {
                        continue;
                    }

                    hasOpportunity |= definition.Category == EVENT_CATEGORY.OPPORTUNITY;
                    hasDanger |= definition.Category == EVENT_CATEGORY.DANGER;
                }

                if ( !hasOpportunity || !hasDanger )
                {
                    throw new ArgumentException($"{region.Id}: {stage}단계의 기회 또는 위험 사건 후보가 없습니다.");
                }
            }
        }
    }
}
