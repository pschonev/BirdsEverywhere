using BirdsEverywhere;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BirdsEverwhyere.UnitTests
{
    class MockBirdData : BirdData
    {
        public static string projectDir =
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));

        public static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "assets");

        public static string TemplatePath(string template)
        {
            string local = Path.Combine(projectDir, $"{template}.json");
            return File.Exists(local) ? local : Path.Combine(AssetsDir, "templates", $"{template}.json");
        }

        [JsonConstructor]
        public MockBirdData(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
            : base()
        {
            if (allSpawnData == null)
            {
                allSpawnData = new Dictionary<string, List<SpawnData>>();
            }
            if (defaultSpawnData == null)
            {
                defaultSpawnData = SpawnData.getDefaultSpawnData();
            }
            if (!string.IsNullOrEmpty(template))
            {
                string templateJson = File.ReadAllText(TemplatePath(template));
                MockBirdTemplate templateData = JsonSerializer.Deserialize<MockBirdTemplate>(templateJson);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);
            }

            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData = SpawnData.insertDefaults(this.defaultSpawnData, allSpawnData);
            this.allSpawnData = SpawnData.insertDefaults(globalDefaultSpawndata, this.allSpawnData);
        }
    }

    class MockBirdTemplate : BirdTemplate
    {
        [JsonConstructor]
        public MockBirdTemplate(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
           : base()
        {
            if (allSpawnData == null)
            {
                allSpawnData = new Dictionary<string, List<SpawnData>>();
            }
            if (defaultSpawnData == null)
            {
                defaultSpawnData = SpawnData.getDefaultSpawnData();
            }
            if (!string.IsNullOrEmpty(template))
            {
                string templateJson = File.ReadAllText(MockBirdData.TemplatePath(template));
                MockBirdTemplate templateData = JsonSerializer.Deserialize<MockBirdTemplate>(templateJson);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);
            }

            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData = allSpawnData;
        }
    }
}
