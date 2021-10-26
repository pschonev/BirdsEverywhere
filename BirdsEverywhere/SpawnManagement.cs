using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewModdingAPI;
using BirdsEverywhere.Spawners;
using static BirdsEverywhere.ModEntry;

namespace BirdsEverywhere
{
    public class SpawnManagement
    {
        public static List<string> getFullBirdList(Dictionary<string, List<string>> birdLists)
        {
            // this should create a combined list of all birds that gets shuffled with weights based on log of the lists index
            // when new areas are discovered, the individual birds lists should be added to the eligible birds list
            // then eligibleBirds.Contains can be used before choosing a new bird from the shuffle master list
            return birdLists["valleyBirds"];
        }

        public static void sampleTodaysBirds(string currentSeason)
        {
            birdsToday = new Dictionary<string, BirdData>();

            getUnseenBird(currentSeason);

            if (saveData.seenBirds.Count == 0)
                return;

            getSeenBirds(currentSeason);


        }

        public static void getUnseenBird(string currentSeason)
        {
            // RANDOMNESS!
            if (Game1.random.NextDouble() < 1.0 && saveData.unseenBirds.Count > 0)
            {
                for (int i = 0; i < saveData.unseenBirds.Count; i++)
                {
                    string birdName = saveData.unseenBirds[i];
                    BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
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

        public static void getSeenBirds(string currentSeason)
        {
            int maxLocationsWithBirds = 5;

            foreach (string birdName in saveData.seenBirds)
            {
                BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);

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


        private static SpawnData getSpawnData(BirdData data)
        {
            if (data.advancedSpawn.Count == 0 || !data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                return data.spawnData;
            else
                return sampleAdvancedSpawnData(data);
        }


        private static SpawnData sampleAdvancedSpawnData(BirdData data)
        {
            string season = Game1.currentSeason;
            List<SpawnData> possibleSpawns = data.advancedSpawn[season];

            // THIS IS RANDOM AND WILL BE BASED ON CHANCE LATER
            return Utils.getRandomElementFromList(possibleSpawns);

        }

        private static bool isVanillaBird(Critter critter)
        {
            if (critter is Birdie)
                return true;
            if (critter is Seagull)
                return true;
            if (critter is Owl)
                return true;
            if (critter is Woodpecker)
                return true;
            return false;
        }

        public static void removeVanillaBirds(GameLocation location)
        {
            foreach (var x in location.critters)
            {
                modInstance.Monitor.Log($"Spawn {x} at {x.position}", LogLevel.Debug);
            }
            location.critters.RemoveAll(c => isVanillaBird(c));
        }

        public static void Populate(GameLocation location)
        {
            if (location == null)
                return;

            string locationName = location.Name ?? ""; //get the location's name (blank if null)

            if (!birdsToday.ContainsKey(locationName))
                return;

            modInstance.Monitor.Log($"{locationName} is in birdsToday and should spawn {birdsToday[locationName]}", LogLevel.Debug);

            location.instantiateCrittersList(); //make sure the critter list isn't null

            string birdName = birdsToday[locationName].id;
            BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);

            SpawnerFactory.createSpawner(location, data).spawnBirds(location, data);
        }
    }
}
