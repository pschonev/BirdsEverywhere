using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using BirdsEverywhere.BirdTypes;
using StardewModdingAPI.Utilities;

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
                    addSeenBirdsForToday(seenBirds, birdListToday, biome.locations.Count / 2);
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
                List<SpawnData> allPossibleSpawnData = data.possibleSpawnDataToday(Game1.currentSeason, Game1.isRaining, SDate.Now().Day);
                if (allPossibleSpawnData == null)
                    continue;

                data.currentSpawnData = BirdData.chooseOneSpawnData(allPossibleSpawnData);
                LocationSpecies.Add(Utils.getRandomElementFromList(data.currentSpawnData.locations), data);
                return;
            }
        }

        private void addSeenBirdsForToday(HashSet<string> seenBirds, List<string> birdListToday, int locationCount)
        {
            int maxLocationsWithBirds = Math.Max(1, locationCount);
            List<string> seenBirdsInTodaysOrder = birdListToday.Where(x => seenBirds.Contains(x)).ToList();

            foreach (string birdName in seenBirdsInTodaysOrder)
            {
                BirdData data = ModEntry.birdDataCollection[birdName];
                List<SpawnData> allPossibleSpawnData = data.possibleSpawnDataToday(Game1.currentSeason, Game1.isRaining, SDate.Now().Day);

                // look at next bird if this bird doesn't meet spawn requirements
                if (allPossibleSpawnData == null)
                    continue;

                // get default or advanced spawn data
                data.currentSpawnData = BirdData.chooseOneSpawnData(allPossibleSpawnData);

                foreach (string spawnLocation in data.currentSpawnData.locations)
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
                if (!sParams.stillValidSpawnPosition(location))
                    continue;
                location.critters.Add(BirdFactory.createBird(sParams, location));
                ModEntry.modInstance.Monitor.Log($"Added {sParams.ID} at {(int)sParams.position.X} - {(int)sParams.position.Y}.", LogLevel.Debug);
            }
        }
    }
}
