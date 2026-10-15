using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    // 일반 탐색 이벤트 하나의 정의
    public sealed class EventDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public EVENT_CATEGORY Category { get; }
        public EVENT_MODE Mode { get; }

        public int MinStage { get; }
        public int MaxStage { get; }

        public EventEffectDefinition InstantEffect { get; }
        public IReadOnlyList<EventActionDefinition> Actions { get; }

        public EventDefinition(string id , string name , string description ,
            EVENT_CATEGORY category , EVENT_MODE mode , int minStage , int maxStage ,
            EventEffectDefinition instantEffect = null , params EventActionDefinition[] actions)
        {
            if ( string.IsNullOrWhiteSpace(id) )
            {
                throw new ArgumentException("사건 ID가 필요합니다." , nameof(id));
            }

            if ( string.IsNullOrWhiteSpace(name) )
            {
                throw new ArgumentException("사건 이름이 필요합니다." , nameof(name));
            }

            if ( string.IsNullOrWhiteSpace(description) )
            {
                throw new ArgumentException("사건 상황 설명이 필요합니다." , nameof(description));
            }

            if ( !Enum.IsDefined(typeof(EVENT_CATEGORY) , category) )
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            if ( !Enum.IsDefined(typeof(EVENT_MODE) , mode) )
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if ( minStage < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(minStage));
            }

            if ( maxStage < minStage )
            {
                throw new ArgumentOutOfRangeException(nameof(maxStage));
            }

            if ( actions == null )
            {
                throw new ArgumentNullException(nameof(actions));
            }

            var copiedActions = (EventActionDefinition[])actions.Clone();
            var actionIds = new HashSet<string>(StringComparer.Ordinal);

            for ( int i = 0; i < copiedActions.Length; i++ )
            {
                EventActionDefinition action = copiedActions[ i ];

                if ( action == null )
                {
                    throw new ArgumentException("사건 행동에 null이 있습니다." , nameof(actions));
                }

                if ( !actionIds.Add(action.Id) )
                {
                    throw new ArgumentException($"행동 ID가 중복됩니다: {action.Id}" , nameof(actions));
                }
            }

            if ( mode == EVENT_MODE.INSTANT )
            {
                if ( instantEffect == null )
                {
                    throw new ArgumentNullException(nameof(instantEffect));
                }

                if ( copiedActions.Length != 0 )
                {
                    throw new ArgumentException("즉시 결과형 사건에는 행동을 지정하지 않습니다." , nameof(actions));
                }
            }
            else
            {
                if ( instantEffect != null )
                {
                    throw new ArgumentException("즉시 효과는 즉시 결과형 사건에만 지정합니다." , nameof(instantEffect));
                }

                if ( mode == EVENT_MODE.CHECK && ( copiedActions.Length != 1 || !copiedActions[ 0 ].RequiresCheck ) )
                {
                    throw new ArgumentException("판정형 사건에는 판정 행동이 정확히 하나 필요합니다." , nameof(actions));
                }

                if ( mode == EVENT_MODE.CHOICE && ( copiedActions.Length < 2 || copiedActions.Length > 3 ) )
                {
                    throw new ArgumentException("선택형 사건에는 행동이 2~3개 필요합니다." , nameof(actions));
                }
            }

            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Mode = mode;
            MinStage = minStage;
            MaxStage = maxStage;
            InstantEffect = instantEffect;
            Actions = Array.AsReadOnly(copiedActions);
        }
    }
}