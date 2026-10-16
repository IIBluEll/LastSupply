using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class ResourceRewardData
    {
        [SerializeField] private bool _useRegionResource = true;
        [SerializeField] private RESOURCE_TYPE _resourceType;
        [SerializeField, Min(0)] private int _amount = 1;

        public SearchRewardItemDefinition ToDefinition()
        {
            RESOURCE_TYPE? resourceType = _useRegionResource ? (RESOURCE_TYPE?)null : _resourceType;
            return new SearchRewardItemDefinition(_amount, resourceType);
        }
    }
}
