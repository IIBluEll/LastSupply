using System;
using System.Collections.Generic;

namespace LastSupply.Core
{
    public static class EventActionChecks
    {
        private static int s_checkCount;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); s_checkCount++; }
        private static GameSession Start(GameDefinition data, string id, SequenceRandom rng)
        {
            var session = new GameSession(data, rng);
            string reason;
            Check(session.TryStartExpedition(0, id, data.Regions[data.Regions.Count - 1].Id, out reason), "Start rejected");
            return session;
        }
        public static string Run()
        {
            var data = DefaultGameData.CreatePrototype();
            string id = data.Survivors[0].ID;
            string reason;

            var shelterRandom = new SequenceRandom();
            var shelter = new GameSession(data, shelterRandom);
            Check(!shelter.TryChooseEventAction(0, "EVENT_001", "PASS", out reason) && shelterRandom.Calls == 0, "Shelter allowed event action");
            var noneRandom = new SequenceRandom(0);
            var none = Start(data, id, noneRandom);
            Check(!none.TryChooseEventAction(1, "EVENT_001", "PASS", out reason) && noneRandom.Calls == 1, "Search phase allowed action");

            var invalidRandom = new SequenceRandom(65, 0);
            var invalid = Start(data, id, invalidRandom);
            var before = invalid.GetSnapshot();
            Check(!invalid.TryChooseEventAction(0, "EVENT_001", "UNLOCK", out reason), "Stale revision accepted");
            Check(!invalid.TryChooseEventAction(1, "EVENT_002", "BREAK_THROUGH", out reason), "Wrong event accepted");
            Check(!invalid.TryChooseEventAction(1, null, "PASS", out reason), "Null event accepted");
            Check(!invalid.TryChooseEventAction(1, "EVENT_001", null, out reason), "Null action accepted");
            Check(!invalid.TryChooseEventAction(1, "EVENT_001", "CONTROL", out reason), "Other event action accepted");
            Check(invalidRandom.Calls == 2 && invalid.GetSnapshot().Revision == 1 && invalid.GetSnapshot().PendingReport == null && invalid.GetSnapshot().Expedition.Loot.IsEmpty, "Invalid input changed state or random");

            var passRandom = new SequenceRandom(65, 0);
            var pass = Start(data, id, passRandom);
            Check(pass.TryChooseEventAction(1, "EVENT_001", "PASS", out reason) && reason == null, "Pass rejected");
            var passState = pass.GetSnapshot();
            Check(passRandom.Calls == 2 && passState.Phase == GAME_PHASE.RESULT && passState.Revision == 2, "Pass consumed a die");
            Check(passState.PendingReport.Dice == null && passState.PendingReport.Difficulty == null && passState.PendingReport.IsSuccessful == null && passState.PendingReport.Total == null, "Pass has check metadata");
            Check(passState.PendingReport.Reward.IsEmpty && passState.PendingReport.HpChange == 0 && passState.PendingReport.NoiseChange == 0, "Pass effects");
            Check(passState.PendingReport.ActionId == "PASS" && passState.PendingReport.EventId == "EVENT_001" && passState.PendingReport.Id == 1, "Pass report identity");

            var successRandom = new SequenceRandom(65, 0, 7);
            var success = Start(data, id, successRandom);
            var prior = success.GetSnapshot();
            Check(success.TryChooseEventAction(1, "EVENT_001", "FORCE_OPEN", out reason), "Force success rejected");
            var after = success.GetSnapshot();
            Check(after.PendingReport.Dice == 7 && after.PendingReport.StatModifier == 4 && after.PendingReport.Total == 11 && after.PendingReport.Difficulty == 11 && after.PendingReport.IsSuccessful == true, "Combat equality success");
            Check(after.Expedition.Loot.Parts == 2 && after.Expedition.Noise == 2 && after.Survivors[0].CurrentHp == 10 && after.Resources.Parts == 0, "Success application");
            Check(after.PendingReport.Reward.Parts == 2 && after.PendingReport.NextStep == RESULT_NEXT_STEP.SEARCH_CHOICE && after.Expedition.StageEventId == "EVENT_001", "Success report");
            Check(prior.Phase == GAME_PHASE.EVENT_ACTION && prior.Expedition.Loot.IsEmpty && prior.Expedition.Noise == 0, "Prior snapshot changed");
            Check(successRandom.Calls == 3 && successRandom.LastRange == "1:21", "Action die range/count");
            Check(!success.TryChooseEventAction(1, "EVENT_001", "FORCE_OPEN", out reason), "Duplicate revision accepted");
            Check(!success.TryChooseEventAction(2, "EVENT_001", "UNLOCK", out reason), "Result allowed retry");
            Check(successRandom.Calls == 3 && success.GetSnapshot().Expedition.Loot.Parts == 2, "Retry re-granted reward");

            var failureRandom = new SequenceRandom(65, 0, 6);
            var failure = Start(data, id, failureRandom);
            Check(failure.TryChooseEventAction(1, "EVENT_001", "FORCE_OPEN", out reason), "Force fail rejected");
            after = failure.GetSnapshot();
            Check(after.PendingReport.IsSuccessful == false && after.PendingReport.HpChange == -2 && after.PendingReport.NoiseChange == 2 && after.PendingReport.Reward.IsEmpty, "Failure metadata");
            Check(after.Survivors[0].CurrentHp == 8 && after.Expedition.Noise == 2 && after.Expedition.Loot.IsEmpty, "Failure effects");

            var checkRandom = new SequenceRandom(99, 7);
            var forced = Start(data, id, checkRandom);
            Check(forced.TryChooseEventAction(1, "EVENT_002", "BREAK_THROUGH", out reason), "Check action");
            Check(forced.GetSnapshot().Survivors[0].CurrentHp == 7 && forced.GetSnapshot().Expedition.Noise == 2 && checkRandom.Calls == 2, "Check failure");

            var full = DefaultGameData.CreateDefault();
            var techRandom = new SequenceRandom(65, 0, 10);
            var tech = Start(full, DefaultGameData.SARA_ID, techRandom);
            Check(tech.TryChooseEventAction(1, "EVENT_001", "UNLOCK", out reason), "Tech action");
            after = tech.GetSnapshot();
            Check(after.PendingReport.StatModifier == 4 && after.PendingReport.Total == 14 && after.PendingReport.IsSuccessful == true && after.Expedition.Loot.Parts == 2 && after.Survivors[1].CurrentHp == 8, "Tech modifier");
            var plainRandom = new SequenceRandom(65, 2, 13);
            var plain = Start(full, DefaultGameData.SARA_ID, plainRandom);
            Check(plain.TryChooseEventAction(1, "EVENT_005", "TAKE", out reason), "Unmodified check");
            after = plain.GetSnapshot();
            Check(after.PendingReport.StatModifier == 0 && after.PendingReport.Total == 13 && after.PendingReport.IsSuccessful == true && after.Expedition.Loot.Parts == 3, "NONE meant no check");

            var fragile = new SurvivorDefinition("S", "S", 2, 0, 0);
            var lethalData = new GameDefinition(data.Rules, new[] {fragile}, new[] {data.Regions[0]}, new List<EventDefinition>(data.Events).ToArray());
            var lethalRandom = new SequenceRandom(99, 1);
            var lethal = Start(lethalData, "S", lethalRandom);
            Check(lethal.TryChooseEventAction(1, "EVENT_002", "BREAK_THROUGH", out reason), "Lethal action");
            after = lethal.GetSnapshot();
            Check(after.Survivors[0].CurrentHp == 0 && after.Expedition.Loot.IsEmpty && after.Phase == GAME_PHASE.RESULT, "Lethal state");
            Check(after.PendingReport.NextStep == RESULT_NEXT_STEP.FINISH_EXPEDITION && after.PendingReport.Reward.IsEmpty && after.PendingReport.SurvivorBefore.CurrentHp == 2, "Lethal report");
            Check(!lethal.TryChooseEventAction(2, "EVENT_002", "BREAK_THROUGH", out reason) && lethalRandom.Calls == 2, "Death allowed action");

            var instantRandom = new SequenceRandom(65, 1);
            var instant = Start(data, id, instantRandom);
            Check(!instant.TryChooseEventAction(1, "EVENT_003", "PASS", out reason) && instantRandom.Calls == 2, "Instant accepted choice");
            return "Event action checks passed: " + s_checkCount;
        }
    }
}
