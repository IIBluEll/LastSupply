using System;
using LastSupply.Content;
using LastSupply.Core;
using LastSupply.UI.Exploration;
using LastSupply.UI.Shelter;
using TMPro;
using UnityEngine;

namespace LastSupply.Game
{
    [DisallowMultipleComponent]
    public sealed class LastSupplyGameProvider : MonoBehaviour
    {
        [SerializeField] private GameContentCatalog _contentCatalog;
        [SerializeField] private ShelterPanel_view _shelterView;
        [SerializeField] private ExplorationPanel_view _explorationView;

        private GameDefinition _definition;
        private GameSession _session;
        private ShelterPresenter_presenter _shelterPresenter;
        private ExplorationPresenter_presenter _explorationPresenter;
        private bool _ownsExplorationView;

        private void Awake()
        {
            var providers = FindObjectsByType<LastSupplyGameProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if ( providers.Length != 1 )
            {
                Debug.LogError("LastSupplyGameProvider는 로드된 Scene에 하나만 있어야 합니다." , this);
                enabled = false;
                return;
            }

            try
            {
                if ( _contentCatalog == null || _shelterView == null )
                {
                    throw new ArgumentException("카탈로그와 은신처 View를 Inspector에 연결해 주세요.");
                }

                if ( transform.IsChildOf(_shelterView.transform) )
                {
                    throw new ArgumentException("Provider는 은신처 View와 별도 오브젝트에 두고, View의 자식으로 배치하지 마세요.");
                }

                _shelterView.ValidateReferences();

                if ( _explorationView == null )
                {
                    Canvas canvas = _shelterView.GetComponentInParent<Canvas>(true);
                    TMP_Text fontSource = _shelterView.GetComponentInChildren<TMP_Text>(true);
                    _explorationView = ExplorationPrototypeUI.Create(canvas, fontSource != null ? fontSource.font : null);
                    _ownsExplorationView = true;
                }

                if ( transform.IsChildOf(_explorationView.transform)
                    || _explorationView.transform.IsChildOf(_shelterView.transform)
                    || _shelterView.transform.IsChildOf(_explorationView.transform) )
                {
                    throw new ArgumentException("Provider와 두 화면의 활성화 계층을 분리해 주세요.");
                }
                _explorationView.ValidateReferences();

                _definition = _contentCatalog.ToDefinition();
                _session = new GameSession(_definition , new GameRandom());

                var model = new ShelterModel_model(_definition, _session.GetSnapshot());
                _shelterPresenter = new ShelterPresenter_presenter(model , _shelterView , this);
                var explorationModel = new ExplorationModel_model(_definition, _session.GetSnapshot());
                _explorationPresenter = new ExplorationPresenter_presenter(explorationModel, _explorationView, this);
                _explorationView.Close();
            }
            catch ( ArgumentException exception )
            {
                Debug.LogError($"게임 초기화 실패: {exception.Message}" , this);
                enabled = false;
                return;
            }

            LogInitialSnapshot();
        }

        private void OnEnable()
        {
            if ( _shelterPresenter != null && _explorationPresenter != null )
            {
                RefreshScreens();
            }
        }

        private void OnDisable()
        {
            _shelterPresenter?.Close();
            _explorationPresenter?.Close();
        }

        private void OnDestroy()
        {
            _shelterPresenter?.Dispose();
            _explorationPresenter?.Dispose();
            if ( _ownsExplorationView && _explorationView != null )
            {
                Destroy(_explorationView.gameObject);
            }
        }

        public GameSnapshot GetSnapshot()
        {
            if ( _session == null )
            {
                throw new InvalidOperationException("GameSession이 아직 초기화되지 않았습니다.");
            }

            return _session.GetSnapshot();
        }

        public bool TryStartExpedition(long expectedRevision , string survivorId , string regionId , out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }

            if ( !_session.TryStartExpedition(expectedRevision , survivorId , regionId , out rejectionReason) )
            {
                return false;
            }

            GameSnapshot snapshot = _session.GetSnapshot();

            CompleteCommand(true);

            Debug.Log($"출발 완료 | 지역: {regionId} | 상태: {snapshot.Phase} | Revision: {snapshot.Revision}" , this);
            return true;
        }

        public bool TryChooseEventAction(long expectedRevision, string eventId, string actionId, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryChooseEventAction(expectedRevision, eventId, actionId, out rejectionReason));
        }

        public bool TrySearch(long expectedRevision, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TrySearch(expectedRevision, out rejectionReason));
        }

        public bool TrySkipSearch(long expectedRevision, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TrySkipSearch(expectedRevision, out rejectionReason));
        }

        public bool TryAdvanceStage(long expectedRevision, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryAdvanceStage(expectedRevision, out rejectionReason));
        }

        public bool TryReturn(long expectedRevision, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryReturn(expectedRevision, out rejectionReason));
        }

        public bool TryResolveReturn(long expectedRevision, string eventId, string actionId, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryResolveReturn(expectedRevision, eventId, actionId, out rejectionReason));
        }

        public bool TryConfirmResult(long expectedRevision, long resultId, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryConfirmResult(expectedRevision, resultId, out rejectionReason));
        }

        public bool TryConfirmDaySummary(long expectedRevision, long summaryId, out string rejectionReason)
        {
            if ( !IsReady(out rejectionReason) )
            {
                return false;
            }
            return CompleteCommand(_session.TryConfirmDaySummary(expectedRevision, summaryId, out rejectionReason));
        }

        private bool IsReady(out string rejectionReason)
        {
            rejectionReason = null;
            if ( !isActiveAndEnabled || _session == null || _explorationPresenter == null )
            {
                rejectionReason = "게임이 준비되지 않았습니다.";
                return false;
            }
            return true;
        }

        private bool CompleteCommand(bool accepted)
        {
            if ( accepted )
            {
                RefreshScreens();
            }
            return accepted;
        }

        private void RefreshScreens()
        {
            GameSnapshot snapshot = _session.GetSnapshot();
            if ( snapshot.Phase == GAME_PHASE.SHELTER )
            {
                _explorationPresenter.Close();
                _shelterPresenter.Refresh(snapshot);
                _shelterPresenter.Open();
            }
            else
            {
                _shelterPresenter.Close();
                _explorationPresenter.Refresh(snapshot);
                _explorationPresenter.Open();
            }
        }

        private void LogInitialSnapshot()
        {
            GameSnapshot snapshot = _session.GetSnapshot();
            ResourceAmount resources = snapshot.Resources;

            Debug.Log($"게임 시작 | {snapshot.Day}일 | 상태: {snapshot.Phase} | Revision: {snapshot.Revision}" , this);
            Debug.Log($"식량: {resources.Food} | 의약품: {resources.Medicine} | 부품: {resources.Parts}" , this);
            Debug.Log($"생존자: {_definition.Survivors.Count}명 | 지역: {_definition.Regions.Count}곳 | 일반 사건: {_definition.Events.Count}개" , this);

            for ( int i = 0; i < snapshot.Survivors.Count; i++ )
            {
                SurvivorState survivor = snapshot.Survivors[i];
                Debug.Log($"생존자: {survivor.SurvivorId} | HP: {survivor.CurrentHp}" , this);
            }

            Debug.Log($"활성 탐색: {( snapshot.Expedition == null ? "없음" : "있음" )}" , this);
        }
    }
}
