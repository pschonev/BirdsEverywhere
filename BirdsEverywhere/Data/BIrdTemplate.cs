using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BirdsEverywhere
{
    public class BirdTemplate
    {
        public string template { get; set; } = "";
        public SpawnData defaultSpawnData { get; set; }
        public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
            { "spring", new List<SpawnData>() {new SpawnData() } }
        }; // advanced spawn patters in the form season : SpawnData

        public static SpawnData globalDefaultSpawndata = SpawnData.getDefaultSpawnData();

        public BirdTemplate() { }

        [JsonConstructor]
        public BirdTemplate(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
        {
            if (!string.IsNullOrEmpty(template))
            {
                BirdTemplate templateData = ModEntry.modInstance.Helper.Content.Load<BirdTemplate>($"templates/{template}");
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
}
