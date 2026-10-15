using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 지역의 주 자원, 보상
    public sealed class RegionDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public RESOURCE_TYPE MainResource { get; }

        public IReadOnlyList<string> EventIds { get; }
        public IReadOnlyList<SearchRewardDefinition> SearchRewards { get; }

        public RegionDefinition(string id , string name , RESOURCE_TYPE mainResource ,
            string[] eventIds , SearchRewardDefinition[] searchRewards = null)
        {
            if ( string.IsNullOrWhiteSpace(id) )
            {
                throw new ArgumentException("지역 ID가 필요합니다." , nameof(id));
            }

            if ( string.IsNullOrWhiteSpace(name) )
            {
                throw new ArgumentException("지역 이름이 필요합니다." , nameof(name));
            }

            if ( !Enum.IsDefined(typeof(RESOURCE_TYPE) , mainResource) )
            {
                throw new ArgumentOutOfRangeException(nameof(mainResource));
            }

            if ( eventIds == null )
            {
                throw new ArgumentNullException(nameof(eventIds));
            }

            var copiedEventIds = (string[])eventIds.Clone();

            for ( int i = 0; i < copiedEventIds.Length; i++ )
            {
                if ( string.IsNullOrWhiteSpace(copiedEventIds[ i ]) )
                {
                    throw new ArgumentException("사건 ID가 비어 있습니다." , nameof(eventIds));
                }
            }

            if ( searchRewards != null )
            {
                var copiedRewards = (SearchRewardDefinition[])searchRewards.Clone();

                for ( int i = 0; i < copiedRewards.Length; i++ )
                {
                    if ( copiedRewards[ i ] == null )
                    {
                        throw new ArgumentException("지역 보상표에 null 행이 있습니다." , nameof(searchRewards));
                    }
                }

                SearchRewards = Array.AsReadOnly(copiedRewards);
            }

            Id = id;
            Name = name;
            MainResource = mainResource;
            EventIds = Array.AsReadOnly(copiedEventIds);
        }
    }
}