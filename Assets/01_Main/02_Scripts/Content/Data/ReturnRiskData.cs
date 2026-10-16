using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class ReturnRiskData
    {
        [SerializeField, Min(0)] private int _minNoise;
        [SerializeField, Min(0)] private int _maxNoise;
        [SerializeField] private string _displayName = "안전";
        [SerializeField, Range(0, 100)] private int _eventChance;
        [SerializeField, Min(0)] private int _failureDamage;

        public ReturnRiskDefinition ToDefinition()
        {
            return new ReturnRiskDefinition(_minNoise, _maxNoise, _displayName, _eventChance, _failureDamage);
        }
    }
}
