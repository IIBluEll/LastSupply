using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    internal sealed class CountingRandom : IRandomSource
    {
        private readonly GameRandom RANDOM;
        public int Calls { get; private set; }
        public CountingRandom(int seed) { RANDOM = new GameRandom(seed); }
        public int NextInt(int minInclusive, int maxExclusive)
        {
            Calls++;
            return RANDOM.NextInt(minInclusive, maxExclusive);
        }
    }

    public static class CoreSessionChecks
    {
        private static int s_checkCount;

        private static void Check(bool condition, string message)
        {
            if ( !condition ) throw new Exception(message);
            s_checkCount++;
        }

        private static GameDefinition Fixture(ResourceAmount? resources = null, int target = 8, int days = 7,
            int dailyFood = 2, int starvationDamage = 2, SurvivorDefinition[] survivors = null,
            EventDefinition[] events = null, StageEventChanceDefinition[] chances = null,
            SearchRewardDefinition[] searchRewards = null, RegionDefinition[] regions = null)
        {
            GameDefinition source = DefaultGameData.CreatePrototype();
            if ( survivors == null ) survivors = new[] { source.Survivors[0] };
            if ( events == null )
            {
                events = new[]
                {
                    new EventDefinition("O", "Opportunity", "Opportunity", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5, new EventEffectDefinition(0, 0)),
                    new EventDefinition("D", "Danger", "Danger", EVENT_CATEGORY.DANGER, EVENT_MODE.CHECK, 1, 5, null,
                        new EventActionDefinition("HIT", "Respond", STAT_TYPE.COMBAT, 12, new EventEffectDefinition(0, 0), new EventEffectDefinition(-3, 2)))
                };
            }

            if ( chances == null ) chances = Chances(100, 0, 0);
            if ( regions == null )
            {
                var ids = new string[events.Length];
                for ( int i = 0; i < ids.Length; i++ ) ids[i] = events[i].Id;
                regions = new[] { new RegionDefinition("R", "Region", RESOURCE_TYPE.PARTS, ids) };
            }

            var rules = new GameRulesDefinition(5, 6, days, resources ?? new ResourceAmount(6, 1, 0),
                target, dailyFood, starvationDamage, 1, 3,
                searchRewards ?? new List<SearchRewardDefinition>(source.Rules.DefaultSearchRewards).ToArray(),
                chances, new List<ReturnRiskDefinition>(source.Rules.ReturnRisks).ToArray(), source.Rules.ReturnEvent);
            return new GameDefinition(rules, survivors, regions, events);
        }

        private static StageEventChanceDefinition[] Chances(int none, int opportunity, int danger)
        {
            var rows = new StageEventChanceDefinition[5];
            for ( int i = 0; i < rows.Length; i++ ) rows[i] = new StageEventChanceDefinition(i + 1, none, opportunity, danger);
            return rows;
        }

        private static EventDefinition[] NoiseEvents(int noise)
        {
            return new[]
            {
                new EventDefinition("O", "Noise", "Noise and loot", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5,
                    new EventEffectDefinition(0, noise, new SearchRewardItemDefinition(2))),
                new EventDefinition("D", "Danger", "Danger", EVENT_CATEGORY.DANGER, EVENT_MODE.INSTANT, 1, 5, new EventEffectDefinition(0, 0))
            };
        }

        private static void Start(GameSession session, GameDefinition data, string survivorId = null)
        {
            GameSnapshot state = session.GetSnapshot();
            string reason;
            Check(session.TryStartExpedition(state.Revision, survivorId ?? data.Survivors[0].ID, data.Regions[0].Id, out reason), "Start: " + reason);
        }

        private static void Confirm(GameSession session)
        {
            GameSnapshot state = session.GetSnapshot();
            string reason;
            Check(session.TryConfirmResult(state.Revision, state.PendingReport.Id, out reason), "Confirm: " + reason);
        }

        private static void Skip(GameSession session)
        {
            string reason;
            Check(session.TrySkipSearch(session.GetSnapshot().Revision, out reason), "Skip: " + reason);
        }

        private static void Advance(GameSession session)
        {
            string reason;
            Check(session.TryAdvanceStage(session.GetSnapshot().Revision, out reason), "Advance: " + reason);
        }

        private static void Return(GameSession session)
        {
            string reason;
            Check(session.TryReturn(session.GetSnapshot().Revision, out reason), "Return: " + reason);
        }

        private static void ConfirmSummary(GameSession session)
        {
            GameSnapshot state = session.GetSnapshot();
            string reason;
            Check(session.TryConfirmDaySummary(state.Revision, state.DaySummary.Id, out reason), "Summary: " + reason);
        }

        private static void AssertSame(GameSnapshot before, GameSnapshot after)
        {
            Check(before.Revision == after.Revision && before.Day == after.Day && before.Phase == after.Phase &&
                before.EndReason == after.EndReason && before.Resources.Food == after.Resources.Food &&
                before.Resources.Medicine == after.Resources.Medicine && before.Resources.Parts == after.Resources.Parts &&
                ReferenceEquals(before.Expedition, after.Expedition) && ReferenceEquals(before.PendingReport, after.PendingReport) &&
                ReferenceEquals(before.DaySummary, after.DaySummary), "Rejected command changed state");
            for ( int i = 0; i < before.Survivors.Count; i++ )
            {
                Check(ReferenceEquals(before.Survivors[i], after.Survivors[i]), "Rejected command changed survivor");
            }
        }

        private static void CheckInvalidCommands(GameSession session, Func<int> callCount)
        {
            GameSnapshot state = session.GetSnapshot();
            int calls = callCount();
            string reason;
            Check(!session.TryStartExpedition(state.Revision, "MISSING", "MISSING", out reason), "Invalid start accepted");
            Check(!session.TryChooseEventAction(state.Revision, "MISSING", "MISSING", out reason), "Invalid action accepted");
            Check(!session.TryResolveReturn(state.Revision, "MISSING", "MISSING", out reason), "Invalid return action accepted");
            Check(!session.TryHeal(state.Revision, "MISSING", out reason), "Invalid heal accepted");
            Check(!session.TryConfirmResult(state.Revision, -1, out reason), "Invalid result accepted");
            Check(!session.TryConfirmDaySummary(state.Revision, -1, out reason), "Invalid summary accepted");
            Check(!session.TrySearch(state.Revision - 1, out reason), "Stale revision accepted");
            if ( state.Phase != GAME_PHASE.SEARCH_CHOICE )
            {
                Check(!session.TrySearch(state.Revision, out reason), "Search in wrong phase");
                Check(!session.TrySkipSearch(state.Revision, out reason), "Skip in wrong phase");
            }
            if ( state.Phase != GAME_PHASE.STAGE_CHOICE )
            {
                Check(!session.TryAdvanceStage(state.Revision, out reason), "Advance in wrong phase");
                Check(!session.TryReturn(state.Revision, out reason), "Return in wrong phase");
            }

            Check(calls == callCount(), "Rejected input consumed random");
            AssertSame(state, session.GetSnapshot());
        }

        private static void MixedSearchAndSummary()
        {
            var row = new SearchRewardDefinition(1, 5, 1, 20, new SearchRewardItemDefinition(1),
                new SearchRewardItemDefinition(2, RESOURCE_TYPE.FOOD), new SearchRewardItemDefinition(1, RESOURCE_TYPE.MEDICINE));
            var region = new RegionDefinition("R", "Mixed", RESOURCE_TYPE.PARTS, new[] { "O", "D" }, new[] { row });
            GameDefinition data = Fixture(regions: new[] { region });
            var rng = new SequenceRandom(20);
            var session = new GameSession(data, rng);
            Start(session, data);
            GameSnapshot before = session.GetSnapshot();
            string reason;
            Check(session.TrySearch(before.Revision, out reason), "Mixed search rejected");
            GameSnapshot result = session.GetSnapshot();
            Check(result.PendingReport.Dice == 20 && result.PendingReport.IsSuccessful == null &&
                result.Expedition.Loot.Food == 2 && result.Expedition.Loot.Medicine == 1 && result.Expedition.Loot.Parts == 1,
                "Mixed bundle or search check metadata incorrect");
            Check(result.Resources.Parts == 0 && before.Expedition.Loot.IsEmpty && rng.Calls == 1, "Search deposited early or changed old snapshot");
            CheckInvalidCommands(session, () => rng.Calls);
            Confirm(session);
            Check(!session.TryConfirmResult(session.GetSnapshot().Revision, result.PendingReport.Id, out reason), "Result confirmed twice");
            Return(session);
            GameSnapshot summary = session.GetSnapshot();
            Check(summary.Phase == GAME_PHASE.DAY_SUMMARY && summary.Expedition == null && summary.PendingReport == null, "Exploration not cleared");
            Check(summary.Day == 2 && summary.DaySummary.CompletedDay == 1 && summary.DaySummary.NextDay == 2, "Summary day mismatch");
            Check(summary.Resources.Food == 6 && summary.Resources.Medicine == 2 && summary.Resources.Parts == 1 &&
                summary.DaySummary.RecoveredLoot.Food == 2 && summary.DaySummary.FoodConsumed == 2, "Bundle settlement mismatch");
            CheckInvalidCommands(session, () => rng.Calls);
            ConfirmSummary(session);
            Check(session.GetSnapshot().Phase == GAME_PHASE.SHELTER && session.GetSnapshot().Resources.Parts == 1 && rng.Calls == 1, "Summary reconfirmed payment");
            Check(!session.TryConfirmDaySummary(session.GetSnapshot().Revision, summary.DaySummary.Id, out reason), "Summary accepted twice");
            Check(summary.DaySummary.ResourcesAfter.Parts == 1 && result.Expedition.Loot.Parts == 1, "Old records mutated");

            var missRandom = new SequenceRandom(1);
            var miss = new GameSession(Fixture(), missRandom);
            Start(miss, Fixture());
            Check(miss.TrySearch(miss.GetSnapshot().Revision, out reason) && miss.GetSnapshot().PendingReport.Reward.IsEmpty &&
                miss.GetSnapshot().Expedition.Noise == 0, "Miss reward or search cost");
        }

        private static void LastStage()
        {
            GameDefinition data = Fixture();
            var rng = new SequenceRandom();
            var skip = new GameSession(data, rng);
            Start(skip, data);
            for ( int stage = 1; stage <= 5; stage++ )
            {
                Check(skip.GetSnapshot().Phase == GAME_PHASE.SEARCH_CHOICE && skip.GetSnapshot().Expedition.Stage == stage, "Last stage skipped search");
                Skip(skip);
                if ( stage < 5 ) Advance(skip);
            }
            Check(skip.GetSnapshot().Phase == GAME_PHASE.DAY_SUMMARY && skip.GetSnapshot().DaySummary.Expedition.Stage == 5 && rng.Calls == 0, "Last skip did not return safely");
            CheckInvalidCommands(skip, () => rng.Calls);

            var searchRandom = new SequenceRandom(20);
            var search = new GameSession(data, searchRandom);
            Start(search, data);
            for ( int i = 1; i < 5; i++ ) { Skip(search); Advance(search); }
            string reason;
            Check(search.TrySearch(search.GetSnapshot().Revision, out reason), "Last search rejected");
            Check(search.GetSnapshot().Phase == GAME_PHASE.RESULT && search.GetSnapshot().Expedition.Stage == 5, "Last search auto-confirmed");
            Confirm(search);
            Check(search.GetSnapshot().Phase == GAME_PHASE.DAY_SUMMARY && search.GetSnapshot().Resources.Parts == 5 && searchRandom.Calls == 1, "Last search return mismatch");
        }

        private static void ReturnRisk()
        {
            for ( int noise = 0; noise <= 5; noise++ )
            {
                int chance = noise <= 2 ? 0 : noise == 3 ? 30 : noise == 4 ? 50 : 75;
                GameDefinition boundaryData = Fixture(events: NoiseEvents(noise), chances: Chances(0, 100, 0));
                var boundaryRandom = chance == 0 ? new SequenceRandom() : new SequenceRandom(chance);
                var boundary = new GameSession(boundaryData, boundaryRandom);
                Start(boundary, boundaryData); Confirm(boundary); Skip(boundary); Return(boundary);
                Check(boundary.GetSnapshot().Phase == GAME_PHASE.DAY_SUMMARY && boundaryRandom.Calls == (chance == 0 ? 0 : 1),
                    "Safe return or exact probability boundary mismatch");
                if ( chance > 0 )
                {
                    var occurrenceRandom = new SequenceRandom(chance - 1, 1);
                    var occurrence = new GameSession(boundaryData, occurrenceRandom);
                    Start(occurrence, boundaryData); Confirm(occurrence); Skip(occurrence); Return(occurrence);
                    Check(occurrence.GetSnapshot().Phase == GAME_PHASE.RETURN_ACTION, "Occurrence lower boundary mismatch");
                    string reason;
                    Check(occurrence.TryResolveReturn(occurrence.GetSnapshot().Revision, "RETURN_001", "BREAK_THROUGH", out reason), "Boundary return response");
                    Check(occurrence.GetSnapshot().Survivors[0].CurrentHp == (noise == 5 ? 7 : 8) && occurrenceRandom.Calls == 2,
                        "Noise-specific failure damage mismatch");
                }
            }

            foreach ( int sample in new[] { 29, 30 } )
            {
                GameDefinition data = Fixture(events: NoiseEvents(3), chances: Chances(0, 100, 0));
                var rng = sample == 29 ? new SequenceRandom(29, 1) : new SequenceRandom(30);
                var session = new GameSession(data, rng);
                Start(session, data);
                Confirm(session);
                Skip(session);
                Return(session);
                if ( sample == 29 )
                {
                    Check(session.GetSnapshot().Phase == GAME_PHASE.RETURN_ACTION && session.GetSnapshot().Expedition.ReturnRisk.FailureDamage == 2, "30 percent boundary occurrence");
                    CheckInvalidCommands(session, () => rng.Calls);
                    string reason;
                    Check(session.TryResolveReturn(session.GetSnapshot().Revision, "RETURN_001", "BREAK_THROUGH", out reason), "Return response rejected");
                    GameSnapshot response = session.GetSnapshot();
                    Check(response.PendingReport.IsSuccessful == false && response.Survivors[0].CurrentHp == 8 &&
                        response.Expedition.Noise == 3 && response.Expedition.Loot.Parts == 2, "Return failure should survive with loot");
                    CheckInvalidCommands(session, () => rng.Calls);
                    Confirm(session);
                }
                Check(session.GetSnapshot().Phase == GAME_PHASE.DAY_SUMMARY && session.GetSnapshot().Resources.Parts == 2 &&
                    rng.Calls == (sample == 29 ? 2 : 1), "Return re-rolled or discarded survivor loot");
            }

            foreach ( bool success in new[] { true, false } )
            {
                GameDefinition data = Fixture(events: NoiseEvents(6), chances: Chances(0, 100, 0));
                var rng = new SequenceRandom(success ? 8 : 1);
                var session = new GameSession(data, rng);
                Start(session, data); Confirm(session); Skip(session); Return(session);
                Check(rng.Calls == 0 && session.GetSnapshot().Phase == GAME_PHASE.RETURN_ACTION, "100 percent occurrence consumed random");
                string reason;
                Check(session.TryResolveReturn(session.GetSnapshot().Revision, "RETURN_001", "BREAK_THROUGH", out reason), "Certain return response");
                Check(session.GetSnapshot().Survivors[0].CurrentHp == (success ? 10 : 7), "Certain occurrence confused with failure");
                Check(session.GetSnapshot().Expedition.Noise == 6 && rng.Calls == 1, "Return added noise or re-rolled");
                Confirm(session);
                Check(session.GetSnapshot().Resources.Parts == 2, "Surviving risky return lost loot");
            }

            foreach ( bool secondSurvivor in new[] { false, true } )
            {
                var survivors = secondSurvivor
                    ? new[] { new SurvivorDefinition("S", "S", 3, 0, 0), new SurvivorDefinition("OTHER", "Other", 10, 0, 0) }
                    : new[] { new SurvivorDefinition("S", "S", 3, 0, 0) };
                GameDefinition data = Fixture(target: 1, survivors: survivors, events: NoiseEvents(6), chances: Chances(0, 100, 0));
                var rng = new SequenceRandom(1);
                var session = new GameSession(data, rng);
                Start(session, data); Confirm(session); Skip(session); Return(session);
                string reason;
                Check(session.TryResolveReturn(session.GetSnapshot().Revision, "RETURN_001", "BREAK_THROUGH", out reason), "Lethal return");
                GameSnapshot death = session.GetSnapshot();
                Check(death.Survivors[0].CurrentHp == 0 && death.Expedition.Loot.IsEmpty && death.PendingReport.ExpeditionBefore.Loot.Parts == 2, "Death did not preserve lost-loot record");
                Confirm(session);
                GameSnapshot summary = session.GetSnapshot();
                Check(summary.DaySummary.LostLoot.Parts == 2 && summary.Resources.Parts == 0 && summary.DaySummary.RecoveredLoot.IsEmpty, "Death deposited loot");
                Check(summary.EndReason == (secondSurvivor ? RUN_END_REASON.NONE : RUN_END_REASON.ALL_DEAD), "First death ended surviving team");
                Check(summary.DaySummary.DiedSurvivorIds.Count == 1 && summary.DaySummary.DiedSurvivorIds[0] == "S", "Death absent from summary");
                if ( secondSurvivor )
                {
                    ConfirmSummary(session);
                    Check(!session.TryHeal(session.GetSnapshot().Revision, "S", out reason), "Dead survivor healed");
                    Check(!session.TryStartExpedition(session.GetSnapshot().Revision, "S", "R", out reason), "Dead survivor dispatched");
                }
                Check(rng.Calls == 1, "Lethal return consumed additional random");
            }
        }

        private static void EventDeathAndPreviousEvent()
        {
            EventDefinition[] events = NoiseEvents(0);
            var chances = Chances(100, 0, 0);
            chances[0] = new StageEventChanceDefinition(1, 0, 100, 0);
            chances[1] = new StageEventChanceDefinition(2, 0, 0, 100);
            events[1] = new EventDefinition("D", "D", "D", EVENT_CATEGORY.DANGER, EVENT_MODE.CHECK, 1, 5, null,
                new EventActionDefinition("HIT", "Hit", STAT_TYPE.NONE, 12, new EventEffectDefinition(0, 0), new EventEffectDefinition(-3, 2)));
            GameDefinition data = Fixture(survivors: new[] { new SurvivorDefinition("S", "S", 3, 0, 0) }, events: events, chances: chances);
            var rng = new SequenceRandom(1);
            var session = new GameSession(data, rng);
            Start(session, data); Confirm(session); Skip(session); Advance(session);
            string reason;
            Check(session.TryChooseEventAction(session.GetSnapshot().Revision, "D", "HIT", out reason), "Lethal stage action");
            Check(session.GetSnapshot().PendingReport.ExpeditionBefore.Loot.Parts == 2, "Stage death forgot carried loot");
            CheckInvalidCommands(session, () => rng.Calls);
            Confirm(session);
            Check(session.GetSnapshot().EndReason == RUN_END_REASON.ALL_DEAD && session.GetSnapshot().DaySummary.LostLoot.Parts == 2 &&
                session.GetSnapshot().DaySummary.FoodConsumed == 0 && rng.Calls == 1, "Stage death continued search/return/food");

            var opportunity1 = new EventDefinition("O1", "O1", "O1", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5, new EventEffectDefinition(0, 0));
            var opportunity2 = new EventDefinition("O2", "O2", "O2", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5, new EventEffectDefinition(0, 0));
            chances = Chances(0, 100, 0);
            chances[2] = new StageEventChanceDefinition(3, 100, 0, 0);
            data = Fixture(events: new[] { opportunity1, opportunity2, events[1] }, chances: chances);
            var previousRandom = new SequenceRandom(1, 0);
            var previous = new GameSession(data, previousRandom);
            Start(previous, data);
            Check(previous.GetSnapshot().Expedition.StageEventId == "O2", "Stage-one selection");
            Confirm(previous); Skip(previous); Advance(previous);
            Check(previous.GetSnapshot().Expedition.StageEventId == "O1" &&
                previous.GetSnapshot().Expedition.PreviousStageEventId == "O2" && previousRandom.Calls == 1, "Previous duplicate/single candidate");
            Confirm(previous); Skip(previous); Advance(previous);
            Check(previous.GetSnapshot().Expedition.StageEventId == null, "No-event stage");
            Skip(previous); Advance(previous);
            Check(previous.GetSnapshot().Expedition.StageEventId == "O1" &&
                previous.GetSnapshot().Expedition.PreviousStageEventId == null && previousRandom.Calls == 2, "Previous event not reset after no event");
        }

        private static void SurvivalAndHealing()
        {
            GameDefinition timeoutData = Fixture(resources: new ResourceAmount(0, 0, 0), dailyFood: 0);
            var rng = new SequenceRandom();
            var timeout = new GameSession(timeoutData, rng);
            for ( int day = 1; day <= 7; day++ )
            {
                Start(timeout, timeoutData); Skip(timeout); Return(timeout);
                GameSnapshot summary = timeout.GetSnapshot();
                Check(summary.DaySummary.CompletedDay == day && summary.DaySummary.EndReason == (day == 7 ? RUN_END_REASON.TIME_EXPIRED : RUN_END_REASON.NONE), "Deadline result");
                Check(summary.DaySummary.FoodConsumed == 0 && !summary.DaySummary.FoodShortage, "Zero food consumption caused hunger");
                CheckInvalidCommands(timeout, () => rng.Calls);
                ConfirmSummary(timeout);
            }
            Check(timeout.GetSnapshot().Phase == GAME_PHASE.GAME_OVER && timeout.GetSnapshot().Day == 7, "Deadline did not end game");
            CheckInvalidCommands(timeout, () => rng.Calls);

            var singleReward = new[] { new SearchRewardDefinition(1, 5, 1, 20, new SearchRewardItemDefinition(1)) };
            var fragile = new[] { new SurvivorDefinition("S", "S", 2, 0, 0) };
            GameDefinition missedGoalData = Fixture(resources: new ResourceAmount(0, 0, 0), days: 1, survivors: fragile);
            var missedGoal = new GameSession(missedGoalData, new SequenceRandom());
            Start(missedGoal, missedGoalData); Skip(missedGoal); Return(missedGoal);
            Check(missedGoal.GetSnapshot().EndReason == RUN_END_REASON.TIME_EXPIRED &&
                missedGoal.GetSnapshot().Survivors[0].CurrentHp == 2 && !missedGoal.GetSnapshot().DaySummary.FoodShortage,
                "Deadline applied starvation before ending");

            var deadState = new SurvivorState(fragile[0], 0);
            DaySummary deadPriority = SurvivalRules.CalculateDaySummary(1, missedGoalData, 1, new ResourceAmount(0, 0, 8),
                new[] { deadState }, new ExpeditionState("S", "R", 1, 0, default), default);
            Check(deadPriority.EndReason == RUN_END_REASON.ALL_DEAD, "Victory took precedence over all-dead");

            GameDefinition lastDayData = Fixture(resources: new ResourceAmount(12, 0, 0), target: 1, survivors: fragile, searchRewards: singleReward);
            var lastRandom = new SequenceRandom(20);
            var lastDay = new GameSession(lastDayData, lastRandom);
            for ( int day = 1; day <= 7; day++ )
            {
                Start(lastDay, lastDayData);
                if ( day < 7 ) Skip(lastDay);
                else
                {
                    string reason;
                    Check(lastDay.TrySearch(lastDay.GetSnapshot().Revision, out reason), "Last-day goal search");
                    Confirm(lastDay);
                }
                Return(lastDay);
                if ( day < 7 ) ConfirmSummary(lastDay);
            }
            Check(lastDay.GetSnapshot().EndReason == RUN_END_REASON.RESCUED && lastDay.GetSnapshot().Resources.Parts == 1 &&
                lastDay.GetSnapshot().Survivors[0].CurrentHp == 2 && lastDay.GetSnapshot().DaySummary.FoodConsumed == 0, "Last-day victory lost to deadline/hunger");
            ConfirmSummary(lastDay);
            Check(lastDay.GetSnapshot().Phase == GAME_PHASE.GAME_OVER, "Victory summary did not end");

            GameDefinition earlyData = Fixture(resources: new ResourceAmount(0, 0, 0), target: 1, survivors: fragile, searchRewards: singleReward);
            var early = new GameSession(earlyData, new SequenceRandom(1));
            Start(early, earlyData);
            string rejectedReason;
            Check(early.TrySearch(early.GetSnapshot().Revision, out rejectedReason), "Early search");
            Confirm(early); Return(early);
            Check(early.GetSnapshot().EndReason == RUN_END_REASON.RESCUED && early.GetSnapshot().DaySummary.NextDay == null &&
                early.GetSnapshot().Survivors[0].CurrentHp == 2, "Early victory consumed food");

            var hungrySurvivors = new[] { new SurvivorDefinition("S", "S", 2, 0, 0), new SurvivorDefinition("OTHER", "Other", 1, 0, 0) };
            GameDefinition hungryData = Fixture(resources: new ResourceAmount(1, 0, 0), survivors: hungrySurvivors);
            var hungry = new GameSession(hungryData, new SequenceRandom());
            Start(hungry, hungryData); Skip(hungry); Return(hungry);
            DaySummary hunger = hungry.GetSnapshot().DaySummary;
            Check(hunger.FoodConsumed == 1 && hunger.FoodShortage && hunger.StarvationDamage == 2 && hunger.EndReason == RUN_END_REASON.ALL_DEAD &&
                hunger.NextDay == null && hunger.DiedSurvivorIds.Count == 2 && hunger.ResourcesAfter.Food == 0, "Starvation all-dead order/damage");
            Check(hunger.SurvivorsBefore[0].CurrentHp == 2 && hunger.SurvivorsAfter[0].CurrentHp == 0, "Starvation before state changed");

            var wound = new EventDefinition("O", "Wound", "Wound", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5, new EventEffectDefinition(-5, 0));
            var danger = NoiseEvents(0)[1];
            foreach ( int medicine in new[] { 0, 2 } )
            {
                GameDefinition healData = Fixture(resources: new ResourceAmount(6, medicine, 0), events: new[] { wound, danger }, chances: Chances(0, 100, 0));
                var healRandom = new SequenceRandom();
                var heal = new GameSession(healData, healRandom);
                Start(heal, healData); Confirm(heal); Skip(heal); Return(heal); ConfirmSummary(heal);
                GameSnapshot before = heal.GetSnapshot();
                if ( medicine == 0 )
                {
                    Check(!heal.TryHeal(before.Revision, healData.Survivors[0].ID, out rejectedReason), "Heal without medicine");
                    AssertSame(before, heal.GetSnapshot());
                }
                else
                {
                    Check(heal.TryHeal(before.Revision, healData.Survivors[0].ID, out rejectedReason), "First heal");
                    Check(heal.GetSnapshot().Survivors[0].CurrentHp == 8 && heal.GetSnapshot().Resources.Medicine == 1, "First healing values");
                    Check(!heal.TryHeal(before.Revision, healData.Survivors[0].ID, out rejectedReason), "Duplicate heal");
                    Check(heal.TryHeal(heal.GetSnapshot().Revision, healData.Survivors[0].ID, out rejectedReason), "Second heal");
                    Check(heal.GetSnapshot().Survivors[0].CurrentHp == 10 && heal.GetSnapshot().Resources.Medicine == 0, "Healing cap/cost");
                    GameSnapshot full = heal.GetSnapshot();
                    Check(!heal.TryHeal(full.Revision, healData.Survivors[0].ID, out rejectedReason), "Full-health heal accepted");
                    AssertSame(full, heal.GetSnapshot());
                }
                Check(healRandom.Calls == 0, "Healing consumed game random");
            }

            var extreme = new SurvivorDefinition("S", "S", 10, int.MaxValue, 0);
            GameDefinition extremeData = Fixture(survivors: new[] { extreme }, chances: Chances(0, 0, 100));
            var extremeSession = new GameSession(extremeData, new SequenceRandom(1));
            Start(extremeSession, extremeData);
            Check(extremeSession.TryChooseEventAction(extremeSession.GetSnapshot().Revision, "D", "HIT", out rejectedReason), "Large modifier action");
            Check(extremeSession.GetSnapshot().PendingReport.Total == (long)int.MaxValue + 1 &&
                extremeSession.GetSnapshot().PendingReport.IsSuccessful == true, "Action total overflowed");
        }

        private static void FullGames()
        {
            GameDefinition data = DefaultGameData.CreateDefault();
            for ( int seed = 0; seed < 100; seed++ )
            {
                var random = new CountingRandom(seed);
                var policy = new Random(seed + 1000);
                var session = new GameSession(data, random);
                int commands = 0;
                while ( session.GetSnapshot().Phase != GAME_PHASE.GAME_OVER )
                {
                    Check(++commands < 500, "Game failed to terminate");
                    GameSnapshot state = session.GetSnapshot();
                    CheckInvalidCommands(session, () => random.Calls);
                    Check(state.Day >= 1 && state.Day <= data.Rules.MaxDays && state.Resources.Food >= 0 &&
                        state.Resources.Medicine >= 0 && state.Resources.Parts >= 0, "State resource/day bounds");
                    for ( int i = 0; i < state.Survivors.Count; i++ )
                        Check(state.Survivors[i].CurrentHp >= 0 && state.Survivors[i].CurrentHp <= data.Survivors[i].MaxHP, "HP bounds");
                    if ( state.Expedition != null )
                        Check(state.Expedition.Stage >= 1 && state.Expedition.Stage <= 5 && state.Expedition.Noise >= 0 && state.Expedition.Noise <= 6, "Expedition bounds");

                    string reason;
                    bool accepted;
                    switch ( state.Phase )
                    {
                        case GAME_PHASE.SHELTER:
                            var living = new List<int>();
                            int healIndex = -1;
                            for ( int i = 0; i < state.Survivors.Count; i++ )
                            {
                                if ( state.Survivors[i].IsAlive )
                                {
                                    living.Add(i);
                                    if ( state.Survivors[i].CurrentHp < data.Survivors[i].MaxHP ) healIndex = i;
                                }
                            }
                            if ( state.Resources.Medicine > 0 && healIndex >= 0 )
                                accepted = session.TryHeal(state.Revision, state.Survivors[healIndex].SurvivorId, out reason);
                            else
                                accepted = session.TryStartExpedition(state.Revision, state.Survivors[living[policy.Next(living.Count)]].SurvivorId,
                                    data.Regions[policy.Next(data.Regions.Count)].Id, out reason);
                            break;
                        case GAME_PHASE.EVENT_ACTION:
                            EventDefinition current = null;
                            for ( int i = 0; i < data.Events.Count; i++ )
                                if ( data.Events[i].Id == state.Expedition.StageEventId ) current = data.Events[i];
                            accepted = session.TryChooseEventAction(state.Revision, current.Id, current.Actions[policy.Next(current.Actions.Count)].Id, out reason);
                            break;
                        case GAME_PHASE.SEARCH_CHOICE:
                            accepted = policy.Next(4) == 0 ? session.TrySkipSearch(state.Revision, out reason) : session.TrySearch(state.Revision, out reason);
                            break;
                        case GAME_PHASE.STAGE_CHOICE:
                            accepted = policy.Next(3) == 0 ? session.TryReturn(state.Revision, out reason) : session.TryAdvanceStage(state.Revision, out reason);
                            break;
                        case GAME_PHASE.RETURN_ACTION:
                            accepted = session.TryResolveReturn(state.Revision, data.Rules.ReturnEvent.Id, data.Rules.ReturnEvent.ActionId, out reason);
                            break;
                        case GAME_PHASE.RESULT:
                            Check(state.PendingReport != null, "Missing result");
                            accepted = session.TryConfirmResult(state.Revision, state.PendingReport.Id, out reason);
                            break;
                        case GAME_PHASE.DAY_SUMMARY:
                            Check(state.Expedition == null && state.PendingReport == null && state.DaySummary != null, "Summary has active expedition");
                            accepted = session.TryConfirmDaySummary(state.Revision, state.DaySummary.Id, out reason);
                            break;
                        default:
                            throw new Exception("Unexpected phase");
                    }
                    Check(accepted && reason == null && session.GetSnapshot().Revision == state.Revision + 1, "Valid command rejected/revision mismatch");
                }

                GameSnapshot ended = session.GetSnapshot();
                Check(ended.EndReason != RUN_END_REASON.NONE && ended.Expedition == null && ended.DaySummary == null, "Game-over state");
                CheckInvalidCommands(session, () => random.Calls);
                var restarted = new GameSession(data, new CountingRandom(seed));
                GameSnapshot fresh = restarted.GetSnapshot();
                Check(fresh.Day == 1 && fresh.Revision == 0 && fresh.Phase == GAME_PHASE.SHELTER &&
                    fresh.EndReason == RUN_END_REASON.NONE && fresh.Resources.Food == 6 && fresh.PendingReport == null &&
                    fresh.Expedition == null && fresh.DaySummary == null, "Fresh session kept old game state");
                for ( int i = 0; i < fresh.Survivors.Count; i++ )
                    Check(fresh.Survivors[i].CurrentHp == data.Survivors[i].MaxHP, "Restart kept damaged HP");
                Check(session.GetSnapshot().EndReason == ended.EndReason, "Restart changed old session");
            }
        }

        public static string Run()
        {
            MixedSearchAndSummary();
            LastStage();
            ReturnRisk();
            EventDeathAndPreviousEvent();
            SurvivalAndHealing();
            FullGames();
            return "Session flow checks passed: " + s_checkCount + " assertions, including 100 complete seeded games and fresh-session restart.";
        }
    }
}
