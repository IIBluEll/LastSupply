using System;

namespace LastSupply.Core
{
    // 생존자 현재 HP
    public sealed class SurvivorState
    {
        public string SurvivorId { get; }
        public int CurrentHp { get; }

        public bool IsAlive => CurrentHp > 0;

        public SurvivorState(SurvivorDefinition definition , int currentHp)
        {
            if ( definition == null )
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if ( currentHp < 0 || currentHp > definition.MaxHP )
            {
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            }

            SurvivorId = definition.ID;
            CurrentHp = currentHp;
        }
    }
}