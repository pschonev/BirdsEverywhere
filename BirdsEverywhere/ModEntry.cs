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
        private static Dictionary<string, string> birdsToday;


        public override void Entry(IModHelper helper)
        {
            ModEntry.modInstance = this;

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Player.Warped += Player_Warped;

            unseenBirds = new List<string>(helper.Content.Load<Dictionary<string, string>>("assets/birdlist.json", ContentSource.ModFolder).Values);
            seenBirds = new HashSet<string>();
                
        }

        private static void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT LOCATIONS TO VALID LOCATIONS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            sampleTodaysBirds(Game1.currentSeason);
            modInstance.Monitor.Log($" Unseen birds: {String.Join(" - ", unseenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Seen birds: {String.Join(" - ", seenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, string> kvp in birdsToday)
            {
                modInstance.Monitor.Log($"Key = {kvp.Key}, Value = {kvp.Value}", LogLevel.Debug);
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

        private static void sampleTodaysBirds(string currentSeason)
        {
            birdsToday = new Dictionary<string, string>();

            getUnseenBird(currentSeason);

            if (seenBirds.Count == 0)
                return;

            getSeenBirds(currentSeason);


        }

        private static void getUnseenBird(string currentSeason)
        {
            if (Game1.random.NextDouble() < 1.0 && unseenBirds.Count > 0)
            {
                for (int i = 0; i < unseenBirds.Count; i++)
                {
                    string birdName = unseenBirds[i];
                    BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
                    if (data.seasons.Contains(currentSeason))
                    {
                        // HAVE TO IMPLEMENT CHANCE VALUE WITH IF HERE
                        int locationIndex = Game1.random.Next(0, data.locations.Count);
                        birdsToday.Add(data.locations[locationIndex], birdName);
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

                foreach (string spawnLocation in data.locations)
                {
                    if (!birdsToday.ContainsKey(spawnLocation))
                    {
                        // HAVE TO IMPLEMENT CHANCE VALUE WITH IF HERE
                        birdsToday.Add(spawnLocation, birdName);
                        if (birdsToday.Count >= maxLocationsWithBirds)
                            return;
                    }
                }
            }
        }


        private static void Populate(GameLocation location)
        {
            if (location == null)
                return;

            string locationName = location.Name ?? ""; //get the location's name (blank if null)

            if (!birdsToday.ContainsKey(locationName))
                return;

            modInstance.Monitor.Log($"{locationName} is in birdsToday and will spawn {birdsToday[locationName]}", LogLevel.Debug);

            location.instantiateCrittersList(); //make sure the critter list isn't null

            string birdName = birdsToday[locationName];
            BirdData data = modInstance.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);

            SpawnBirdsAroundLocation(location, data);
        }
    }
}