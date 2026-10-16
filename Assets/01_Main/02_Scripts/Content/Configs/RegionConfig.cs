using System;
using System.Collections.Generic;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [CreateAssetMenu(fileName = "RegionConfig" , menuName = "Last Supply/Region")]
    public sealed class RegionConfig : ScriptableObject
    {
        [Header("식별 정보")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("화면 표시")]
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _backgroundSprite;

        [Header("탐색 설정")]
        [SerializeField] private RESOURCE_TYPE _mainResource;
        [SerializeField] private EventConfig[] _events = Array.Empty<EventConfig>();

        [Header("지역 전용 수색 보상표")]
        [SerializeField] private bool _useCustomSearchRewards;
        [SerializeField] private SearchRewardData[] _searchRewards = Array.Empty<SearchRewardData>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite BackgroundSprite => _backgroundSprite;

        internal void ValidateEventReferences(ISet<EventConfig> availableEvents)
        {
            for ( int i = 0; i < _events.Length; i++ )
            {
                if ( !availableEvents.Contains(_events[i]) )
                {
                    throw new ArgumentException($"일반 사건 에셋이 카탈로그에 등록되지 않았습니다: {_events[i].name} ({_events[i].Id})", nameof(_events));
                }
            }
        }

        public RegionDefinition ToDefinition()
        {
            if ( _events == null )
            {
                throw new ArgumentNullException(nameof(_events));
            }

            var eventIds = new string[_events.Length];
            for ( int i = 0; i < eventIds.Length; i++ )
            {
                if ( _events[ i ] == null )
                {
                    throw new ArgumentException($"사건 {i}번 항목이 비어 있습니다." , nameof(_events));
                }

                eventIds[ i ] = _events[ i ].Id;
            }

            SearchRewardDefinition[] searchRewards = null;
            if ( _useCustomSearchRewards )
            {
                if ( _searchRewards == null )
                {
                    throw new ArgumentNullException(nameof(_searchRewards));
                }

                if ( _searchRewards.Length == 0 )
                {
                    throw new ArgumentException("지역 전용 수색 보상표가 비어 있습니다." , nameof(_searchRewards));
                }

                searchRewards = new SearchRewardDefinition[ _searchRewards.Length ];
                for ( int i = 0; i < searchRewards.Length; i++ )
                {
                    if ( _searchRewards[ i ] == null )
                    {
                        throw new ArgumentException($"수색 보상표 {i}번 행이 비어 있습니다." , nameof(_searchRewards));
                    }

                    searchRewards[ i ] = _searchRewards[ i ].ToDefinition();
                }
            }

            return new RegionDefinition(_id , _displayName , _mainResource , eventIds , searchRewards);
        }
    }
}
