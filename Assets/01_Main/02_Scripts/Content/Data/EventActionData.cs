using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [Serializable]
    public sealed class EventActionData
    {
        [SerializeField] private string _id;
        [SerializeField] private string _text;
        [SerializeField] private bool _requiresCheck = true;
        [SerializeField] private STAT_TYPE _statType;
        [SerializeField, Min(1)] private int _difficulty = 12;
        [SerializeField] private EventEffectData _effect = new EventEffectData();
        [SerializeField] private EventEffectData _failureEffect = new EventEffectData();

        public EventActionDefinition ToDefinition()
        {
            if ( _effect == null )
            {
                throw new ArgumentNullException(nameof(_effect));
            }

            if ( !_requiresCheck )
            {
                return new EventActionDefinition(_id, _text, _effect.ToDefinition());
            }

            if ( _failureEffect == null )
            {
                throw new ArgumentNullException(nameof(_failureEffect));
            }

            return new EventActionDefinition(_id, _text, _statType, _difficulty, _effect.ToDefinition(), _failureEffect.ToDefinition());
        }
    }
}
