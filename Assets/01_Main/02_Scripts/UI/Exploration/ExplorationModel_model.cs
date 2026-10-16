using System;
using LastSupply.Core;

namespace LastSupply.UI.Exploration
{
    public sealed class ExplorationModel_model
    {
        private readonly GameDefinition DEFINITION;

        private GameSnapshot _snapshot;

        public GameSnapshot Snapshot => _snapshot;
        public ExpeditionState Expedition => _snapshot.Expedition;
        public ActionReport Report => _snapshot.PendingReport;
        public DaySummary Summary => _snapshot.DaySummary;
        public ReturnRiskDefinition ReturnRisk => Expedition == null ? null : ExplorationRules.GetReturnRisk(DEFINITION.Rules, Expedition.Noise);
        public ReturnEventDefinition ReturnEvent => DEFINITION.Rules.ReturnEvent;
        public int MaxStage => DEFINITION.Rules.MaxStage;
        public int MaxNoise => DEFINITION.Rules.MaxNoise;

        public SurvivorDefinition Survivor { get; private set; }
        public SurvivorState SurvivorState { get; private set; }
        public RegionDefinition Region { get; private set; }
        public EventDefinition StageEvent { get; private set; }

        public bool CanChooseEventAction => _snapshot.Phase == GAME_PHASE.EVENT_ACTION;
        public bool CanSearch => _snapshot.Phase == GAME_PHASE.SEARCH_CHOICE;
        public bool CanAdvanceStage => _snapshot.Phase == GAME_PHASE.STAGE_CHOICE
            && Expedition != null && Expedition.Stage < MaxStage;
        public bool CanReturn => _snapshot.Phase == GAME_PHASE.STAGE_CHOICE;
        public bool CanResolveReturn => _snapshot.Phase == GAME_PHASE.RETURN_ACTION;
        public bool CanConfirmResult => _snapshot.Phase == GAME_PHASE.RESULT && Report != null;
        public bool CanConfirmDay => _snapshot.Phase == GAME_PHASE.DAY_SUMMARY && Summary != null;

        public ExplorationModel_model(GameDefinition definition, GameSnapshot snapshot)
        {
            DEFINITION = definition ?? throw new ArgumentNullException(nameof(definition));
            ApplySnapshot(snapshot);
        }

        public void ApplySnapshot(GameSnapshot snapshot)
        {
            if ( snapshot == null )
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            SurvivorDefinition survivor = null;
            SurvivorState survivorState = null;
            RegionDefinition region = null;
            EventDefinition stageEvent = null;

            ExpeditionState expedition = snapshot.Expedition;
            if ( expedition != null )
            {
                for ( int i = 0; i < DEFINITION.Survivors.Count; i++ )
                {
                    if ( DEFINITION.Survivors[i].ID == expedition.SurvivorId )
                    {
                        survivor = DEFINITION.Survivors[i];
                        break;
                    }
                }

                for ( int i = 0; i < snapshot.Survivors.Count; i++ )
                {
                    if ( snapshot.Survivors[i].SurvivorId == expedition.SurvivorId )
                    {
                        survivorState = snapshot.Survivors[i];
                        break;
                    }
                }

                for ( int i = 0; i < DEFINITION.Regions.Count; i++ )
                {
                    if ( DEFINITION.Regions[i].Id == expedition.RegionId )
                    {
                        region = DEFINITION.Regions[i];
                        break;
                    }
                }

                if ( expedition.StageEventId != null )
                {
                    for ( int i = 0; i < DEFINITION.Events.Count; i++ )
                    {
                        if ( DEFINITION.Events[i].Id == expedition.StageEventId )
                        {
                            stageEvent = DEFINITION.Events[i];
                            break;
                        }
                    }
                }

                if ( survivor == null || survivorState == null || region == null
                    || (expedition.StageEventId != null && stageEvent == null) )
                {
                    throw new ArgumentException("탐색 상태와 콘텐츠 정의가 일치하지 않습니다.", nameof(snapshot));
                }
            }

            _snapshot = snapshot;
            Survivor = survivor;
            SurvivorState = survivorState;
            Region = region;
            StageEvent = stageEvent;
        }
    }
}
