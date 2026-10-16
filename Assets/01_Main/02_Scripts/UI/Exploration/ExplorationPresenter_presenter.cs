using System;
using System.Collections.Generic;
using System.Text;
using HM.CodeBase;
using LastSupply.Core;
using LastSupply.Game;

namespace LastSupply.UI.Exploration
{
    public sealed class ExplorationPresenter_presenter : APresenter
    {
        private readonly ExplorationModel_model MODEL;
        private readonly ExplorationPanel_view VIEW;
        private readonly LastSupplyGameProvider PROVIDER;
        private bool _isOpen;
        private bool _isDisposed;

        public ExplorationPresenter_presenter(ExplorationModel_model model, ExplorationPanel_view view, LastSupplyGameProvider provider)
        {
            MODEL = model ?? throw new ArgumentNullException(nameof(model));
            VIEW = view != null ? view : throw new ArgumentNullException(nameof(view));
            PROVIDER = provider != null ? provider : throw new ArgumentNullException(nameof(provider));
        }

        public override void Open()
        {
            if ( _isDisposed )
            {
                throw new ObjectDisposedException(nameof(ExplorationPresenter_presenter));
            }
            if ( _isOpen )
            {
                return;
            }
            MODEL.ApplySnapshot(PROVIDER.GetSnapshot());
            VIEW.Open();
            _isOpen = true;
            VIEW.EventActionRequested += OnEventActioned;
            VIEW.SearchRequested += OnSearchActioned;
            VIEW.SkipSearchRequested += OnSkipSearchActioned;
            VIEW.AdvanceRequested += OnAdvanceActioned;
            VIEW.ReturnRequested += OnReturnActioned;
            VIEW.ReturnActionRequested += OnReturnCheckActioned;
            VIEW.ResultConfirmationRequested += OnResultConfirmationActioned;
            VIEW.DayConfirmationRequested += OnDayConfirmationActioned;
            Render();
        }

        public override void Close()
        {
            if ( !_isOpen )
            {
                return;
            }
            _isOpen = false;
            if ( VIEW == null )
            {
                return;
            }
            VIEW.EventActionRequested -= OnEventActioned;
            VIEW.SearchRequested -= OnSearchActioned;
            VIEW.SkipSearchRequested -= OnSkipSearchActioned;
            VIEW.AdvanceRequested -= OnAdvanceActioned;
            VIEW.ReturnRequested -= OnReturnActioned;
            VIEW.ReturnActionRequested -= OnReturnCheckActioned;
            VIEW.ResultConfirmationRequested -= OnResultConfirmationActioned;
            VIEW.DayConfirmationRequested -= OnDayConfirmationActioned;
            VIEW.Close();
        }

        public override void Dispose()
        {
            if ( _isDisposed )
            {
                return;
            }
            Close();
            _isDisposed = true;
        }

        public void Refresh(GameSnapshot snapshot)
        {
            if ( _isDisposed )
            {
                return;
            }
            MODEL.ApplySnapshot(snapshot);
            if ( _isOpen )
            {
                Render();
            }
        }

        private void Render()
        {
            string status = GetStatus();
            string title = "";
            string story = "";
            string result = "";
            var actions = new List<string>();

            switch ( MODEL.Snapshot.Phase )
            {
                case GAME_PHASE.EVENT_ACTION:
                    title = MODEL.StageEvent.Name;
                    story = MODEL.StageEvent.Description;
                    foreach (EventActionDefinition action in MODEL.StageEvent.Actions)
                    {
                        actions.Add(FormatAction(action));
                    }
                    break;
                case GAME_PHASE.SEARCH_CHOICE:
                    title = "주변을 수색할까요?";
                    story = MODEL.StageEvent == null ? "별다른 사건 없이 도착했습니다." : "사건 처리가 끝났습니다.";
                    story += "\n수색하면 물자를 찾을 수 있습니다. 수색을 생략할 수도 있습니다.";
                    break;
                case GAME_PHASE.STAGE_CHOICE:
                    title = "더 깊이 들어갈까요?";
                    story = "더 깊은 곳에는 더 많은 물자가 있을 수 있습니다.\n사망하면 이번 탐색의 전리품을 모두 잃습니다.";
                    break;
                case GAME_PHASE.RETURN_ACTION:
                    title = MODEL.ReturnEvent.Name;
                    story = MODEL.ReturnEvent.Description;
                    result = $"귀환 판정 실패 피해: HP {MODEL.Expedition.ReturnRisk.FailureDamage}";
                    break;
                case GAME_PHASE.RESULT:
                    title = GetResultTitle();
                    story = MODEL.Report.ResultType == ACTION_RESULT_TYPE.EVENT ? MODEL.StageEvent?.Description
                        : MODEL.Report.ResultType == ACTION_RESULT_TYPE.RETURN ? MODEL.ReturnEvent.Description : "주변에서 물자를 찾아본 결과입니다.";
                    result = GetResultText();
                    break;
                case GAME_PHASE.DAY_SUMMARY:
                    DaySummary summary = MODEL.Summary;
                    status = $"{summary.CompletedDay}일 정산 | 은신처 {FormatResources(summary.ResourcesAfter)}";
                    title = "탐색을 마쳤습니다";
                    story = summary.NextDay.HasValue ? $"정산을 확인하면 {summary.NextDay.Value}일 은신처로 돌아갑니다." : GetEndText(summary.EndReason);
                    result = $"회수: {FormatResources(summary.RecoveredLoot)}\n상실: {FormatResources(summary.LostLoot)}"
                        + $"\n식량 소비 {summary.FoodConsumed} | 식량 부족: {(summary.FoodShortage ? "있음" : "없음")}"
                        + $"\n부족 피해 {summary.StarvationDamage} | 사망자 {summary.DiedSurvivorIds.Count}명";
                    break;
                case GAME_PHASE.GAME_OVER:
                    title = GetEndText(MODEL.Snapshot.EndReason);
                    story = "이번 생존은 여기서 끝났습니다.";
                    result = $"최종 자원: {FormatResources(MODEL.Snapshot.Resources)}";
                    break;
            }

            VIEW.SetText(status, title, story ?? "", result);
            VIEW.SetActions(actions);
            VIEW.SetControls(MODEL.CanSearch, MODEL.CanAdvanceStage, MODEL.CanReturn,
                MODEL.CanResolveReturn, MODEL.CanConfirmResult, MODEL.CanConfirmDay);
            if ( MODEL.CanResolveReturn )
            {
                VIEW.SetReturnActionText(FormatCheck(MODEL.ReturnEvent.ActionText, MODEL.ReturnEvent.StatType, MODEL.ReturnEvent.Difficulty));
            }
            VIEW.SetMessage("");
        }

        private string GetStatus()
        {
            if ( MODEL.Expedition == null )
            {
                return $"{MODEL.Snapshot.Day}일 | 은신처 {FormatResources(MODEL.Snapshot.Resources)}";
            }

            ReturnRiskDefinition risk = MODEL.ReturnRisk;
            return $"{MODEL.Region.Name} | {MODEL.Snapshot.Day}일 | {MODEL.Expedition.Stage}/{MODEL.MaxStage}단계"
                + $"\n{MODEL.Survivor.Name} HP {MODEL.SurvivorState.CurrentHp}/{MODEL.Survivor.MaxHP}"
                + $" | 소음 {MODEL.Expedition.Noise}/{MODEL.MaxNoise} | 귀환 사건 {risk.EventChance}%"
                + $"\n전리품: {FormatResources(MODEL.Expedition.Loot)}";
        }

        private string FormatAction(EventActionDefinition action)
        {
            if ( !action.RequiresCheck )
            {
                return action.Text;
            }
            return FormatCheck(action.Text, action.StatType, action.Difficulty.Value);
        }

        private string FormatCheck(string text, STAT_TYPE statType, int difficulty)
        {
            string stat = statType == STAT_TYPE.COMBAT ? "전투" : statType == STAT_TYPE.TECH ? "기술" : "D20";
            int modifier = statType == STAT_TYPE.COMBAT ? MODEL.Survivor.CombatModifier
                : statType == STAT_TYPE.TECH ? MODEL.Survivor.TechModifier : 0;
            return $"{text}\n{stat} {modifier:+0;-0;0} / DC {difficulty}";
        }

        private string GetResultTitle()
        {
            if ( MODEL.Report.ResultType == ACTION_RESULT_TYPE.SEARCH )
            {
                return "수색 결과";
            }
            if ( MODEL.Report.ResultType == ACTION_RESULT_TYPE.RETURN )
            {
                return "귀환 판정 결과";
            }
            return MODEL.StageEvent?.Name ?? "사건 결과";
        }

        private string GetResultText()
        {
            ActionReport report = MODEL.Report;
            var text = new StringBuilder();
            if ( report.Difficulty.HasValue )
            {
                text.AppendLine($"D20 {report.Dice} + 보정 {report.StatModifier} = {report.Total} / DC {report.Difficulty}");
                text.AppendLine(report.IsSuccessful == true ? "성공" : "실패");
            }
            else if ( report.Dice.HasValue )
            {
                text.AppendLine($"수색 D20: {report.Dice}");
            }
            else
            {
                text.AppendLine("확정 결과");
            }
            text.AppendLine($"획득: {FormatResources(report.Reward)}");
            text.AppendLine($"HP {report.SurvivorBefore.CurrentHp} → {report.SurvivorAfter.CurrentHp}"
                + $" | 소음 {report.ExpeditionBefore.Noise} → {report.ExpeditionAfter.Noise}");
            if ( !report.SurvivorAfter.IsAlive )
            {
                text.AppendLine("생존자가 사망했습니다. 전리품은 회수할 수 없습니다.");
            }
            return text.ToString();
        }

        private static string FormatResources(ResourceAmount resources)
        {
            return $"식량 {resources.Food} / 의약품 {resources.Medicine} / 부품 {resources.Parts}";
        }

        private static string GetEndText(RUN_END_REASON reason)
        {
            switch ( reason )
            {
                case RUN_END_REASON.RESCUED: return "구조 요청에 성공했습니다";
                case RUN_END_REASON.ALL_DEAD: return "모든 생존자가 사망했습니다";
                case RUN_END_REASON.TIME_EXPIRED: return "구조 요청 기한이 지났습니다";
                default: return "탐색이 끝났습니다";
            }
        }

        private void HandleCommandResult(bool accepted, string reason)
        {
            if ( accepted || !_isOpen )
            {
                return;
            }
            Refresh(PROVIDER.GetSnapshot());
            VIEW.SetMessage(reason);
        }

        private void OnEventActioned(int index)
        {
            if ( !_isOpen || !MODEL.CanChooseEventAction || index < 0 || index >= MODEL.StageEvent.Actions.Count )
            {
                return;
            }
            bool accepted = PROVIDER.TryChooseEventAction(MODEL.Snapshot.Revision, MODEL.StageEvent.Id, MODEL.StageEvent.Actions[index].Id, out string reason);
            HandleCommandResult(accepted, reason);
        }

        private void OnSearchActioned()
        {
            if ( !_isOpen || !MODEL.CanSearch )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TrySearch(MODEL.Snapshot.Revision, out string reason), reason);
        }

        private void OnSkipSearchActioned()
        {
            if ( !_isOpen || !MODEL.CanSearch )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TrySkipSearch(MODEL.Snapshot.Revision, out string reason), reason);
        }

        private void OnAdvanceActioned()
        {
            if ( !_isOpen || !MODEL.CanAdvanceStage )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TryAdvanceStage(MODEL.Snapshot.Revision, out string reason), reason);
        }

        private void OnReturnActioned()
        {
            if ( !_isOpen || !MODEL.CanReturn )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TryReturn(MODEL.Snapshot.Revision, out string reason), reason);
        }

        private void OnReturnCheckActioned()
        {
            if ( !_isOpen || !MODEL.CanResolveReturn )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TryResolveReturn(MODEL.Snapshot.Revision, MODEL.ReturnEvent.Id, MODEL.ReturnEvent.ActionId, out string reason), reason);
        }

        private void OnResultConfirmationActioned()
        {
            if ( !_isOpen || !MODEL.CanConfirmResult )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TryConfirmResult(MODEL.Snapshot.Revision, MODEL.Report.Id, out string reason), reason);
        }

        private void OnDayConfirmationActioned()
        {
            if ( !_isOpen || !MODEL.CanConfirmDay )
            {
                return;
            }
            HandleCommandResult(PROVIDER.TryConfirmDaySummary(MODEL.Snapshot.Revision, MODEL.Summary.Id, out string reason), reason);
        }
    }
}
