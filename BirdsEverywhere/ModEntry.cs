using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.Birds;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static Mod modInstance;

        private static List<string> availableSpawnLocatiions = new List<string>() {"Backwoods", "Forest"};
        private static List<string> islandLocations = new List<string>() {"ISLANDWEST" };
        private static List<string> expandedLocations = new List<string>() { "VINEYARD" };

        private static List<string> unseenBirds;
        public static HashSet<string> seenBirds;
        private static Dictionary<string, BirdData> birdsToday;


        public override void Entry(IModHelper helper)
        {
            ModEntry.modInstance = this;

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Player.Warped += Player_Warped;

            unseenBirds = getFullBirdList(helper.Content.Load<Dictionary<string, List<string>>>("assets/birdlist.json", ContentSource.ModFolder));
            seenBirds = new HashSet<string>();
                
        }

        private static void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT LOCATIONS TO VALID LOCATIONS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            sampleTodaysBirds(Game1.currentSeason);
            modInstance.Monitor.Log($" Unseen birds: {String.Join(" - ", unseenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Seen birds: {String.Join(" - ", seenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in birdsToday)
            {
                modInstance.Monitor.Log($"Key = {kvp.Key}, Value = {kvp.Value.name}", LogLevel.Debug);
            }
        }

        private static void Player_Warped(object sender, WarpedEventArgs e)
        {
            modInstance.Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
            Populate(e.NewLocation);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            modInstance.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }

        private static List<string> getFullBirdList(Dictionary<string, List<string>> birdLists)
        {
            // this should create a combined list of all birds that gets shuffled with weights based on log of the lists index
            // when new areas are discovered, the individual birds lists should be added to the eligible birds list
            // then eligibleBirds.Contains can be used before choosing a new bird from the shuffle master list
            return birdLists["valleyBirds"];
        }

        private static void sampleTodaysBirds(string currentSeason)
        {
            birdsToday = new Dictionary<string, BirdData>();

            getUnseenBird(currentSeason);

            if (seenBirds.Count == 0)
                return;

            getSeenBirds(currentSeason);


        }

        private static void getUnseenBird(string currentSeason)
        {
            // RANDOMNESS!
            if (Game1.random.NextDouble() < 1.0 && unseenBirds.Count > 0)
            {
                for (int i = 0; i < unseenBirds.Count; i++)
                {
                    string birdName = unseenBirds[i];
                    BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
                    if (data.seasons.Contains(currentSeason) || data.advancedSpawn.Keys.Contains(Game1.currentSeason))
                    {
                        data.spawnData = getSpawnData(data);
                        // HAVE TO IMPLEMENT CHANCE VALUE WITH IF HERE
                        birdsToday.Add(getRandomElementFromList(data.spawnData.locations), data);
                        return;
                    }

                }
            }
        }

        private static void getSeenBirds(string currentSeason)
        {
            int maxLocationsWithBirds = 5;

            foreach (string birdName in seenBirds)
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
            return getRandomElementFromList(possibleSpawns);

        }

        private static T getRandomElementFromList<T>(List<T> listToPickFrom)
            {
            int index = Game1.random.Next(0, Math.Max(listToPickFrom.Count-1, 0));
            return listToPickFrom[index];
        }

        private static void Populate(GameLocation location)
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