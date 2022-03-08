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
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

        [JsonConstructor]
        public MockBirdData(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template) 
            : base()
        {
            if (!string.IsNullOrEmpty(template))
            {
                string templatePath = Path.Combine(projectDir, $"{template}.json");
                string templateJson = File.ReadAllText(templatePath);
                MockBirdTemplate templateData = JsonSerializer.Deserialize<MockBirdTemplate>(templateJson);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);
            }
            if (allSpawnData == null)
            {
                allSpawnData = new Dictionary<string, List<SpawnData>>();
            }
            if (defaultSpawnData == null)
            {
                defaultSpawnData = SpawnData.getDefaultSpawnData();
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
            if (!string.IsNullOrEmpty(template))
            {
                string templatePath = Path.Combine(MockBirdData.projectDir, $"{template}.json");
                string templateJson = File.ReadAllText(templatePath);
                MockBirdTemplate templateData = JsonSerializer.Deserialize<MockBirdTemplate>(templateJson);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);
            }
            if (allSpawnData == null)
            {
                allSpawnData = new Dictionary<string, List<SpawnData>>();
            }
            if (defaultSpawnData == null)
            {
                defaultSpawnData = SpawnData.getDefaultSpawnData();
            }

            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData =  allSpawnData;
        }
    }
}
