using Birding.Content;
using Birding.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static Birding.Spawners.Condition;

namespace Birding.Spawners;

/// <summary>Places birds on eligible tree trunks, anchored by the tree's tile.</summary>
public class TreeTrunkSpawner : TerrainFeatureSpawner
{
    public TreeTrunkSpawner() : base((location, feature, _) => IsEligibleTreeAt(location, feature)) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        GameLocationQuery query = new(location);
        IEnumerable<Vector2> trees = location.terrainFeatures.Pairs.Where(pair => IsEligibleTree(pair.Value)).Select(pair => pair.Key)
            .Where(tile => TileRules.IsReachableNear(query, tile)).ToList();
        return Placements(TileSelection.Features(trees, rule.GroupCount, random, (tile, _) => tile, avoid, rule.Params.SpeciesDistance ?? TileSelection.MinSpeciesDistance),
            location, data, rule, random, monitor);
    }
}

/// <summary>Places birds 2-5 tiles beside large bushes on the side facing the nearest arrival, with a free straight path, anchored by the bush's tile.</summary>
public class BushSpawner : TerrainFeatureSpawner
{
    public BushSpawner() : base(HasStraightPathToBush) { }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        IEnumerable<Vector2> bushes = location.largeTerrainFeatures.OfType<Bush>().Where(IsLargeBush).Select(bush => bush.Tile).ToList();
        GameLocationQuery query = new(location);
        List<Vector2> arrivals = query.ArrivalTiles().ToList();
        int alertRadius = (rule.Composition?.Reactions ?? BirdTypeProfiles.ByBirdType["BushBird"].Reactions)?.Threat.Alert?.Radius ?? 0;
        return Placements(TileSelection.Features(bushes, rule.GroupCount, random, (bush, r) => PlaceNearBush(location, query, bush, arrivals, alertRadius, r), avoid,
            rule.Params.SpeciesDistance ?? TileSelection.MinSpeciesDistance), location, data, rule, random, monitor);
    }

    private static Vector2? PlaceNearBush(GameLocation location, ILocationQuery query, Vector2 bush, IReadOnlyCollection<Vector2> arrivals, int alertRadius, Random random)
    {
        int distance = random.Next(BushPlacement.MinDistance, BushPlacement.MaxDistance + 1);
        return BushPlacement.Place(bush, distance, arrivals, alertRadius, random) is { } position && query.IsReachable(position) && HasStraightPathToBush(location, bush, position) ? position : null;
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
