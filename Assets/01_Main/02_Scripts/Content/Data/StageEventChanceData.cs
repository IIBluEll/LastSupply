using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class StageEventChanceData
    {
        [SerializeField, Min(1)] private int _stage = 1;
        [SerializeField, Range(0, 100)] private int _noEventChance = 65;
        [SerializeField, Range(0, 100)] private int _opportunityChance = 20;
        [SerializeField, Range(0, 100)] private int _dangerChance = 15;

        public StageEventChanceDefinition ToDefinition()
        {
            return new StageEventChanceDefinition(_stage, _noEventChance, _opportunityChance, _dangerChance);
        }
    }
}
