using BirdsEverywhere.BirdList;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Spawners;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace BirdsEverywhere;

public class ModEntry : Mod
{
    private const string SaveKey = "bird-save";

    private EnvironmentData _environment = new();
    private SaveData _saveData = new();
    private DailySpawner _dailySpawner = new();
    private HashSet<string> _eligibleLocations = new();
    private Dictionary<string, BirdData> _catalog = new();
    private MessageSender _messages = null!;
    private BirdServices _birdServices = null!;

    public override void Entry(IModHelper helper)
    {
        _messages = new MessageSender(helper.Multiplayer, ModManifest.UniqueID);
        _birdServices = new BirdServices(Monitor, helper.ModContent, helper.Multiplayer.GetNewID, RecordObservation);

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.SaveLoaded += OnLoaded;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

        helper.ConsoleCommands.Add("show_all_birds", "Shows all seen and unseen birds.",
            (_, _) => Logging.LogBirdSeenStatus(Monitor, _environment, _saveData, _catalog));
    }

    // ##################
    // # Game Load/Save #
    // ##################

    private void OnLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _environment = Helper.ModContent.Load<EnvironmentData>("assets/environmentData.json");
        if (Context.IsMainPlayer)
        {
            _saveData = Helper.Data.ReadSaveData<SaveData>(SaveKey) ?? new SaveData();
        }

        _eligibleLocations = new HashSet<string>();
        foreach (Biome biome in _environment.biomes)
        {
            _eligibleLocations.UnionWith(biome.locations);
        }

        LoadCatalog();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Helper.Data.WriteSaveData(SaveKey, _saveData);
        }
    }

    private void LoadCatalog()
    {
        Dictionary<string, BirdTemplate> templates = new();
        BirdTemplate ResolveTemplate(string name)
        {
            if (templates.TryGetValue(name, out BirdTemplate? cached))
            {
                return cached;
            }

            BirdTemplate template = Helper.ModContent.Load<BirdTemplate>($"assets/templates/{name}.json");
            template.ApplyTemplate(ResolveTemplate);
            templates[name] = template;
            return template;
        }

        _catalog = new Dictionary<string, BirdData>();
        foreach (Biome biome in _environment.biomes)
        {
            foreach (string birdName in biome.birds)
            {
                BirdData data = Helper.ModContent.Load<BirdData>($"assets/{birdName}/{birdName}.json");
                data.ApplyTemplate(ResolveTemplate);
                _catalog[birdName] = data;
                Helper.Data.WriteJsonFile($"assets/inspect_output/{birdName}.json", data);
            }
        }
    }

    // #############
    // # Day Start #
    // #############

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Random random = new(Utils.StableHash($"{Game1.uniqueIDForThisGame}|{Game1.stats.DaysPlayed}|day"));
            _dailySpawner = new DailySpawner();
            _dailySpawner.initializeBirdLocations(_saveData.seenBirds, _environment.biomes, _catalog, random, Monitor);
            _messages.SendToAll(MessageJson.Serialize(_dailySpawner), MessageTypes.BirdLocationsNewDay);
        }

        Logging.LogBirdSeenStatus(Monitor, _environment, _saveData, _catalog);
    }

    // ###############
    // # Multiplayer #
    // ###############

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModManifest.UniqueID)
        {
            return;
        }

        switch (e.Type)
        {
            case MessageTypes.RequestCurrentBirds when Game1.player.UniqueMultiplayerID == e.ReadAs<long>():
                SendCurrentBirds(e.FromPlayerID);
                break;
            case MessageTypes.UpdateCurrentBirds:
                ReceiveCurrentBirds(e.ReadAs<string>());
                break;
            case MessageTypes.SaveNewObservation:
                _saveData = e.ReadAs<SaveData>();
                break;
            case MessageTypes.GlobalObservationMessage:
                Game1.showGlobalMessage(e.ReadAs<string>());
                break;
            case MessageTypes.BirdLocationsNewDay when !Context.IsMainPlayer:
                _dailySpawner = MessageJson.Deserialize<DailySpawner>(e.ReadAs<string>());
                break;
            case MessageTypes.UpdateFarmhandSave when !Context.IsMainPlayer:
                Monitor.Log($"{Game1.player.Name} received save data.", LogLevel.Debug);
                _saveData = e.ReadAs<SaveData>();
                break;
            case MessageTypes.BirdLocationsOnConnect when !Context.IsMainPlayer:
                Monitor.Log($"{Game1.player.Name} received bird data.", LogLevel.Debug);
                _dailySpawner = MessageJson.Deserialize<DailySpawner>(e.ReadAs<string>());
                break;
        }
    }

    private void SendCurrentBirds(long requestingPlayerId)
    {
        Monitor.Log($"{Game1.player.Name} got request to send active birds.", LogLevel.Debug);
        List<CustomBirdType> birds = Game1.currentLocation.critters.OfType<CustomBirdType>().ToList();
        _messages.SendToPeer(MessageJson.Serialize((requestingPlayerId, birds)), MessageTypes.UpdateCurrentBirds, requestingPlayerId);
        foreach (CustomBirdType bird in birds)
        {
            bird.seedRandom();
        }
    }

    private void ReceiveCurrentBirds(string json)
    {
        (long targetPlayerId, List<CustomBirdType> birds) = MessageJson.Deserialize<(long, List<CustomBirdType>)>(json);
        if (Game1.player.UniqueMultiplayerID != targetPlayerId)
        {
            return;
        }

        Monitor.Log($"{Game1.player.Name} got active birds and updates critter list.", LogLevel.Debug);
        foreach (CustomBirdType bird in birds)
        {
            bird.services = _birdServices;
        }

        Game1.currentLocation.instantiateCrittersList();
        Game1.currentLocation.critters.AddRange(birds);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        _messages.SendToPeer(_saveData, MessageTypes.UpdateFarmhandSave, e.Peer.PlayerID);
        _messages.SendToPeer(MessageJson.Serialize(_dailySpawner), MessageTypes.BirdLocationsOnConnect, e.Peer.PlayerID);
    }

    private void RecordObservation(string birdName, string location)
    {
        if (!_saveData.seenBirds.Add(birdName))
        {
            return;
        }

        _saveData.birdObservations[birdName] = new ObservationData(location, new SDate(Game1.dayOfMonth, Game1.currentSeason), Game1.timeOfDay, Game1.player.Name);
        Monitor.Log("New bird was added to seen birds:", LogLevel.Debug);
        Utils.logObservation(Monitor, _catalog, _saveData, birdName);
        _messages.SendToAll(_saveData, MessageTypes.SaveNewObservation);

        string message = $"{Game1.player.Name} discovered a new bird: {_catalog[birdName].name}!";
        Game1.showGlobalMessage(message);
        _messages.SendToAll(message, MessageTypes.GlobalObservationMessage);
    }

    // #############################
    // # Spawning / Removing Birds #
    // #############################

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.NewLocation == null)
        {
            return;
        }

        Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
        VanillaBirds.Remove(e.NewLocation);

        if (!_eligibleLocations.Contains(e.NewLocation.Name))
        {
            return;
        }

        Farmer? farmer = Game1.getOnlineFarmers().FirstOrDefault(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && f.currentLocation == e.NewLocation);
        if (farmer == null)
        {
            _dailySpawner.Populate(e.NewLocation, _birdServices);
            return;
        }

        Monitor.Log($"{Game1.player.Name} request active birds at {farmer.currentLocation}.", LogLevel.Debug);
        _messages.SendToPeer(farmer.UniqueMultiplayerID, MessageTypes.RequestCurrentBirds, farmer.UniqueMultiplayerID);
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Game1.currentLocation != null)
        {
            VanillaBirds.Remove(Game1.currentLocation);
        }
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is GameMenu menu)
        {
            BirdCollectionsTab.Install(menu, Helper.ModContent, Helper.Reflection, _catalog, _saveData);
        }
    }
}
