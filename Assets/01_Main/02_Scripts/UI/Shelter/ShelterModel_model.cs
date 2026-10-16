using System;
using System.Collections.Generic;
using LastSupply.Core;

namespace LastSupply.UI.Shelter
{
    public sealed class ShelterModel_model
    {
        private readonly GameDefinition DEFINITION;

        private GameSnapshot _snapshot;
        private string _selectedSurvivorId;
        private string _selectedRegionId;

        public GameSnapshot Snapshot => _snapshot;
        public IReadOnlyList<SurvivorDefinition> Survivors => DEFINITION.Survivors;
        public IReadOnlyList<RegionDefinition> Regions => DEFINITION.Regions;
        public string SelectedSurvivorId => _selectedSurvivorId;
        public string SelectedRegionId => _selectedRegionId;

        public bool CanStart => _snapshot.Phase == GAME_PHASE.SHELTER
            && _selectedSurvivorId != null && _selectedRegionId != null;

        public ShelterModel_model(GameDefinition definition , GameSnapshot snapshot)
        {
            DEFINITION = definition ?? throw new ArgumentNullException(nameof(definition));
            ApplySnapshot(snapshot);
        }

        public void ApplySnapshot(GameSnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            if ( !IsLivingSurvivor(_selectedSurvivorId) )
            {
                _selectedSurvivorId = null;
            }
        }

        public bool TrySelectSurvivor(string survivorId)
        {
            if ( _snapshot.Phase != GAME_PHASE.SHELTER || !IsLivingSurvivor(survivorId) )
            {
                return false;
            }

            _selectedSurvivorId = survivorId;
            return true;
        }

        public bool TrySelectRegion(string regionId)
        {
            if ( _snapshot.Phase != GAME_PHASE.SHELTER )
            {
                return false;
            }

            for ( int i = 0; i < DEFINITION.Regions.Count; i++ )
            {
                if ( DEFINITION.Regions[ i ].Id == regionId )
                {
                    _selectedRegionId = regionId;
                    return true;
                }
            }

            return false;
        }

        private bool IsLivingSurvivor(string survivorId)
        {
            if ( string.IsNullOrWhiteSpace(survivorId) )
            {
                return false;
            }

            for ( int i = 0; i < _snapshot.Survivors.Count; i++ )
            {
                SurvivorState survivor = _snapshot.Survivors[i];
                if ( survivor.SurvivorId == survivorId && survivor.IsAlive )
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearSurvivorSelection()
        {
            _selectedSurvivorId = null;
        }

        public void ClearRegionSelection()
        {
            _selectedRegionId = null;
        }
    }
}