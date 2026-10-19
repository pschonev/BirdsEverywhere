using StardewValley;
using StardewModdingAPI;
using BirdsEverywhere.BirdTypes;
using StardewModdingAPI.Utilities;

namespace BirdsEverywhere.Spawners
{
    public class DailySpawner
    {
        // stores birds to spawn as { string locationName : BirdData }
        public Dictionary<string, BirdData> LocationSpecies { get; set; } = new Dictionary<string, BirdData>();
        public Dictionary<string, List<SingleBirdSpawnParameters>> LocationBirdPosition { get; set; } = new Dictionary<string, List<SingleBirdSpawnParameters>>();

        public void initializeBirdLocations(HashSet<string> seenBirds, List<Biome> biomes, IReadOnlyDictionary<string, BirdData> catalog, Random random, IMonitor monitor)
        {
            Today today = new Today(Game1.currentSeason, Game1.isRaining, SDate.Now().Day);
            sampleTodaysBirds(seenBirds, biomes, catalog, today, random, monitor);
            GetBirdPositions(random, monitor);
        }

        // ########################
        // # Sample Today's Birds #
        // ########################

        public readonly record struct Today(string Season, bool IsRaining, int Day);

        public void sampleTodaysBirds(HashSet<string> seenBirds, List<Biome> biomes, IReadOnlyDictionary<string, BirdData> catalog, Today today, Random random, IMonitor monitor)
        {
            // this should be between 0 (completely random) and 10 (keep strict order of bird rarity) in the future
            double todaysLuckFactor = 2.0;

            foreach (Biome biome in biomes)
            {
                List<string> birdListToday = Utils.shuffleListByOrder(random, biome.birds, todaysLuckFactor);
                addUnseenBirdForToday(seenBirds, birdListToday, catalog, today, random);
                if (seenBirds.Count > 0)
                    addSeenBirdsForToday(seenBirds, birdListToday, biome.locations.Count / 2, catalog, today, random);
            }

            monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in LocationSpecies)
            {
                monitor.Log($"A {kvp.Value.name} will spawn at {kvp.Key} today!", LogLevel.Debug);
            }
        }

        private void addUnseenBirdForToday(HashSet<string> seenBirds, List<string> birdListToday, IReadOnlyDictionary<string, BirdData> catalog, Today today, Random random)
        {
            foreach (string birdName in birdListToday)
            {
                if (seenBirds.Contains(birdName))
                    continue;

                BirdData data = catalog[birdName];
                List<SpawnData> allPossibleSpawnData = data.possibleSpawnDataToday(today.Season, today.IsRaining, today.Day);
                if (allPossibleSpawnData == null)
                    continue;

                data.currentSpawnData = BirdData.chooseOneSpawnData(random, allPossibleSpawnData);
                LocationSpecies.Add(Utils.getRandomElementFromList(random, data.currentSpawnData.locations), data);
                return;
            }
        }

        private void addSeenBirdsForToday(HashSet<string> seenBirds, List<string> birdListToday, int locationCount, IReadOnlyDictionary<string, BirdData> catalog, Today today, Random random)
        {
            int maxLocationsWithBirds = Math.Max(1, locationCount);

            foreach (string birdName in birdListToday)
            {
                if (!seenBirds.Contains(birdName))
                    continue;

                BirdData data = catalog[birdName];
                List<SpawnData> allPossibleSpawnData = data.possibleSpawnDataToday(today.Season, today.IsRaining, today.Day);

                // look at next bird if this bird doesn't meet spawn requirements
                if (allPossibleSpawnData == null)
                    continue;

                // get default or advanced spawn data
                data.currentSpawnData = BirdData.chooseOneSpawnData(random, allPossibleSpawnData);

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

        private void GetBirdPositions(Random random, IMonitor monitor)
        {
            foreach (KeyValuePair<string, BirdData> kvp in LocationSpecies)
            {
                string locationName = kvp.Key;
                GameLocation location = Game1.getLocationFromName(locationName);
                BirdData data = kvp.Value;

                LocationBirdPosition[locationName] = SpawnerFactory.createSpawner(location, data, monitor).spawnBirds(location, data, random, monitor);
            }
        }

        // #####################
        // # Populate Location #
        // #####################

        public void Populate(GameLocation location, BirdServices services)
        {
            if (!LocationBirdPosition.TryGetValue(location.Name ?? "", out List<SingleBirdSpawnParameters>? positions))
                return;

            location.instantiateCrittersList();

            foreach (SingleBirdSpawnParameters sParams in positions)
            {
                if (!sParams.stillValidSpawnPosition(location, services.Monitor))
                    continue;
                location.critters.Add(BirdFactory.createBird(sParams, location, services));
                services.Monitor.Log($"Added {sParams.ID} at {(int)sParams.position.X} - {(int)sParams.position.Y}.", LogLevel.Debug);
            }
        }
    }
}
