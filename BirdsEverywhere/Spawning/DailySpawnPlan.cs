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
        SpawnPatternRegistry patterns, Domain.Season season, Weather weather, int day, double luck, Random random, IMonitor monitor)
    {
        // Missing locations, farm types and fish areas usually belong to a mod or farm type that isn't active.
        bool HabitatExists(Habitat habitat)
        {
            GameLocation? location = Game1.getLocationFromName(habitat.Location);
            return location is not null
                && (habitat.FarmType is null || string.Equals(habitat.FarmType, Game1.GetFarmTypeKey(), StringComparison.OrdinalIgnoreCase))
                && (habitat.FishArea is null || location.GetData()?.FishAreas?.ContainsKey(habitat.FishArea) == true);
        }

        Dictionary<string, IReadOnlyList<BirdPlacement>> locations = new(StringComparer.Ordinal);
        bool Place(SpawnAssignment assignment)
        {
            if (!patterns.TryCreate(assignment.Rule.SpawnPattern, out Spawner? spawner))
            {
                monitor.Log($"Unknown spawn pattern '{assignment.Rule.SpawnPattern.Value}' for {assignment.Bird.Id.Value}; skipping.", LogLevel.Warn);
                return false;
            }

            List<BirdPlacement> placements = spawner.spawnBirds(Game1.getLocationFromName(assignment.Location), assignment.Bird, assignment.Rule, assignment.Habitat.FishArea, random, monitor);
            if (placements.Count == 0)
            {
                return false;
            }

            locations[assignment.Location] = placements;
            return true;
        }

        SpawnPlanner.Plan(catalog, seenBirds, season, weather, day, luck, random, HabitatExists, Place);
        return new DailySpawnPlan(locations);
    }
}
