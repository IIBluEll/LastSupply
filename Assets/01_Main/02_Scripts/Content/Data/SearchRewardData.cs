using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class SearchRewardData
    {
        [SerializeField, Min(1)] private int _minStage = 1;
        [SerializeField, Min(1)] private int _maxStage = 5;
        [SerializeField, Range(1, 20)] private int _minDice = 1;
        [SerializeField, Range(1, 20)] private int _maxDice = 20;
        [SerializeField] private ResourceRewardData[] _rewards = Array.Empty<ResourceRewardData>();

        public SearchRewardDefinition ToDefinition()
        {
            if ( _rewards == null )
            {
                throw new ArgumentNullException(nameof(_rewards));
            }

            var rewards = new SearchRewardItemDefinition[_rewards.Length];
            for ( int i = 0; i < rewards.Length; i++ )
            {
                if ( _rewards[ i ] == null )
                {
                    throw new ArgumentException($"보상 {i}번 항목이 비어 있습니다." , nameof(_rewards));
                }

                rewards[ i ] = _rewards[ i ].ToDefinition();
            }

            return new SearchRewardDefinition(_minStage , _maxStage , _minDice , _maxDice , rewards);
        }
    }
}