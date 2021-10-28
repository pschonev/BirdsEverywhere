using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewModdingAPI;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere
{
    public class DailySpawner
    {
        public Dictionary<string, BirdData> birdsToday;

        public DailySpawner(string currentSeason, SaveData saveData)
        {
            sampleTodaysBirds(currentSeason,  saveData);
        }

        public void sampleTodaysBirds(string currentSeason, SaveData saveData)
        {
            birdsToday = new Dictionary<string, BirdData>();

            getUnseenBird(currentSeason, saveData);

            if (saveData.seenBirds.Count == 0)
                return;

            getSeenBirds(currentSeason, saveData);


        }

        private void getUnseenBird(string currentSeason, SaveData saveData)
        {
            // RANDOMNESS!
            if (Game1.random.NextDouble() < 1.0 && saveData.unseenBirds.Count > 0)
            {
                for (int i = 0; i < saveData.unseenBirds.Count; i++)
                {
                    string birdName = saveData.unseenBirds[i];
                    BirdData data = ModEntry.modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
                    if (data.seasons.Contains(currentSeason) || data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                    {
                        data.spawnData = getSpawnData(data);
                        // HAVE TO IMPLEMENT CHANCE VALUE WITH IF HERE
                        birdsToday.Add(Utils.getRandomElementFromList(data.spawnData.locations), data);
                        return;
                    }

                }
            }
        }

        private void getSeenBirds(string currentSeason, SaveData saveData)
        {
            int maxLocationsWithBirds = 5;

            foreach (string birdName in saveData.seenBirds)
            {
                BirdData data = ModEntry.modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);

                // look at next bird if this bird doesn't spawn in this season
                if (!data.seasons.Contains(currentSeason) && !data.advancedSpawn.Keys.Contains(currentSeason))
                    continue;
                // get default or advanced spawn data
                data.spawnData = getSpawnData(data);

                foreach (string spawnLocation in data.spawnData.locations)
                {
                    // only add bird if current location doesn't have birds yet
                    if (!birdsToday.ContainsKey(spawnLocation))
                    {
                        // HAVE TO IMPLEMENT CHANCE VALUE WITH IF HERE
                        birdsToday.Add(spawnLocation, data);
                        if (birdsToday.Count >= maxLocationsWithBirds)
                            return;
                    }
                }
            }
        }


        private SpawnData getSpawnData(BirdData data)
        {
            if (data.advancedSpawn.Count == 0 || !data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                return data.spawnData;
            else
                return sampleAdvancedSpawnData(data);
        }


        private SpawnData sampleAdvancedSpawnData(BirdData data)
        {
            string season = Game1.currentSeason;
            List<SpawnData> possibleSpawns = data.advancedSpawn[season];

            // THIS IS RANDOM AND WILL BE BASED ON CHANCE LATER
            return Utils.getRandomElementFromList(possibleSpawns);

        }


        public void Populate(GameLocation location)
        {
            if (location == null)
                return;

            string locationName = location.Name ?? ""; //get the location's name (blank if null)

            if (!birdsToday.ContainsKey(locationName))
                return;

            ModEntry.modInstance.Monitor.Log($"{locationName} is in birdsToday and should spawn {birdsToday[locationName]}", LogLevel.Debug);

            location.instantiateCrittersList(); //make sure the critter list isn't null

            string birdName = birdsToday[locationName].id;
            BirdData data = ModEntry.modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);

            SpawnerFactory.createSpawner(location, data).spawnBirds(location, data);
        }
    }
}
