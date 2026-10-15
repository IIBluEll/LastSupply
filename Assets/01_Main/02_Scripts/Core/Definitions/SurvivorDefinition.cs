using System;

namespace LastSupply.Core
{
    // 생존자 능력치
    public sealed class SurvivorDefinition
    {
        public string ID { get; }
        public string Name { get; }
        public int MaxHP { get; }
        public int CombatModifier { get; }
        public int TechModifier { get; }

        public SurvivorDefinition(string id , string name , int maxHp , int combatModifier , int techModifier)
        {
            if ( string.IsNullOrWhiteSpace(id) )
            {
                throw new ArgumentException("생존자 ID가 필요합니다." , nameof(id));
            }

            if ( string.IsNullOrWhiteSpace(name) )
            {
                throw new ArgumentException("생존자 이름이 필요합니다." , nameof(name));
            }

            if ( maxHp < 1 )
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            }

            ID = id;
            Name = name;
            MaxHP = maxHp;
            CombatModifier = combatModifier;
            TechModifier = techModifier;
        }
    }
}