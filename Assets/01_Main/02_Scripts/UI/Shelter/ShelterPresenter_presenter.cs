using System;
using System.Collections.Generic;
using HM.CodeBase;
using LastSupply.Core;
using LastSupply.Game;

namespace LastSupply.UI.Shelter
{
    public sealed class ShelterPresenter_presenter : APresenter
    {
        private readonly ShelterModel_model MODEL;
        private readonly ShelterPanel_view VIEW;
        private readonly LastSupplyGameProvider PROVIDER;
        private readonly List<string> SURVIVOR_IDS = new List<string>();
        private readonly List<string> REGION_IDS = new List<string>();

        private bool _isOpen;
        private bool _isDisposed;

        public ShelterPresenter_presenter(ShelterModel_model model , ShelterPanel_view view , LastSupplyGameProvider provider)
        {
            MODEL = model ?? throw new ArgumentNullException(nameof(model));
            VIEW = view != null ? view : throw new ArgumentNullException(nameof(view));
            PROVIDER = provider != null ? provider : throw new ArgumentNullException(nameof(provider));
        }

        public override void Open()
        {
            if ( _isDisposed )
            {
                throw new ObjectDisposedException(nameof(ShelterPresenter_presenter));
            }

            if ( _isOpen )
            {
                return;
            }

            MODEL.ApplySnapshot(PROVIDER.GetSnapshot());
            VIEW.Open();
            _isOpen = true;

            VIEW.SurvivorSelectionChanged += OnSurvivorSelectionActioned;
            VIEW.RegionSelectionChanged += OnRegionSelectionActioned;
            VIEW.StartRequested += OnStartActioned;

            Render();
        }

        public override void Close()
        {
            if ( !_isOpen )
            {
                return;
            }

            _isOpen = false;

            if ( VIEW != null )
            {
                VIEW.SurvivorSelectionChanged -= OnSurvivorSelectionActioned;
                VIEW.RegionSelectionChanged -= OnRegionSelectionActioned;
                VIEW.StartRequested -= OnStartActioned;
                VIEW.Close();
            }
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
            Render();
        }

        private void Render()
        {
            if ( !_isOpen )
            {
                return;
            }

            var survivorLabels = new List<string>();
            SURVIVOR_IDS.Clear();

            for ( int i = 0; i < MODEL.Survivors.Count; i++ )
            {
                SurvivorDefinition definition = MODEL.Survivors[i];
                SurvivorState state = MODEL.Snapshot.Survivors[i];

                if ( !state.IsAlive )
                {
                    continue;
                }

                SURVIVOR_IDS.Add(definition.ID);
                survivorLabels.Add($"{definition.Name} | HP {state.CurrentHp}/{definition.MaxHP}");
            }

            var regionLabels = new List<string>();
            REGION_IDS.Clear();

            for ( int i = 0; i < MODEL.Regions.Count; i++ )
            {
                RegionDefinition region = MODEL.Regions[i];
                REGION_IDS.Add(region.Id);
                regionLabels.Add(region.Name);
            }

            GameSnapshot snapshot = MODEL.Snapshot;
            ResourceAmount resources = snapshot.Resources;

            VIEW.SetOptions(survivorLabels , regionLabels);
            VIEW.SetSelections(SURVIVOR_IDS.IndexOf(MODEL.SelectedSurvivorId) + 1 , REGION_IDS.IndexOf(MODEL.SelectedRegionId) + 1);
            VIEW.SetStatus($"{snapshot.Day}일 | 식량 {resources.Food} | 의약품 {resources.Medicine} | 부품 {resources.Parts}");
            VIEW.SetInputState(snapshot.Phase == GAME_PHASE.SHELTER , MODEL.CanStart);
            VIEW.SetMessage(string.Empty);
        }

        private void OnSurvivorSelectionActioned(int index)
        {
            if ( !_isOpen )
            {
                return;
            }

            if ( index == 0 )
            {
                MODEL.ClearSurvivorSelection();
            }
            else if ( index > 0 && index <= SURVIVOR_IDS.Count )
            {
                MODEL.TrySelectSurvivor(SURVIVOR_IDS[ index - 1 ]);
            }

            Render();
        }

        private void OnRegionSelectionActioned(int index)
        {
            if ( !_isOpen )
            {
                return;
            }

            if ( index == 0 )
            {
                MODEL.ClearRegionSelection();
            }
            else if ( index > 0 && index <= REGION_IDS.Count )
            {
                MODEL.TrySelectRegion(REGION_IDS[ index - 1 ]);
            }

            Render();
        }

        private void OnStartActioned()
        {
            if ( !_isOpen || !MODEL.CanStart )
            {
                return;
            }

            VIEW.SetInputState(false , false);

            if ( !PROVIDER.TryStartExpedition(MODEL.Snapshot.Revision , MODEL.SelectedSurvivorId , MODEL.SelectedRegionId , out string reason) )
            {
                Refresh(PROVIDER.GetSnapshot());
                VIEW.SetMessage(reason);
            }
        }
    }
}