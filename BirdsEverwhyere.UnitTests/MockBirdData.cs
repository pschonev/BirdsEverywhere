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
        public static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "assets");

        public static string TemplatePath(string template)
        {
            string local = Path.Combine(AppContext.BaseDirectory, $"{template}.json");
            return File.Exists(local) ? local : Path.Combine(AssetsDir, "templates", $"{template}.json");
        }

        public static BirdTemplate LoadTemplate(string name)
        {
            MockBirdTemplate template = JsonSerializer.Deserialize<MockBirdTemplate>(File.ReadAllText(TemplatePath(name)))!;
            template.ApplyTemplate(LoadTemplate);
            return template;
        }

        [JsonConstructor]
        public MockBirdData(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
            : base(defaultSpawnData, allSpawnData, template)
        {
            ApplyTemplate(LoadTemplate);
        }
    }

    class MockBirdTemplate : BirdTemplate
    {
        [JsonConstructor]
        public MockBirdTemplate(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
           : base(defaultSpawnData, allSpawnData, template)
        {
        }
    }
}
