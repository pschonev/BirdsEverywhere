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
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdList;
using Newtonsoft.Json;

namespace BirdsEverywhere
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public static Mod modInstance;
        const string saveKey = "bird-save";

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

            JsonConverter[] converters = { new SpawnConverter(),
                new CustomBirdTypeConverterWriter(), new CustomBirdTypeConverterReader()};
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = converters,
                Error = OnJsonError
            };
        }

        private static void OnJsonError(object? sender,
Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            if (e.CurrentObject is null)
            {
                ModEntry.modInstance.Monitor.Log($"Serialization on {e.ErrorContext.Path} generated an exception {e.ErrorContext.Error.GetType().Name} with warning {e.ErrorContext.Error.Message}",
                LogLevel.Error);
            }
            else
            {
                ModEntry.modInstance.Monitor.Log($"Serialization on {e.CurrentObject.GetType().Name} had issues with path {e.ErrorContext.Path} and generated an exception {e.ErrorContext.Error.GetType().Name} with warning {e.ErrorContext.Error.Message}",
                LogLevel.Error);
            }

            e.ErrorContext.Handled = true;

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
            if (Context.IsMainPlayer)
            {
                // ADD ISLAND AND DESERT BIRDS TO VALID BIRDS FOR SPAWNING ONCE THEY ARE ACCESSIBLE
                dailySpawner = new DailySpawner();
                dailySpawner.initializeBirdLocations(saveData.seenBirds, environmentData.biomes);

                // send bird locations for today to any farmhands
                this.Helper.Multiplayer.SendMessage(dailySpawner, "BirdLocationsForFarmhandsNewDay", modIDs: new[] { this.ModManifest.UniqueID });
            }

            Logging.LogBirdSeenStatus();
        }

        // ###############
        // # Multiplayer #
        // ###############

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // a specific player at a location receives a request from entering player to send them the bird objects there
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "RequestCurrentBirds" && Game1.player.UniqueMultiplayerID == e.ReadAs<long>())
            { 
                Monitor.Log($"{Game1.player.Name} got request to send active birds.", LogLevel.Debug);
                List<CustomBirdType> birds = Game1.currentLocation.critters.Where(x => x is CustomBirdType).Select(x => x as CustomBirdType).ToList();

                this.Helper.Multiplayer.SendMessage((e.FromPlayerID, birds),
                    "UpdateCurrentBirds", modIDs: new[] { this.ModManifest.UniqueID });
                foreach(CustomBirdType bird in Game1.currentLocation.critters.OfType<CustomBirdType>())
                {
                    bird.seedRandom();
                }
            }
                

            // update birds according to the previous request
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateCurrentBirds")
            {
                long sendingPlayerID = e.ReadAs<(long id, List<CustomBirdType> birds)>().id;
                Monitor.Log($"Player ID who received active bird data: {Game1.player.UniqueMultiplayerID} Player ID who supposedly first sent request {sendingPlayerID}. Matching? - {Game1.player.UniqueMultiplayerID == sendingPlayerID}", LogLevel.Debug);
                if (Game1.player.UniqueMultiplayerID == sendingPlayerID)
                {
                    Monitor.Log($"{Game1.player.Name} got active birds and updates critter list.", LogLevel.Debug);
                    Game1.currentLocation.instantiateCrittersList();
                    List<CustomBirdType> birds = e.ReadAs<(long id, List<CustomBirdType> birds)>().birds;
                    Game1.currentLocation.critters.AddRange(birds);
                }
                
            }
                

                // any player receives updated observation data (SaveData)
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "SaveNewObservation")
                saveData = e.ReadAs<SaveData>();

            // any player receives global message of bird observation to show
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == "GlobalObservationMessage")
            {
                string ObservationMessage = e.ReadAs<string>();
                Game1.showGlobalMessage(ObservationMessage);
            }

            // farmhand
            if (!Context.IsMainPlayer)
            {
                // farmhand receives birds today after day starts
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "BirdLocationsForFarmhandsNewDay")
                    dailySpawner = e.ReadAs<DailySpawner>();

                // farmhand receives SaveData on connect
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "UpdateFarmhandSave")
                {
                    Monitor.Log($"{Game1.player.Name} received save data.", LogLevel.Debug);
                    saveData = e.ReadAs<SaveData>();
                }

                // farmhand receives birds today after connecting
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "BirdLocationsForFarmhandsOnConnect")
                {
                    Monitor.Log($"{Game1.player.Name} received bird data.", LogLevel.Debug);
                    dailySpawner = e.ReadAs<DailySpawner>();
                }
                    
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
            if (e.NewLocation == null)
                return;
            removeVanillaBirds(e.NewLocation);

            if (!Utils.isEligibleLocation(e.NewLocation))
                return;

            var farmer = Game1.getOnlineFarmers().FirstOrDefault(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && f.currentLocation == e.NewLocation);
            if (farmer == null)
            {
                dailySpawner.Populate(e.NewLocation);
            }
            else
            {
                Monitor.Log($"{Game1.player.Name} request active birds at {farmer.currentLocation}.", LogLevel.Debug);
                this.Helper.Multiplayer.SendMessage(farmer.UniqueMultiplayerID, "RequestCurrentBirds", modIDs: new[] { this.ModManifest.UniqueID });
            }
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
                modInstance.Monitor.Log($"Spawn {x} at {x.position.X / 64} / {x.position.Y / 64}", LogLevel.Debug);
            }
            location.critters.RemoveAll(c => isVanillaBird(c));
        }

        // ##################
        // # Bird List Menu #
        // ##################
        private void OnMenuChanged(object sender, MenuChangedEventArgs args)
        {
            if (args.NewMenu is GameMenu g)
            {
                int indexOfCollectionsPage = g.pages.FindIndex(x => x is CollectionsPage);
                    if (g.pages[indexOfCollectionsPage] is CollectionsPage c)
                    {
                    // converting to my custom CollectionsPage
                    CollectionsPageWithBirds cp = new CollectionsPageWithBirds(c);

                    // cp adding the clickable tab
                    string iconAssetPath = $"assets/collections_tab_icon.png";
                    Texture2D birdTabIconTexture = modInstance.Helper.Content.Load<Texture2D>(iconAssetPath);

                        cp.sideTabs.Add(8, new ClickableTextureComponent(string.Concat(8), 
                            new Rectangle(cp.xPositionOnScreen - 48, cp.yPositionOnScreen + 64 * (2 + cp.sideTabs.Count), 64, 64), "",
                            "Birds", birdTabIconTexture, new Rectangle(0, 0, 16, 16), 4f)
                        {
                            myID = 7009,
                            upNeighborID = -99998,
                            downNeighborID = -99998,
                            rightNeighborID = 0
                        });
                        cp.collections.Add(8, new List<List<ClickableTextureComponent>>());

                        // addings the bird icons
                        int widthUsed = 0;
                        int baseX = cp.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearSideBorder;
                        int baseY = cp.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - 16;
                        int collectionWidth = 10;
                        int whichCollection2 = 8;

                        foreach (KeyValuePair<string, BirdData> kvp2 in birdDataCollection)
                        {
                            bool farmerHas2 = saveData.seenBirds.Contains(kvp2.Value.id);
                            string birdAssetPath = $"assets/{kvp2.Value.id}/{kvp2.Value.id}_icon.png";
                            Texture2D birdTexture = modInstance.Helper.Content.Load<Texture2D>(birdAssetPath);

                            int xPos4 = baseX + widthUsed % collectionWidth * 68;
                            int yPos4 = baseY + widthUsed / collectionWidth * 68;
                            if (yPos4 > cp.yPositionOnScreen + cp.height - 128)
                            {
                                cp.collections[whichCollection2].Add(new List<ClickableTextureComponent>());
                                widthUsed = 0;
                                xPos4 = baseX;
                                yPos4 = baseY;
                            }
                            if (cp.collections[whichCollection2].Count == 0)
                            {
                                cp.collections[whichCollection2].Add(new List<ClickableTextureComponent>());
                            }
                            cp.collections[whichCollection2].Last().
                                Add(new ClickableTextureComponent(kvp2.Value.id + " " + farmerHas2.ToString(), 
                                new Rectangle(xPos4, yPos4, 64, 64), null, "", birdTexture,
                                new Rectangle(0, 0, 16, 16), 4f, farmerHas2)
                                {
                                    myID = cp.collections[whichCollection2].Last().Count,
                                    rightNeighborID = (((cp.collections[whichCollection2].Last().Count + 1) % collectionWidth == 0) ? (-1) : (cp.collections[whichCollection2].Last().Count + 1)),
                                    leftNeighborID = ((cp.collections[whichCollection2].Last().Count % collectionWidth == 0) ? 7001 : (cp.collections[whichCollection2].Last().Count - 1)),
                                    downNeighborID = ((yPos4 + 68 > cp.yPositionOnScreen + cp.height - 128) ? (-7777) : (cp.collections[whichCollection2].Last().Count + collectionWidth)),
                                    upNeighborID = ((cp.collections[whichCollection2].Last().Count < collectionWidth) ? 12345 : (cp.collections[whichCollection2].Last().Count - collectionWidth)),
                                    fullyImmutable = true
                                });
                            widthUsed++;
                        }

                    g.pages[indexOfCollectionsPage] = cp;
                }
            }
        }
    }
}