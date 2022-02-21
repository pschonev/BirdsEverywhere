using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public SpawnData spawnData { get; set; } = new SpawnData();
        public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
            { "spring", new List<SpawnData>() {new SpawnData() } }
        }; // advanced spawn patters in the form season : SpawnData

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
