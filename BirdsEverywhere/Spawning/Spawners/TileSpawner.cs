using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners
{   /// <summary>
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

        public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, Random random, IMonitor monitor) =>
            spawnBirds(new GameLocationQuery(location), data, random, monitor);

        public List<SingleBirdSpawnParameters> spawnBirds(ILocationQuery location, BirdData data, Random random, IMonitor monitor, int attemptsStartTile = 100, int attemptsPerBird = 20)
        {
            List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
            SpawnData spawnData = data.currentSpawnData;
            Point mapSize = location.MapSize;

            int groupCount = Utils.NextInclusive(random, Math.Max(1, spawnData.minGroupCount), spawnData.maxGroupCount);

            for (int k = 0; k < groupCount; k++)
            {
                int groupSize = Utils.NextInclusive(random, Math.Max(1, spawnData.minGroupSize), spawnData.maxGroupSize);
                monitor.Log($" Attempting to spawn one of {groupCount} group(s) with {groupSize} {data.name}s.", LogLevel.Debug);
                int groupTarget = spawnList.Count + groupSize;

                for (int j = 0; j < attemptsStartTile; j++)
                {
                    Vector2 initialTile = new Vector2(random.Next(3, mapSize.X - 4), random.Next(3, mapSize.Y - 4));
                    if (!condition(location, initialTile))
                        continue;

                    foreach (Vector2 tile in Utils.getRandomPositionsStartingFromThisTile(random, initialTile, mapSize, groupSize * attemptsPerBird, branchChance: 0.5))
                    {
                        if (condition(location, tile))
                        {
                            spawnList.Add(new SingleBirdSpawnParamsTile(tile, data.id, spawnData.birdType, spawnData.spawnPattern, spawnData.timeOfDayRanges, spawnData.getNextTexture(data.id)));
                            monitor.Log($"Added {data.id} at {(int)tile.X} - {(int)tile.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);
                        }
                        if (spawnList.Count >= groupTarget)
                            break;
                    }
                    break;
                }
            }

            return spawnList;
        }
    }

    public abstract class Spawner
    {
        public abstract List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, Random random, IMonitor monitor);
    }
}

