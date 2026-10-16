using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    internal sealed class SequenceRandom : IRandomSource
    {
        private readonly Queue<int> VALUES;
        public int Calls { get; private set; }
        public string LastRange { get; private set; }
        public SequenceRandom(params int[] values) { VALUES = new Queue<int>(values); }
        public int NextInt(int minInclusive, int maxExclusive)
        {
            Calls++;
            LastRange = minInclusive + ":" + maxExclusive;
            if (VALUES.Count == 0) throw new Exception("Unexpected random consumption");
            int value = VALUES.Dequeue();
            if (value < minInclusive || value >= maxExclusive) throw new Exception("Unexpected range");
            return value;
        }
    }
    public static class SessionEntryChecks
    {
        private static int s_checkCount;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); s_checkCount++; }
        private static GameRulesDefinition CopyRules(GameRulesDefinition source, StageEventChanceDefinition[] chances)
        {
            return new GameRulesDefinition(source.MaxStage, source.MaxNoise, source.MaxDays, source.InitialResources,
                source.TargetParts, source.DailyFoodConsumption, source.StarvationDamage, source.HealingMedicineCost,
                source.HealingAmount, new List<SearchRewardDefinition>(source.DefaultSearchRewards).ToArray(),
                chances, new List<ReturnRiskDefinition>(source.ReturnRisks).ToArray(), source.ReturnEvent);
        }
        public static string Run()
        {
            var data = DefaultGameData.CreatePrototype();
            string id = data.Survivors[0].ID;
            string regionId = data.Regions[0].Id;
            var rng = new SequenceRandom();
            var invalid = new GameSession(data, rng);
            var initial = invalid.GetSnapshot();
            Check(initial.Day == 1 && initial.Phase == GAME_PHASE.SHELTER && initial.Revision == 0, "Initial phase");
            Check(initial.Resources.Food == 6 && initial.Resources.Medicine == 1 && initial.Resources.Parts == 0, "Initial resources");
            Check(initial.Survivors.Count == 1 && initial.Survivors[0].CurrentHp == 10 && initial.Expedition == null && initial.PendingReport == null, "Initial state");
            string reason;
            Check(!invalid.TryStartExpedition(1, id, regionId, out reason) && reason != null, "Stale revision");
            Check(!invalid.TryStartExpedition(0, null, regionId, out reason), "Null survivor");
            Check(!invalid.TryStartExpedition(0, "MISSING", regionId, out reason), "Missing survivor");
            Check(!invalid.TryStartExpedition(0, id, null, out reason), "Null region");
            Check(!invalid.TryStartExpedition(0, id, "MISSING", out reason), "Missing region");
            Check(rng.Calls == 0 && invalid.GetSnapshot().Revision == 0 && invalid.GetSnapshot().Expedition == null, "Rejected mutation");

            var noneRng = new SequenceRandom(0);
            var none = new GameSession(data, noneRng);
            Check(none.TryStartExpedition(0, id, regionId, out reason) && reason == null, "None start");
            var started = none.GetSnapshot();
            Check(started.Phase == GAME_PHASE.SEARCH_CHOICE && started.Revision == 1 && started.Expedition.Stage == 1, "None phase");
            Check(started.Expedition.Noise == 0 && started.Expedition.Loot.IsEmpty && started.Expedition.StageEventId == null && started.Expedition.PreviousStageEventId == null, "Start state");
            Check(noneRng.Calls == 1 && noneRng.LastRange == "0:100", "None RNG");
            Check(!none.TryStartExpedition(0, id, regionId, out reason), "Stale duplicate");
            Check(!none.TryStartExpedition(1, id, regionId, out reason) && noneRng.Calls == 1, "Phase duplicate");

            var checkRng = new SequenceRandom(99);
            var forced = new GameSession(data, checkRng);
            Check(forced.TryStartExpedition(0, id, regionId, out reason), "Check start");
            Check(forced.GetSnapshot().Phase == GAME_PHASE.EVENT_ACTION && forced.GetSnapshot().Expedition.StageEventId == "EVENT_002", "Check skipped");
            Check(forced.GetSnapshot().PendingReport == null && checkRng.Calls == 1, "Check action rolled early");

            var choiceRng = new SequenceRandom(65, 0);
            var choice = new GameSession(data, choiceRng);
            Check(choice.TryStartExpedition(0, id, regionId, out reason), "Choice start");
            Check(choice.GetSnapshot().Phase == GAME_PHASE.EVENT_ACTION && choice.GetSnapshot().Expedition.StageEventId == "EVENT_001", "Choice skipped");
            Check(choiceRng.Calls == 2 && choiceRng.LastRange == "0:2", "Selection range");

            var instantRng = new SequenceRandom(65, 1);
            var instant = new GameSession(data, instantRng);
            var old = instant.GetSnapshot();
            Check(instant.TryStartExpedition(0, id, regionId, out reason), "Instant start");
            var next = instant.GetSnapshot();
            Check(next.Phase == GAME_PHASE.RESULT && next.Revision == 1 && next.Expedition.Loot.Parts == 1 && next.Resources.Parts == 0, "Instant loot");
            Check(next.PendingReport.Id == 1 && next.PendingReport.EventId == "EVENT_003" && next.PendingReport.Reward.Parts == 1, "Instant report");
            Check(next.PendingReport.Dice == null && next.PendingReport.IsSuccessful == null && next.PendingReport.NextStep == RESULT_NEXT_STEP.SEARCH_CHOICE, "Instant dice");
            Check(next.PendingReport.ExpeditionBefore.Loot.IsEmpty && next.PendingReport.ExpeditionAfter.Loot.Parts == 1, "Before/after");
            Check(old.Expedition == null && old.Phase == GAME_PHASE.SHELTER && old.Revision == 0, "Old snapshot changed");
            Check(!instant.TryStartExpedition(1, id, regionId, out reason) && instantRng.Calls == 2 && instant.GetSnapshot().Expedition.Loot.Parts == 1, "Repeated grant");
            bool readOnly = false;
            try { ((IList<SurvivorState>)old.Survivors)[0] = null; } catch (NotSupportedException) { readOnly = true; }
            Check(readOnly, "Mutable snapshot");

            var opportunity = new EventDefinition("O", "O", "O", EVENT_CATEGORY.OPPORTUNITY, EVENT_MODE.INSTANT, 1, 5,
                new EventEffectDefinition(0, 0, new SearchRewardItemDefinition(2)));
            var lethal = new EventDefinition("D", "D", "D", EVENT_CATEGORY.DANGER, EVENT_MODE.INSTANT, 1, 5,
                new EventEffectDefinition(-10, 2, new SearchRewardItemDefinition(2)));
            var region = new RegionDefinition("R", "R", RESOURCE_TYPE.PARTS, new[] {"O", "D"});
            var custom = new GameDefinition(data.Rules, new[] {data.Survivors[0]}, new[] {region}, new[] {opportunity, lethal});
            var deadRng = new SequenceRandom(99);
            var dead = new GameSession(custom, deadRng);
            var beforeDeath = dead.GetSnapshot();
            Check(dead.TryStartExpedition(0, id, "R", out reason), "Lethal start");
            var afterDeath = dead.GetSnapshot();
            Check(afterDeath.Phase == GAME_PHASE.RESULT && afterDeath.Survivors[0].CurrentHp == 0 && afterDeath.Expedition.Loot.IsEmpty, "Death state");
            Check(afterDeath.PendingReport.NextStep == RESULT_NEXT_STEP.FINISH_EXPEDITION && afterDeath.PendingReport.Reward.IsEmpty, "Death continuation");
            Check(beforeDeath.Survivors[0].CurrentHp == 10 && afterDeath.PendingReport.SurvivorBefore.CurrentHp == 10, "Old HP changed");

            for (int category = 0; category < 3; category++)
            {
                var chances = new StageEventChanceDefinition[5];
                for (int i = 0; i < 5; i++) chances[i] = new StageEventChanceDefinition(i + 1, category == 0 ? 100 : 0, category == 1 ? 100 : 0, category == 2 ? 100 : 0);
                var deterministicData = new GameDefinition(CopyRules(data.Rules, chances), new[] {data.Survivors[0]}, new[] {region}, new[] {opportunity, lethal});
                var deterministicRng = new SequenceRandom();
                var deterministic = new GameSession(deterministicData, deterministicRng);
                Check(deterministic.TryStartExpedition(0, id, "R", out reason), "Deterministic start");
                Check(deterministicRng.Calls == 0, "Unnecessary random");
                var snap = deterministic.GetSnapshot();
                Check(category == 0 ? snap.Phase == GAME_PHASE.SEARCH_CHOICE : snap.PendingReport.EventId == (category == 1 ? "O" : "D"), "Deterministic category");
            }

            var reversedChances = new List<StageEventChanceDefinition>(data.Rules.StageEventChances).ToArray();
            Array.Reverse(reversedChances);
            var reversedData = new GameDefinition(CopyRules(data.Rules, reversedChances), new[] {data.Survivors[0]}, new[] {data.Regions[0]}, new List<EventDefinition>(data.Events).ToArray());
            var reversed = new GameSession(reversedData, new SequenceRandom(60));
            Check(reversed.TryStartExpedition(0, id, regionId, out reason) && reversed.GetSnapshot().Expedition.StageEventId == null, "Assumed table order");
            bool rejected = false;
            try { new GameSession(null, rng); } catch (ArgumentNullException) { rejected = true; }
            Check(rejected, "Null definition");
            rejected = false;
            try { new GameSession(data, null); } catch (ArgumentNullException) { rejected = true; }
            Check(rejected && rng.Calls == 0, "Null RNG");
            return "Session entry checks passed: " + s_checkCount + "";
        }
    }
}
