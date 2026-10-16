using System;
using System.Collections.Generic;
using System.Reflection;
using LastSupply.Content;
using LastSupply.Core;
using LastSupply.Game;
using LastSupply.UI.Exploration;
using LastSupply.UI.Shelter;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastSupply.Tests
{
    [InitializeOnLoad]
    public static class ExplorationPlayModeChecks
    {
        private const string RUN_KEY = "LastSupply.ExplorationChecks";
        private static int s_checks;
        private static bool s_started;
        private static readonly HashSet<string> PHASES = new HashSet<string>();
        private static readonly HashSet<EVENT_MODE> EVENT_MODES = new HashSet<EVENT_MODE>();

        static ExplorationPlayModeChecks()
        {
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
            EditorApplication.update += OnEditorUpdated;
        }

        public static void RunBatch()
        {
            if ( !Application.isBatchMode )
            {
                throw new InvalidOperationException("이 검사는 복사한 임시 프로젝트의 배치 실행 전용입니다.");
            }
            CreateFixture();
            SessionState.SetString(RUN_KEY, "entering");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayStateChanged(PlayModeStateChange state)
        {
            if ( state == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(RUN_KEY, "") == "entering" )
            {
                SessionState.SetString(RUN_KEY, "running");
            }
        }

        private static void OnEditorUpdated()
        {
            string state = SessionState.GetString(RUN_KEY, "");
            if ( state == "success" && !EditorApplication.isPlaying )
            {
                EditorApplication.Exit(0);
            }
            else if ( state == "running" && EditorApplication.isPlaying && !s_started )
            {
                s_started = true;
                Execute();
            }
        }

        private static void Execute()
        {
            try
            {
                RunChecks();
                Debug.Log($"PLAYMODE_UI_CHECKS_PASSED: {s_checks} assertions; 100 seeded UI expeditions; phases {string.Join(",", PHASES)}; event modes {string.Join(",", EVENT_MODES)}.");
                SessionState.SetString(RUN_KEY, "success");
                EditorApplication.ExitPlaymode();
            }
            catch ( Exception exception )
            {
                Debug.LogException(exception);
                SessionState.SetString(RUN_KEY, "failed");
                EditorApplication.Exit(1);
            }
        }

        private static void RunChecks()
        {
            var provider = UnityEngine.Object.FindFirstObjectByType<LastSupplyGameProvider>();
            Assert(provider != null && provider.isActiveAndEnabled, "Provider 초기화 실패");
            var shelter = Read<ShelterPanel_view>(provider, "_shelterView");
            var exploration = Read<ExplorationPanel_view>(provider, "_explorationView");
            Assert(exploration != null, "런타임 탐색 화면 생성 실패");
            Assert(exploration.transform.parent == shelter.transform.parent, "두 화면은 같은 Canvas의 독립 자식이어야 합니다.");
            GameDefinition definition = Read<GameDefinition>(provider, "_definition");

            for ( int seed = 0; seed < 100; seed++ )
            {
                var session = new GameSession(definition, new GameRandom(seed));
                Write(provider, "_session", session);
                typeof(LastSupplyGameProvider).GetMethod("RefreshScreens", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(provider, null);

                Assert(shelter.gameObject.activeInHierarchy && !exploration.gameObject.activeInHierarchy, "초기 화면 전환 실패");
                TMP_Dropdown survivorDrop = Read<TMP_Dropdown>(shelter, "_survivorDrop");
                TMP_Dropdown regionDrop = Read<TMP_Dropdown>(shelter, "_regionDrop");
                survivorDrop.value = 0;
                regionDrop.value = 0;
                Assert(!Read<Button>(shelter, "_startBtn").interactable, "미선택 출발 버튼 활성화");
                survivorDrop.value = 1;
                regionDrop.value = 3;
                Assert(Read<Button>(shelter, "_startBtn").interactable, "두 선택 후 출발 불가");

                regionDrop.value = 0;
                Assert(!Read<Button>(shelter, "_startBtn").interactable, "미선택 복귀 후 출발 가능");
                regionDrop.value = 3;
                ClickAndCheck(provider, Read<Button>(shelter, "_startBtn"));
                Assert(provider.isActiveAndEnabled && !shelter.gameObject.activeInHierarchy && exploration.gameObject.activeInHierarchy, "출발 화면 라우팅 실패");

                bool finished = false;
                for ( int step = 0; step < 80; step++ )
                {
                    GameSnapshot snapshot = provider.GetSnapshot();
                    PHASES.Add(snapshot.Phase.ToString());
                    TMP_Text titleTxt = Read<TMP_Text>(exploration, "_titleTxt");
                    Assert(!string.IsNullOrWhiteSpace(titleTxt.text), "탐색 제목 누락");

                    if ( step == 0 && seed % 10 == 0 )
                    {
                        long revision = snapshot.Revision;
                        string title = titleTxt.text;
                        provider.enabled = false;
                        Assert(!shelter.gameObject.activeInHierarchy && !exploration.gameObject.activeInHierarchy, "비활성화 시 화면 미해제");
                        provider.enabled = true;
                        Assert(provider.GetSnapshot().Revision == revision && titleTxt.text == title, "재활성화 시 재판정 또는 표시 변경");
                        Assert(exploration.gameObject.activeInHierarchy, "진행 중 탐색 화면 복구 실패");
                    }

                    switch ( snapshot.Phase )
                    {
                        case GAME_PHASE.EVENT_ACTION:
                            EventDefinition current = FindEvent(definition, snapshot.Expedition.StageEventId);
                            EVENT_MODES.Add(current.Mode);
                            Assert(titleTxt.text == current.Name, "사건 제목이 정의와 불일치");
                            Assert(Read<TMP_Text>(exploration, "_storyTxt").text == current.Description, "사건 설명이 정의와 불일치");
                            Button[] actions = Read<Button[]>(exploration, "_actionBtns");
                            for ( int i = 0; i < actions.Length; i++ )
                            {
                                Assert(actions[i].gameObject.activeSelf == (i < current.Actions.Count), "사건 행동 버튼 수 불일치");
                            }
                            ClickAndCheck(provider, actions[seed % current.Actions.Count]);
                            break;
                        case GAME_PHASE.RESULT:
                            ActionReport report = snapshot.PendingReport;
                            if ( report.ResultType == ACTION_RESULT_TYPE.EVENT )
                            {
                                EVENT_MODES.Add(FindEvent(definition, report.EventId).Mode);
                            }
                            string result = Read<TMP_Text>(exploration, "_resultTxt").text;
                            Assert(result.Contains("HP " + report.SurvivorBefore.CurrentHp + " → " + report.SurvivorAfter.CurrentHp), "적용된 HP 결과 불일치");
                            if ( report.Difficulty.HasValue )
                            {
                                Assert(result.Contains("DC " + report.Difficulty.Value), "판정 DC 미표시");
                            }
                            Assert(!Read<Button>(exploration, "_searchBtn").gameObject.activeSelf, "결과 확인 전에 수색 표시");
                            Button confirm = Read<Button>(exploration, "_confirmResultBtn");
                            ClickAndCheck(provider, confirm);
                            AssertHiddenClickIgnored(provider, confirm);
                            break;
                        case GAME_PHASE.SEARCH_CHOICE:
                            Button search = Read<Button>(exploration, seed % 3 == 0 ? "_skipSearchBtn" : "_searchBtn");
                            ClickAndCheck(provider, search);
                            AssertHiddenClickIgnored(provider, search);
                            break;
                        case GAME_PHASE.STAGE_CHOICE:
                            ClickAndCheck(provider, Read<Button>(exploration, seed % 2 == 0 ? "_advanceBtn" : "_returnBtn"));
                            break;
                        case GAME_PHASE.RETURN_ACTION:
                            Assert(titleTxt.text == definition.Rules.ReturnEvent.Name, "귀환 사건 대신 일반 사건 표시");
                            ClickAndCheck(provider, Read<Button>(exploration, "_returnActionBtn"));
                            break;
                        case GAME_PHASE.DAY_SUMMARY:
                            Assert(Read<TMP_Text>(exploration, "_statusTxt").text.StartsWith(snapshot.DaySummary.CompletedDay + "일 정산"), "정산 날짜 표시 오류");
                            Button day = Read<Button>(exploration, "_confirmDayBtn");
                            ClickAndCheck(provider, day);
                            AssertHiddenClickIgnored(provider, day);
                            GameSnapshot after = provider.GetSnapshot();
                            Assert(after.Phase == GAME_PHASE.SHELTER || after.Phase == GAME_PHASE.GAME_OVER, "정산 후 라우팅 오류");
                            Assert(shelter.gameObject.activeInHierarchy == (after.Phase == GAME_PHASE.SHELTER), "은신처 복귀 표시 오류");
                            if ( after.Phase == GAME_PHASE.GAME_OVER )
                            {
                                PHASES.Add("GAME_OVER");
                                Assert(exploration.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(titleTxt.text), "종료 화면 표시 오류");
                            }
                            finished = true;
                            break;
                        default:
                            throw new Exception("예상하지 못한 탐색 단계: " + snapshot.Phase);
                    }
                    if ( finished )
                    {
                        break;
                    }
                }
                Assert(finished, "탐색 정산에 도달하지 못했습니다.");
            }

            Assert(EVENT_MODES.Contains(EVENT_MODE.INSTANT) && EVENT_MODES.Contains(EVENT_MODE.CHECK) && EVENT_MODES.Contains(EVENT_MODE.CHOICE), "사건 3종 미검증");
            Assert(PHASES.Contains("RETURN_ACTION") && PHASES.Contains("GAME_OVER"), "귀환·종료 단계 미검증");
        }

        private static void ClickAndCheck(LastSupplyGameProvider provider, Button targetBtn)
        {
            Assert(targetBtn.gameObject.activeInHierarchy && targetBtn.IsInteractable(), "비활성 버튼 호출: " + targetBtn.name);
            long revision = provider.GetSnapshot().Revision;
            targetBtn.onClick.Invoke();
            Assert(provider.GetSnapshot().Revision == revision + 1, "한 번의 UI 입력이 정확히 한 명령으로 처리되지 않았습니다: " + targetBtn.name);
            string reason;
            Assert(!provider.TrySearch(revision, out reason), "오래된 Revision 입력이 허용되었습니다.");
        }

        private static void AssertHiddenClickIgnored(LastSupplyGameProvider provider, Button targetBtn)
        {
            long revision = provider.GetSnapshot().Revision;
            targetBtn.onClick.Invoke();
            Assert(provider.GetSnapshot().Revision == revision, "숨겨진 이전 버튼 입력이 새 행동으로 처리되었습니다.");
        }

        private static EventDefinition FindEvent(GameDefinition definition, string id)
        {
            foreach (EventDefinition item in definition.Events)
            {
                if ( item.Id == id )
                {
                    return item;
                }
            }
            throw new Exception("사건을 찾을 수 없습니다: " + id);
        }

        private static T Read<T>(object target, string field)
        {
            return (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Write(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void Assert(bool condition, string message)
        {
            if ( !condition )
            {
                throw new Exception(message);
            }
            s_checks++;
        }

        private static void CreateFixture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasObj = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);

            var panelObj = new GameObject("ShelterPanel", typeof(RectTransform));
            panelObj.transform.SetParent(canvasObj.transform, false);
            var shelter = panelObj.AddComponent<ShelterPanel_view>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/01_Main/04_Resource/Font/Pretendard/Pretendard-Black SDF.asset");
            if ( font == null )
            {
                throw new Exception("검증용 한글 폰트 누락");
            }
            var statusObj = new GameObject("StatusTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(panelObj.transform, false);
            statusObj.GetComponent<TextMeshProUGUI>().font = font;
            var messageObj = new GameObject("MessageTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            messageObj.transform.SetParent(panelObj.transform, false);
            messageObj.GetComponent<TextMeshProUGUI>().font = font;
            var survivorObj = new GameObject("SurvivorDrop", typeof(RectTransform), typeof(TMP_Dropdown));
            survivorObj.transform.SetParent(panelObj.transform, false);
            var regionObj = new GameObject("RegionDrop", typeof(RectTransform), typeof(TMP_Dropdown));
            regionObj.transform.SetParent(panelObj.transform, false);
            var startObj = new GameObject("StartBtn", typeof(RectTransform), typeof(Button));
            startObj.transform.SetParent(panelObj.transform, false);
            using (var data = new SerializedObject(shelter))
            {
                data.FindProperty("_statusTxt").objectReferenceValue = statusObj.GetComponent<TMP_Text>();
                data.FindProperty("_messageTxt").objectReferenceValue = messageObj.GetComponent<TMP_Text>();
                data.FindProperty("_survivorDrop").objectReferenceValue = survivorObj.GetComponent<TMP_Dropdown>();
                data.FindProperty("_regionDrop").objectReferenceValue = regionObj.GetComponent<TMP_Dropdown>();
                data.FindProperty("_startBtn").objectReferenceValue = startObj.GetComponent<Button>();
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var provider = new GameObject("GameProvider").AddComponent<LastSupplyGameProvider>();
            using (var data = new SerializedObject(provider))
            {
                data.FindProperty("_contentCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameContentCatalog>("Assets/01_Main/04_Resource/Content/Catalogs/DefaultGameContent.asset");
                data.FindProperty("_shelterView").objectReferenceValue = shelter;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/ExplorationFixture.unity");
        }
    }
}
