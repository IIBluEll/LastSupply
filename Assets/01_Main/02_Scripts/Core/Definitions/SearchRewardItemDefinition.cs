using System;

namespace LastSupply.Core
{
    //보상표 한 행에 들어가는 개별 지급 항목
    public sealed class SearchRewardItemDefinition
    {
        public int Amount { get; }

        // null이면 현재 지역의 주 자원을 사용합니다.
        public RESOURCE_TYPE? ResourceType { get; }

        public SearchRewardItemDefinition(int amount , RESOURCE_TYPE? resourceType = null)
        {
            if ( amount < 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if ( resourceType.HasValue && !Enum.IsDefined(typeof(RESOURCE_TYPE) , resourceType.Value) )
            {
                throw new ArgumentOutOfRangeException(nameof(resourceType));
            }

            Amount = amount;
            ResourceType = resourceType;
        }
    }
}