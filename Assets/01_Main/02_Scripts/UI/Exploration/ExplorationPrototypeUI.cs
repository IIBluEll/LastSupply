using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSupply.UI.Exploration
{
    // 기존 Canvas와 한글 폰트를 사용하는 런타임 프로토타입 화면입니다.
    public static class ExplorationPrototypeUI
    {
        public static ExplorationPanel_view Create(Canvas canvas, TMP_FontAsset font)
        {
            if ( canvas == null || font == null )
            {
                throw new ArgumentException("탐색 프로토타입에는 Canvas와 TMP 폰트가 필요합니다.");
            }

            RectTransform panelRectTrans = CreateRect("ExplorationPanel", canvas.transform, Vector2.zero, Vector2.one);
            GameObject panelObj = panelRectTrans.gameObject;
            panelObj.SetActive(false);
            Image backgroundImg = panelObj.AddComponent<Image>();
            backgroundImg.color = new Color(0.055f, 0.075f, 0.10f, 1f);

            TMP_Text statusTxt = CreateText("StatusTxt", panelRectTrans, font, 24, new Vector2(.06f, .86f), new Vector2(.94f, .97f));
            TMP_Text titleTxt = CreateText("TitleTxt", panelRectTrans, font, 34, new Vector2(.06f, .76f), new Vector2(.94f, .85f));
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(.96f, .78f, .39f);
            TMP_Text storyTxt = CreateText("StoryTxt", panelRectTrans, font, 26, new Vector2(.06f, .56f), new Vector2(.94f, .75f));
            TMP_Text resultTxt = CreateText("ResultTxt", panelRectTrans, font, 24, new Vector2(.06f, .32f), new Vector2(.94f, .55f));
            TMP_Text messageTxt = CreateText("MessageTxt", panelRectTrans, font, 20, new Vector2(.06f, .015f), new Vector2(.94f, .065f));
            messageTxt.color = new Color(1f, .58f, .48f);

            var actionBtns = new Button[3];
            for ( int i = 0; i < actionBtns.Length; i++ )
            {
                float left = .06f + i * .30f;
                actionBtns[i] = CreateButton("Action" + i + "Btn", panelRectTrans, font, "", new Vector2(left, .18f), new Vector2(left + .28f, .29f));
            }

            Button searchBtn = CreateButton("SearchBtn", panelRectTrans, font, "수색한다", new Vector2(.06f, .08f), new Vector2(.48f, .16f));
            Button skipSearchBtn = CreateButton("SkipSearchBtn", panelRectTrans, font, "수색을 생략한다", new Vector2(.52f, .08f), new Vector2(.94f, .16f));
            Button advanceBtn = CreateButton("AdvanceBtn", panelRectTrans, font, "다음 단계로", new Vector2(.06f, .08f), new Vector2(.48f, .16f));
            Button returnBtn = CreateButton("ReturnBtn", panelRectTrans, font, "귀환한다", new Vector2(.52f, .08f), new Vector2(.94f, .16f));
            Button returnActionBtn = CreateButton("ReturnActionBtn", panelRectTrans, font, "길을 뚫는다", new Vector2(.20f, .08f), new Vector2(.80f, .16f));
            Button confirmResultBtn = CreateButton("ConfirmResultBtn", panelRectTrans, font, "결과 확인", new Vector2(.20f, .08f), new Vector2(.80f, .16f));
            Button confirmDayBtn = CreateButton("ConfirmDayBtn", panelRectTrans, font, "정산 확인", new Vector2(.20f, .08f), new Vector2(.80f, .16f));

            ExplorationPanel_view view = panelObj.AddComponent<ExplorationPanel_view>();
            view.Configure(statusTxt, titleTxt, storyTxt, resultTxt, messageTxt, actionBtns,
                searchBtn, skipSearchBtn, advanceBtn, returnBtn, returnActionBtn, confirmResultBtn, confirmDayBtn);
            view.ValidateReferences();
            return view;
        }

        private static RectTransform CreateRect(string name, Transform parentTrans, Vector2 min, Vector2 max)
        {
            var targetObj = new GameObject(name, typeof(RectTransform));
            RectTransform targetRectTrans = targetObj.GetComponent<RectTransform>();
            targetRectTrans.SetParent(parentTrans, false);
            targetRectTrans.anchorMin = min;
            targetRectTrans.anchorMax = max;
            targetRectTrans.offsetMin = Vector2.zero;
            targetRectTrans.offsetMax = Vector2.zero;
            return targetRectTrans;
        }

        private static TMP_Text CreateText(string name, Transform parentTrans, TMP_FontAsset font, float size, Vector2 min, Vector2 max)
        {
            RectTransform textRectTrans = CreateRect(name, parentTrans, min, max);
            TextMeshProUGUI textTxt = textRectTrans.gameObject.AddComponent<TextMeshProUGUI>();
            textTxt.font = font;
            textTxt.fontSize = size;
            textTxt.enableAutoSizing = true;
            textTxt.fontSizeMin = 16;
            textTxt.fontSizeMax = size;
            textTxt.textWrappingMode = TextWrappingModes.Normal;
            textTxt.overflowMode = TextOverflowModes.Ellipsis;
            textTxt.alignment = TextAlignmentOptions.TopLeft;
            textTxt.color = new Color(.91f, .94f, .98f);
            textTxt.raycastTarget = false;
            return textTxt;
        }

        private static Button CreateButton(string name, Transform parentTrans, TMP_FontAsset font, string label, Vector2 min, Vector2 max)
        {
            RectTransform buttonRectTrans = CreateRect(name, parentTrans, min, max);
            Image buttonImg = buttonRectTrans.gameObject.AddComponent<Image>();
            buttonImg.color = Color.white;
            Button targetBtn = buttonRectTrans.gameObject.AddComponent<Button>();
            targetBtn.targetGraphic = buttonImg;
            ColorBlock colors = targetBtn.colors;
            colors.normalColor = new Color(.17f, .29f, .42f);
            colors.highlightedColor = new Color(.24f, .40f, .56f);
            colors.pressedColor = new Color(.11f, .21f, .32f);
            colors.selectedColor = colors.highlightedColor;
            targetBtn.colors = colors;

            TMP_Text labelTxt = CreateText("LabelTxt", buttonRectTrans, font, 24, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            labelTxt.alignment = TextAlignmentOptions.Center;
            labelTxt.text = label;
            return targetBtn;
        }
    }
}
