using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static BirdsEverywhere.Spawners.Condition;
using BirdsEverywhere.Domain;

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

    public TreeTrunkSpawner()
    {
        condition = (location, index, position) => isEligibleTree(location, index, position);
    }

    public override List<BirdPlacement> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor)
    {
        List<TerrainFeature> features = location.terrainFeatures.Pairs.Select(pair => pair.Value).ToList();
        IEnumerable<int> trees = Enumerable.Range(0, features.Count).Where(index => isEligibleTree(location, index, Vector2.Zero));
        return Placements(TileSelection.Features(trees, rule.GroupCount, random, (index, _) => features[index].Tile), location, data, rule, random, monitor);
    }
}

public class BushSpawner : TerrainFeatureSpawner
{
    /// <summary>
    /// Checks for valid bush with free path.
    /// </summary>

    public BushSpawner()
    {
        condition = (location, index, position) => straightPathToBush(location, index, position);
    }

    public override List<BirdPlacement> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor)
    {
        IEnumerable<int> bushes = Enumerable.Range(0, location.largeTerrainFeatures.Count).Where(index => location.largeTerrainFeatures[index] is Bush);
        return Placements(TileSelection.Features(bushes, rule.GroupCount, random, (index, r) => PlaceNearBush(location, index, r)), location, data, rule, random, monitor);
    }

    private static Vector2? PlaceNearBush(GameLocation location, int index, Random random)
    {
        Vector2 bush = location.largeTerrainFeatures[index].Tile;
        int distance = random.Next(5, 12);
        Vector2 position = new(bush.X + (random.NextDouble() < 0.5 ? distance : -distance), bush.Y);
        return straightPathToBush(location, index, position) ? position : null;
    }
}

public abstract class TerrainFeatureSpawner : Spawner
{
    public TerrainSpawnCondition condition = null!;

    public override bool IsStillValid(GameLocation location, BirdPlacement placement) =>
        placement.FeatureIndex is int index && condition(location, index, placement.Tile);

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

