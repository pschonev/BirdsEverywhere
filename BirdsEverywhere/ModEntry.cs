using System;
using System.Linq;
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

            helper.ConsoleCommands.Add("show_all_birds", "Shows all seen and unseen birds.", PrintSeenBirds);
        }


        private void PrintSeenBirds(string command, string[] args)
        {
            LogBirdSeenStatus();
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer)
                Helper.Data.WriteSaveData(saveKey, saveData);

        }

        private void OnLoaded(object sender, SaveLoadedEventArgs e)
        {
            environmentData = modInstance.Helper.Content.Load<EnvironmentData>("assets/environmentData.json", ContentSource.ModFolder);
            saveData = Helper.Data.ReadSaveData<SaveData>(saveKey) ?? new SaveData();

            setEligibleLocations();
            LogBirdSeenStatus();
        }


        private void setEligibleLocations()
        {
            eligibleLocations = new HashSet<string>();
            foreach (Biome biome in environmentData.biomes) 
                eligibleLocations.UnionWith(biome.locations);
        }

        public static bool isEligibleLocation(GameLocation location)
        {
            return eligibleLocations.Contains(location.Name);
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT BIRDS TO VALID BIRDS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            dailySpawner = new DailySpawner(Game1.currentSeason, saveData);

            LogBirdSeenStatus();

            modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in dailySpawner.birdsToday)
            {
                modInstance.Monitor.Log($"Key = {kvp.Key}, Value = {kvp.Value.name}", LogLevel.Debug);
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            modInstance.Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
            if (e.NewLocation == null || !isEligibleLocation(e.NewLocation))
                return;

            removeVanillaBirds(e.NewLocation);           
            dailySpawner.Populate(e.NewLocation);
        }

        private void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (Game1.currentLocation == null || !isEligibleLocation(Game1.currentLocation))
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
            foreach (var x in location.critters)
            {
                modInstance.Monitor.Log($"Spawn {x} at {x.position}", LogLevel.Debug);
            }
            location.critters.RemoveAll(c => isVanillaBird(c));
        }

        private void LogBirdSeenStatus()
        {
            foreach (Biome biome in environmentData.biomes)
            {
                modInstance.Monitor.Log($"{biome.name} unseen birds:", LogLevel.Debug);
                Utils.logList(biome.birds.Where(x => !saveData.seenBirds.Contains(x)).ToList(), "Unseen Birds");

                modInstance.Monitor.Log($"{biome.name} seen birds:", LogLevel.Debug);
                Utils.logList(biome.birds.Where(x => saveData.seenBirds.Contains(x)).ToList(), "Unseen Birds");
            }
        }
    }
}