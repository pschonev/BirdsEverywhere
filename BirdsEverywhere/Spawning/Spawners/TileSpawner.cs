using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners;
/// <summary>
/// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
/// </summary>
/// 
public class GroundSpawner : TileSpawner
{
    /// <summary>
    /// Checks for valid ground tile (doesn't have to be accessible to player).
    /// </summary>

    public GroundSpawner() : base(IsValidGroundTile) { }
}

public class WaterSpawner : TileSpawner
{
    /// <summary>
    /// Checks for valid water tile.
    /// </summary>
    public WaterSpawner() : base(IsValidWaterTile) { }
}

public class WaterOrGroundSpawner : TileSpawner
{
    /// <summary>
    /// Checks if tile is either water or ground.
    /// </summary>
    public WaterOrGroundSpawner() : base(IsValidWaterOrGroundTile) { }
}

public class SpawnableGroundSpawner : TileSpawner
{
    /// <summary>
    /// Checks whether the tile is on spawnable ground i.e. if the player can walk up to the birds.
    /// </summary>
    public SpawnableGroundSpawner() : base(IsValidSpawnableGroundTile) { }
}

public abstract class TileSpawner : Spawner
{
    protected TileSpawner(TileSpawnCondition tileCondition)
    {
        TileCondition = tileCondition;
    }

    public TileSpawnCondition TileCondition { get; }

    public override List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null) =>
        SpawnBirds(new GameLocationQuery(location), data, rule, fishArea, random, monitor, avoid);

    public List<BirdPlacement> SpawnBirds(ILocationQuery location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null)
    {
        List<BirdPlacement> spawnList = TileSelection.Tiles(location, Restrict(rule, fishArea), rule.GroupCount, rule.GroupSize, random, avoid: avoid,
                layout: rule.Params.Group, speciesDistance: rule.Params.SpeciesDistance ?? TileSelection.MinSpeciesDistance)
            .Select(tile => Place(data, rule, tile, null, random))
            .ToList();
        monitor.Log($"Planned {spawnList.Count} {data.Id.Value} at {location.Name}.", LogLevel.Debug);
        return spawnList;
    }

    public override bool IsStillValid(GameLocation location, BirdPlacement placement) =>
        TileCondition(new GameLocationQuery(location), placement.Tile);

    public TileSpawnCondition Restrict(SpawnRule rule, string? fishArea)
    {
        IntRange? shore = rule.Params.ShoreDistance;
        if (shore is null && fishArea is null)
        {
            return TileCondition;
        }

        return (location, tile) => TileCondition(location, tile)
            && (fishArea is null || location.FishArea(tile) == fishArea)
            && (shore is not IntRange range || range.Contains(ShoreDistance.From(location, tile)));
    }
}

public abstract class Spawner
{
    public abstract List<BirdPlacement> SpawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor, IReadOnlyCollection<Vector2>? avoid = null);

    public abstract bool IsStillValid(GameLocation location, BirdPlacement placement);

    protected static BirdPlacement Place(BirdDefinition data, SpawnRule rule, Vector2 tile, Vector2? featureTile, Random random) =>
        new(data.Id.Value, rule.Id, rule.BirdType.Value, rule.SpawnPattern.Value, tile, featureTile, rule.When.Times, rule.PickTexture(data.Id, random), random.Next());
}

