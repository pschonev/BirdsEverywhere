using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BirdsEverywhere
{
    public class BirdData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string scName { get; set; }
        public string description { get; set; } = "";

        public string egg { get; set; } = "";
        public string feather { get; set; } = "";

        public string template { get; set; } = "";

        public SpawnData defaultSpawnData { get; set; }
        public SpawnData currentSpawnData { get; set; } = new SpawnData();
        public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
            { "spring", new List<SpawnData>() {new SpawnData() } }
        }; // advanced spawn patters in the form season : SpawnData

        private static SpawnData globalDefaultSpawndata = SpawnData.getDefaultSpawnData();

        [JsonConstructor]
        public BirdData(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData)
        {
            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData = insertDefaults(defaultSpawnData, allSpawnData);
            this.allSpawnData = insertDefaults(globalDefaultSpawndata, this.allSpawnData);
        }

        public Dictionary<string, List<SpawnData>> insertDefaults(SpawnData defaults, Dictionary<string, List<SpawnData>> spawnDataBySeason)
        {
            foreach (KeyValuePair<string, List<SpawnData>> kvp in spawnDataBySeason) {
                foreach (SpawnData spawnData in kvp.Value)
                {
                    defaults.CopyProperties(spawnData);
                }
            }
            return spawnDataBySeason;
        }

        public List<SpawnData> possibleSpawnDataToday(string season, bool isRaining, int day)
        {
            List<SpawnData> possibleSpawnData;
            if (this.allSpawnData.TryGetValue(season, out possibleSpawnData)) {
                possibleSpawnData = possibleSpawnData.Where(x => x.isWeatherPossible(isRaining) && x.isDayPossible(day)).ToList();
                if (possibleSpawnData.Count > 0)
                {
                    return possibleSpawnData;
                } 
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            
        }

        public static SpawnData chooseOneSpawnData(List<SpawnData> allPossibleSpawnData)
        {
            SpawnData spawnData =  Utils.getRandomElementFromList(allPossibleSpawnData);
            spawnData.initTextureList();
            return spawnData;
        }
    }
}
