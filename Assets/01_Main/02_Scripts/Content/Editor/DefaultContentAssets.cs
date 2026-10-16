using System;
using System.Collections.Generic;
using System.IO;
using LastSupply.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSupply.Content.Editor
{
    public static class DefaultContentAssets
    {
        public const string CONTENT_ROOT = "Assets/01_Main/04_Resource/Content";
        public const string CATALOG_PATH = CONTENT_ROOT + "/Catalogs/DefaultGameContent.asset";

        [MenuItem("Last Supply/Content/Create Default Assets")]
        public static void CreateDefaultAssets()
        {
            Dictionary<string, ScriptableObject> assets = BuildDefaultAssets();
            try
            {
                // 전체 경로를 먼저 검사하여 기존 에셋을 일부라도 덮어쓰지 않습니다.
                foreach (string path in assets.Keys)
                {
                    if ( File.Exists(path) || File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null )
                    {
                        throw new InvalidOperationException($"기존 에셋을 보존하기 위해 생성을 중단했습니다: {path}");
                    }
                }

                foreach (var entry in assets)
                {
                    EnsureFolder(Path.GetDirectoryName(entry.Key).Replace('\\', '/'));
                    AssetDatabase.CreateAsset(entry.Value, entry.Key);
                }

                foreach (ScriptableObject asset in assets.Values)
                {
                    AssetDatabase.SaveAssetIfDirty(asset);
                }

                ValidateDefaultCatalog();
                Debug.Log($"기본 콘텐츠 에셋 {assets.Count}개 생성 완료: {CATALOG_PATH}");
            }
            finally
            {
                DestroyTransientAssets(assets);
            }
        }

        [MenuItem("Last Supply/Content/Validate Default Catalog")]
        public static void ValidateDefaultCatalog()
        {
            GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CATALOG_PATH);
            if ( catalog == null )
            {
                throw new InvalidOperationException($"기본 카탈로그를 찾을 수 없습니다: {CATALOG_PATH}");
            }

            ValidateCatalog(catalog);
        }

        [MenuItem("Last Supply/Content/Validate Selected Catalog")]
        public static void ValidateSelectedCatalog()
        {
            if ( !(Selection.activeObject is GameContentCatalog catalog) )
            {
                throw new InvalidOperationException("검증할 GameContentCatalog 에셋을 선택해 주세요.");
            }

            ValidateCatalog(catalog);
        }

        internal static Dictionary<string, ScriptableObject> BuildDefaultAssets()
        {
            GameDefinition source = DefaultGameData.CreateDefault();
            var assets = new Dictionary<string, ScriptableObject>(StringComparer.Ordinal);

            var gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.name = "DefaultGameConfig";
            WriteRules(gameConfig, source.Rules);
            assets.Add(CONTENT_ROOT + "/Rules/DefaultGameConfig.asset", gameConfig);

            var events = new EventConfig[source.Events.Count];
            var eventsById = new Dictionary<string, EventConfig>(StringComparer.Ordinal);
            for ( int i = 0; i < events.Length; i++ )
            {
                EventDefinition definition = source.Events[i];
                var config = ScriptableObject.CreateInstance<EventConfig>();
                config.name = definition.Id;
                using (var data = new SerializedObject(config))
                {
                    data.FindProperty("_id").stringValue = definition.Id;
                    data.FindProperty("_displayName").stringValue = definition.Name;
                    data.FindProperty("_description").stringValue = definition.Description;
                    data.FindProperty("_category").intValue = (int)definition.Category;
                    data.FindProperty("_mode").intValue = (int)definition.Mode;
                    data.FindProperty("_minStage").intValue = definition.MinStage;
                    data.FindProperty("_maxStage").intValue = definition.MaxStage;
                    WriteEffect(data.FindProperty("_instantEffect"), definition.InstantEffect);
                    SerializedProperty actions = data.FindProperty("_actions");
                    actions.arraySize = definition.Actions.Count;
                    for ( int j = 0; j < actions.arraySize; j++ )
                    {
                        EventActionDefinition action = definition.Actions[j];
                        SerializedProperty item = actions.GetArrayElementAtIndex(j);
                        item.FindPropertyRelative("_id").stringValue = action.Id;
                        item.FindPropertyRelative("_text").stringValue = action.Text;
                        item.FindPropertyRelative("_requiresCheck").boolValue = action.RequiresCheck;
                        item.FindPropertyRelative("_statType").intValue = (int)action.StatType;
                        item.FindPropertyRelative("_difficulty").intValue = action.Difficulty ?? 12;
                        WriteEffect(item.FindPropertyRelative("_effect"), action.Effect);
                        WriteEffect(item.FindPropertyRelative("_failureEffect"), action.FailureEffect);
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                events[i] = config;
                eventsById.Add(definition.Id, config);
                assets.Add(CONTENT_ROOT + "/Events/" + definition.Id + ".asset", config);
            }

            var survivors = new SurvivorConfig[source.Survivors.Count];
            for ( int i = 0; i < survivors.Length; i++ )
            {
                SurvivorDefinition definition = source.Survivors[i];
                var config = ScriptableObject.CreateInstance<SurvivorConfig>();
                config.name = definition.ID;
                using (var data = new SerializedObject(config))
                {
                    data.FindProperty("_id").stringValue = definition.ID;
                    data.FindProperty("_displayName").stringValue = definition.Name;
                    data.FindProperty("_maxHp").intValue = definition.MaxHP;
                    data.FindProperty("_combatModifier").intValue = definition.CombatModifier;
                    data.FindProperty("_techModifier").intValue = definition.TechModifier;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                survivors[i] = config;
                assets.Add(CONTENT_ROOT + "/Survivors/" + definition.ID + ".asset", config);
            }

            var regions = new RegionConfig[source.Regions.Count];
            for ( int i = 0; i < regions.Length; i++ )
            {
                RegionDefinition definition = source.Regions[i];
                var config = ScriptableObject.CreateInstance<RegionConfig>();
                config.name = definition.Id;
                using (var data = new SerializedObject(config))
                {
                    data.FindProperty("_id").stringValue = definition.Id;
                    data.FindProperty("_displayName").stringValue = definition.Name;
                    data.FindProperty("_description").stringValue = GetRegionDescription(definition.MainResource);
                    data.FindProperty("_mainResource").intValue = (int)definition.MainResource;
                    var regionEvents = new Object[definition.EventIds.Count];
                    for ( int j = 0; j < regionEvents.Length; j++ )
                    {
                        regionEvents[j] = eventsById[definition.EventIds[j]];
                    }

                    WriteReferences(data.FindProperty("_events"), regionEvents);
                    data.FindProperty("_useCustomSearchRewards").boolValue = definition.SearchRewards != null;
                    WriteSearchRewards(data.FindProperty("_searchRewards"), definition.SearchRewards);
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                regions[i] = config;
                assets.Add(CONTENT_ROOT + "/Regions/" + definition.Id + ".asset", config);
            }

            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.name = "DefaultGameContent";
            using (var data = new SerializedObject(catalog))
            {
                data.FindProperty("_gameConfig").objectReferenceValue = gameConfig;
                WriteReferences(data.FindProperty("_survivors"), survivors);
                WriteReferences(data.FindProperty("_regions"), regions);
                WriteReferences(data.FindProperty("_events"), events);
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            assets.Add(CATALOG_PATH, catalog);
            catalog.ToDefinition();
            return assets;
        }

        internal static void DestroyTransientAssets(Dictionary<string, ScriptableObject> assets)
        {
            foreach (ScriptableObject asset in assets.Values)
            {
                if ( asset != null && !EditorUtility.IsPersistent(asset) )
                {
                    Object.DestroyImmediate(asset);
                }
            }
        }

        private static void ValidateCatalog(GameContentCatalog catalog)
        {
            GameDefinition definition = catalog.ToDefinition();
            Debug.Log($"{catalog.name}: 검증 통과. 생존자 {definition.Survivors.Count}, 지역 {definition.Regions.Count}, 일반 사건 {definition.Events.Count}", catalog);
        }

        private static void EnsureFolder(string path)
        {
            if ( AssetDatabase.IsValidFolder(path) )
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            if ( string.IsNullOrEmpty(guid) )
            {
                throw new IOException($"폴더를 만들 수 없습니다: {path}");
            }
        }

        private static void WriteReferences(SerializedProperty property, Object[] references)
        {
            property.arraySize = references.Length;
            for ( int i = 0; i < references.Length; i++ )
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }
        }

        private static void WriteRules(GameConfig config, GameRulesDefinition rules)
        {
            using (var data = new SerializedObject(config))
            {
                data.FindProperty("_maxStage").intValue = rules.MaxStage;
                data.FindProperty("_maxNoise").intValue = rules.MaxNoise;
                data.FindProperty("_maxDays").intValue = rules.MaxDays;
                data.FindProperty("_initialFood").intValue = rules.InitialResources.Food;
                data.FindProperty("_initialMedicine").intValue = rules.InitialResources.Medicine;
                data.FindProperty("_initialParts").intValue = rules.InitialResources.Parts;
                data.FindProperty("_targetParts").intValue = rules.TargetParts;
                data.FindProperty("_dailyFoodConsumption").intValue = rules.DailyFoodConsumption;
                data.FindProperty("_starvationDamage").intValue = rules.StarvationDamage;
                data.FindProperty("_healingMedicineCost").intValue = rules.HealingMedicineCost;
                data.FindProperty("_healingAmount").intValue = rules.HealingAmount;
                WriteSearchRewards(data.FindProperty("_defaultSearchRewards"), rules.DefaultSearchRewards);

                SerializedProperty chances = data.FindProperty("_stageEventChances");
                chances.arraySize = rules.StageEventChances.Count;
                for ( int i = 0; i < chances.arraySize; i++ )
                {
                    StageEventChanceDefinition chance = rules.StageEventChances[i];
                    SerializedProperty item = chances.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("_stage").intValue = chance.Stage;
                    item.FindPropertyRelative("_noEventChance").intValue = chance.NoEventChance;
                    item.FindPropertyRelative("_opportunityChance").intValue = chance.OpportunityChance;
                    item.FindPropertyRelative("_dangerChance").intValue = chance.DangerChance;
                }

                SerializedProperty risks = data.FindProperty("_returnRisks");
                risks.arraySize = rules.ReturnRisks.Count;
                for ( int i = 0; i < risks.arraySize; i++ )
                {
                    ReturnRiskDefinition risk = rules.ReturnRisks[i];
                    SerializedProperty item = risks.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("_minNoise").intValue = risk.MinNoise;
                    item.FindPropertyRelative("_maxNoise").intValue = risk.MaxNoise;
                    item.FindPropertyRelative("_displayName").stringValue = risk.Name;
                    item.FindPropertyRelative("_eventChance").intValue = risk.EventChance;
                    item.FindPropertyRelative("_failureDamage").intValue = risk.FailureDamage;
                }

                SerializedProperty returnEvent = data.FindProperty("_returnEvent");
                returnEvent.FindPropertyRelative("_id").stringValue = rules.ReturnEvent.Id;
                returnEvent.FindPropertyRelative("_displayName").stringValue = rules.ReturnEvent.Name;
                returnEvent.FindPropertyRelative("_description").stringValue = rules.ReturnEvent.Description;
                returnEvent.FindPropertyRelative("_actionId").stringValue = rules.ReturnEvent.ActionId;
                returnEvent.FindPropertyRelative("_actionText").stringValue = rules.ReturnEvent.ActionText;
                returnEvent.FindPropertyRelative("_statType").intValue = (int)rules.ReturnEvent.StatType;
                returnEvent.FindPropertyRelative("_difficulty").intValue = rules.ReturnEvent.Difficulty;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WriteSearchRewards(SerializedProperty property, IReadOnlyList<SearchRewardDefinition> rows)
        {
            property.arraySize = rows?.Count ?? 0;
            for ( int i = 0; i < property.arraySize; i++ )
            {
                SearchRewardDefinition row = rows[i];
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("_minStage").intValue = row.MinStage;
                item.FindPropertyRelative("_maxStage").intValue = row.MaxStage;
                item.FindPropertyRelative("_minDice").intValue = row.MinDice;
                item.FindPropertyRelative("_maxDice").intValue = row.MaxDice;
                WriteRewards(item.FindPropertyRelative("_rewards"), row.Rewards);
            }
        }

        private static void WriteEffect(SerializedProperty property, EventEffectDefinition effect)
        {
            property.FindPropertyRelative("_hpChange").intValue = effect?.HpChange ?? 0;
            property.FindPropertyRelative("_noiseIncrease").intValue = effect?.NoiseIncrease ?? 0;
            WriteRewards(property.FindPropertyRelative("_rewards"), effect?.Rewards);
        }

        private static void WriteRewards(SerializedProperty property, IReadOnlyList<SearchRewardItemDefinition> rewards)
        {
            property.arraySize = rewards?.Count ?? 0;
            for ( int i = 0; i < property.arraySize; i++ )
            {
                SearchRewardItemDefinition reward = rewards[i];
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("_useRegionResource").boolValue = !reward.ResourceType.HasValue;
                item.FindPropertyRelative("_resourceType").intValue = (int)(reward.ResourceType ?? RESOURCE_TYPE.FOOD);
                item.FindPropertyRelative("_amount").intValue = reward.Amount;
            }
        }

        private static string GetRegionDescription(RESOURCE_TYPE resourceType)
        {
            switch ( resourceType )
            {
                case RESOURCE_TYPE.FOOD: return "남겨진 식량을 찾을 수 있는 버려진 마트.";
                case RESOURCE_TYPE.MEDICINE: return "남겨진 의약품을 찾을 수 있는 폐쇄된 병원.";
                case RESOURCE_TYPE.PARTS: return "구조 요청에 필요한 부품을 찾을 수 있는 공업 창고.";
                default: throw new ArgumentOutOfRangeException(nameof(resourceType));
            }
        }
    }
}
