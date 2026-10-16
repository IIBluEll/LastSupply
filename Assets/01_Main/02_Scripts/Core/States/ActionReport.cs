using System;

namespace LastSupply.Core
{
    // 이미 확정하고 적용한 행동 결과
    // 이 객체를 표시하거나 확인한다고 효과가 다시 적용되지는 않음
    public sealed class ActionReport
    {
        public long Id { get; }
        public ACTION_RESULT_TYPE ResultType { get; }
        public RESULT_NEXT_STEP NextStep { get; }

        public string EventId { get; }
        public string ActionId { get; }

        public SurvivorState SurvivorBefore { get; }
        public SurvivorState SurvivorAfter { get; }
        public ExpeditionState ExpeditionBefore { get; }
        public ExpeditionState ExpeditionAfter { get; }

        // 이번 행동에서 실제 획득한 보상
        public ResourceAmount Reward { get; }

        public int? Dice { get; }
        public int StatModifier { get; }
        public int? Difficulty { get; }
        public long? Total { get; }
        public bool? IsSuccessful { get; }

        public int HpChange => SurvivorAfter.CurrentHp - SurvivorBefore.CurrentHp;
        public int NoiseChange => ExpeditionAfter.Noise - ExpeditionBefore.Noise;

        public ActionReport(
            long id , ACTION_RESULT_TYPE resultType , RESULT_NEXT_STEP nextStep ,
            SurvivorState survivorBefore , SurvivorState survivorAfter ,
            ExpeditionState expeditionBefore , ExpeditionState expeditionAfter ,
            ResourceAmount reward , string eventId = null , string actionId = null ,
            int? dice = null , int statModifier = 0 , int? difficulty = null)
        {
            if ( id < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if ( !Enum.IsDefined(typeof(ACTION_RESULT_TYPE) , resultType) )
            {
                throw new ArgumentOutOfRangeException(nameof(resultType));
            }

            if ( !Enum.IsDefined(typeof(RESULT_NEXT_STEP) , nextStep) || nextStep == RESULT_NEXT_STEP.NONE )
            {
                throw new ArgumentOutOfRangeException(nameof(nextStep));
            }

            if ( dice.HasValue && ( dice.Value < 1 || dice.Value > 20 ) )
            {
                throw new ArgumentOutOfRangeException(nameof(dice));
            }

            if ( difficulty.HasValue && ( difficulty.Value < 1 || !dice.HasValue ) )
            {
                throw new ArgumentException("행동 판정에는 유효한 DC와 주사위 눈이 필요합니다." , nameof(difficulty));
            }

            if ( !difficulty.HasValue && statModifier != 0 )
            {
                throw new ArgumentException("행동 판정이 없으면 능력치 보정을 지정하지 않습니다." , nameof(statModifier));
            }

            SurvivorBefore = survivorBefore ?? throw new ArgumentNullException(nameof(survivorBefore));
            SurvivorAfter = survivorAfter ?? throw new ArgumentNullException(nameof(survivorAfter));
            ExpeditionBefore = expeditionBefore ?? throw new ArgumentNullException(nameof(expeditionBefore));
            ExpeditionAfter = expeditionAfter ?? throw new ArgumentNullException(nameof(expeditionAfter));

            if ( SurvivorBefore.SurvivorId != SurvivorAfter.SurvivorId ||
                 ExpeditionBefore.SurvivorId != SurvivorBefore.SurvivorId ||
                 ExpeditionAfter.SurvivorId != SurvivorAfter.SurvivorId )
            {
                throw new ArgumentException("적용 전후의 파견 생존자가 일치해야 합니다.");
            }

            if ( ExpeditionBefore.RegionId != ExpeditionAfter.RegionId ||
                 ExpeditionBefore.Stage != ExpeditionAfter.Stage )
            {
                throw new ArgumentException("하나의 행동 결과에서 지역과 단계가 바뀌면 안 됩니다.");
            }

            Id = id;
            ResultType = resultType;
            NextStep = nextStep;
            EventId = eventId;
            ActionId = actionId;
            Reward = reward;
            Dice = dice;
            StatModifier = statModifier;
            Difficulty = difficulty;

            Total = dice.HasValue ? (long)dice.Value + statModifier : (long?)null;
            IsSuccessful = difficulty.HasValue ? Total.Value >= difficulty.Value : (bool?)null;
        }
    }
}
