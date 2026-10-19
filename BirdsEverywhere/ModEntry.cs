using System.Collections.Immutable;
using BirdsEverywhere.BirdList;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Registries;
using BirdsEverywhere.Spawners;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace BirdsEverywhere;

public class ModEntry : Mod
{
    private const string SaveKey = "bird-save";

    private SaveData _saveData = new();
    private DailySpawnPlan _plan = DailySpawnPlan.Empty;
    private LocationSpawner _locationSpawner = null!;
    private HashSet<string> _eligibleLocations = new();
    private IReadOnlyDictionary<string, BirdDefinition> _catalog = new Dictionary<string, BirdDefinition>();
    private IReadOnlyDictionary<string, (JObject Effective, IReadOnlyDictionary<string, string> Sources)> _effective =
        new Dictionary<string, (JObject, IReadOnlyDictionary<string, string>)>();
    private MessageSender _messages = null!;
    private BirdServices _birdServices = null!;
    private bool _populatesCurrentVisit = true;
    private readonly SpawnPatternRegistry _spawnPatterns = SpawnPatternRegistry.CreateDefault();
    private readonly BirdTypeRegistry _birdTypes = BirdTypeRegistry.CreateDefault();

    public override void Entry(IModHelper helper)
    {
        _messages = new MessageSender(helper.Multiplayer, ModManifest.UniqueID);
        _birdServices = new BirdServices(Monitor, helper.ModContent, helper.Multiplayer.GetNewID, RecordObservation);
        _locationSpawner = new LocationSpawner(_spawnPatterns, _birdTypes, _birdServices);

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.SaveLoaded += OnLoaded;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

        helper.ConsoleCommands.Add("show_all_birds", "Shows all seen and unseen birds.",
            (_, _) => Logging.LogBirdSeenStatus(Monitor, _saveData, _catalog));
        helper.ConsoleCommands.Add("birds_dump", "Print effective rules and sources for a bird. Usage: birds_dump <id>", DumpBird);
        helper.ConsoleCommands.Add("birds_fishareas", "List fish areas in Data/Locations. Usage: birds_fishareas [here]", DumpFishAreas);
    }

    // ##################
    // # Game Load/Save #
    // ##################

    private void OnLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            _saveData = Helper.Data.ReadSaveData<SaveData>(SaveKey) ?? new SaveData();
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
        string root = Path.Combine(Helper.DirectoryPath, "assets");
        string? Read(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
        string compatDirectory = ContentPaths.CompatDirectory(root);
        IEnumerable<string> compatModIds = Directory.Exists(compatDirectory)
            ? Directory.GetFiles(compatDirectory, "*.json").Select(Path.GetFileNameWithoutExtension).Select(modId => modId!).Where(Helper.ModRegistry.IsLoaded)
            : Enumerable.Empty<string>();
        (IReadOnlyDictionary<string, IReadOnlyList<string>> groups, IReadOnlyList<ValidationIssue> groupIssues) = ContentLoader.LoadGroups(root, compatModIds, Read);
        foreach (ValidationIssue issue in groupIssues)
        {
            LogIssue(issue);
        }

        Dictionary<string, BirdDefinition> catalog = new(StringComparer.Ordinal);
        Dictionary<string, (JObject Effective, IReadOnlyDictionary<string, string> Sources)> effective = new(StringComparer.Ordinal);
        foreach (string id in ContentPaths.BirdIds(root))
        {
            (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, JObject? document, IReadOnlyDictionary<string, string> sources) =
                ContentLoader.LoadBird(root, id, groups, _spawnPatterns.Ids, _birdTypes.Ids, Read, File.Exists);
            foreach (ValidationIssue issue in issues)
            {
                LogIssue(issue);
            }

            if (bird is null || document is null)
            {
                continue;
            }

            catalog.Add(id, bird);
            effective.Add(id, (document, sources));
        }

        _catalog = catalog.ToImmutableDictionary(StringComparer.Ordinal);
        _effective = effective;
        _eligibleLocations = catalog.Values.SelectMany(bird => bird.Rules).SelectMany(rule => rule.Locations).ToHashSet(StringComparer.Ordinal);
    }

    private void LogIssue(ValidationIssue issue) => Monitor.Log(issue.ToString(), issue.Severity switch
    {
        IssueSeverity.Error => LogLevel.Error,
        IssueSeverity.Warn => LogLevel.Warn,
        _ => LogLevel.Info,
    });

    private void DumpBird(string command, string[] args)
    {
        if (args.Length != 1 || !_effective.TryGetValue(args[0], out var entry))
        {
            Monitor.Log("Usage: birds_dump <loaded bird id>", LogLevel.Info);
            return;
        }

        foreach (JProperty rule in ((JObject?)entry.Effective["rules"] ?? new JObject()).Properties())
        {
            Monitor.Log($"{args[0]} / {rule.Name}: {rule.Value.ToString(Newtonsoft.Json.Formatting.None)}", LogLevel.Info);
            foreach (KeyValuePair<string, string> source in entry.Sources.Where(source => source.Key.StartsWith($"$.rules.{rule.Name}.", StringComparison.Ordinal)))
            {
                Monitor.Log($"  {source.Key}: {source.Value}", LogLevel.Info);
            }
        }
    }

    private void DumpFishAreas(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        if (args.Length == 1 && args[0] == "here")
        {
            GameLocation location = Game1.currentLocation;
            string? area = location.TryGetFishAreaForTile(Game1.player.Tile, out string id, out _) ? id : null;
            Monitor.Log($"{location.Name} (farm type {Game1.GetFarmTypeKey()}) tile {Game1.player.Tile}: fish area {area ?? "<none>"}", LogLevel.Info);
            return;
        }

        foreach ((string key, StardewValley.GameData.Locations.LocationData data) in Game1.locationData.Where(entry => entry.Value.FishAreas?.Count > 0))
        {
            foreach ((string id, StardewValley.GameData.Locations.FishAreaData area) in data.FishAreas)
            {
                string crabPot = area.CrabPotFishTypes is null ? "" : string.Join(",", area.CrabPotFishTypes);
                Monitor.Log($"{key}:{id} position={area.Position?.ToString() ?? "<whole map>"} crabPot=[{crabPot}]", LogLevel.Info);
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
            Domain.Season season = Enum.Parse<Domain.Season>(Game1.currentSeason, ignoreCase: true);
            Weather weather = Game1.isGreenRain ? Weather.GreenRain
                : Game1.isLightning ? Weather.Storm
                : Game1.isSnowing ? Weather.Snow
                : Game1.isDebrisWeather ? Weather.Wind
                : Game1.isRaining ? Weather.Rain : Weather.Sun;
            _plan = DailySpawnPlan.Build(_saveData.seenBirds, _catalog, _spawnPatterns, season, weather, Game1.dayOfMonth, random, Monitor);
            _messages.SendToAll(MessageJson.Serialize(_plan), MessageTypes.BirdLocationsNewDay);
        }

        Logging.LogBirdSeenStatus(Monitor, _saveData, _catalog);
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
                _plan = MessageJson.Deserialize<DailySpawnPlan>(e.ReadAs<string>());
                break;
            case MessageTypes.UpdateFarmhandSave when !Context.IsMainPlayer:
                Monitor.Log($"{Game1.player.Name} received save data.", LogLevel.Debug);
                _saveData = e.ReadAs<SaveData>();
                break;
            case MessageTypes.BirdLocationsOnConnect when !Context.IsMainPlayer:
                Monitor.Log($"{Game1.player.Name} received bird data.", LogLevel.Debug);
                _plan = MessageJson.Deserialize<DailySpawnPlan>(e.ReadAs<string>());
                break;
        }
    }

    private void SendCurrentBirds(long requestingPlayerId)
    {
        Monitor.Log($"{Game1.player.Name} got request to send active birds.", LogLevel.Debug);
        List<BirdCritter> birds = Game1.currentLocation.critters.OfType<BirdCritter>().ToList();
        _messages.SendToPeer(MessageJson.Serialize((requestingPlayerId, birds)), MessageTypes.UpdateCurrentBirds, requestingPlayerId);
        foreach (BirdCritter bird in birds)
        {
            bird.seedRandom();
        }
    }

    private void ReceiveCurrentBirds(string json)
    {
        (long targetPlayerId, List<BirdCritter> birds) = MessageJson.Deserialize<(long, List<BirdCritter>)>(json);
        if (Game1.player.UniqueMultiplayerID != targetPlayerId)
        {
            return;
        }

        Monitor.Log($"{Game1.player.Name} got active birds and updates critter list.", LogLevel.Debug);
        foreach (BirdCritter bird in birds)
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
        _messages.SendToPeer(MessageJson.Serialize(_plan), MessageTypes.BirdLocationsOnConnect, e.Peer.PlayerID);
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

        string message = $"{Game1.player.Name} discovered a new bird: {_catalog[birdName].Name}!";
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

        _locationSpawner.BeginVisit();
        Farmer? farmer = Game1.getOnlineFarmers().FirstOrDefault(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && f.currentLocation == e.NewLocation);
        _populatesCurrentVisit = farmer is null;
        if (farmer == null)
        {
            _locationSpawner.Populate(e.NewLocation, _plan);
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
            if (_populatesCurrentVisit && _eligibleLocations.Contains(Game1.currentLocation.Name))
            {
                _locationSpawner.Populate(Game1.currentLocation, _plan);
            }
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
