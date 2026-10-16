using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [CreateAssetMenu(fileName = "EventConfig", menuName = "Last Supply/Event")]
    public sealed class EventConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private EVENT_CATEGORY _category;
        [SerializeField] private EVENT_MODE _mode;
        [SerializeField, Min(1)] private int _minStage = 1;
        [SerializeField, Min(1)] private int _maxStage = 5;
        [SerializeField] private EventEffectData _instantEffect = new EventEffectData();
        [SerializeField] private EventActionData[] _actions = Array.Empty<EventActionData>();

        public string Id => _id;

        public EventDefinition ToDefinition()
        {
            if ( _actions == null )
            {
                throw new ArgumentNullException(nameof(_actions));
            }

            var actions = new EventActionDefinition[_actions.Length];
            for ( int i = 0; i < actions.Length; i++ )
            {
                if ( _actions[i] == null )
                {
                    throw new ArgumentException($"행동 {i}번 항목이 비어 있습니다.", nameof(_actions));
                }

                actions[i] = _actions[i].ToDefinition();
            }

            EventEffectDefinition instantEffect = null;
            if ( _mode == EVENT_MODE.INSTANT )
            {
                if ( _instantEffect == null )
                {
                    throw new ArgumentNullException(nameof(_instantEffect));
                }

                instantEffect = _instantEffect.ToDefinition();
            }

            return new EventDefinition(_id, _displayName, _description, _category, _mode, _minStage, _maxStage, instantEffect, actions);
        }
    }
}
