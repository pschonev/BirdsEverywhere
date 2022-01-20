using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.Spawners;
using BirdsEverywhere.BirdList;


namespace BirdsEverywhere
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static Mod modInstance;
        const string saveKey = "bird-save";
        private static int MyTabId;

        public static EnvironmentData environmentData;
        public static SaveData saveData;
        public static DailySpawner dailySpawner;

        public static HashSet<string> eligibleLocations;
        public static Dictionary<string, BirdData> birdDataCollection;

        public override void Entry(IModHelper helper)
        {
            modInstance = this;
            
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Player.Warped += Player_Warped;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.SaveLoaded += OnLoaded;
            helper.Events.GameLoop.TimeChanged += TimeChanged;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

            helper.ConsoleCommands.Add("show_all_birds", "Shows all seen and unseen birds.", Logging.PrintSeenBirds);

            ModEntry.MyTabId = SpaceCore.Menus.ReserveGameMenuTab("birds");
        }

        // ##################
        // # Game Load/Save #
        // ##################

        private void OnLoaded(object sender, SaveLoadedEventArgs e)
        {
            environmentData = modInstance.Helper.Content.Load<EnvironmentData>("assets/environmentData.json", ContentSource.ModFolder);
            if (Context.IsMainPlayer)
                saveData = Helper.Data.ReadSaveData<SaveData>(saveKey) ?? new SaveData();

            setEligibleLocations();
            loadAllBirdData();
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer)
                Helper.Data.WriteSaveData(saveKey, saveData);

        }

        private void setEligibleLocations()
        {
            eligibleLocations = new HashSet<string>();
            foreach (Biome biome in environmentData.biomes) 
                eligibleLocations.UnionWith(biome.locations);
        }

        private void loadAllBirdData()
        {
            birdDataCollection = new Dictionary<string, BirdData> ();
            foreach (Biome biome in environmentData.biomes)
            {
                foreach(string birdName in biome.birds)
                {
                    birdDataCollection[birdName] = this.Helper.Content.Load<BirdData>($"assets/{birdName}/{birdName}.json", ContentSource.ModFolder);
                }
            }

        }

        // #############
        // # Day Start #
        // #############

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // ADD ISLAND AND DESERT BIRDS TO VALID BIRDS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
            dailySpawner = new DailySpawner(saveData.seenBirds, environmentData.biomes);

            // send bird locations for today to any farmhands
            if (Context.IsMainPlayer)
                this.Helper.Multiplayer.SendMessage(dailySpawner, "BirdLocationsForFarmhandsNewDay", modIDs: new[] { this.ModManifest.UniqueID });

            Logging.LogBirdSeenStatus();
        }

        // ###############
        // # Multiplayer #
        // ###############

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // any player receives updated observation data (SaveData)
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "SaveNewObservation")
                saveData = e.ReadAs<SaveData>();

            // farmhand
            if (!Context.IsMainPlayer)
            {
                // farmhand receives SaveData on connect
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateFarmhandSave")
                    saveData = e.ReadAs<SaveData>();

                // farmhand receives birds today after connecting
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "BirdLocationsForFarmhandsOnConnect")
                    dailySpawner = e.ReadAs<DailySpawner>();

                // farmhand receives birds today after day starts
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "BirdLocationsForFarmhandsNewDay")
                    dailySpawner = e.ReadAs<DailySpawner>();
            }
        }

        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                // host sends SaveData to newly connected farmhand
                this.Helper.Multiplayer.SendMessage(saveData, "UpdateFarmhandSave", modIDs: new[] { this.ModManifest.UniqueID });

                // host sends bird locations (DailySapwner) to connected farmhand
                this.Helper.Multiplayer.SendMessage(dailySpawner, "BirdLocationsForFarmhandsOnConnect", modIDs: new[] { this.ModManifest.UniqueID });
            }
        }

        // #############################
        // # Spawning / Removing Birds #
        // #############################

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            modInstance.Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
            if (e.NewLocation == null || !Utils.isEligibleLocation(e.NewLocation))
                return;
            removeVanillaBirds(e.NewLocation);

            if (!Utils.isEligibleLocation(e.NewLocation))
                return;
            dailySpawner.Populate(e.NewLocation);
        }

        private void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (Game1.currentLocation == null)
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
            if (location.critters == null)
                return;

            foreach (var x in location.critters)
            {
                modInstance.Monitor.Log($"Spawn {x} at {x.position}", LogLevel.Debug);
            }
            location.critters.RemoveAll(c => isVanillaBird(c));
        }

        // ##################
        // # Bird List Menu #
        // ##################

        private int MyTabIndex = -1;
        private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            if (args.NewMenu is GameMenu gm)
            {
                var pages = gm.pages;
                var tabs = gm.tabs;

                this.MyTabIndex = tabs.Count;
                tabs.Add(new ClickableComponent(new Rectangle(gm.xPositionOnScreen + 192, gm.yPositionOnScreen + IClickableMenu.tabYPositionRelativeToMenuY + 64 - 64, 64, 64), "birds", "Birds")
                {
                    myID = 812342,
                    downNeighborID = 12342,
                    rightNeighborID = 12343,
                    leftNeighborID = 12341,
                    tryDefaultIfNoDownNeighborExists = true,
                    fullyImmutable = true
                });
                tabs[1].upNeighborID = 812342;
                pages.Add(new BirdListPage(gm.xPositionOnScreen, gm.yPositionOnScreen, gm.width, gm.height));

                this.Helper.Events.Display.RenderedActiveMenu += this.DrawSocialIcon;
            }
            else if (args.OldMenu is GameMenu)
            {
                this.Helper.Events.Display.RenderedActiveMenu -= this.DrawSocialIcon;
            }
        }

        // The tab by default is rendered with the inventory icon due to how the tabs are hard-coded
        // This draws over it with the social icon instead of the inventory one
        private void DrawSocialIcon(object sender, RenderedActiveMenuEventArgs e)
        {
            // For some reason this check is necessary despite removing it in the onMenuChanged event.
            if (Game1.activeClickableMenu is not GameMenu menu)
            {
                this.Helper.Events.Display.RenderedActiveMenu -= this.DrawSocialIcon;
                return;
            }
            if (menu.invisible || this.MyTabIndex == -1)
                return;

            var tabs = menu.tabs;
            if (tabs.Count <= this.MyTabIndex)
            {
                return;
            }
            var tab = tabs[this.MyTabIndex];
            e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(tab.bounds.X, tab.bounds.Y + (menu.currentTab == menu.getTabNumberFromName(tab.name) ? 8 : 0)), new Rectangle(2 * 16, 368, 16, 16), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 0.0001f);

            if (!Game1.options.hardwareCursor)
            {
                e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.options.gamepadControls ? 44 : 0, 16, 16), Color.White, 0.0f, Vector2.Zero, (float)(4.0 + Game1.dialogueButtonScale / 150.0), SpriteEffects.None, 1f);
            }
        }
    }
}