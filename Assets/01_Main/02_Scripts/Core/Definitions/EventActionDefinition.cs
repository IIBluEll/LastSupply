using System;

namespace LastSupply.Core
{
    // 이벤트 행동 하나와 판정 조건, 결과 효과
    public sealed class EventActionDefinition
    {
        public string Id { get; }
        public string Text { get; }

        public STAT_TYPE StatType { get; }
        public int? Difficulty { get; }

        public bool RequiresCheck => Difficulty.HasValue;

        // 판정이 없으면 확정 / 있으면 성공
        public EventEffectDefinition Effect { get; }
        public EventEffectDefinition FailureEffect { get; }

        // 주사위 판정 없이 효과를 적용하는 행동
        public EventActionDefinition(string id, string text, EventEffectDefinition effect)
        {
            if ( string.IsNullOrWhiteSpace(id) )
            {
                throw new ArgumentException("행동 ID가 필요합니다." , nameof(id));
            }

            if ( string.IsNullOrWhiteSpace(text) )
            {
                throw new ArgumentException("행동 문구가 필요합니다." , nameof(text));
            }

            if ( effect == null )
            {
                throw new ArgumentNullException(nameof(effect));
            }

            Id = id;
            Text = text;
            StatType = STAT_TYPE.NONE;
            Effect = effect;
        }

        // 주사위 판정의 성공 여부에 따라 효과를 적용하는 행동
        public EventActionDefinition(string id , string text , STAT_TYPE statType , int difficulty , EventEffectDefinition successEffect , EventEffectDefinition failureEffect) 
            : this(id , text , successEffect)
        {
            if ( !Enum.IsDefined(typeof(STAT_TYPE) , statType) )
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            if ( difficulty < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            }

            if ( failureEffect == null )
            {
                throw new ArgumentNullException(nameof(failureEffect));
            }

            StatType = statType;
            Difficulty = difficulty;
            FailureEffect = failureEffect;
        }
    }
}