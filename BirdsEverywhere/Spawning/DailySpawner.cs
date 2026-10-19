using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Registries;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere.Spawners;

public sealed class DailySpawner
{
    private readonly HashSet<SingleBirdSpawnParameters> _spawned = new();

    public Dictionary<string, List<SingleBirdSpawnParameters>> LocationBirdPosition { get; set; } = new();

    public void BeginVisit() => _spawned.Clear();

    internal void InitializeBirdLocations(HashSet<string> seenBirds, IReadOnlyDictionary<string, BirdDefinition> catalog,
        SpawnPatternRegistry patterns, Domain.Season season, Weather weather, int day, Random random, IMonitor monitor)
    {
        bool LocationExists(string name)
        {
            if (Game1.getLocationFromName(name) is not null)
            {
                return true;
            }

            monitor.LogOnce($"Unknown location '{name}'; skipping.", LogLevel.Warn);
            return false;
        }

        foreach (SpawnAssignment assignment in SpawnPlanner.Plan(catalog, seenBirds, season, weather, day, random, LocationExists))
        {
            if (!patterns.TryCreate(assignment.Rule.SpawnPattern, out Spawner? spawner))
            {
                monitor.Log($"Unknown spawn pattern '{assignment.Rule.SpawnPattern.Value}' for {assignment.Bird.Id.Value}; skipping.", LogLevel.Warn);
                continue;
            }

            LocationBirdPosition[assignment.Location] = spawner.spawnBirds(Game1.getLocationFromName(assignment.Location), assignment.Bird, assignment.Rule, random, monitor);
        }
    }

    internal void Populate(GameLocation location, BirdServices services, SpawnPatternRegistry patterns, BirdTypeRegistry birdTypes)
    {
        if (!LocationBirdPosition.TryGetValue(location.Name ?? "", out List<SingleBirdSpawnParameters>? positions))
        {
            return;
        }

        location.instantiateCrittersList();
        foreach (SingleBirdSpawnParameters placement in positions)
        {
            if (_spawned.Contains(placement) || !placement.stillValidTime(Game1.timeOfDay))
            {
                continue;
            }

            if (!patterns.TryCreate(new SpawnPatternId(placement.spawnPattern), out Spawner? spawner))
            {
                services.Monitor.LogOnce($"Bird '{placement.ID}' has unknown spawn pattern '{placement.spawnPattern}'; skipping.", LogLevel.Warn);
                continue;
            }

            if (!placement.stillValidSpawnPosition(location, spawner))
            {
                continue;
            }

            if (!birdTypes.TryCreate(new BirdTypeId(placement.BirdType), placement, location, services, out BirdCritter? bird))
            {
                services.Monitor.LogOnce($"Bird '{placement.ID}' has unknown bird type '{placement.BirdType}' for its placement; skipping.", LogLevel.Warn);
                continue;
            }

            location.critters.Add(bird);
            _spawned.Add(placement);
            services.Monitor.Log($"Added {placement.ID} at {(int)placement.position.X} - {(int)placement.position.Y}.", LogLevel.Debug);
        }
    }
}
