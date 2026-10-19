using Newtonsoft.Json;

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

        public SpawnData defaultSpawnData { get; set; } = new SpawnData(); // contains default values that will replace empty values in allSpawnData
        public SpawnData currentSpawnData { get; set; } = new SpawnData(); // contains the SpawnData that is currently used during the game
        public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
            { "spring", new List<SpawnData>() {new SpawnData() } }
        }; // all spawn patterns that the bird has in the form season : SpawnData

        public static readonly SpawnData globalDefaultSpawndata = SpawnData.getDefaultSpawnData();

        public BirdData() { }

        [JsonConstructor]
        public BirdData(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
        {
            if (allSpawnData == null)
            {
                allSpawnData = new Dictionary<string, List<SpawnData>>();
            }
            if (defaultSpawnData == null)
            {
                defaultSpawnData = SpawnData.getDefaultSpawnData();
            }
            this.defaultSpawnData = defaultSpawnData;
            this.allSpawnData = allSpawnData;
            this.template = template ?? "";
        }

        public void ApplyTemplate(Func<string, BirdTemplate> resolvedTemplate)
        {
            if (!string.IsNullOrEmpty(template))
            {
                BirdTemplate templateData = resolvedTemplate(template);
                templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
                allSpawnData.MergeDictionaries(templateData.allSpawnData);
            }
            allSpawnData = SpawnData.insertDefaults(defaultSpawnData, allSpawnData);
            allSpawnData = SpawnData.insertDefaults(globalDefaultSpawndata, allSpawnData);
        }

        public List<SpawnData> possibleSpawnDataToday(string season, bool isRaining, int day)
        {
            List<SpawnData> possibleSpawnData;
            if (this.allSpawnData.TryGetValue(season, out possibleSpawnData))
            {
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

        public static SpawnData chooseOneSpawnData(Random random, List<SpawnData> allPossibleSpawnData)
        {
            SpawnData spawnData = Utils.getRandomElementFromList(random, allPossibleSpawnData);
            spawnData.initTextureList();
            return spawnData;
        }
    }
}
