using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;

namespace BirdsEverywhere
{
    public class BirdTemplate
    {
        public string template { get; set; } = "";
        public SpawnData defaultSpawnData { get; set; } = null;
        public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
            { "spring", new List<SpawnData>() {new SpawnData() } }
        }; // advanced spawn patters in the form season : SpawnData

        public static SpawnData globalDefaultSpawndata = SpawnData.getDefaultSpawnData();

        // only for testing
        public BirdTemplate() {}

        [JsonConstructor]
        public BirdTemplate(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
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
                BirdTemplate templateData = ModEntry.modInstance.Helper.Content.Load<BirdTemplate>($"assets/templates/{template}.json", ContentSource.ModFolder);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);            
            }

            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData = allSpawnData;
        }

        
    }
}
