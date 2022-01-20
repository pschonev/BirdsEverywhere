using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using BirdsEverywhere.BirdTypes;

namespace BirdsEverywhere.Spawners
{
    public class DailySpawner
    {
        // stores birds to spawn as { string locationName : BirdData }
        public Dictionary<string, BirdData> LocationSpecies { get; set; } = new Dictionary<string, BirdData>();
        public Dictionary<string, List<SingleBirdSpawnParameters>> LocationBirdPosition { get; set; } = new Dictionary<string, List<SingleBirdSpawnParameters>>();

        public void initializeBirdLocations(HashSet<string> seenBirds, List<Biome> biomes)
        {
            // samples which bird species spawn today at what location
            // also samples which spawner to use for the species
            sampleTodaysBirds(seenBirds, biomes);

            // get exact spawn positions of each bird to be spawned
            GetBirdPositions();
        }

        // ########################
        // # Sample Today's Birds #
        // ########################

        private void sampleTodaysBirds(HashSet<string> seenBirds, List<Biome> biomes)
        {
            // this should be between 0 (completely random) and 10 (keep strict order of bird rarity) in the future
            double todaysLuckFactor = 2.0;

            foreach (Biome biome in biomes)
            {
                List<string> birdListToday = Utils.shuffleListByOrder(biome.birds, todaysLuckFactor);
                addUnseenBirdForToday(seenBirds, birdListToday);
                if (seenBirds.Count > 0)
                    addSeenBirdsForToday(seenBirds, birdListToday, biome.locations.Count);
            }
            
            // logging
            ModEntry.modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in LocationSpecies)
            {
                ModEntry.modInstance.Monitor.Log($"A {kvp.Value.name} will spawn at {kvp.Key} today!", LogLevel.Debug);
            }
        }

        private void addUnseenBirdForToday(HashSet<string> seenBirds, List<string> birdListToday)
        {
            // filter out birds that were already seen
            List<string> unseenBirdsInTodaysOrder = birdListToday.Where(x => !seenBirds.Contains(x)).ToList();

            if (unseenBirdsInTodaysOrder.Count == 0)
                return;

            foreach (string birdName in unseenBirdsInTodaysOrder)
            {
                BirdData data = ModEntry.birdDataCollection[birdName];
                if (data.seasons.Contains(Game1.currentSeason) || data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                {
                    data.spawnData = getSpawnData(data);
                    LocationSpecies.Add(Utils.getRandomElementFromList(data.spawnData.locations), data);
                    return;
                }

            }
        }

        private void addSeenBirdsForToday(HashSet<string> seenBirds, List<string> birdListToday, int locationCount)
        {
            int maxLocationsWithBirds = Math.Max(1, locationCount / 2);
            List<string> seenBirdsInTodaysOrder = birdListToday.Where(x => seenBirds.Contains(x)).ToList();

            foreach (string birdName in seenBirdsInTodaysOrder)
            {
                BirdData data = ModEntry.birdDataCollection[birdName];

                // look at next bird if this bird doesn't spawn in this season
                if (!data.seasons.Contains(Game1.currentSeason) && !data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                    continue;

                // get default or advanced spawn data
                data.spawnData = getSpawnData(data);

                foreach (string spawnLocation in data.spawnData.locations)
                {
                    // only add bird if current location doesn't have birds yet
                    if (!LocationSpecies.ContainsKey(spawnLocation))
                    {
                        LocationSpecies.Add(spawnLocation, data);
                        if (LocationSpecies.Count >= maxLocationsWithBirds)
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

        // #############################
        // # Get Exact Spawn Locations #
        // #############################

        private void GetBirdPositions()
        {
            foreach (KeyValuePair<string, BirdData> kvp in LocationSpecies)
            {
                string locationName = kvp.Key;
                GameLocation location = Game1.getLocationFromName(locationName);
                BirdData data = kvp.Value;

                LocationBirdPosition[locationName] = SpawnerFactory.createSpawner(location, data).spawnBirds(location, data);
            }


        }

        // #####################
        // # Populate Location #
        // #####################

        public void Populate(GameLocation location)
        {
            string locationName = location.Name ?? ""; //get the location's name (blank if null)

            if (!LocationBirdPosition.ContainsKey(locationName))
                return;

            location.instantiateCrittersList(); //make sure the critter list isn't null

            foreach (SingleBirdSpawnParameters sParams in LocationBirdPosition[locationName])
            {
                location.critters.Add(BirdFactory.createBird((int)sParams.Position.X, (int)sParams.Position.Y, sParams.ID, sParams.BirdType));
                ModEntry.modInstance.Monitor.Log($"Added {sParams.ID} at {(int)sParams.Position.X} - {(int)sParams.Position.Y}.", LogLevel.Debug);
            }
        }
    }

    public class SingleBirdSpawnParameters{
        public Vector2 Position;
        public string ID;
        public string BirdType;
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType)
        {
            this.Position = position;
            this.ID = id;
            this.BirdType = birdType;
        }
    }
}
