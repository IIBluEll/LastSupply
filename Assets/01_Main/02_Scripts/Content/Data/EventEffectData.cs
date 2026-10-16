using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class EventEffectData
    {
        [SerializeField] private int _hpChange;
        [SerializeField, Min(0)] private int _noiseIncrease;
        [SerializeField] private ResourceRewardData[] _rewards = Array.Empty<ResourceRewardData>();

        public EventEffectDefinition ToDefinition()
        {
            if ( _rewards == null )
            {
                throw new ArgumentNullException(nameof(_rewards));
            }

            var rewards = new SearchRewardItemDefinition[_rewards.Length];
            for ( int i = 0; i < rewards.Length; i++ )
            {
                if ( _rewards[i] == null )
                {
                    throw new ArgumentException($"보상 {i}번 항목이 비어 있습니다.", nameof(_rewards));
                }

                rewards[i] = _rewards[i].ToDefinition();
            }

            return new EventEffectDefinition(_hpChange, _noiseIncrease, rewards);
        }
    }
}
