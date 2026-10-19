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

    public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor) =>
        spawnBirds(new GameLocationQuery(location), data, rule, random, monitor);

    public List<SingleBirdSpawnParameters> spawnBirds(ILocationQuery location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor)
    {
        List<SingleBirdSpawnParameters> spawnList = TileSelection.Tiles(location, condition, rule.GroupCount, rule.GroupSize, random)
            .Select(tile => (SingleBirdSpawnParameters)new SingleBirdSpawnParamsTile(tile, data.Id.Value, rule.BirdType.Value, rule.SpawnPattern.Value, rule.When.Times.ToList(), rule.PickTexture(data.Id, random)))
            .ToList();
        monitor.Log($"Planned {spawnList.Count} {data.Id.Value} at {location.Name}.", LogLevel.Debug);
        return spawnList;
    }
}

public abstract class Spawner
{
    public abstract List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor);
}

