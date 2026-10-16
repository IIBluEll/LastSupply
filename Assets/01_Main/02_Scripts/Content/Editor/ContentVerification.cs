using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LastSupply.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSupply.Content.Editor
{
    // 실제 Unity 직렬화와 에셋 참조를 검사합니다. Scene이나 게임 화면을 구성하지 않습니다.
    public static class ContentVerification
    {
        private static int s_checkCount;

        public static void CreateAndVerify()
        {
            DefaultContentAssets.CreateDefaultAssets();
            Run();
        }

        [MenuItem("Last Supply/Content/Run Content Checks")]
        public static void Run()
        {
            s_checkCount = 0;
            string[] assetGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { DefaultContentAssets.CONTENT_ROOT });
            Assert(assetGuids.Length == 12, "기본 콘텐츠 에셋 수는 12개여야 합니다.");
            foreach (string guid in assetGuids)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }

            var savedCatalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(DefaultContentAssets.CATALOG_PATH);
            Assert(savedCatalog != null, "저장된 기본 카탈로그가 없습니다.");
            GameDefinition expected = DefaultGameData.CreateDefault();
            GameDefinition actual = savedCatalog.ToDefinition();
            AssertEquivalent(expected, actual, "SavedCatalog");

            Assert(savedCatalog.TryGetSurvivorConfig(DefaultGameData.MARCUS_ID, out SurvivorConfig marcus), "생존자 ID 조회 실패");
            Assert(marcus.DisplayName == "마커스", "생존자 표시 정보 불일치");
            Assert(savedCatalog.TryGetRegionConfig(DefaultGameData.WAREHOUSE_ID, out RegionConfig warehouse), "지역 ID 조회 실패");
            Assert(!string.IsNullOrWhiteSpace(warehouse.Description), "지역 설명 누락");
            Assert(!savedCatalog.TryGetSurvivorConfig("survivor_marcus", out _), "ID는 대소문자를 구분해야 합니다.");
            Assert(!savedCatalog.TryGetSurvivorConfig(null, out _), "null ID가 조회되었습니다.");
            Assert(!savedCatalog.TryGetRegionConfig("MISSING", out _), "없는 지역이 조회되었습니다.");

            var session = new GameSession(actual, new GameRandom());
            Assert(session.TryStartExpedition(session.GetSnapshot().Revision, actual.Survivors[0].ID, actual.Regions[0].Id, out _), "에셋 정의로 Session 출발 실패");

            string catalogBefore = File.ReadAllText(DefaultContentAssets.CATALOG_PATH);
            try
            {
                DefaultContentAssets.CreateDefaultAssets();
                throw new Exception("기존 기본 에셋의 재생성이 허용되었습니다.");
            }
            catch ( InvalidOperationException exception )
            {
                Assert(exception.Message.Contains("기존 에셋"), "재생성 거절 사유 불일치");
            }
            Assert(catalogBefore == File.ReadAllText(DefaultContentAssets.CATALOG_PATH), "재생성 거절 후 카탈로그 파일 변경");

            CheckRejected(assets => Change(assets[DefaultContentAssets.CATALOG_PATH], "_gameConfig", p => p.objectReferenceValue = null), "공통 게임 설정");
            CheckRejected(assets => Change(assets[DefaultContentAssets.CATALOG_PATH], "_survivors.Array.data[1]", p => p.objectReferenceValue = assets[PathFor("Survivors/SURVIVOR_MARCUS")]), "중복 ID");
            CheckRejected(assets => Change(assets[DefaultContentAssets.CATALOG_PATH], "_events.Array.data[0]", p => p.objectReferenceValue = null), "사건 0번");
            CheckRejected(assets => Change(assets[PathFor("Events/EVENT_001")], "_id", p => p.stringValue = ""), "EVENT_001");
            CheckRejected(assets => Change(assets[PathFor("Regions/REGION_WAREHOUSE")], "_events.Array.data[0]", p => p.objectReferenceValue = null), "REGION_WAREHOUSE");
            CheckRejected(assets =>
            {
                var other = Object.Instantiate((EventConfig)assets[PathFor("Events/EVENT_001")]);
                other.name = "UnregisteredSameId";
                assets.Add("transient-only", other);
                Change(assets[PathFor("Regions/REGION_WAREHOUSE")], "_events.Array.data[0]", p => p.objectReferenceValue = other);
            }, "카탈로그에 등록되지");
            CheckRejected(assets => Change(assets[PathFor("Rules/DefaultGameConfig")], "_stageEventChances", p => p.arraySize = 4), "누락된 단계");
            CheckRejected(assets => Change(assets[PathFor("Rules/DefaultGameConfig")], "_stageEventChances.Array.data[0]._noEventChance", p => p.intValue = 64), "합은 100");
            CheckRejected(assets => Change(assets[PathFor("Rules/DefaultGameConfig")], "_defaultSearchRewards", p => p.arraySize = 11), "구간 누락");
            CheckRejected(assets => Change(assets[PathFor("Rules/DefaultGameConfig")], "_returnRisks.Array.data[1]._minNoise", p => p.intValue = 2), "소음 구간");
            CheckRejected(assets => Change(assets[PathFor("Events/EVENT_002")], "_actions", p => p.arraySize = 0), "판정 행동");
            CheckRejected(assets => Change(assets[PathFor("Regions/REGION_WAREHOUSE")], "_events", p => p.arraySize = 0), "후보가 없습니다");
            CheckRejected(assets => Change(assets[PathFor("Regions/REGION_WAREHOUSE")], "_useCustomSearchRewards", p => p.boolValue = true), "보상표가 비어");

            CheckIndependenceAndMixedRewards();
            Debug.Log($"CONTENT_CHECKS_PASSED: {s_checkCount} assertions; 12 saved assets reimported; default-data equivalence, lookup, invalid data, independent definitions, mixed rewards, overwrite protection, Session startup.");
        }

        private static string PathFor(string relativePath)
        {
            return DefaultContentAssets.CONTENT_ROOT + "/" + relativePath + ".asset";
        }

        private static void CheckRejected(Action<Dictionary<string, ScriptableObject>> change, string expectedMessage)
        {
            Dictionary<string, ScriptableObject> assets = DefaultContentAssets.BuildDefaultAssets();
            try
            {
                change(assets);
                var catalog = (GameContentCatalog)assets[DefaultContentAssets.CATALOG_PATH];
                try
                {
                    catalog.ToDefinition();
                }
                catch ( ArgumentException exception )
                {
                    Assert(exception.Message.Contains(expectedMessage), $"오류 문맥 누락: {expectedMessage}; 실제: {exception.Message}");
                    return;
                }

                throw new Exception("잘못된 콘텐츠가 허용되었습니다: " + expectedMessage);
            }
            finally
            {
                DefaultContentAssets.DestroyTransientAssets(assets);
            }
        }

        private static void CheckIndependenceAndMixedRewards()
        {
            Dictionary<string, ScriptableObject> assets = DefaultContentAssets.BuildDefaultAssets();
            try
            {
                var catalog = (GameContentCatalog)assets[DefaultContentAssets.CATALOG_PATH];
                GameDefinition before = catalog.ToDefinition();
                Change(assets[PathFor("Rules/DefaultGameConfig")], "_targetParts", p => p.intValue = 9);
                Change(assets[PathFor("Survivors/SURVIVOR_MARCUS")], "_maxHp", p => p.intValue = 11);
                Change(assets[PathFor("Events/EVENT_003")], "_instantEffect._rewards.Array.data[0]._amount", p => p.intValue = 2);
                AssertEquivalent(DefaultGameData.CreateDefault(), before, "IndependentDefinition");
                GameDefinition after = catalog.ToDefinition();
                Assert(after.Rules.TargetParts == 9 && after.Survivors[0].MaxHP == 11 && after.Events[2].InstantEffect.Rewards[0].Amount == 2, "새 정의에 변경이 반영되지 않았습니다.");

                var region = (RegionConfig)assets[PathFor("Regions/REGION_WAREHOUSE")];
                Change(region, "_useCustomSearchRewards", p => p.boolValue = true);
                Change(region, "_searchRewards", p =>
                {
                    p.arraySize = 1;
                    SerializedProperty row = p.GetArrayElementAtIndex(0);
                    row.FindPropertyRelative("_minStage").intValue = 1;
                    row.FindPropertyRelative("_maxStage").intValue = 5;
                    row.FindPropertyRelative("_minDice").intValue = 1;
                    row.FindPropertyRelative("_maxDice").intValue = 20;
                    SerializedProperty rewards = row.FindPropertyRelative("_rewards");
                    rewards.arraySize = 2;
                    rewards.GetArrayElementAtIndex(0).FindPropertyRelative("_useRegionResource").boolValue = true;
                    rewards.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = 2;
                    rewards.GetArrayElementAtIndex(1).FindPropertyRelative("_useRegionResource").boolValue = false;
                    rewards.GetArrayElementAtIndex(1).FindPropertyRelative("_resourceType").intValue = (int)RESOURCE_TYPE.MEDICINE;
                    rewards.GetArrayElementAtIndex(1).FindPropertyRelative("_amount").intValue = 1;
                });

                GameDefinition mixed = catalog.ToDefinition();
                RegionDefinition warehouse = mixed.Regions[2];
                ResourceAmount reward = ExplorationRules.CalculateSearchReward(1, 1, warehouse.MainResource, mixed.Rules.DefaultSearchRewards, warehouse.SearchRewards);
                Assert(reward.Food == 0 && reward.Medicine == 1 && reward.Parts == 2, "지역 전용 복수 보상 해석 오류");
                Assert(before.Regions[2].SearchRewards == null && after.Regions[2].SearchRewards == null, "이전 지역 정의가 변경되었습니다.");
            }
            finally
            {
                DefaultContentAssets.DestroyTransientAssets(assets);
            }
        }

        private static void Change(Object target, string propertyPath, Action<SerializedProperty> change)
        {
            using (var data = new SerializedObject(target))
            {
                SerializedProperty property = data.FindProperty(propertyPath);
                Assert(property != null, "직렬화 경로를 찾을 수 없습니다: " + propertyPath);
                change(property);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssertEquivalent(object expected, object actual, string path)
        {
            if ( expected == null || actual == null )
            {
                Assert(expected == null && actual == null, path + ": null 불일치");
                return;
            }

            Type type = expected.GetType();
            Assert(type == actual.GetType(), path + ": 타입 불일치");
            if ( type.IsPrimitive || type.IsEnum || expected is string )
            {
                Assert(expected.Equals(actual), path + ": 값 불일치");
                return;
            }

            if ( expected is IEnumerable sequence )
            {
                object[] left = sequence.Cast<object>().ToArray();
                object[] right = ((IEnumerable)actual).Cast<object>().ToArray();
                Assert(left.Length == right.Length, path + ": 목록 길이 불일치");
                for ( int i = 0; i < left.Length; i++ )
                {
                    AssertEquivalent(left[i], right[i], path + "[" + i + "]");
                }
                return;
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if ( property.GetIndexParameters().Length == 0 )
                {
                    AssertEquivalent(property.GetValue(expected), property.GetValue(actual), path + "." + property.Name);
                }
            }
        }

        private static void Assert(bool condition, string message)
        {
            if ( !condition )
            {
                throw new Exception(message);
            }
            s_checkCount++;
        }
    }
}
