using System.Collections.Immutable;
using BirdsEverywhere.BirdList;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Observations;
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
    private MultiplayerSync _sync = null!;
    private BirdServices _birdServices = null!;
    private bool _populatesCurrentVisit = true;
    private readonly HashSet<string> _pendingObservations = new(StringComparer.Ordinal);
    private readonly SpawnPatternRegistry _spawnPatterns = SpawnPatternRegistry.CreateDefault();
    private readonly BirdTypeRegistry _birdTypes = BirdTypeRegistry.CreateDefault();

    public override void Entry(IModHelper helper)
    {
        _sync = new MultiplayerSync(helper.Multiplayer, Monitor, ModManifest.UniqueID);
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
        else
        {
            _sync.SendToPeer(new ObservationLogRequest(IMessage.CurrentVersion), Game1.MasterPlayer.UniqueMultiplayerID);
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
        _eligibleLocations = catalog.Values.SelectMany(bird => bird.Rules).SelectMany(rule => rule.Locations)
            .Select(key => Habitat.Parse(key).Location).ToHashSet(StringComparer.Ordinal);
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
            _plan = DailySpawnPlan.Build(_saveData.seenBirds, _catalog, _spawnPatterns, season, weather, Game1.dayOfMonth, Game1.player.DailyLuck, random, Monitor);
            _sync.SendToAll(new SpawnPlanMessage(IMessage.CurrentVersion, MessageJson.Serialize(_plan)));
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
            case nameof(ObservationRequest) when Context.IsMainPlayer:
                if (_sync.Read<ObservationRequest>(e) is { } request)
                {
                    AcceptObservation(request.BirdId, request.Details);
                }

                break;
            case nameof(ObservationLogRequest) when Context.IsMainPlayer:
                if (_sync.Read<ObservationLogRequest>(e) is not null)
                {
                    _sync.SendToPeer(new ObservationLogSnapshot(IMessage.CurrentVersion, _saveData), e.FromPlayerID);
                }

                break;
            case nameof(ObservationAccepted) when IsFromHost(e):
                if (_sync.Read<ObservationAccepted>(e) is { } accepted)
                {
                    ApplyAccepted(accepted);
                }

                break;
            case nameof(ObservationLogSnapshot) when IsFromHost(e):
                if (_sync.Read<ObservationLogSnapshot>(e) is { } snapshot && snapshot.Log.logVersion >= _saveData.logVersion)
                {
                    _saveData = snapshot.Log;
                    _pendingObservations.RemoveWhere(_saveData.seenBirds.Contains);
                }

                break;
            case nameof(SpawnPlanMessage) when IsFromHost(e):
                if (_sync.Read<SpawnPlanMessage>(e) is { } plan)
                {
                    _plan = MessageJson.Deserialize<DailySpawnPlan>(plan.PlanJson);
                }

                break;
            case nameof(LocationBirdsRequest):
                if (_sync.Read<LocationBirdsRequest>(e) is { } birdsRequest)
                {
                    SendLocationBirds(birdsRequest.LocationName, e.FromPlayerID);
                }

                break;
            case nameof(LocationBirdsState):
                if (_sync.Read<LocationBirdsState>(e) is { } birdsState)
                {
                    ReceiveLocationBirds(birdsState);
                }

                break;
            default:
                Monitor.Log($"Ignored message {e.Type} from player {e.FromPlayerID}.", LogLevel.Trace);
                break;
        }
    }

    private static bool IsFromHost(ModMessageReceivedEventArgs e) =>
        !Context.IsMainPlayer && e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID;

    private void SendLocationBirds(string locationName, long requestingPlayerId)
    {
        if (Game1.currentLocation?.Name != locationName)
        {
            Monitor.Log($"Ignored bird request for {locationName}: not in that location.", LogLevel.Debug);
            return;
        }

        List<BirdCritter> birds = Game1.currentLocation.critters.OfType<BirdCritter>().ToList();
        _sync.SendToPeer(new LocationBirdsState(IMessage.CurrentVersion, locationName, MessageJson.Serialize(birds)), requestingPlayerId);
        foreach (BirdCritter bird in birds)
        {
            bird.seedRandom();
        }
    }

    private void ReceiveLocationBirds(LocationBirdsState state)
    {
        if (Game1.currentLocation?.Name != state.LocationName)
        {
            Monitor.Log($"Dropped birds for {state.LocationName}: no longer in that location.", LogLevel.Debug);
            return;
        }

        List<BirdCritter> birds = MessageJson.Deserialize<List<BirdCritter>>(state.BirdsJson);
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

        _sync.SendToPeer(new ObservationLogSnapshot(IMessage.CurrentVersion, _saveData), e.Peer.PlayerID);
        _sync.SendToPeer(new SpawnPlanMessage(IMessage.CurrentVersion, MessageJson.Serialize(_plan)), e.Peer.PlayerID);
    }

    private void RecordObservation(string birdName, string location)
    {
        if (_saveData.seenBirds.Contains(birdName))
        {
            return;
        }

        ObservationData details = new(location, new SDate(Game1.dayOfMonth, Game1.currentSeason), Game1.timeOfDay, Game1.player.Name);
        if (Context.IsMainPlayer)
        {
            AcceptObservation(birdName, details);
        }
        else if (_pendingObservations.Add(birdName))
        {
            _sync.SendToPeer(new ObservationRequest(IMessage.CurrentVersion, birdName, details), Game1.MasterPlayer.UniqueMultiplayerID);
        }
    }

    /// <summary>Host only: validates and records an observation, then tells every peer.</summary>
    private void AcceptObservation(string birdId, ObservationData details)
    {
        if (!_catalog.TryGetValue(birdId, out BirdDefinition? bird))
        {
            Monitor.Log($"Dropped observation of unknown bird '{birdId}'.", LogLevel.Warn);
            return;
        }

        if (!ObservationLog.TryApply(_saveData, birdId, details, out SaveData updated))
        {
            return;
        }

        _saveData = updated;
        Monitor.Log($"New bird seen (log version {updated.logVersion}):", LogLevel.Debug);
        Utils.logObservation(Monitor, _catalog, _saveData, birdId);
        _sync.SendToAll(new ObservationAccepted(IMessage.CurrentVersion, updated.logVersion, birdId, details));
        ShowDiscovery(bird, details);
    }

    /// <summary>Farmhand only: applies the host's delta, or asks for the full log when versions don't line up.</summary>
    private void ApplyAccepted(ObservationAccepted accepted)
    {
        _pendingObservations.Remove(accepted.BirdId);
        switch (ObservationLog.Check(_saveData, accepted.LogVersion))
        {
            case DeltaStatus.Duplicate:
                return;
            case DeltaStatus.Next when ObservationLog.TryApply(_saveData, accepted.BirdId, accepted.Details, out SaveData updated):
                _saveData = updated;
                if (_catalog.TryGetValue(accepted.BirdId, out BirdDefinition? bird))
                {
                    ShowDiscovery(bird, accepted.Details);
                }

                return;
            default:
                Monitor.Log($"Missed an observation update (local {_saveData.logVersion}, received {accepted.LogVersion}); requesting the full log.", LogLevel.Debug);
                _sync.SendToPeer(new ObservationLogRequest(IMessage.CurrentVersion), Game1.MasterPlayer.UniqueMultiplayerID);
                return;
        }
    }

    private static void ShowDiscovery(BirdDefinition bird, ObservationData details) =>
        Game1.showGlobalMessage($"{details.playerName} discovered a new bird: {bird.Name}!");

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

        Monitor.Log($"{Game1.player.Name} request active birds at {e.NewLocation.Name}.", LogLevel.Debug);
        _sync.SendToPeer(new LocationBirdsRequest(IMessage.CurrentVersion, e.NewLocation.Name), farmer.UniqueMultiplayerID);
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
