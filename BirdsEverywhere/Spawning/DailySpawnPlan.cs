using BirdsEverywhere.Diagnostics;
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
    int Seed,
    bool Forced = false,
    bool Unseen = false,
    BirdComposition? Composition = null)
{
    public bool IsActiveAt(int time) => Times.Count == 0 || Times.Any(range => range.Contains(time));
}

public sealed record DailySpawnPlan(IReadOnlyDictionary<string, IReadOnlyList<BirdPlacement>> Locations)
{
    public static DailySpawnPlan Empty { get; } = new(new Dictionary<string, IReadOnlyList<BirdPlacement>>());

    internal static DailySpawnPlan Build(IReadOnlyList<Region> regions, IReadOnlySet<string> seenBirds, IReadOnlyDictionary<string, BirdDefinition> catalog,
        SpawnPatternRegistry patterns, Domain.Season season, Weather weather, int day, double luck, int level, Random random, IMonitor monitor,
        Action<string>? skipped = null)
    {
        Dictionary<string, List<BirdPlacement>> locations = new(StringComparer.Ordinal);
        bool Place(SpawnAssignment assignment)
        {
            if (!patterns.TryCreate(assignment.Rule.SpawnPattern, out Spawner? spawner))
            {
                monitor.Log($"Unknown spawn pattern '{assignment.Rule.SpawnPattern.Value}' for {assignment.Bird.Id.Value}; skipping.", LogLevel.Warn);
                return false;
            }

            locations.TryGetValue(assignment.Location, out List<BirdPlacement>? existing);
            List<BirdPlacement> placements = spawner.SpawnBirds(Game1.getLocationFromName(assignment.Location), assignment.Bird, assignment.Rule, assignment.Habitat.FishArea, random, monitor,
                existing?.Select(placement => placement.Tile).ToList());
            if (placements.Count == 0)
            {
                return false;
            }

            placements = placements.Select(placement => placement with { Unseen = assignment.Unseen, Composition = assignment.Rule.Composition }).ToList();
            if (existing is null)
            {
                locations.Add(assignment.Location, placements);
            }
            else
            {
                existing.AddRange(placements);
            }

            return true;
        }

        int LocationArea(string name) => Game1.getLocationFromName(name) is { map: not null } location ? location.map.DisplayWidth / 64 * (location.map.DisplayHeight / 64) : 0;

        SpawnPlanner.Plan(regions, catalog, seenBirds, season, weather, day, luck, level, random, GameHabitat.Exists, Place, LocationArea, skipped);
        return new DailySpawnPlan(locations.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<BirdPlacement>)entry.Value, StringComparer.Ordinal));
    }
}
