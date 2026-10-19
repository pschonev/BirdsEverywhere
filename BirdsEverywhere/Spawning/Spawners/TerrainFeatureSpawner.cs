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

    public TreeTrunkSpawner()
    {
        condition = (location, index, position) => isEligibleTree(location, index, position);
    }

    public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, Random random, IMonitor monitor)
    {
        List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
        int featureCount = location.terrainFeatures.Count();
        List<int> shuffledIndices = Shuffle(random, featureCount);

        int groupCount = Utils.NextInclusive(random, Math.Max(1, data.currentSpawnData.minGroupCount), data.currentSpawnData.maxGroupCount);
        monitor.Log($" Attempting to spawn {groupCount} {data.name}s.", LogLevel.Debug);

        int attempts = Math.Min(MaxAttempts, featureCount);
        for (int i = 0; i < attempts; i++)
        {
            int index = shuffledIndices[i];

            if (isEligibleTree(location, index, new Vector2()))
            {
                Vector2 position = location.terrainFeatures.Pairs.ElementAt(index).Value.Tile;
                spawnList.Add(new SingleBirdSpawnParamsTerrainFeature(index, position, data.id, data.currentSpawnData.birdType, data.currentSpawnData.spawnPattern, data.currentSpawnData.timeOfDayRanges, data.currentSpawnData.getNextTexture(data.id)));
                monitor.Log($"Added {data.id} to tree at {(int)position.X} - {(int)position.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);

                groupCount--;
                if (groupCount <= 0)
                {
                    break;
                }
            }
        }
        return spawnList;
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

    public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, Random random, IMonitor monitor)
    {
        List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
        int featureCount = location.largeTerrainFeatures.Count;
        List<int> shuffledIndices = Shuffle(random, featureCount);

        int groupCount = Utils.NextInclusive(random, Math.Max(1, data.currentSpawnData.minGroupCount), data.currentSpawnData.maxGroupCount);
        monitor.Log($" Attempting to spawn {groupCount} {data.name}s.", LogLevel.Debug);

        int attempts = Math.Min(MaxAttempts, featureCount);
        for (int i = 0; i < attempts; i++)
        {
            int index = shuffledIndices[i];

            if (location.largeTerrainFeatures[index] is Bush)
            {
                Vector2 bushPosition = location.largeTerrainFeatures[index].Tile;
                int distance = random.Next(5, 12);
                bool flip = random.NextDouble() < 0.5;

                Vector2 position = new Vector2(bushPosition.X + (flip ? distance : (-distance)), bushPosition.Y);

                if (straightPathToBush(location, index, position))
                {
                    spawnList.Add(new SingleBirdSpawnParamsTerrainFeature(index, position, data.id, data.currentSpawnData.birdType, data.currentSpawnData.spawnPattern, data.currentSpawnData.timeOfDayRanges, data.currentSpawnData.getNextTexture(data.id)));
                    monitor.Log($"Added {data.id} to bush at {(int)position.X} - {(int)position.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);

                    groupCount--;
                    if (groupCount <= 0)
                    {
                        break;
                    }
                }
            }
        }
        return spawnList;
    }
}

public abstract class TerrainFeatureSpawner : Spawner
{
    protected const int MaxAttempts = 100;

    public TerrainSpawnCondition condition = null!;

    protected static List<int> Shuffle(Random random, int count)
    {
        List<int> indices = Enumerable.Range(0, count).ToList();
        for (int i = count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}

