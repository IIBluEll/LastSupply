using System;

namespace LastSupply.Core
{
    // 귀환 전용 판정 정보. 실패 피해는 ReturnRiskDefinition에만 보관합니다.
    public sealed class ReturnEventDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string ActionId { get; }
        public string ActionText { get; }
        public STAT_TYPE StatType { get; }
        public int Difficulty { get; }

        public ReturnEventDefinition(string id , string name , string description , string actionId ,
            string actionText , STAT_TYPE statType , int difficulty)
        {
            if ( string.IsNullOrWhiteSpace(id) )
            {
                throw new ArgumentException("귀환 사건 ID가 필요합니다." , nameof(id));
            }

            if ( string.IsNullOrWhiteSpace(name) )
            {
                throw new ArgumentException("귀환 사건 이름이 필요합니다." , nameof(name));
            }

            if ( string.IsNullOrWhiteSpace(description) )
            {
                throw new ArgumentException("귀환 사건 상황 설명이 필요합니다." , nameof(description));
            }

            if ( string.IsNullOrWhiteSpace(actionId) )
            {
                throw new ArgumentException("귀환 행동 ID가 필요합니다." , nameof(actionId));
            }

            if ( string.IsNullOrWhiteSpace(actionText) )
            {
                throw new ArgumentException("귀환 행동 문구가 필요합니다." , nameof(actionText));
            }

            if ( !Enum.IsDefined(typeof(STAT_TYPE) , statType) )
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            if ( difficulty < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            }

            Id = id;
            Name = name;
            Description = description;
            ActionId = actionId;
            ActionText = actionText;
            StatType = statType;
            Difficulty = difficulty;
        }
    }
}
