using System;
using System.Collections.Generic;
using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LastSupply.UI.Exploration
{
    public sealed class ExplorationPanel_view : AView
    {
        [SerializeField] private TMP_Text _statusTxt;
        [SerializeField] private TMP_Text _titleTxt;
        [SerializeField] private TMP_Text _storyTxt;
        [SerializeField] private TMP_Text _resultTxt;
        [SerializeField] private TMP_Text _messageTxt;
        [SerializeField] private Button[] _actionBtns;
        [SerializeField] private Button _searchBtn;
        [SerializeField] private Button _skipSearchBtn;
        [SerializeField] private Button _advanceBtn;
        [SerializeField] private Button _returnBtn;
        [SerializeField] private Button _returnActionBtn;
        [SerializeField] private Button _confirmResultBtn;
        [SerializeField] private Button _confirmDayBtn;

        private UnityAction[] _actionHandlers;
        private bool _listenersBound;

        public event Action<int> EventActionRequested;
        public event Action SearchRequested;
        public event Action SkipSearchRequested;
        public event Action AdvanceRequested;
        public event Action ReturnRequested;
        public event Action ReturnActionRequested;
        public event Action ResultConfirmationRequested;
        public event Action DayConfirmationRequested;

        internal void Configure(TMP_Text statusTxt, TMP_Text titleTxt, TMP_Text storyTxt, TMP_Text resultTxt, TMP_Text messageTxt,
            Button[] actionBtns, Button searchBtn, Button skipSearchBtn, Button advanceBtn, Button returnBtn,
            Button returnActionBtn, Button confirmResultBtn, Button confirmDayBtn)
        {
            _statusTxt = statusTxt;
            _titleTxt = titleTxt;
            _storyTxt = storyTxt;
            _resultTxt = resultTxt;
            _messageTxt = messageTxt;
            _actionBtns = actionBtns;
            _searchBtn = searchBtn;
            _skipSearchBtn = skipSearchBtn;
            _advanceBtn = advanceBtn;
            _returnBtn = returnBtn;
            _returnActionBtn = returnActionBtn;
            _confirmResultBtn = confirmResultBtn;
            _confirmDayBtn = confirmDayBtn;
        }

        public void ValidateReferences()
        {
            if ( _statusTxt == null || _titleTxt == null || _storyTxt == null || _resultTxt == null || _messageTxt == null
                || _searchBtn == null || _skipSearchBtn == null || _advanceBtn == null || _returnBtn == null
                || _returnActionBtn == null || _confirmResultBtn == null || _confirmDayBtn == null
                || _actionBtns == null || _actionBtns.Length != 3 )
            {
                throw new ArgumentException($"{name}: 탐색 UI 참조를 모두 연결해 주세요.");
            }

            foreach (Button actionBtn in _actionBtns)
            {
                if ( actionBtn == null || actionBtn.GetComponentInChildren<TMP_Text>(true) == null )
                {
                    throw new ArgumentException($"{name}: 사건 행동 버튼과 TMP 문구가 필요합니다.");
                }
            }
        }

        public override void Open()
        {
            ValidateReferences();
            if ( !_listenersBound )
            {
                _actionHandlers = new UnityAction[_actionBtns.Length];
                for ( int i = 0; i < _actionBtns.Length; i++ )
                {
                    int index = i;
                    _actionHandlers[i] = () =>
                    {
                        if ( CanReceiveInput(_actionBtns[index]) )
                        {
                            EventActionRequested?.Invoke(index);
                        }
                    };
                    _actionBtns[i].onClick.AddListener(_actionHandlers[i]);
                }

                _searchBtn.onClick.AddListener(OnSearchActioned);
                _skipSearchBtn.onClick.AddListener(OnSkipSearchActioned);
                _advanceBtn.onClick.AddListener(OnAdvanceActioned);
                _returnBtn.onClick.AddListener(OnReturnActioned);
                _returnActionBtn.onClick.AddListener(OnReturnCheckActioned);
                _confirmResultBtn.onClick.AddListener(OnResultConfirmationActioned);
                _confirmDayBtn.onClick.AddListener(OnDayConfirmationActioned);
                _listenersBound = true;
            }
            base.Open();
        }

        public override void Close()
        {
            UnbindListeners();
            base.Close();
        }

        public void SetText(string status, string title, string story, string result)
        {
            _statusTxt.text = status;
            _titleTxt.text = title;
            _storyTxt.text = story;
            _resultTxt.text = result;
        }

        public void SetMessage(string message)
        {
            _messageTxt.text = message ?? string.Empty;
        }

        public void SetActions(IReadOnlyList<string> labels)
        {
            if ( labels.Count > _actionBtns.Length )
            {
                throw new ArgumentException("사건 행동은 최대 3개까지 표시합니다.", nameof(labels));
            }

            for ( int i = 0; i < _actionBtns.Length; i++ )
            {
                _actionBtns[i].gameObject.SetActive(i < labels.Count);
                _actionBtns[i].interactable = i < labels.Count;
                if ( i < labels.Count )
                {
                    _actionBtns[i].GetComponentInChildren<TMP_Text>(true).text = labels[i];
                }
            }
        }

        public void SetControls(bool canSearch, bool canAdvance, bool canReturn, bool canResolveReturn, bool canConfirmResult, bool canConfirmDay)
        {
            ShowButton(_searchBtn, canSearch);
            ShowButton(_skipSearchBtn, canSearch);
            ShowButton(_advanceBtn, canAdvance);
            ShowButton(_returnBtn, canReturn);
            ShowButton(_returnActionBtn, canResolveReturn);
            ShowButton(_confirmResultBtn, canConfirmResult);
            ShowButton(_confirmDayBtn, canConfirmDay);
        }

        public void SetReturnActionText(string text)
        {
            _returnActionBtn.GetComponentInChildren<TMP_Text>(true).text = text;
        }

        private static void ShowButton(Button targetBtn, bool visible)
        {
            targetBtn.interactable = visible;
            targetBtn.gameObject.SetActive(visible);
        }

        private bool CanReceiveInput(Button sourceBtn)
        {
            return isActiveAndEnabled && sourceBtn != null && sourceBtn.gameObject.activeInHierarchy && sourceBtn.IsInteractable();
        }

        private void OnSearchActioned()
        {
            if ( CanReceiveInput(_searchBtn) )
            {
                SearchRequested?.Invoke();
            }
        }
        private void OnSkipSearchActioned()
        {
            if ( CanReceiveInput(_skipSearchBtn) )
            {
                SkipSearchRequested?.Invoke();
            }
        }
        private void OnAdvanceActioned()
        {
            if ( CanReceiveInput(_advanceBtn) )
            {
                AdvanceRequested?.Invoke();
            }
        }
        private void OnReturnActioned()
        {
            if ( CanReceiveInput(_returnBtn) )
            {
                ReturnRequested?.Invoke();
            }
        }
        private void OnReturnCheckActioned()
        {
            if ( CanReceiveInput(_returnActionBtn) )
            {
                ReturnActionRequested?.Invoke();
            }
        }
        private void OnResultConfirmationActioned()
        {
            if ( CanReceiveInput(_confirmResultBtn) )
            {
                ResultConfirmationRequested?.Invoke();
            }
        }
        private void OnDayConfirmationActioned()
        {
            if ( CanReceiveInput(_confirmDayBtn) )
            {
                DayConfirmationRequested?.Invoke();
            }
        }

        private void OnDisable()
        {
            UnbindListeners();
        }
        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void UnbindListeners()
        {
            if ( !_listenersBound )
            {
                return;
            }
            _listenersBound = false;
            for ( int i = 0; i < _actionBtns.Length; i++ )
            {
                if ( _actionBtns[i] != null )
                {
                    _actionBtns[i].onClick.RemoveListener(_actionHandlers[i]);
                }
            }
            if ( _searchBtn != null )
            {
                _searchBtn.onClick.RemoveListener(OnSearchActioned);
            }
            if ( _skipSearchBtn != null )
            {
                _skipSearchBtn.onClick.RemoveListener(OnSkipSearchActioned);
            }
            if ( _advanceBtn != null )
            {
                _advanceBtn.onClick.RemoveListener(OnAdvanceActioned);
            }
            if ( _returnBtn != null )
            {
                _returnBtn.onClick.RemoveListener(OnReturnActioned);
            }
            if ( _returnActionBtn != null )
            {
                _returnActionBtn.onClick.RemoveListener(OnReturnCheckActioned);
            }
            if ( _confirmResultBtn != null )
            {
                _confirmResultBtn.onClick.RemoveListener(OnResultConfirmationActioned);
            }
            if ( _confirmDayBtn != null )
            {
                _confirmDayBtn.onClick.RemoveListener(OnDayConfirmationActioned);
            }
            _actionHandlers = null;
        }
    }
}
