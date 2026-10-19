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
        HashSet<string> locations = catalog.Values.SelectMany(bird => bird.Rules).SelectMany(rule => rule.Locations).ToHashSet(StringComparer.Ordinal);
        int limit = Math.Max(1, locations.Count / 2);
        IEnumerable<BirdDefinition> candidates = catalog.Values.OrderBy(bird => seenBirds.Contains(bird.Id.Value));
        foreach (BirdDefinition bird in candidates)
        {
            SpawnRule? rule = bird.PickRule(season, weather, day, random);
            if (rule is null || rule.Locations.IsEmpty)
            {
                continue;
            }

            IEnumerable<string> choices = seenBirds.Contains(bird.Id.Value)
                ? rule.Locations
                : new[] { rule.Locations[random.Next(rule.Locations.Length)] };
            foreach (string name in choices)
            {
                if (LocationBirdPosition.ContainsKey(name))
                {
                    continue;
                }

                GameLocation? location = Game1.getLocationFromName(name);
                if (location is null)
                {
                    monitor.Log($"Unknown location '{name}' for {bird.Id.Value}; skipping.", LogLevel.Warn);
                    continue;
                }

                if (!patterns.TryCreate(rule.SpawnPattern, out Spawner? spawner))
                {
                    monitor.Log($"Unknown spawn pattern '{rule.SpawnPattern.Value}' for {bird.Id.Value}; skipping.", LogLevel.Warn);
                    continue;
                }

                LocationBirdPosition[name] = spawner.spawnBirds(location, bird, rule, random, monitor);
                if (LocationBirdPosition.Count >= limit)
                {
                    return;
                }
            }
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

            if (!birdTypes.TryCreate(new BirdTypeId(placement.BirdType), placement, location, services, out CustomBirdType? bird))
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
