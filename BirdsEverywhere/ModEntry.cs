using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static Mod modInstance;
        const string saveKey = "bird-save";

        public static EnvironmentData environmentData;
        public static SaveData saveData;
        private DailySpawner dailySpawner;

        public static HashSet<string> eligibleLocations;

        public override void Entry(IModHelper helper)
        {
            modInstance = this;

            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Player.Warped += Player_Warped;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.SaveLoaded += OnLoaded;
            helper.Events.GameLoop.TimeChanged += TimeChanged;
        }


        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer)
                Helper.Data.WriteSaveData(saveKey, saveData);

        }

        private void OnLoaded(object sender, SaveLoadedEventArgs e)
        {
            environmentData = modInstance.Helper.Content.Load<EnvironmentData>("assets/environmentData.json", ContentSource.ModFolder);
            saveData = Helper.Data.ReadSaveData<SaveData>(saveKey) ?? new SaveData {
                unseenBirds = getInitialBirdList(environmentData.birds.valleyBirds)
            };

            setEligibleLocations();

            logList(saveData.unseenBirds, "Unseen Birds");
            logList(saveData.seenBirds, "Seen Birds");
        }

        private List<string> getInitialBirdList(List<string> birdList)
        {
            // this should create a combined list of all birds that gets shuffled with weights based on log of the lists index
            // when new areas are discovered, the individual birds lists should be added to the eligible birds list
            // then eligibleBirds.Contains can be used before choosing a new bird from the shuffle master list
            return birdList;
        }

        private void setEligibleLocations()
        {
            eligibleLocations = new HashSet<string>();
            eligibleLocations.UnionWith(environmentData.locations.valley);
            if (true)
                eligibleLocations.UnionWith(environmentData.locations.desert);
            if (true)
                eligibleLocations.UnionWith(environmentData.locations.island);
        }

        public static bool isEligibleLocation(GameLocation location)
        {
            return eligibleLocations.Contains(location.Name);
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT BIRDS TO VALID BIRDS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            dailySpawner = new DailySpawner(Game1.currentSeason, saveData);

            logList(saveData.unseenBirds, "Unseen Birds");
            logList(saveData.seenBirds, "Seen Birds");
            modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in dailySpawner.birdsToday)
            {
                modInstance.Monitor.Log($"Key = {kvp.Key}, Value = {kvp.Value.name}", LogLevel.Debug);
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            modInstance.Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
            removeVanillaBirds(e.NewLocation);

            if (!isEligibleLocation(Game1.currentLocation))
                return;
            dailySpawner.Populate(e.NewLocation);
        }

        private void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!isEligibleLocation(Game1.currentLocation))
                return;
            removeVanillaBirds(Game1.currentLocation);
        }

        private bool isVanillaBird(Critter critter)
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

        private void removeVanillaBirds(GameLocation location)
        {
            if (!isEligibleLocation(location))
                return;

            foreach (var x in location.critters)
            {
                modInstance.Monitor.Log($"Spawn {x} at {x.position}", LogLevel.Debug);
            }
            location.critters.RemoveAll(c => isVanillaBird(c));
        }

        private void logList(IEnumerable<string> list, string description = "-")
        {
            modInstance.Monitor.Log($" {description}: {String.Join(" - ", list)}.", LogLevel.Debug);
        }
    }
}