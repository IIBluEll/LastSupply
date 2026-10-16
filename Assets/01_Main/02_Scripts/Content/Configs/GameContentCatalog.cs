using System;
using System.Collections.Generic;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [CreateAssetMenu(fileName = "GameContentCatalog", menuName = "Last Supply/Content Catalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private SurvivorConfig[] _survivors = Array.Empty<SurvivorConfig>();
        [SerializeField] private RegionConfig[] _regions = Array.Empty<RegionConfig>();
        [SerializeField] private EventConfig[] _events = Array.Empty<EventConfig>();

        public GameDefinition ToDefinition()
        {
            if ( _gameConfig == null )
            {
                throw new ArgumentException($"{name}: 공통 게임 설정이 비어 있습니다.", nameof(_gameConfig));
            }

            if ( _survivors == null || _regions == null || _events == null )
            {
                throw new ArgumentException($"{name}: 생존자·지역·사건 목록은 null일 수 없습니다.");
            }

            GameRulesDefinition rules = ConvertDefinition(() => _gameConfig.ToDefinition(), _gameConfig.name);
            var survivors = new SurvivorDefinition[_survivors.Length];
            for ( int i = 0; i < survivors.Length; i++ )
            {
                SurvivorConfig config = _survivors[i];
                if ( config == null )
                {
                    throw new ArgumentException($"{name}: 생존자 {i}번 참조가 비어 있습니다.", nameof(_survivors));
                }

                survivors[i] = ConvertDefinition(() => config.ToDefinition(), $"{config.name} ({config.Id})");
            }

            var events = new EventDefinition[_events.Length];
            var availableEvents = new HashSet<EventConfig>();
            for ( int i = 0; i < events.Length; i++ )
            {
                EventConfig config = _events[i];
                if ( config == null )
                {
                    throw new ArgumentException($"{name}: 사건 {i}번 참조가 비어 있습니다.", nameof(_events));
                }

                events[i] = ConvertDefinition(() => config.ToDefinition(), $"{config.name} ({config.Id})");
                availableEvents.Add(config);
            }

            var regions = new RegionDefinition[_regions.Length];
            for ( int i = 0; i < regions.Length; i++ )
            {
                RegionConfig config = _regions[i];
                if ( config == null )
                {
                    throw new ArgumentException($"{name}: 지역 {i}번 참조가 비어 있습니다.", nameof(_regions));
                }

                regions[i] = ConvertDefinition(() =>
                {
                    RegionDefinition definition = config.ToDefinition();
                    config.ValidateEventReferences(availableEvents);
                    return definition;
                }, $"{config.name} ({config.Id})");
            }

            return ConvertDefinition(() => new GameDefinition(rules, survivors, regions, events), name);
        }

        public bool TryGetSurvivorConfig(string id, out SurvivorConfig config)
        {
            config = null;
            if ( string.IsNullOrWhiteSpace(id) || _survivors == null )
            {
                return false;
            }

            for ( int i = 0; i < _survivors.Length; i++ )
            {
                if ( _survivors[i] != null && string.Equals(_survivors[i].Id, id, StringComparison.Ordinal) )
                {
                    config = _survivors[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRegionConfig(string id, out RegionConfig config)
        {
            config = null;
            if ( string.IsNullOrWhiteSpace(id) || _regions == null )
            {
                return false;
            }

            for ( int i = 0; i < _regions.Length; i++ )
            {
                if ( _regions[i] != null && string.Equals(_regions[i].Id, id, StringComparison.Ordinal) )
                {
                    config = _regions[i];
                    return true;
                }
            }

            return false;
        }

        private static T ConvertDefinition<T>(Func<T> convert, string context)
        {
            try
            {
                return convert();
            }
            catch ( ArgumentException exception )
            {
                throw new ArgumentException($"{context}: {exception.Message}", exception);
            }
        }
    }
}
