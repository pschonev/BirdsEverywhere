using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;


namespace BirdsEverywhere
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static Mod modInstance;
        const string saveKey = "bird-save";

        public static SaveData saveData;
        public static Dictionary<string, BirdData> birdsToday;

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
            saveData = Helper.Data.ReadSaveData<SaveData>(saveKey) ?? new SaveData{
                unseenBirds = SpawnManagement.getFullBirdList(modInstance.Helper.Content.Load<Dictionary<string, List<string>>>("assets/birdlist.json", ContentSource.ModFolder))
            };
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT BIRDS TO VALID BIRDS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            SpawnManagement.sampleTodaysBirds(Game1.currentSeason);

            modInstance.Monitor.Log($" Unseen birds: {String.Join(" - ", saveData.unseenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Seen birds: {String.Join(" - ", saveData.seenBirds)}.", LogLevel.Debug);
            modInstance.Monitor.Log($" Birds today: ", LogLevel.Debug);
            foreach (KeyValuePair<string, BirdData> kvp in birdsToday)
            {
                modInstance.Monitor.Log($"Key = {kvp.Key}, Value = {kvp.Value.name}", LogLevel.Debug);
            }
        }

        private static void Player_Warped(object sender, WarpedEventArgs e)
        {
            modInstance.Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
            SpawnManagement.removeVanillaBirds(e.NewLocation);
            SpawnManagement.Populate(e.NewLocation);
            modInstance.Monitor.Log($" Critters after: {String.Join(" - ", e.NewLocation.critters)}.", LogLevel.Debug);
        }

        private void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            SpawnManagement.removeVanillaBirds(Game1.currentLocation);
            modInstance.Monitor.Log($" Critters after: {String.Join(" - ", Game1.currentLocation.critters)}.", LogLevel.Debug);
        }
    }
}