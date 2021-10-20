using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using BirdsEverywhere.Birds;
using static BirdsEverywhere.Birds.CustomBirdType;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners
{   /// <summary>
    /// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
    /// </summary>
    /// 
    public class GroundSpawner : Spawner
    {
        /// <summary>
        /// Checks for valid ground tile (doesn't have to be accessible to player).
        /// </summary>

        public GroundSpawner()
        {
            condition = (location, tile, xCoord2, yCoord2) => isValidGroundTile(location, tile, xCoord2, yCoord2);
        }
    }

    public class WaterSpawner : Spawner
    {
        /// <summary>
        /// Checks for valid water tile.
        /// </summary>
        protected new SpawnCondition condition = (location, tile, xCoord2, yCoord2) => isValidGroundTile(location, tile, xCoord2, yCoord2);
    }

    public class WaterOrGroundSpawner : Spawner
    { /// <summary>
      /// Checks if tile is either water or ground and handles the resulting state.
      /// </summary>
        protected new SpawnCondition condition = (location, tile, xCoord2, yCoord2) => isValidWaterOrGroundTile(location, tile, xCoord2, yCoord2);

        protected override void spawnSingleBird(GameLocation location, Vector2 tile, int xCoord2, int yCoord2, SpawnData data, string id)
        {
            if (isValidWaterOrGroundTile(location, tile, xCoord2, yCoord2))
            {
                BehaviorStatus state = BehaviorStatus.Stopped;
                if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null)
                {
                    state = BehaviorStatus.Swimming;
                }
                location.critters.Add(BirdFactory.createBird(xCoord2, yCoord2, id, data.birdType).setState(state));
                ModEntry.modInstance.Monitor.Log($"Added {id} at {xCoord2} - {yCoord2}.", LogLevel.Debug);
            }
        }
    }

    public class SpawnableGroundSpawner : Spawner
    {
        /// <summary>
        /// Checks whether the tile is on spawnable ground i.e. if the player can walk up to the birds.
        /// </summary>
        protected new SpawnCondition condition = (location, tile, xCoord2, yCoord2) => isSpawnableGroundTile(location, tile, xCoord2, yCoord2);
    }

    public abstract class Spawner
    {
        protected SpawnCondition condition;

        public void spawnBirds(GameLocation location, BirdData data, int attempts = 100)
        {
            int groupCount = Game1.random.Next(1, data.spawnData.maxGroupCount);

            ModEntry.modInstance.Monitor.Log($" Attempting to spawn {groupCount} groups with max {data.spawnData.maxGroupSize} {data.name}s.", LogLevel.Debug);

            for (int k = 0; k < groupCount; k++)
            {
                int groupSize = Game1.random.Next(1, data.spawnData.maxGroupSize);

                for (int j = 0; j < attempts; j++)
                {
                    int xCoord2 = Game1.random.Next(location.map.DisplayWidth / 64);
                    int yCoord2 = Game1.random.Next(location.map.DisplayHeight / 64);
                    Vector2 initialTile = new Vector2(xCoord2, yCoord2);

                    if (condition(location, initialTile, xCoord2, yCoord2))
                    {
                        spawnSingleBird(location, initialTile, (int)initialTile.X, (int)initialTile.Y, data.spawnData, data.id);
                        foreach (Vector2 tile in Utils.getRandomPositionsStartingFromThisTile(initialTile, groupSize-1))
                        {
                            spawnSingleBird(location, tile, (int)tile.X, (int)tile.Y, data.spawnData, data.id);
                        }
                        break;
                    }
                }
            }
        }

        protected virtual void spawnSingleBird(GameLocation location, Vector2 tile, int xCoord2, int yCoord2, SpawnData data, string id)
        {
            if (condition(location, tile, xCoord2, yCoord2))
            {
                location.critters.Add(BirdFactory.createBird(xCoord2, yCoord2, id, data.birdType));
                ModEntry.modInstance.Monitor.Log($"Added {id} at {(int)tile.X} - {(int)tile.Y}.", LogLevel.Debug);
            }
        }
    }
}

