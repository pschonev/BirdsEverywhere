using StardewModdingAPI;
using StardewValley;
using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Spawners;
/// <summary>
/// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
/// </summary>
public static class SpawnerFactory
{
    public delegate Spawner SpawnPatternDelegate();

    private static readonly Dictionary<string, SpawnPatternDelegate> spawnerTypes = new Dictionary<string, SpawnPatternDelegate>() {
        {"GroundSpawner", () => new GroundSpawner()},
        {"WaterSpawner", () => new WaterSpawner()},
        {"WaterOrGroundSpawner", () => new WaterOrGroundSpawner()},
        {"SpawnableGroundSpawner", () => new SpawnableGroundSpawner()},
        {"TreeTrunkSpawner", () => new TreeTrunkSpawner() },
        {"BushSpawner", () => new BushSpawner() }
    };

    public static IReadOnlyCollection<string> KnownPatterns => spawnerTypes.Keys;

    public static Spawner createSpawner(GameLocation location, BirdDefinition data, SpawnRule rule, IMonitor monitor)
    {
        string pattern = rule.SpawnPattern.Value;
        monitor.Log($"For location {location} {data.Name}s will be created with {pattern}", LogLevel.Debug);
        return createSpawner(pattern) ?? throw new KeyNotFoundException($"Unknown spawn pattern '{pattern}' for bird '{data.Id.Value}'.");
    }

    public static Spawner? createSpawner(string? pattern)
    {
        if (pattern is null || !spawnerTypes.TryGetValue(pattern, out SpawnPatternDelegate? create))
        {
            return null;
        }

        return create();
    }
}

