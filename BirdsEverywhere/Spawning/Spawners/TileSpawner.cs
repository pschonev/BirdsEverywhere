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

    public List<SingleBirdSpawnParameters> spawnBirds(ILocationQuery location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor, int attemptsStartTile = 100, int attemptsPerBird = 20)
    {
        List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
        Point mapSize = location.MapSize;

        int groupCount = Utils.NextInclusive(random, rule.GroupCount.Min, rule.GroupCount.Max);

        for (int k = 0; k < groupCount; k++)
        {
            int groupSize = Utils.NextInclusive(random, rule.GroupSize.Min, rule.GroupSize.Max);
            monitor.Log($" Attempting to spawn one of {groupCount} group(s) with {groupSize} {data.Name}s.", LogLevel.Debug);
            int groupTarget = spawnList.Count + groupSize;

            for (int j = 0; j < attemptsStartTile; j++)
            {
                Vector2 initialTile = new Vector2(random.Next(3, mapSize.X - 4), random.Next(3, mapSize.Y - 4));
                if (!condition(location, initialTile))
                {
                    continue;
                }

                foreach (Vector2 tile in Utils.getRandomPositionsStartingFromThisTile(random, initialTile, mapSize, groupSize * attemptsPerBird, branchChance: 0.5))
                {
                    if (condition(location, tile))
                    {
                        spawnList.Add(new SingleBirdSpawnParamsTile(tile, data.Id.Value, rule.BirdType.Value, rule.SpawnPattern.Value, rule.When.Times.ToList(), rule.PickTexture(data.Id, random)));
                        monitor.Log($"Added {data.Id.Value} at {(int)tile.X} - {(int)tile.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);
                    }
                    if (spawnList.Count >= groupTarget)
                    {
                        break;
                    }
                }
                break;
            }
        }

        return spawnList;
    }
}

public abstract class Spawner
{
    public abstract List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdDefinition data, SpawnRule rule, Random random, IMonitor monitor);
}

