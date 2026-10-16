using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    public sealed class GameSession
    {
        private readonly GameDefinition DEFINITION;
        private readonly IRandomSource RANDOM;
        private readonly SurvivorState[] SURVIVORS;

        private int _day = 1;
        private GAME_PHASE _phase = GAME_PHASE.SHELTER;
        private long _revision;
        private long _nextResultId = 1;
        private ResourceAmount _resources;
        private ExpeditionState _expedition;
        private ActionReport _pendingReport;
        private DaySummary _daySummary;
        private RUN_END_REASON _endReason;

        public GameSession(GameDefinition definition , IRandomSource random)
        {
            DEFINITION = definition ?? throw new ArgumentNullException(nameof(definition));
            RANDOM = random ?? throw new ArgumentNullException(nameof(random));
            SURVIVORS = new SurvivorState[ definition.Survivors.Count ];

            for ( int i = 0; i < SURVIVORS.Length; i++ )
            {
                SurvivorDefinition survivor = definition.Survivors[i];
                SURVIVORS[ i ] = new SurvivorState(survivor , survivor.MaxHP);
            }

            _resources = definition.Rules.InitialResources;
        }

        public GameSnapshot GetSnapshot()
        {
            return new GameSnapshot(_day , _phase , _revision , _resources , SURVIVORS , _expedition , _pendingReport, _daySummary, _endReason);
        }

        public bool TryStartExpedition(long expectedRevision , string survivorId , string regionId , out string rejectionReason)
        {
            rejectionReason = null;

            if ( expectedRevision != _revision )
            {
                rejectionReason = "표시된 상태가 변경되었습니다. 현재 상태를 다시 확인해 주세요.";
                return false;
            }

            if ( _phase != GAME_PHASE.SHELTER )
            {
                rejectionReason = "은신처에서만 출발할 수 있습니다.";
                return false;
            }

            int survivorIndex = -1;
            for ( int i = 0; i < SURVIVORS.Length; i++ )
            {
                if ( SURVIVORS[ i ].SurvivorId == survivorId )
                {
                    survivorIndex = i;
                    break;
                }
            }

            if ( survivorIndex < 0 || !SURVIVORS[ survivorIndex ].IsAlive )
            {
                rejectionReason = "출발할 수 있는 생존자를 선택해 주세요.";
                return false;
            }

            RegionDefinition region = null;
            for ( int i = 0; i < DEFINITION.Regions.Count; i++ )
            {
                if ( DEFINITION.Regions[ i ].Id == regionId )
                {
                    region = DEFINITION.Regions[ i ];
                    break;
                }
            }

            if ( region == null )
            {
                rejectionReason = "유효한 지역을 선택해 주세요.";
                return false;
            }

            long nextRevision = checked(_revision + 1);
            EnterStage(survivorIndex, region, 1, 0, default, null);
            _revision = nextRevision;
            return true;
        }

        public bool TryHeal(long expectedRevision, string survivorId, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.SHELTER, out rejectionReason) )
            {
                return false;
            }

            int index = Array.FindIndex(SURVIVORS, survivor => survivor.SurvivorId == survivorId);
            if ( index < 0 || !SURVIVORS[index].IsAlive || SURVIVORS[index].CurrentHp == DEFINITION.Survivors[index].MaxHP )
            {
                rejectionReason = "치료할 수 있는 생존자를 선택해 주세요.";
                return false;
            }

            GameRulesDefinition rules = DEFINITION.Rules;
            if ( _resources.Medicine < rules.HealingMedicineCost )
            {
                rejectionReason = "치료에 필요한 의약품이 부족합니다.";
                return false;
            }

            long nextRevision = checked(_revision + 1);
            int hp = SurvivalRules.CalculateHealedHp(SURVIVORS[index].CurrentHp, DEFINITION.Survivors[index].MaxHP, rules.HealingAmount);
            var nextSurvivor = new SurvivorState(DEFINITION.Survivors[index], hp);
            var nextResources = new ResourceAmount(_resources.Food, _resources.Medicine - rules.HealingMedicineCost, _resources.Parts);
            SURVIVORS[index] = nextSurvivor;
            _resources = nextResources;
            _revision = nextRevision;
            return true;
        }

        public bool TrySearch(long expectedRevision, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.SEARCH_CHOICE, out rejectionReason) )
            {
                return false;
            }

            int index = GetActiveSurvivorIndex();
            RegionDefinition region = GetActiveRegion();
            long nextRevision = checked(_revision + 1);
            int dice = RANDOM.NextInt(1, 21);
            ResourceAmount reward = ExplorationRules.CalculateSearchReward(
                _expedition.Stage, dice, region.MainResource, DEFINITION.Rules.DefaultSearchRewards, region.SearchRewards);
            var afterExpedition = new ExpeditionState(_expedition.SurvivorId, _expedition.RegionId,
                _expedition.Stage, _expedition.Noise, _expedition.Loot.Add(reward), _expedition.StageEventId, _expedition.PreviousStageEventId);
            var report = new ActionReport(_nextResultId, ACTION_RESULT_TYPE.SEARCH, RESULT_NEXT_STEP.FINISH_STAGE,
                SURVIVORS[index], SURVIVORS[index], _expedition, afterExpedition, reward, dice: dice);
            PublishReport(index, report);
            _revision = nextRevision;
            return true;
        }

        public bool TrySkipSearch(long expectedRevision, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.SEARCH_CHOICE, out rejectionReason) )
            {
                return false;
            }

            long nextRevision = checked(_revision + 1);
            FinishStage();
            _revision = nextRevision;
            return true;
        }

        public bool TryAdvanceStage(long expectedRevision, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.STAGE_CHOICE, out rejectionReason) )
            {
                return false;
            }

            if ( _expedition.Stage >= DEFINITION.Rules.MaxStage )
            {
                rejectionReason = "더 진입할 단계가 없습니다.";
                return false;
            }

            long nextRevision = checked(_revision + 1);
            EnterStage(GetActiveSurvivorIndex(), GetActiveRegion(), _expedition.Stage + 1,
                _expedition.Noise, _expedition.Loot, _expedition.StageEventId);
            _revision = nextRevision;
            return true;
        }

        public bool TryReturn(long expectedRevision, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.STAGE_CHOICE, out rejectionReason) )
            {
                return false;
            }

            long nextRevision = checked(_revision + 1);
            BeginReturn();
            _revision = nextRevision;
            return true;
        }

        public bool TryResolveReturn(long expectedRevision, string eventId, string actionId, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.RETURN_ACTION, out rejectionReason) )
            {
                return false;
            }

            ReturnEventDefinition returnEvent = DEFINITION.Rules.ReturnEvent;
            if ( eventId != returnEvent.Id || actionId != returnEvent.ActionId )
            {
                rejectionReason = "현재 귀환 사건과 행동을 확인해 주세요.";
                return false;
            }

            int index = GetActiveSurvivorIndex();
            SurvivorDefinition survivor = DEFINITION.Survivors[index];
            int modifier = GetStatModifier(survivor, returnEvent.StatType);
            long nextRevision = checked(_revision + 1);
            int dice = RANDOM.NextInt(1, 21);
            bool successful = ExplorationRules.IsActionSuccessful(dice, modifier, returnEvent.Difficulty);
            int hp = successful ? SURVIVORS[index].CurrentHp : Math.Max(0, SURVIVORS[index].CurrentHp - _expedition.ReturnRisk.FailureDamage);
            var afterSurvivor = new SurvivorState(survivor, hp);
            var afterExpedition = new ExpeditionState(_expedition.SurvivorId, _expedition.RegionId,
                _expedition.Stage, _expedition.Noise, hp > 0 ? _expedition.Loot : default,
                _expedition.StageEventId, _expedition.PreviousStageEventId, _expedition.ReturnRisk);
            var report = new ActionReport(_nextResultId, ACTION_RESULT_TYPE.RETURN, RESULT_NEXT_STEP.FINISH_EXPEDITION,
                SURVIVORS[index], afterSurvivor, _expedition, afterExpedition, default,
                returnEvent.Id, returnEvent.ActionId, dice, modifier, returnEvent.Difficulty);
            PublishReport(index, report);
            _revision = nextRevision;
            return true;
        }

        public bool TryConfirmResult(long expectedRevision, long resultId, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.RESULT, out rejectionReason) )
            {
                return false;
            }

            if ( _pendingReport == null || _pendingReport.Id != resultId )
            {
                rejectionReason = "현재 표시된 결과가 아닙니다.";
                return false;
            }

            long nextRevision = checked(_revision + 1);
            switch ( _pendingReport.NextStep )
            {
                case RESULT_NEXT_STEP.SEARCH_CHOICE:
                    GetActiveSurvivorIndex();
                    _phase = GAME_PHASE.SEARCH_CHOICE;
                    _pendingReport = null;
                    break;
                case RESULT_NEXT_STEP.FINISH_STAGE:
                    FinishStage();
                    break;
                case RESULT_NEXT_STEP.FINISH_EXPEDITION:
                    ResourceAmount lostLoot = _pendingReport.SurvivorAfter.IsAlive ? default : _pendingReport.ExpeditionBefore.Loot;
                    FinishExpedition(_expedition, lostLoot);
                    break;
                default:
                    throw new InvalidOperationException("처리할 수 없는 결과 후속 흐름입니다.");
            }

            _revision = nextRevision;
            return true;
        }

        public bool TryConfirmDaySummary(long expectedRevision, long summaryId, out string rejectionReason)
        {
            if ( !ValidateCommand(expectedRevision, GAME_PHASE.DAY_SUMMARY, out rejectionReason) )
            {
                return false;
            }

            if ( _daySummary == null || _daySummary.Id != summaryId )
            {
                rejectionReason = "현재 표시된 정산 결과가 아닙니다.";
                return false;
            }

            long nextRevision = checked(_revision + 1);
            _phase = _endReason == RUN_END_REASON.NONE ? GAME_PHASE.SHELTER : GAME_PHASE.GAME_OVER;
            _daySummary = null;
            _revision = nextRevision;
            return true;
        }

        private bool ValidateCommand(long expectedRevision, GAME_PHASE requiredPhase, out string rejectionReason)
        {
            rejectionReason = null;
            if ( expectedRevision != _revision )
            {
                rejectionReason = "표시된 상태가 변경되었습니다. 현재 상태를 다시 확인해 주세요.";
                return false;
            }

            if ( _phase != requiredPhase )
            {
                rejectionReason = "지금은 해당 행동을 할 수 없습니다.";
                return false;
            }

            return true;
        }

        private int GetActiveSurvivorIndex()
        {
            int index = _expedition == null ? -1 : Array.FindIndex(SURVIVORS, survivor => survivor.SurvivorId == _expedition.SurvivorId);
            if ( index < 0 || !SURVIVORS[index].IsAlive )
            {
                throw new InvalidOperationException("살아 있는 파견 생존자가 없습니다.");
            }

            return index;
        }

        private RegionDefinition GetActiveRegion()
        {
            for ( int i = 0; i < DEFINITION.Regions.Count; i++ )
            {
                if ( _expedition != null && DEFINITION.Regions[i].Id == _expedition.RegionId )
                {
                    return DEFINITION.Regions[i];
                }
            }

            throw new InvalidOperationException("현재 탐색 지역이 없습니다.");
        }

        private static int GetStatModifier(SurvivorDefinition survivor, STAT_TYPE statType)
        {
            switch ( statType )
            {
                case STAT_TYPE.NONE: return 0;
                case STAT_TYPE.COMBAT: return survivor.CombatModifier;
                case STAT_TYPE.TECH: return survivor.TechModifier;
                default: throw new ArgumentOutOfRangeException(nameof(statType));
            }
        }

        private void EnterStage(int survivorIndex, RegionDefinition region, int stage, int noise, ResourceAmount loot, string previousStageEventId)
        {
            EventDefinition stageEvent = RollStageEvent(region, stage, previousStageEventId);
            var expedition = new ExpeditionState(SURVIVORS[survivorIndex].SurvivorId, region.Id, stage, noise, loot, stageEvent?.Id, previousStageEventId);
            if ( stageEvent != null && stageEvent.Mode == EVENT_MODE.INSTANT )
            {
                PublishReport(survivorIndex, CreateEventReport(survivorIndex, expedition, region, stageEvent, stageEvent.InstantEffect));
            }
            else
            {
                _expedition = expedition;
                _pendingReport = null;
                _phase = stageEvent == null ? GAME_PHASE.SEARCH_CHOICE : GAME_PHASE.EVENT_ACTION;
            }
        }

        private void PublishReport(int survivorIndex, ActionReport report)
        {
            long nextResultId = checked(_nextResultId + 1);
            SURVIVORS[survivorIndex] = report.SurvivorAfter;
            _expedition = report.ExpeditionAfter;
            _pendingReport = report;
            _phase = GAME_PHASE.RESULT;
            _nextResultId = nextResultId;
        }

        private void FinishStage()
        {
            if ( _expedition.Stage == DEFINITION.Rules.MaxStage )
            {
                BeginReturn();
            }
            else
            {
                _pendingReport = null;
                _phase = GAME_PHASE.STAGE_CHOICE;
            }
        }

        private void BeginReturn()
        {
            GetActiveSurvivorIndex();
            ReturnRiskDefinition risk = ExplorationRules.GetReturnRisk(DEFINITION.Rules, _expedition.Noise);
            var expedition = new ExpeditionState(_expedition.SurvivorId, _expedition.RegionId,
                _expedition.Stage, _expedition.Noise, _expedition.Loot, _expedition.StageEventId, _expedition.PreviousStageEventId, risk);
            bool occurs = risk.EventChance == 100 || (risk.EventChance > 0 && RANDOM.NextInt(0, 100) < risk.EventChance);
            if ( occurs )
            {
                _expedition = expedition;
                _pendingReport = null;
                _phase = GAME_PHASE.RETURN_ACTION;
            }
            else
            {
                FinishExpedition(expedition, default);
            }
        }

        private void FinishExpedition(ExpeditionState expedition, ResourceAmount lostLoot)
        {
            long nextResultId = checked(_nextResultId + 1);
            DaySummary summary = SurvivalRules.CalculateDaySummary(_nextResultId, DEFINITION, _day, _resources, SURVIVORS, expedition, lostLoot);
            for ( int i = 0; i < SURVIVORS.Length; i++ )
            {
                SURVIVORS[i] = summary.SurvivorsAfter[i];
            }

            _resources = summary.ResourcesAfter;
            _day = summary.NextDay ?? summary.CompletedDay;
            _endReason = summary.EndReason;
            _daySummary = summary;
            _expedition = null;
            _pendingReport = null;
            _phase = GAME_PHASE.DAY_SUMMARY;
            _nextResultId = nextResultId;
        }

        public bool TryChooseEventAction(long expectedRevision, string eventId, string actionId, out string rejectionReason)
        {
            rejectionReason = null;

            if ( expectedRevision != _revision )
            {
                rejectionReason = "표시된 상태가 변경되었습니다. 현재 상태를 다시 확인해 주세요.";
                return false;
            }

            if ( _phase != GAME_PHASE.EVENT_ACTION || _expedition == null )
            {
                rejectionReason = "지금은 사건 행동을 선택할 수 없습니다.";
                return false;
            }

            if ( string.IsNullOrWhiteSpace(eventId) || eventId != _expedition.StageEventId )
            {
                rejectionReason = "현재 사건에 대한 요청이 아닙니다.";
                return false;
            }

            EventDefinition stageEvent = null;
            for ( int i = 0; i < DEFINITION.Events.Count; i++ )
            {
                if ( DEFINITION.Events[i].Id == eventId )
                {
                    stageEvent = DEFINITION.Events[i];
                    break;
                }
            }

            if ( stageEvent == null || stageEvent.Mode == EVENT_MODE.INSTANT )
            {
                rejectionReason = "행동을 선택할 수 있는 사건이 아닙니다.";
                return false;
            }

            EventActionDefinition action = null;
            for ( int i = 0; i < stageEvent.Actions.Count; i++ )
            {
                if ( stageEvent.Actions[i].Id == actionId )
                {
                    action = stageEvent.Actions[i];
                    break;
                }
            }

            if ( action == null )
            {
                rejectionReason = "현재 사건에서 사용할 수 없는 행동입니다.";
                return false;
            }

            int survivorIndex = Array.FindIndex(SURVIVORS, survivor => survivor.SurvivorId == _expedition.SurvivorId);
            RegionDefinition region = null;
            for ( int i = 0; i < DEFINITION.Regions.Count; i++ )
            {
                if ( DEFINITION.Regions[i].Id == _expedition.RegionId )
                {
                    region = DEFINITION.Regions[i];
                    break;
                }
            }

            if ( survivorIndex < 0 || !SURVIVORS[survivorIndex].IsAlive || region == null )
            {
                throw new InvalidOperationException("현재 탐색의 생존자 또는 지역 상태가 올바르지 않습니다.");
            }

            long nextRevision = checked(_revision + 1);
            long nextResultId = checked(_nextResultId + 1);
            int? dice = null;
            int statModifier = 0;
            EventEffectDefinition effect = action.Effect;

            if ( action.RequiresCheck )
            {
                SurvivorDefinition survivor = DEFINITION.Survivors[survivorIndex];
                switch ( action.StatType )
                {
                    case STAT_TYPE.NONE:
                        break;
                    case STAT_TYPE.COMBAT:
                        statModifier = survivor.CombatModifier;
                        break;
                    case STAT_TYPE.TECH:
                        statModifier = survivor.TechModifier;
                        break;
                    default:
                        throw new InvalidOperationException("지원하지 않는 행동 능력치입니다.");
                }

                dice = RANDOM.NextInt(1, 21);
                bool successful = ExplorationRules.IsActionSuccessful(dice.Value, statModifier, action.Difficulty.Value);
                effect = successful ? action.Effect : action.FailureEffect;
            }

            ActionReport report = CreateEventReport(
                survivorIndex, _expedition, region, stageEvent, effect,
                actionId: action.Id, dice: dice, statModifier: statModifier, difficulty: action.Difficulty);

            SURVIVORS[survivorIndex] = report.SurvivorAfter;
            _expedition = report.ExpeditionAfter;
            _pendingReport = report;
            _phase = GAME_PHASE.RESULT;
            _revision = nextRevision;
            _nextResultId = nextResultId;
            return true;
        }

        private EventDefinition RollStageEvent(RegionDefinition region , int stage , string previousStageEventId)
        {
            StageEventChanceDefinition chance = null;
            for ( int i = 0; i < DEFINITION.Rules.StageEventChances.Count; i++ )
            {
                if ( DEFINITION.Rules.StageEventChances[ i ].Stage == stage )
                {
                    chance = DEFINITION.Rules.StageEventChances[ i ];
                    break;
                }
            }

            if ( chance == null )
            {
                throw new InvalidOperationException($"사건 확률표에 {stage}단계가 없습니다.");
            }

            if ( chance.NoEventChance == 100 )
            {
                return null;
            }

            EVENT_CATEGORY? category;
            if ( chance.OpportunityChance == 100 )
            {
                category = EVENT_CATEGORY.OPPORTUNITY;
            }
            else if ( chance.DangerChance == 100 )
            {
                category = EVENT_CATEGORY.DANGER;
            }
            else
            {
                category = ExplorationRules.CalculateEventCategory(chance , RANDOM.NextInt(0 , 100));
            }

            if ( !category.HasValue )
            {
                return null;
            }

            IReadOnlyList<EventDefinition> candidates = ExplorationRules.GetEventCandidates(
                region, DEFINITION.Events, stage, category.Value, previousStageEventId);

            if ( candidates.Count == 0 )
            {
                System.Diagnostics.Trace.TraceWarning($"{region.Id}: {stage}단계의 {category.Value} 사건 후보가 없습니다.");
                return null;
            }

            return candidates.Count == 1 ? candidates[ 0 ] : candidates[ RANDOM.NextInt(0 , candidates.Count) ];
        }

        private ActionReport CreateEventReport(
            int survivorIndex , ExpeditionState expedition , RegionDefinition region ,
            EventDefinition stageEvent , EventEffectDefinition effect,
            string actionId = null, int? dice = null, int statModifier = 0, int? difficulty = null)
        {
            SurvivorState beforeSurvivor = SURVIVORS[survivorIndex];
            SurvivorDefinition survivorDefinition = DEFINITION.Survivors[survivorIndex];
            EventEffectResult effectResult = ExplorationRules.CalculateEventEffect(
                effect, region.MainResource, beforeSurvivor.CurrentHp, survivorDefinition.MaxHP,
                expedition.Noise, DEFINITION.Rules.MaxNoise, expedition.Loot);

            var afterSurvivor = new SurvivorState(survivorDefinition, effectResult.CurrentHp);
            var afterExpedition = new ExpeditionState(
                expedition.SurvivorId, expedition.RegionId, expedition.Stage, effectResult.Noise,
                effectResult.Loot, expedition.StageEventId, expedition.PreviousStageEventId, expedition.ReturnRisk);

            ResourceAmount reward = afterSurvivor.IsAlive
                ? new ResourceAmount(effectResult.Loot.Food - expedition.Loot.Food,
                    effectResult.Loot.Medicine - expedition.Loot.Medicine, effectResult.Loot.Parts - expedition.Loot.Parts)
                : default;

            RESULT_NEXT_STEP nextStep = afterSurvivor.IsAlive
                ? RESULT_NEXT_STEP.SEARCH_CHOICE
                : RESULT_NEXT_STEP.FINISH_EXPEDITION;

            return new ActionReport(_nextResultId , ACTION_RESULT_TYPE.EVENT , nextStep ,
                beforeSurvivor , afterSurvivor , expedition , afterExpedition , reward , eventId: stageEvent.Id,
                actionId: actionId, dice: dice, statModifier: statModifier, difficulty: difficulty);
        }
    }
}
