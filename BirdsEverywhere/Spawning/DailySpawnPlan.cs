using BirdsEverywhere.Domain;
using BirdsEverywhere.Registries;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere.Spawners;

public sealed record BirdPlacement(
    string BirdId,
    string RuleId,
    string BirdType,
    string SpawnPattern,
    Vector2 Tile,
    int? FeatureIndex,
    IReadOnlyList<IntRange> Times,
    string Texture,
    int Seed)
{
    public bool IsActiveAt(int time) => Times.Count == 0 || Times.Any(range => range.Contains(time));
}

public sealed record DailySpawnPlan(IReadOnlyDictionary<string, IReadOnlyList<BirdPlacement>> Locations)
{
    public static DailySpawnPlan Empty { get; } = new(new Dictionary<string, IReadOnlyList<BirdPlacement>>());

    internal static DailySpawnPlan Build(IReadOnlySet<string> seenBirds, IReadOnlyDictionary<string, BirdDefinition> catalog,
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

        Dictionary<string, IReadOnlyList<BirdPlacement>> locations = new(StringComparer.Ordinal);
        foreach (SpawnAssignment assignment in SpawnPlanner.Plan(catalog, seenBirds, season, weather, day, random, LocationExists))
        {
            if (!patterns.TryCreate(assignment.Rule.SpawnPattern, out Spawner? spawner))
            {
                monitor.Log($"Unknown spawn pattern '{assignment.Rule.SpawnPattern.Value}' for {assignment.Bird.Id.Value}; skipping.", LogLevel.Warn);
                continue;
            }

            locations[assignment.Location] = spawner.spawnBirds(Game1.getLocationFromName(assignment.Location), assignment.Bird, assignment.Rule, random, monitor);
        }

        return new DailySpawnPlan(locations);
    }
}
