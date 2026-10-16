using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [CreateAssetMenu(fileName = "SurvivorConfig" , menuName = "Last Supply/Survivor")]
    public class SurvivorConfig : ScriptableObject
    {
        [Header("식별 정보")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("화면 표시")]
        [SerializeField] private Sprite _portraitSprite;

        [Header("능력치")]
        [SerializeField, Min(1)] private int _maxHp = 10;
        [SerializeField] private int _combatModifier;
        [SerializeField] private int _techModifier;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite PortraitSprite => _portraitSprite;

        public SurvivorDefinition ToDefinition()
        {
            return new SurvivorDefinition(_id , _displayName , _maxHp , _combatModifier , _techModifier);
        }
    }
}

