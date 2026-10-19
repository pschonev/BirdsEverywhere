using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using static BirdsEverywhere.Spawners.Condition;
using BirdsEverywhere.Domain;

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

    public GroundSpawner()
    {
        condition = isValidGroundTile;
    }
}

public class WaterSpawner : TileSpawner
{
    /// <summary>
    /// Checks for valid water tile.
    /// </summary>
    public WaterSpawner()
    {
        condition = isValidWaterTile;
    }
}

public class WaterOrGroundSpawner : TileSpawner
{
    /// <summary>
    /// Checks if tile is either water or ground.
    /// </summary>
    public WaterOrGroundSpawner()
    {
        condition = isValidWaterOrGroundTile;
    }
}

public class SpawnableGroundSpawner : TileSpawner
{
    /// <summary>
    /// Checks whether the tile is on spawnable ground i.e. if the player can walk up to the birds.
    /// </summary>
    public SpawnableGroundSpawner()
    {
        condition = isSpawnableGroundTile;
    }
}

public abstract class TileSpawner : Spawner
{
    public TileSpawnCondition condition = null!;

    public override List<BirdPlacement> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor) =>
        spawnBirds(new GameLocationQuery(location), data, rule, fishArea, random, monitor);

    public List<BirdPlacement> spawnBirds(ILocationQuery location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor)
    {
        List<BirdPlacement> spawnList = TileSelection.Tiles(location, Restrict(rule, fishArea), rule.GroupCount, rule.GroupSize, random)
            .Select(tile => Place(data, rule, tile, null, random))
            .ToList();
        monitor.Log($"Planned {spawnList.Count} {data.Id.Value} at {location.Name}.", LogLevel.Debug);
        return spawnList;
    }

    public override bool IsStillValid(GameLocation location, BirdPlacement placement) =>
        condition(new GameLocationQuery(location), placement.Tile);

    private TileSpawnCondition Restrict(SpawnRule rule, string? fishArea)
    {
        IntRange? shore = rule.Params.ShoreDistance;
        if (shore is null && fishArea is null)
        {
            return condition;
        }

        return (location, tile) => condition(location, tile)
            && (fishArea is null || location.FishArea(tile) == fishArea)
            && (shore is not IntRange range || range.Contains(ShoreDistance.From(location, tile)));
    }
}

public abstract class Spawner
{
    public abstract List<BirdPlacement> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, string? fishArea, Random random, IMonitor monitor);

    public abstract bool IsStillValid(GameLocation location, BirdPlacement placement);

    protected static BirdPlacement Place(BirdDefinition data, SpawnRule rule, Vector2 tile, int? featureIndex, Random random) =>
        new(data.Id.Value, rule.Id, rule.BirdType.Value, rule.SpawnPattern.Value, tile, featureIndex, rule.When.Times, rule.PickTexture(data.Id, random), random.Next());
}

