using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;

namespace BirdsEverywhere.Spawners
{
    public class DailySpawner
    {
        public Dictionary<string, BirdData> birdsToday;

        public DailySpawner(string currentSeason, SaveData saveData)
        {
            sampleTodaysBirds(currentSeason, saveData.seenBirds, ModEntry.environmentData.biomes);
        }

        public void sampleTodaysBirds(string currentSeason, HashSet<string> seenBirds, List<Biome> biomes)
        {
            birdsToday = new Dictionary<string, BirdData>();
            // this should be between 0 (completely random) and 10 (keep strict order of bird rarity) in the future
            double todaysLuckFactor = 2.0;

            foreach (Biome biome in biomes)
            {
                List<string> birdListToday = Utils.shuffleListByOrder(biome.birds, todaysLuckFactor);
                addUnseenBirdForToday(currentSeason, seenBirds, birdListToday);
                if (seenBirds.Count > 0)
                    addSeenBirdsForToday(currentSeason, seenBirds, birdListToday, biome.locations.Count);
            }
        }

        private void addUnseenBirdForToday(string currentSeason, HashSet<string> seenBirds, List<string> birdListToday)
        {
            List<string> unseenBirdsInTodaysOrder = birdListToday.Where(x => !seenBirds.Contains(x)).ToList();

            if (unseenBirdsInTodaysOrder.Count == 0)
                return;

            foreach(string birdName in unseenBirdsInTodaysOrder)
            {
                BirdData data = ModEntry.modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
                if (data.seasons.Contains(currentSeason) || data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                {
                    data.spawnData = getSpawnData(data);
                    birdsToday.Add(Utils.getRandomElementFromList(data.spawnData.locations), data);
                    return;
                }

            }
        }

        private void addSeenBirdsForToday(string currentSeason, HashSet<string> seenBirds, List<string> birdListToday, int locationCount)
        {
            int maxLocationsWithBirds = Math.Max(1, locationCount / 2);
            List<string> seenBirdsInTodaysOrder = birdListToday.Where(x => seenBirds.Contains(x)).ToList();

            foreach (string birdName in seenBirdsInTodaysOrder)
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
