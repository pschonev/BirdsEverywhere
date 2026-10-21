using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Observations;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere;

public partial class ModEntry
{
    private void RegisterDebugCommands(IModHelper helper)
    {
        var commands = helper.ConsoleCommands;
        commands.Add("birds_debug", "Toggle bird debug HUD: birds_debug on|off", DebugMode);
        commands.Add("birds_plan", "Print today's bird plan.", ShowPlan);
        commands.Add("birds_spawn", "birds_spawn <bird> [<ruleId>] [-l <location>] [-f]", SpawnBird);
        commands.Add("birds_replan", "Rebuild today's plan: birds_replan [seed]", Replan);
        commands.Add("birds_clear", "Remove birds in this location.", ClearBirds);
        commands.Add("birds_xp", "Show the farm's birding level and XP.", (_, _) => Monitor.Log($"Birding {BirdingProgress.Describe(_saveData.BirdingXp)} ({_saveData.BirdingXp} XP).", LogLevel.Info));
        commands.Add("birds_reload", "Reload bird definitions.", ReloadBirds);
    }

    private bool CheckDebugCommand(bool host = false)
    {
        if (!Context.IsWorldReady || (host && !Context.IsMainPlayer))
        {
            Monitor.Log(host ? "Load a save as host first." : "Load a save first.", LogLevel.Warn);
            return false;
        }

        return true;
    }

    private void DebugMode(string command, string[] args)
    {
        if (args.Length != 1 || (args[0] != "on" && args[0] != "off"))
        {
            Monitor.Log("Usage: birds_debug on|off", LogLevel.Info);
            return;
        }

        _config.Debug = args[0] == "on";
        Monitor.Log($"Bird debug HUD {args[0]} (session only).", LogLevel.Info);
    }

    private void ShowPlan(string command, string[] args)
    {
        if (!CheckDebugCommand())
        {
            return;
        }
        List<string> lines = Diagnostics.PlanText.Lines(_plan).ToList();
        Monitor.Log(lines.Count == 0 ? "No birds planned today." : string.Join(Environment.NewLine, lines), LogLevel.Info);
    }

    private void ReportPlan()
    {
        if (!_config.Debug)
        {
            return;
        }
        List<string> lines = Diagnostics.PlanText.Lines(_plan).ToList();
        Monitor.Log(lines.Count == 0 ? "No birds planned today." : string.Join(Environment.NewLine, lines), LogLevel.Info);
        foreach (string line in lines)
        {
            Game1.addHUDMessage(new HUDMessage(line));
        }
    }

    private DailySpawnPlan BuildDebugPlan(int? seed)
    {
        Random random = new(seed ?? Utils.StableHash($"{Game1.uniqueIDForThisGame}|{Game1.stats.DaysPlayed}|day"));
        Domain.Season season = Enum.Parse<Domain.Season>(Game1.currentSeason, ignoreCase: true);
        Weather weather = CurrentWeather();
        if (_config.Debug)
        {
            foreach (BirdDefinition bird in _catalog.Values.Where(bird => bird.EligibleRules(season, weather, Game1.dayOfMonth).Count == 0))
            {
                Monitor.VerboseLog($"{bird.Id.Value}: no eligible rule today.");
            }
        }

        return DailySpawnPlan.Build(_regions, _saveData.SeenBirds, _catalog, _spawnPatterns, season, weather,
            Game1.dayOfMonth, Game1.player.DailyLuck, BirdingProgress.Level(_saveData.BirdingXp), random, Monitor,
            skipped: message => Monitor.VerboseLog(message));
    }

    private static Weather CurrentWeather() => Game1.isGreenRain ? Weather.GreenRain
        : Game1.isLightning ? Weather.Storm
        : Game1.isSnowing ? Weather.Snow
        : Game1.isDebrisWeather ? Weather.Wind
        : Game1.isRaining ? Weather.Rain : Weather.Sun;

    private void BroadcastPlan() => _sync.SendToAll(new SpawnPlanMessage(IMessage.CurrentVersion, MessageJson.Serialize(_plan)));

    private void SpawnBird(string command, string[] args)
    {
        if (!CheckDebugCommand(host: true))
        {
            return;
        }
        if (!Diagnostics.BirdSelector.TryParse(args, out Diagnostics.BirdSelector? selector, out string error))
        {
            Monitor.Log(error, LogLevel.Warn);
            return;
        }

        if (!TryGetBird(selector!.BirdId, out BirdDefinition bird))
        {
            return;
        }

        Random random = new Random();
        if (!Diagnostics.SpawnTarget.TryResolve(bird, selector, Game1.currentLocation.Name,
            Enum.Parse<Domain.Season>(Game1.currentSeason, true), CurrentWeather(), Game1.dayOfMonth,
            Diagnostics.GameHabitat.Exists, random, out SpawnAssignment? target, out error,
            locationExists: name => Game1.getLocationFromName(name) is not null))
        {
            Monitor.Log(error, LogLevel.Warn);
            return;
        }

        GameLocation location = Game1.getLocationFromName(target!.Location);
        if (!_spawnPatterns.TryCreate(target.Rule.SpawnPattern, out Spawner? spawner))
        {
            Monitor.Log($"Unknown pattern '{target.Rule.SpawnPattern.Value}'.", LogLevel.Warn);
            return;
        }

        List<BirdPlacement> placements = spawner.SpawnBirds(location, bird, target.Rule, target.Habitat.FishArea, random, Monitor);
        if (placements.Count == 0 && selector.Force && location == Game1.currentLocation)
        {
            GameLocationQuery query = new(location);
            Vector2 tile = new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) }
                .Select(offset => Game1.player.Tile + offset).FirstOrDefault(query.IsSpawnableGroundTile);
            if (tile != Vector2.Zero)
            {
                string fallbackType = target.Rule.BirdType.Value is "TreeTrunkBird" or "BushBird" ? "LandBird" : target.Rule.BirdType.Value;
                placements.Add(new BirdPlacement(bird.Id.Value, target.Rule.Id, fallbackType,
                    target.Rule.SpawnPattern.Value, tile, null, Array.Empty<IntRange>(), target.Rule.PickTexture(bird.Id, random), random.Next(), true));
            }
        }

        if (placements.Count == 0)
        {
            Monitor.Log("Pattern found no tile; -f can use a free tile near the player in the current location.", LogLevel.Warn);
            return;
        }

        List<BirdPlacement> immediate = placements.Select(placement => placement with { Times = Array.Empty<IntRange>(), Composition = target.Rule.Composition }).ToList();
        Dictionary<string, IReadOnlyList<BirdPlacement>> locations = new(_plan.Locations, StringComparer.Ordinal);
        locations[target.Location] = locations.TryGetValue(target.Location, out IReadOnlyList<BirdPlacement>? existing)
            ? existing.Concat(immediate).ToList() : immediate;
        _plan = new DailySpawnPlan(locations);
        BroadcastPlan();
        foreach (string line in Diagnostics.PlanText.Lines(new DailySpawnPlan(new Dictionary<string, IReadOnlyList<BirdPlacement>> { [target.Location] = immediate })))
        {
            Monitor.Log(line, LogLevel.Info);
        }

        if (location == Game1.currentLocation)
        {
            _locationSpawner.Populate(location, _plan);
        }
        else
        {
            Monitor.Log($"Debug warp to {target.Location} to see the bird.", LogLevel.Info);
        }
    }

    private void Replan(string command, string[] args)
    {
        if (!CheckDebugCommand(host: true))
        {
            return;
        }
        if (args.Length > 1 || (args.Length == 1 && !int.TryParse(args[0], out _)))
        {
            Monitor.Log("Usage: birds_replan [seed]", LogLevel.Warn);
            return;
        }

        _locationSpawner.Clear(Game1.currentLocation);
        _plan = BuildDebugPlan(args.Length == 1 ? int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : null);
        BroadcastPlan();
        ReportPlan();
        _locationSpawner.Populate(Game1.currentLocation, _plan);
    }

    private void ClearBirds(string command, string[] args)
    {
        if (!CheckDebugCommand(host: true))
        {
            return;
        }
        _locationSpawner.Clear(Game1.currentLocation);
        Dictionary<string, IReadOnlyList<BirdPlacement>> locations = new(_plan.Locations, StringComparer.Ordinal);
        locations.Remove(Game1.currentLocation.Name);
        _plan = new DailySpawnPlan(locations);
        BroadcastPlan();
        Monitor.Log("Removed birds from this location's plan and critters.", LogLevel.Info);
    }

    private void ReloadBirds(string command, string[] args)
    {
        if (!CheckDebugCommand(host: true))
        {
            return;
        }
        _locationSpawner.Clear(Game1.currentLocation);
        LoadCatalog();
        _plan = BuildDebugPlan(null);
        BroadcastPlan();
        ReportPlan();
        _locationSpawner.Populate(Game1.currentLocation, _plan);
        Monitor.Log("Bird definitions reloaded. Existing texture assets may remain cached until restart.", LogLevel.Info);
    }
}