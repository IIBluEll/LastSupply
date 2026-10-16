using System;
using LastSupply.Core;
using UnityEngine;

namespace LastSupply.Content
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Last Supply/Game")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("진행")]
        [SerializeField, Min(1)] private int _maxStage = 5;
        [SerializeField, Min(0)] private int _maxNoise = 6;
        [SerializeField, Min(1)] private int _maxDays = 7;

        [Header("초기 자원과 목표")]
        [SerializeField, Min(0)] private int _initialFood = 6;
        [SerializeField, Min(0)] private int _initialMedicine = 1;
        [SerializeField, Min(0)] private int _initialParts;
        [SerializeField, Min(1)] private int _targetParts = 8;

        [Header("정산과 치료")]
        [SerializeField, Min(0)] private int _dailyFoodConsumption = 2;
        [SerializeField, Min(0)] private int _starvationDamage = 2;
        [SerializeField, Min(1)] private int _healingMedicineCost = 1;
        [SerializeField, Min(1)] private int _healingAmount = 3;

        [Header("공통 표")]
        [SerializeField] private SearchRewardData[] _defaultSearchRewards = Array.Empty<SearchRewardData>();
        [SerializeField] private StageEventChanceData[] _stageEventChances = Array.Empty<StageEventChanceData>();
        [SerializeField] private ReturnRiskData[] _returnRisks = Array.Empty<ReturnRiskData>();

        [Header("귀환 사건")]
        [SerializeField] private ReturnEventData _returnEvent = new ReturnEventData();

        public GameRulesDefinition ToDefinition()
        {
            if ( _defaultSearchRewards == null )
            {
                throw new ArgumentNullException(nameof(_defaultSearchRewards));
            }

            if ( _stageEventChances == null )
            {
                throw new ArgumentNullException(nameof(_stageEventChances));
            }

            if ( _returnRisks == null )
            {
                throw new ArgumentNullException(nameof(_returnRisks));
            }

            if ( _returnEvent == null )
            {
                throw new ArgumentNullException(nameof(_returnEvent));
            }

            var rewards = new SearchRewardDefinition[_defaultSearchRewards.Length];
            for ( int i = 0; i < rewards.Length; i++ )
            {
                if ( _defaultSearchRewards[i] == null )
                {
                    throw new ArgumentException($"기본 수색 보상표 {i}번 행이 비어 있습니다.", nameof(_defaultSearchRewards));
                }

                rewards[i] = _defaultSearchRewards[i].ToDefinition();
            }

            var chances = new StageEventChanceDefinition[_stageEventChances.Length];
            for ( int i = 0; i < chances.Length; i++ )
            {
                if ( _stageEventChances[i] == null )
                {
                    throw new ArgumentException($"사건 확률표 {i}번 행이 비어 있습니다.", nameof(_stageEventChances));
                }

                chances[i] = _stageEventChances[i].ToDefinition();
            }

            var risks = new ReturnRiskDefinition[_returnRisks.Length];
            for ( int i = 0; i < risks.Length; i++ )
            {
                if ( _returnRisks[i] == null )
                {
                    throw new ArgumentException($"귀환 위험표 {i}번 행이 비어 있습니다.", nameof(_returnRisks));
                }

                risks[i] = _returnRisks[i].ToDefinition();
            }

            var initialResources = new ResourceAmount(_initialFood, _initialMedicine, _initialParts);
            return new GameRulesDefinition(
                _maxStage, _maxNoise, _maxDays, initialResources, _targetParts,
                _dailyFoodConsumption, _starvationDamage, _healingMedicineCost, _healingAmount,
                rewards, chances, risks, _returnEvent.ToDefinition());
        }
    }
}
