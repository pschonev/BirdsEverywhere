using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using BirdsEverywhere.BirdTypes;
using static BirdsEverywhere.Spawners.Condition;
using static BirdsEverywhere.BirdTypes.CustomBirdType;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere.Spawners
{   /// <summary>
    /// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
    /// </summary>
    ///

    public class TreeTrunkSpawner : TerrainFeatureSpawner
    {
        /// <summary>
        /// Checks for valid tree.
        /// </summary>
        public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, int attempts = 100)
        {
            List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
            var shuffledIndices = Enumerable.Range(0, (location.terrainFeatures.Count())).OrderBy(a => Game1.random.NextDouble()).ToList();

            int groupCount = Game1.random.Next(data.spawnData.minGroupCount, data.spawnData.maxGroupCount);
            ModEntry.modInstance.Monitor.Log($" Attempting to spawn {groupCount} {data.name}s.", LogLevel.Debug);

            attempts = Math.Min(attempts, location.terrainFeatures.Count());
            for (int i = 0; i < attempts; i++)
            {
                int index = shuffledIndices[i];

                if (isEligibleTree(location, index, new Vector2()))
                {
                    Vector2 position = (location.terrainFeatures.Pairs.ElementAt(index).Value as Tree).currentTileLocation;
                    spawnList.Add(new SingleBirdSpawnParamsTerrainFeature(condition, index, position, data.id, data.spawnData.birdType));
                    ModEntry.modInstance.Monitor.Log($"Added {data.id} to tree at {(int)position.X} - {(int)position.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);

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
        /// Checks for valid tree.
        /// </summary>
        public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, int attempts = 100)
        {
            List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();
            var shuffledIndices = Enumerable.Range(0, (location.largeTerrainFeatures.Count())).OrderBy(a => Game1.random.NextDouble()).ToList();

            int groupCount = Game1.random.Next(data.spawnData.minGroupCount, data.spawnData.maxGroupCount);
            ModEntry.modInstance.Monitor.Log($" Attempting to spawn {groupCount} {data.name}s.", LogLevel.Debug);

            attempts = Math.Min(attempts, location.largeTerrainFeatures.Count());
            for (int i = 0; i < attempts; i++)
            {
                int index = shuffledIndices[i];

                if (location.largeTerrainFeatures[index] is Bush)
                {
                    Vector2 bushPosition = location.largeTerrainFeatures[index].tilePosition;
                    int distance = Game1.random.Next(5, 12);
                    bool flip = (Game1.random.NextDouble() < 0.5);

                    Vector2 position = new Vector2(bushPosition.X + (flip ? 1 : (-1)), bushPosition.Y);

                    if (straightPathToBush(location, index, position))
                    {
                        spawnList.Add(new SingleBirdSpawnParamsTerrainFeature(condition, index, position, data.id, data.spawnData.birdType));
                        ModEntry.modInstance.Monitor.Log($"Added {data.id} to tree at {(int)position.X} - {(int)position.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);

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
        protected TerrainSpawnCondition condition;
    }
}

