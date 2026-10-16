using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class ReturnEventData
    {
        [SerializeField] private string _id = "RETURN_001";
        [SerializeField] private string _displayName = "돌아오는 길의 좀비 무리";
        [SerializeField, TextArea] private string _description = "소리를 따라온 좀비들이 은신처로 돌아가는 길을 막았다.";
        [SerializeField] private string _actionId = "BREAK_THROUGH";
        [SerializeField] private string _actionText = "길을 뚫는다";
        [SerializeField] private STAT_TYPE _statType = STAT_TYPE.COMBAT;
        [SerializeField, Min(1)] private int _difficulty = 12;

        public ReturnEventDefinition ToDefinition()
        {
            return new ReturnEventDefinition(_id, _displayName, _description, _actionId, _actionText, _statType, _difficulty);
        }
    }
}
