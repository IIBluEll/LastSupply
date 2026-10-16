using System;
using System.Collections.Generic;
using HM.CodeBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSupply.UI.Shelter
{
    public sealed class ShelterPanel_view : AView
    {
        [Header("상태 표시")]
        [SerializeField] private TMP_Text _statusTxt;
        [SerializeField] private TMP_Text _messageTxt;

        [Header("선택과 출발")]
        [SerializeField] private TMP_Dropdown _survivorDrop;
        [SerializeField] private TMP_Dropdown _regionDrop;
        [SerializeField] private Button _startBtn;

        private bool _listenersBound;

        public event Action<int> SurvivorSelectionChanged;
        public event Action<int> RegionSelectionChanged;
        public event Action StartRequested;

        public void ValidateReferences()
        {
            if ( _statusTxt == null || _messageTxt == null || _survivorDrop == null || _regionDrop == null || _startBtn == null )
            {
                throw new ArgumentException($"{name}: 은신처 UI 참조를 모두 연결해 주세요.");
            }
        }

        public override void Open()
        {
            ValidateReferences();

            if ( !_listenersBound )
            {
                _survivorDrop.onValueChanged.AddListener(OnSurvivorSelectionActioned);
                _regionDrop.onValueChanged.AddListener(OnRegionSelectionActioned);
                _startBtn.onClick.AddListener(OnStartActioned);
                _listenersBound = true;
            }

            base.Open();
        }

        public override void Close()
        {
            UnbindListeners();
            base.Close();
        }

        public void SetStatus(string text)
        {
            _statusTxt.text = text;
        }

        public void SetMessage(string text)
        {
            _messageTxt.text = text ?? string.Empty;
        }

        public void SetOptions(IReadOnlyList<string> survivorLabels , IReadOnlyList<string> regionLabels)
        {
            ValidateReferences();
            SetDropdownOptions(_survivorDrop , survivorLabels , "생존자 선택");
            SetDropdownOptions(_regionDrop , regionLabels , "지역 선택");
        }

        public void SetSelections(int survivorIndex , int regionIndex)
        {
            _survivorDrop.SetValueWithoutNotify(survivorIndex);
            _regionDrop.SetValueWithoutNotify(regionIndex);
        }

        public void SetInputState(bool canSelect , bool canStart)
        {
            _survivorDrop.interactable = canSelect;
            _regionDrop.interactable = canSelect;
            _startBtn.interactable = canStart;
        }

        private static void SetDropdownOptions(TMP_Dropdown targetDrop , IReadOnlyList<string> labels , string placeholder)
        {
            if ( labels == null )
            {
                throw new ArgumentNullException(nameof(labels));
            }

            var options = new List<string> { placeholder };
            for ( int i = 0; i < labels.Count; i++ )
            {
                options.Add(labels[ i ]);
            }

            targetDrop.ClearOptions();
            targetDrop.AddOptions(options);
            targetDrop.SetValueWithoutNotify(0);
        }

        private void OnSurvivorSelectionActioned(int index)
        {
            if ( isActiveAndEnabled && _survivorDrop.interactable )
            {
                SurvivorSelectionChanged?.Invoke(index);
            }
        }

        private void OnRegionSelectionActioned(int index)
        {
            if ( isActiveAndEnabled && _regionDrop.interactable )
            {
                RegionSelectionChanged?.Invoke(index);
            }
        }

        private void OnStartActioned()
        {
            if ( isActiveAndEnabled && _startBtn.interactable )
            {
                StartRequested?.Invoke();
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

            if ( _survivorDrop != null )
            {
                _survivorDrop.onValueChanged.RemoveListener(OnSurvivorSelectionActioned);
            }

            if ( _regionDrop != null )
            {
                _regionDrop.onValueChanged.RemoveListener(OnRegionSelectionActioned);
            }

            if ( _startBtn != null )
            {
                _startBtn.onClick.RemoveListener(OnStartActioned);
            }
        }
    }
}