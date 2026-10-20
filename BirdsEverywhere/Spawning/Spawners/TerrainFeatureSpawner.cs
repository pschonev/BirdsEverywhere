using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners;
/// <summary>
/// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
/// </summary>
///

public class TreeTrunkSpawner : TerrainFeatureSpawner
{
    /// <summary>
    /// Checks for valid tree.
    /// </summary>

    public TreeTrunkSpawner() : base((location, index, _) => IsEligibleTree(location, index)) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        List<TerrainFeature> features = location.terrainFeatures.Pairs.Select(pair => pair.Value).ToList();
        IEnumerable<int> trees = Enumerable.Range(0, features.Count).Where(index => IsEligibleTree(location, index));
        return Placements(TileSelection.Features(trees, rule.GroupCount, random, (index, _) => features[index].Tile, avoid), location, data, rule, random, monitor);
    }
}

public class BushSpawner : TerrainFeatureSpawner
{
    /// <summary>
    /// Checks for valid bush with free path.
    /// </summary>

    public BushSpawner() : base(HasStraightPathToBush) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        IEnumerable<int> bushes = Enumerable.Range(0, location.largeTerrainFeatures.Count).Where(index => location.largeTerrainFeatures[index] is Bush);
        return Placements(TileSelection.Features(bushes, rule.GroupCount, random, (index, r) => PlaceNearBush(location, index, r), avoid), location, data, rule, random, monitor);
    }

    private static Vector2? PlaceNearBush(GameLocation location, int index, Random random)
    {
        Vector2 bush = location.largeTerrainFeatures[index].Tile;
        int distance = random.Next(5, 12);
        Vector2 position = new(bush.X + (random.NextDouble() < 0.5 ? distance : -distance), bush.Y);
        return HasStraightPathToBush(location, index, position) ? position : null;
    }
}

public abstract class TerrainFeatureSpawner : Spawner
{
    protected TerrainFeatureSpawner(TerrainSpawnCondition featureCondition)
    {
        FeatureCondition = featureCondition;
    }

    public TerrainSpawnCondition FeatureCondition { get; }

    public override bool IsStillValid(GameLocation location, BirdPlacement placement) =>
        placement.FeatureIndex is int index && FeatureCondition(location, index, placement.Tile);

    protected static List<BirdPlacement> Placements(List<(int Index, Vector2 Position)> placed, GameLocation location,
        BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor)
    {
        List<BirdPlacement> spawnList = placed
            .Select(p => Place(data, rule, p.Position, p.Index, random))
            .ToList();
        monitor.Log($"Planned {spawnList.Count} {data.Id.Value} at {location.Name}.", LogLevel.Debug);
        return spawnList;
    }
}

