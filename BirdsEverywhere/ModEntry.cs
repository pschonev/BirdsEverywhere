using System.Collections.Immutable;
using BirdsEverywhere.BirdList;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Observations;
using BirdsEverywhere.Registries;
using BirdsEverywhere.Spawners;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;

namespace BirdsEverywhere;

public partial class ModEntry : Mod
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
    private IReadOnlyDictionary<string, SpriteLayout> _layouts = new Dictionary<string, SpriteLayout>();
    private SharedDefinitions _definitions = new(new Dictionary<string, Routine>(), new Dictionary<string, JObject>(), new Dictionary<string, JObject>());
    private bool _populatesCurrentVisit = true;
    private long _birdOwnerId;
    private string _birdVisitId = "";
    private string _birdRequestId = "";
    private long _birdSequence;
    private readonly HashSet<string> _pendingObservations = new(StringComparer.Ordinal);
    private readonly SpawnPatternRegistry _spawnPatterns = SpawnPatternRegistry.CreateDefault();
    private readonly BirdTypeRegistry _birdTypes = BirdTypeRegistry.CreateDefault();
    private ModConfig _config = new();
    private IReadOnlyList<Region> _regions = Array.Empty<Region>();
    private readonly HashSet<string> _reportedSightings = new(StringComparer.Ordinal);
    private int _reportedDay = -1;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _sync = new MultiplayerSync(helper.Multiplayer, Monitor, ModManifest.UniqueID);
        (_layouts, IReadOnlyList<ValidationIssue> layoutIssues) = LayoutLoader.LoadAll(Path.Combine(helper.DirectoryPath, "assets"));
        _definitions = SharedDefinitions.Load(Path.Combine(helper.DirectoryPath, "assets"), out IReadOnlyList<ValidationIssue> definitionIssues);
        foreach (ValidationIssue issue in layoutIssues.Concat(definitionIssues))
        {
            LogIssue(issue);
        }
        _birdServices = new BirdServices(Monitor, helper.ModContent, helper.Multiplayer.GetNewID, RecordObservation, _layouts, _definitions.Routines,
            _definitions.ParsedActions(), ModManifest.UniqueID + "_", Math.Clamp(_config.SoundVolume, 0f, 1f),
            () => Context.IsWorldReady ? new IdleWorld(Enum.Parse<Domain.Season>(Game1.currentSeason, true), CurrentWeather(), Game1.dayOfMonth, Game1.timeOfDay) : null);
        RegisterAudio(helper);
        _locationSpawner = new LocationSpawner(_spawnPatterns, _birdTypes, _birdServices);

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.SaveLoaded += OnLoaded;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;

        helper.ConsoleCommands.Add("show_all_birds", "Shows all seen and unseen birds.",
            (_, _) => Logging.LogBirdSeenStatus(Monitor, _saveData, _catalog));
        helper.ConsoleCommands.Add("birds_dump", "Print effective rules, sources and resolved composition for a bird. Usage: birds_dump <bird> [<rule>]", DumpBird);
        helper.ConsoleCommands.Add("birds_fishareas", "List fish areas in Data/Locations. Usage: birds_fishareas [here]", DumpFishAreas);
        RegisterDebugCommands(helper);
        RegisterInspectCommands(helper);
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
        (IReadOnlyList<Region> regions, IReadOnlyList<ValidationIssue> regionIssues) = ContentLoader.LoadRegions(root, compatModIds, Read, ContentPaths.BirdIds(root).ToList());
        foreach (ValidationIssue issue in regionIssues)
        {
            LogIssue(issue);
        }

        _regions = regions;
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups = Region.AllGroups(regions);

        Dictionary<string, BirdDefinition> catalog = new(StringComparer.Ordinal);
        Dictionary<string, (JObject Effective, IReadOnlyDictionary<string, string> Sources)> effective = new(StringComparer.Ordinal);
        foreach (string id in ContentPaths.BirdIds(root))
        {
            (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, JObject? document, IReadOnlyDictionary<string, string> sources) =
                ContentLoader.LoadBird(root, id, groups, _spawnPatterns.Ids, _birdTypes.Ids, _definitions, Read, File.Exists);
            foreach (ValidationIssue issue in issues)
            {
                LogIssue(issue);
            }

            if (bird is null || document is null)
            {
                continue;
            }

            IReadOnlyList<ValidationIssue> layoutIssues = LayoutValidator.ValidateBird(bird, root, _layouts, TextureSize);
            foreach (ValidationIssue issue in layoutIssues)
            {
                LogIssue(issue);
            }
            if (layoutIssues.Count > 0)
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

    private static (int Width, int Height)? TextureSize(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        Span<byte> header = stackalloc byte[24];
        using FileStream stream = File.OpenRead(path);
        return stream.Read(header) == header.Length ? LayoutValidator.PngSize(header) : null;
    }

    private void LogIssue(ValidationIssue issue) => Monitor.Log(issue.ToString(), issue.Severity switch
    {
        IssueSeverity.Error => LogLevel.Error,
        IssueSeverity.Warn => LogLevel.Warn,
        _ => LogLevel.Info,
    });

    /// <summary>Adds a sound cue <c>&lt;ModId&gt;_&lt;name&gt;</c> per file in <c>assets/audio</c>.</summary>
    private void RegisterAudio(IModHelper helper)
    {
        IReadOnlyDictionary<string, string> files = AudioCues.Files(Path.Combine(helper.DirectoryPath, "assets"));
        if (files.Count == 0)
        {
            return;
        }
        helper.Events.Content.AssetRequested += (_, e) =>
        {
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
            {
                return;
            }
            e.Edit(asset =>
            {
                IDictionary<string, AudioCueData> data = asset.AsDictionary<string, AudioCueData>().Data;
                foreach ((string name, string path) in files)
                {
                    string id = ModManifest.UniqueID + "_" + name;
                    data[id] = new AudioCueData { Id = id, Category = "Sound", FilePaths = new List<string> { path } };
                }
            });
        };
    }

    private void DumpBird(string command, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Monitor.Log("Usage: birds_dump <bird> [<rule>]", LogLevel.Info);
            return;
        }

        if (!TryGetBird(args[0], out BirdDefinition bird))
        {
            return;
        }

        if (!_effective.TryGetValue(bird.Id.Value, out var entry))
        {
            Monitor.Log($"Bird '{bird.Id.Value}' has no loaded effective content.", LogLevel.Warn);
            return;
        }

        List<JProperty> rules = ((JObject?)entry.Effective["rules"] ?? new JObject()).Properties()
            .Where(rule => args.Length == 1 || rule.Name == args[1]).ToList();
        if (rules.Count == 0)
        {
            Monitor.Log($"Bird '{bird.Id.Value}' has no rule '{args[1]}'.", LogLevel.Warn);
            return;
        }

        foreach (JProperty rule in rules)
        {
            Monitor.Log($"{bird.Id.Value} / {rule.Name}: {rule.Value.ToString(Newtonsoft.Json.Formatting.None)}", LogLevel.Info);
            foreach (KeyValuePair<string, string> source in entry.Sources.Where(source => source.Key.StartsWith($"$.rules.{rule.Name}.", StringComparison.Ordinal)))
            {
                Monitor.Log($"  {source.Key}: {source.Value}", LogLevel.Info);
            }
            if (bird.Rules.FirstOrDefault(resolved => resolved.Id == rule.Name)?.Composition is { } composition)
            {
                Monitor.Log($"  resolved composition: {JsonConvert.SerializeObject(composition, Newtonsoft.Json.Formatting.None)}", LogLevel.Info);
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
        _birdVisitId = "";
        _birdRequestId = "";
        _birdSequence = 0;
        _birdOwnerId = 0;
        _locationSpawner.BeginVisit();
        if (Game1.currentLocation is { } location)
        {
            _birdOwnerId = Game1.getOnlineFarmers().Where(farmer => farmer.currentLocation == location)
                .Select(farmer => farmer.UniqueMultiplayerID).DefaultIfEmpty(Game1.player.UniqueMultiplayerID).Min();
            _populatesCurrentVisit = _birdOwnerId == Game1.player.UniqueMultiplayerID;
            if (_populatesCurrentVisit)
            {
                _birdVisitId = Guid.NewGuid().ToString("N");
            }
        }

        if (Context.IsMainPlayer)
        {
            _plan = BuildDebugPlan(null);
            ReportPlan();
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
            case nameof(XpAwarded) when IsFromHost(e) && !Context.IsMainPlayer:
                if (_sync.Read<XpAwarded>(e) is { } xp)
                {
                    SetXp(Math.Max(xp.BirdingXp, _saveData.BirdingXp));
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
                if (_sync.Read<ObservationLogSnapshot>(e) is { } snapshot && snapshot.Log.LogVersion >= _saveData.LogVersion)
                {
                    _saveData = snapshot.Log;
                    _pendingObservations.RemoveWhere(_saveData.SeenBirds.Contains);
                }

                break;
            case nameof(SpawnPlanMessage) when IsFromHost(e):
                if (_sync.Read<SpawnPlanMessage>(e) is { } plan)
                {
                    _plan = MessageJson.Deserialize<DailySpawnPlan>(plan.PlanJson);
                    if (Context.IsWorldReady && Game1.currentLocation is { } location)
                    {
                        _birdVisitId = "";
                        _birdRequestId = "";
                        _birdSequence = 0;
                        _locationSpawner.Clear(location);
                        if (_populatesCurrentVisit)
                        {
                            _birdOwnerId = Game1.player.UniqueMultiplayerID;
                            _birdVisitId = Guid.NewGuid().ToString("N");
                            _locationSpawner.Populate(location, _plan);
                        }
                        else
                        {
                            Farmer? farmer = Game1.getOnlineFarmers().FirstOrDefault(f =>
                                f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && f.currentLocation == location);
                            if (farmer is not null)
                            {
                                RequestLocationBirds(location, farmer.UniqueMultiplayerID);
                            }
                            else
                            {
                                _populatesCurrentVisit = true;
                                _birdOwnerId = Game1.player.UniqueMultiplayerID;
                                _birdVisitId = Guid.NewGuid().ToString("N");
                                _birdSequence = 0;
                                _locationSpawner.Populate(location, _plan);
                            }
                        }
                    }
                }

                break;
            case nameof(LocationBirdsRequest):
                if (_sync.Read<LocationBirdsRequest>(e) is { } birdsRequest)
                {
                    if (birdsRequest.RequestId.Length != 0 && (birdsRequest.RequesterId == e.FromPlayerID ||
                        Game1.getOnlineFarmers().Any(farmer => farmer.UniqueMultiplayerID == birdsRequest.RequesterId && farmer.currentLocation == Game1.currentLocation))
                    )
                    {
                        SendLocationBirds(birdsRequest.LocationName, birdsRequest.RequesterId, birdsRequest.RequestId);
                    }
                }

                break;
            case nameof(LocationBirdsState):
                if (_sync.Read<LocationBirdsState>(e) is { } birdsState)
                {
                    ReceiveLocationBirds(birdsState, e.FromPlayerID);
                }

                break;
            case nameof(LocationBirdsHandoff):
                if (_sync.Read<LocationBirdsHandoff>(e) is { } handoff)
                {
                    ReceiveBirdHandoff(handoff, e.FromPlayerID);
                }

                break;
            default:
                Monitor.Log($"Ignored message {e.Type} from player {e.FromPlayerID}.", LogLevel.Trace);
                break;
        }
    }

    private static bool IsFromHost(ModMessageReceivedEventArgs e) =>
        !Context.IsMainPlayer && e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID;

    private void RequestLocationBirds(GameLocation location, long peerId)
    {
        _birdRequestId = Guid.NewGuid().ToString("N");
        _sync.SendToPeer(new LocationBirdsRequest(IMessage.CurrentVersion, location.Name,
            Game1.player.UniqueMultiplayerID, _birdRequestId), peerId);
    }

    private void SendLocationBirds(string locationName, long requestingPlayerId, string requestId)
    {
        if (Game1.currentLocation?.Name != locationName)
        {
            Monitor.Log($"Ignored bird request for {locationName}: not in that location.", LogLevel.Debug);
            return;
        }

        if (!_populatesCurrentVisit)
        {
            if (_birdOwnerId != 0 && _birdOwnerId != requestingPlayerId)
            {
                _sync.SendToPeer(new LocationBirdsRequest(IMessage.CurrentVersion, locationName, requestingPlayerId, requestId), _birdOwnerId);
            }
            return;
        }

        _sync.SendToPeer(new LocationBirdsState(IMessage.CurrentVersion, locationName, _birdVisitId,
            _birdSequence, SerializeCurrentBirds(Game1.currentLocation), requestId), requestingPlayerId);
    }

    private static string SerializeCurrentBirds(GameLocation location) =>
        BirdSnapshots.Serialize(location.critters.OfType<BirdCritter>());

    private void ReceiveLocationBirds(LocationBirdsState state, long senderId)
    {
        if (_populatesCurrentVisit || Game1.currentLocation?.Name != state.LocationName || state.VisitId.Length == 0
            || (_birdVisitId.Length != 0 && (state.VisitId != _birdVisitId || senderId != _birdOwnerId || state.Sequence <= _birdSequence))
            || (_birdVisitId.Length == 0 && (state.RequestId != _birdRequestId || _birdRequestId.Length == 0 ||
                !Game1.getOnlineFarmers().Any(farmer => farmer.UniqueMultiplayerID == senderId && farmer.currentLocation == Game1.currentLocation))))
        {
            return;
        }

        List<BirdCritter> birds = BirdSnapshots.Deserialize(state.BirdsJson);
        GameLocation location = Game1.currentLocation;
        location.instantiateCrittersList();
        Dictionary<long, BirdCritter> existing = location.critters.OfType<BirdCritter>().ToDictionary(bird => bird.BirdID);
        HashSet<long> received = new();
        foreach (BirdCritter bird in birds)
        {
            if (!received.Add(bird.BirdID))
            {
                continue;
            }
            if (existing.TryGetValue(bird.BirdID, out BirdCritter? replica) && replica.BirdTypeName == bird.BirdTypeName)
            {
                replica.ApplyReplicaState(bird);
                continue;
            }

            bird.Services = _birdServices;
            bird.IsReplica = true;
            bird.ApplyReplicaState(bird);
            location.critters.Add(bird);
        }

        location.critters.RemoveAll(critter => critter is BirdCritter bird &&
            (!received.Contains(bird.BirdID) || (existing.ContainsKey(bird.BirdID) &&
                existing[bird.BirdID].BirdTypeName != bird.BirdTypeName && ReferenceEquals(critter, existing[bird.BirdID]))));
        _birdVisitId = state.VisitId;
        _birdRequestId = "";
        _birdOwnerId = senderId;
        _birdSequence = state.Sequence;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !_populatesCurrentVisit || !e.IsMultipleOf(6)
            || Game1.currentLocation is not { } location || _birdVisitId.Length == 0)
        {
            return;
        }

        if (!location.farmers.Any(farmer => farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID))
        {
            return;
        }

        _birdSequence++;
        LocationBirdsState state = new(IMessage.CurrentVersion, location.Name, _birdVisitId,
            _birdSequence, SerializeCurrentBirds(location));
        foreach (Farmer farmer in location.farmers)
        {
            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
            {
                _sync.SendToPeer(state, farmer.UniqueMultiplayerID);
            }
        }
    }

    private void ReceiveBirdHandoff(LocationBirdsHandoff handoff, long senderId)
    {
        if (Game1.currentLocation?.Name != handoff.LocationName || _populatesCurrentVisit
            || (senderId != _birdOwnerId && !(_birdVisitId.Length == 0 && _birdRequestId.Length != 0
                && Game1.getOnlineFarmers().Any(farmer => farmer.UniqueMultiplayerID == senderId && farmer.currentLocation == Game1.currentLocation)))
            || handoff.VisitId.Length == 0
            || (_birdVisitId.Length != 0 && (handoff.VisitId != _birdVisitId || handoff.Sequence <= _birdSequence)))
        {
            return;
        }

        if (handoff.NextOwnerId != Game1.player.UniqueMultiplayerID &&
            !Game1.getOnlineFarmers().Any(farmer => farmer.UniqueMultiplayerID == handoff.NextOwnerId && farmer.currentLocation == Game1.currentLocation))
        {
            return;
        }

        _birdOwnerId = handoff.NextOwnerId;
        _birdRequestId = "";
        _birdSequence = handoff.Sequence;
        _birdVisitId = handoff.VisitId;
        if (_birdOwnerId != Game1.player.UniqueMultiplayerID)
        {
            return;
        }

        _populatesCurrentVisit = true;
        RestoreOwnership(handoff.BirdsJson);
    }

    private void RestoreOwnership(string birdsJson)
    {
        GameLocation location = Game1.currentLocation;
        _locationSpawner.Clear(location);
        location.instantiateCrittersList();
        foreach (BirdCritter bird in BirdSnapshots.Deserialize(birdsJson))
        {
            bird.Services = _birdServices;
            bird.sprite.CurrentAnimation = null;
            location.critters.Add(bird);
        }
        _locationSpawner.ResumeVisit(location, _plan);
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (!Context.IsWorldReady || _populatesCurrentVisit || e.Peer.PlayerID != _birdOwnerId
            || Game1.currentLocation is not { } location)
        {
            return;
        }

        long nextOwnerId = Game1.getOnlineFarmers().Where(farmer => farmer.UniqueMultiplayerID != e.Peer.PlayerID && farmer.currentLocation == location)
            .Select(farmer => farmer.UniqueMultiplayerID).DefaultIfEmpty(Game1.player.UniqueMultiplayerID).Min();
        _birdOwnerId = nextOwnerId;
        if (_birdVisitId.Length == 0)
        {
            if (nextOwnerId == Game1.player.UniqueMultiplayerID)
            {
                _populatesCurrentVisit = true;
                _birdVisitId = Guid.NewGuid().ToString("N");
                _locationSpawner.Clear(location);
                _locationSpawner.Populate(location, _plan);
            }
            else
            {
                RequestLocationBirds(location, nextOwnerId);
            }
            return;
        }

        if (nextOwnerId == Game1.player.UniqueMultiplayerID)
        {
            _populatesCurrentVisit = true;
            RestoreOwnership(SerializeCurrentBirds(location));
        }
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
        int day = (int)Game1.stats.DaysPlayed;
        if (_saveData.SeenBirds.Contains(birdName))
        {
            ReportRepeatSighting(birdName, day);
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

    private void ReportRepeatSighting(string birdName, int day)
    {
        if (Context.IsMainPlayer)
        {
            AwardSighting(birdName, day);
            return;
        }

        if (_reportedDay != day)
        {
            _reportedDay = day;
            _reportedSightings.Clear();
        }

        if (_reportedSightings.Add(birdName))
        {
            ObservationData details = new(Game1.currentLocation.Name, new SDate(Game1.dayOfMonth, Game1.currentSeason), Game1.timeOfDay, Game1.player.Name);
            _sync.SendToPeer(new ObservationRequest(IMessage.CurrentVersion, birdName, details), Game1.MasterPlayer.UniqueMultiplayerID);
        }
    }

    private void AwardSighting(string birdId, int day)
    {
        if (BirdingProgress.TryAwardSighting(_saveData, birdId, day, out SaveData updated))
        {
            SetXp(updated.BirdingXp, updated);
            _sync.SendToAll(new XpAwarded(IMessage.CurrentVersion, updated.BirdingXp));
        }
    }

    private void SetXp(long xp, SaveData? replacement = null)
    {
        int before = BirdingProgress.Level(_saveData.BirdingXp);
        if (replacement is not null)
        {
            _saveData = replacement;
        }
        else
        {
            _saveData.BirdingXp = xp;
        }

        NotifyLevelUp(before);
    }

    private void NotifyLevelUp(int before)
    {
        int after = BirdingProgress.Level(_saveData.BirdingXp);
        if (after > before)
        {
            Game1.addHUDMessage(new HUDMessage($"Birding level {after}!", HUDMessage.achievement_type));
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
            AwardSighting(birdId, (int)Game1.stats.DaysPlayed);
            return;
        }

        int levelBefore = BirdingProgress.Level(_saveData.BirdingXp);
        _saveData = updated;
        NotifyLevelUp(levelBefore);
        Monitor.Log($"New bird seen (log version {updated.LogVersion}):", LogLevel.Debug);
        Utils.LogObservation(Monitor, _catalog, _saveData, birdId);
        _sync.SendToAll(new ObservationAccepted(IMessage.CurrentVersion, updated.LogVersion, birdId, details));
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
                int levelBefore = BirdingProgress.Level(_saveData.BirdingXp);
                _saveData = updated;
                NotifyLevelUp(levelBefore);
                if (_catalog.TryGetValue(accepted.BirdId, out BirdDefinition? bird))
                {
                    ShowDiscovery(bird, accepted.Details);
                }

                return;
            default:
                Monitor.Log($"Missed an observation update (local {_saveData.LogVersion}, received {accepted.LogVersion}); requesting the full log.", LogLevel.Debug);
                _sync.SendToPeer(new ObservationLogRequest(IMessage.CurrentVersion), Game1.MasterPlayer.UniqueMultiplayerID);
                return;
        }
    }

    private static void ShowDiscovery(BirdDefinition bird, ObservationData details) =>
        Game1.showGlobalMessage($"{details.PlayerName} discovered a new bird: {bird.Name}!");

    // #############################
    // # Spawning / Removing Birds #
    // #############################

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (_populatesCurrentVisit && _birdVisitId.Length != 0 && e.OldLocation is { } oldLocation)
        {
            long nextOwnerId = oldLocation.farmers.Where(farmer => farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                .Select(farmer => farmer.UniqueMultiplayerID).DefaultIfEmpty(0).Min();
            if (nextOwnerId != 0)
            {
                LocationBirdsHandoff handoff = new(IMessage.CurrentVersion, oldLocation.Name, nextOwnerId,
                    _birdVisitId, _birdSequence + 1, SerializeCurrentBirds(oldLocation));
                foreach (Farmer recipient in oldLocation.farmers)
                {
                    if (recipient.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                    {
                        _sync.SendToPeer(handoff, recipient.UniqueMultiplayerID);
                    }
                }
            }
        }

        _birdVisitId = "";
        _birdRequestId = "";
        _birdSequence = 0;
        _birdOwnerId = 0;
        if (e.NewLocation == null)
        {
            return;
        }

        Monitor.Log($"{Game1.player.Name} entered {e.NewLocation.Name}.", LogLevel.Debug);
        VanillaBirds.Remove(e.NewLocation);

        if (!_eligibleLocations.Contains(e.NewLocation.Name) && !_plan.Locations.ContainsKey(e.NewLocation.Name))
        {
            return;
        }

        _locationSpawner.BeginVisit();
        Farmer? farmer = Game1.getOnlineFarmers().FirstOrDefault(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && f.currentLocation == e.NewLocation);
        _populatesCurrentVisit = farmer is null;
        if (farmer == null)
        {
            _birdOwnerId = Game1.player.UniqueMultiplayerID;
            _birdVisitId = Guid.NewGuid().ToString("N");
            _locationSpawner.Populate(e.NewLocation, _plan);
            return;
        }

        _locationSpawner.Clear(e.NewLocation);
        _birdOwnerId = farmer.UniqueMultiplayerID;
        Monitor.Log($"{Game1.player.Name} request active birds at {e.NewLocation.Name}.", LogLevel.Debug);
        RequestLocationBirds(e.NewLocation, farmer.UniqueMultiplayerID);
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Game1.currentLocation != null)
        {
            VanillaBirds.Remove(Game1.currentLocation);
            if (_populatesCurrentVisit && (_eligibleLocations.Contains(Game1.currentLocation.Name)
                || _plan.Locations.ContainsKey(Game1.currentLocation.Name)))
            {
                _locationSpawner.Populate(Game1.currentLocation, _plan);
            }
        }
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is GameMenu menu)
        {
            BirdCollectionsTab.Install(menu, Helper.ModContent, Helper.Reflection, Helper.Translation, _catalog, _saveData);
        }
    }
}
