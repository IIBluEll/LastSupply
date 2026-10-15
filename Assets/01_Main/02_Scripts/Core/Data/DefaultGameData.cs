namespace LastSupply.Core
{
    // 기획서 v0.3의 초기 테스트 값. Unity 콘텐츠 에셋 연결 전 Core 검증에 사용합니다.
    public static class DefaultGameData
    {
        public const string MARCUS_ID = "SURVIVOR_MARCUS";
        public const string SARA_ID = "SURVIVOR_SARA";
        public const string MART_ID = "REGION_MART";
        public const string HOSPITAL_ID = "REGION_HOSPITAL";
        public const string WAREHOUSE_ID = "REGION_WAREHOUSE";

        public static GameDefinition CreateDefault()
        {
            return new GameDefinition(CreateRules() , CreateSurvivors() , CreateRegions() , CreateEvents());
        }

        // 기본판과 같은 규칙으로 생존자 1명·지역 1곳·일반 사건 3개만 구성합니다.
        public static GameDefinition CreatePrototype()
        {
            SurvivorDefinition[] survivors = CreateSurvivors();
            EventDefinition[] events = CreateEvents();
            var warehouse = new RegionDefinition(WAREHOUSE_ID , "공업 창고" , RESOURCE_TYPE.PARTS ,
                new[] { "EVENT_001", "EVENT_002", "EVENT_003" });

            return new GameDefinition(CreateRules() , new[] { survivors[ 0 ] } , new[] { warehouse } ,
                new[] { events[ 0 ], events[ 1 ], events[ 2 ] });
        }

        private static GameRulesDefinition CreateRules()
        {
            var chances = new[]
            {
                new StageEventChanceDefinition(1 , 65 , 20 , 15),
                new StageEventChanceDefinition(2 , 55 , 20 , 25),
                new StageEventChanceDefinition(3 , 45 , 20 , 35),
                new StageEventChanceDefinition(4 , 35 , 20 , 45),
                new StageEventChanceDefinition(5 , 25 , 20 , 55)
            };

            var risks = new[]
            {
                new ReturnRiskDefinition(0 , 2 , "안전" , 0 , 0),
                new ReturnRiskDefinition(3 , 3 , "주의" , 30 , 2),
                new ReturnRiskDefinition(4 , 4 , "위험" , 50 , 2),
                new ReturnRiskDefinition(5 , 5 , "매우 위험" , 75 , 3),
                new ReturnRiskDefinition(6 , 6 , "위험 확정" , 100 , 3)
            };

            var returnEvent = new ReturnEventDefinition("RETURN_001" , "돌아오는 길의 좀비 무리" ,
                "소리를 따라온 좀비들이 은신처로 돌아가는 길을 막았다." , "BREAK_THROUGH" ,
                "길을 뚫는다" , STAT_TYPE.COMBAT , 12);

            return new GameRulesDefinition(
                maxStage: 5 , maxNoise: 6 , maxDays: 7 , initialResources: new ResourceAmount(6 , 1 , 0) ,
                targetParts: 8 , dailyFoodConsumption: 2 , starvationDamage: 2 , healingMedicineCost: 1 , healingAmount: 3 ,
                defaultSearchRewards: CreateSearchRewards() , stageEventChances: chances , returnRisks: risks , returnEvent: returnEvent);
        }

        private static SearchRewardDefinition[] CreateSearchRewards()
        {
            return new[]
            {
                new SearchRewardDefinition(1 , 2 , 1 , 6),
                new SearchRewardDefinition(1 , 2 , 7 , 15 , new SearchRewardItemDefinition(1)),
                new SearchRewardDefinition(1 , 2 , 16 , 19 , new SearchRewardItemDefinition(2)),
                new SearchRewardDefinition(1 , 2 , 20 , 20 , new SearchRewardItemDefinition(3)),
                new SearchRewardDefinition(3 , 4 , 1 , 6),
                new SearchRewardDefinition(3 , 4 , 7 , 15 , new SearchRewardItemDefinition(1)),
                new SearchRewardDefinition(3 , 4 , 16 , 19 , new SearchRewardItemDefinition(3)),
                new SearchRewardDefinition(3 , 4 , 20 , 20 , new SearchRewardItemDefinition(4)),
                new SearchRewardDefinition(5 , 5 , 1 , 6),
                new SearchRewardDefinition(5 , 5 , 7 , 15 , new SearchRewardItemDefinition(1)),
                new SearchRewardDefinition(5 , 5 , 16 , 19 , new SearchRewardItemDefinition(4)),
                new SearchRewardDefinition(5 , 5 , 20 , 20 , new SearchRewardItemDefinition(5))
            };
        }

        private static SurvivorDefinition[] CreateSurvivors()
        {
            return new[]
            {
                new SurvivorDefinition(MARCUS_ID , "마커스" , 10 , 4 , 0),
                new SurvivorDefinition(SARA_ID , "사라" , 8 , 1 , 4)
            };
        }

        private static RegionDefinition[] CreateRegions()
        {
            return new[]
            {
                new RegionDefinition(MART_ID , "버려진 마트" , RESOURCE_TYPE.FOOD ,
                    new[] { "EVENT_001", "EVENT_002", "EVENT_003", "EVENT_005" }),
                new RegionDefinition(HOSPITAL_ID , "폐쇄된 병원" , RESOURCE_TYPE.MEDICINE ,
                    new[] { "EVENT_001", "EVENT_002", "EVENT_003", "EVENT_004", "EVENT_005" }),
                new RegionDefinition(WAREHOUSE_ID , "공업 창고" , RESOURCE_TYPE.PARTS ,
                    new[] { "EVENT_001", "EVENT_002", "EVENT_003", "EVENT_004", "EVENT_005" })
            };
        }

        private static EventDefinition[] CreateEvents()
        {
            var noEffect = new EventEffectDefinition(0 , 0);
            var pass = new EventActionDefinition("PASS" , "지나친다" , noEffect);

            return new[]
            {
                new EventDefinition("EVENT_001" , "잠긴 보관함" , "잠긴 보관함 안에서 쓸 만한 물자가 보인다." ,
                    EVENT_CATEGORY.OPPORTUNITY , EVENT_MODE.CHOICE , 1 , 5 , null ,
                    new EventActionDefinition("UNLOCK" , "자물쇠 해제" , STAT_TYPE.TECH , 14 ,
                        new EventEffectDefinition(0 , 0 , new SearchRewardItemDefinition(2)) , new EventEffectDefinition(0 , 1)) ,
                    new EventActionDefinition("FORCE_OPEN" , "강제로 개방" , STAT_TYPE.COMBAT , 11 ,
                        new EventEffectDefinition(0 , 2 , new SearchRewardItemDefinition(2)) , new EventEffectDefinition(-2 , 2)) , pass),

                new EventDefinition("EVENT_002" , "배회하는 좀비" , "돌아서기 전에 좀비가 접근했다. 길을 확보해야 한다." ,
                    EVENT_CATEGORY.DANGER , EVENT_MODE.CHECK , 1 , 5 , null ,
                    new EventActionDefinition("BREAK_THROUGH" , "돌파한다" , STAT_TYPE.COMBAT , 12 ,
                        new EventEffectDefinition(0 , 1) , new EventEffectDefinition(-3 , 2))),

                new EventDefinition("EVENT_003" , "버려진 물자" , "주인이 떠난 짐에서 쓸 만한 물자를 발견했다." ,
                    EVENT_CATEGORY.OPPORTUNITY , EVENT_MODE.INSTANT , 1 , 5 ,
                    new EventEffectDefinition(0 , 0 , new SearchRewardItemDefinition(1))),

                new EventDefinition("EVENT_004" , "고장 난 차단문" , "진입한 통로의 차단문이 내려왔다. 제어 장치를 조작해야 한다." ,
                    EVENT_CATEGORY.DANGER , EVENT_MODE.CHECK , 1 , 5 , null ,
                    new EventActionDefinition("CONTROL" , "제어 장치를 조작한다" , STAT_TYPE.TECH , 12 ,
                        noEffect , new EventEffectDefinition(-2 , 1))),

                new EventDefinition("EVENT_005" , "무너질 듯한 선반" , "불안정한 선반 위에 물자가 남아 있다. 건드리면 무너질 수도 있다." ,
                    EVENT_CATEGORY.OPPORTUNITY , EVENT_MODE.CHOICE , 1 , 5 , null ,
                    new EventActionDefinition("TAKE" , "물자를 꺼낸다" , STAT_TYPE.NONE , 13 ,
                        new EventEffectDefinition(0 , 0 , new SearchRewardItemDefinition(3)) , new EventEffectDefinition(-1 , 2)) , pass)
            };
        }
    }
}
