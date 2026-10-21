using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners;

/// <summary>Places birds on eligible tree trunks, anchored by the tree's tile.</summary>
public class TreeTrunkSpawner : TerrainFeatureSpawner
{
    public TreeTrunkSpawner() : base((location, feature, _) => IsEligibleTreeAt(location, feature)) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        IEnumerable<Vector2> trees = location.terrainFeatures.Pairs.Where(pair => IsEligibleTree(pair.Value)).Select(pair => pair.Key).ToList();
        return Placements(TileSelection.Features(trees, rule.GroupCount, random, (tile, _) => tile, avoid, rule.Params.SpeciesDistance ?? TileSelection.MinSpeciesDistance),
            location, data, rule, random, monitor);
    }
}

/// <summary>Places birds beside bushes with a free straight path, anchored by the bush's tile.</summary>
public class BushSpawner : TerrainFeatureSpawner
{
    public BushSpawner() : base(HasStraightPathToBush) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        IEnumerable<Vector2> bushes = location.largeTerrainFeatures.OfType<Bush>().Select(bush => bush.Tile).ToList();
        return Placements(TileSelection.Features(bushes, rule.GroupCount, random, (bush, r) => PlaceNearBush(location, bush, r), avoid,
            rule.Params.SpeciesDistance ?? TileSelection.MinSpeciesDistance), location, data, rule, random, monitor);
    }

    private static Vector2? PlaceNearBush(GameLocation location, Vector2 bush, Random random)
    {
        int distance = random.Next(5, 12);
        Vector2 position = new(bush.X + (random.NextDouble() < 0.5 ? distance : -distance), bush.Y);
        return HasStraightPathToBush(location, bush, position) ? position : null;
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
        placement.FeatureTile is Vector2 feature && FeatureCondition(location, feature, placement.Tile);

    protected static List<BirdPlacement> Placements(List<(Vector2 Feature, Vector2 Position)> placed, GameLocation location,
        BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor)
    {
        List<BirdPlacement> spawnList = placed
            .Select(p => Place(data, rule, p.Position, p.Feature, random))
            .ToList();
        monitor.Log($"Planned {spawnList.Count} {data.Id.Value} at {location.Name}.", LogLevel.Debug);
        return spawnList;
    }
}
