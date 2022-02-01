﻿using System;
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
    public class GroundSpawner : TileSpawner
    {
        /// <summary>
        /// Checks for valid ground tile (doesn't have to be accessible to player).
        /// </summary>

        public GroundSpawner()
        {
            condition = (location, tile, xCoord2, yCoord2) => isValidGroundTile(location, tile, xCoord2, yCoord2);
        }
    }

    public class WaterSpawner : TileSpawner
    {
        /// <summary>
        /// Checks for valid water tile.
        /// </summary>
        protected new TileSpawnCondition condition = (location, tile, xCoord2, yCoord2) => isValidWaterTile(location, tile, xCoord2, yCoord2);
    }

    public class WaterOrGroundSpawner : TileSpawner
    { /// <summary>
      /// Checks if tile is either water or ground and handles the resulting state.
      /// </summary>
      /// 
        public WaterOrGroundSpawner()
        {
            condition = (location, tile, xCoord2, yCoord2) => isValidWaterOrGroundTile(location, tile, xCoord2, yCoord2);
        }

        protected override List<SingleBirdSpawnParameters> spawnSingleBird(GameLocation location, Vector2 tile, int xCoord2, int yCoord2, SpawnData data, string id, List<SingleBirdSpawnParameters> spawnList)
        {
            if (isValidWaterOrGroundTile(location, tile, xCoord2, yCoord2))
            {
                BehaviorStatus state = BehaviorStatus.Stopped;
                if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null)
                {
                    state = BehaviorStatus.Swimming;
                }
                spawnList.Add(new SingleBirdSpawnParamsTile(condition, tile, id, data.birdType));
            }

            return spawnList;
        }
    }

    public class SpawnableGroundSpawner : TileSpawner
    {
        /// <summary>
        /// Checks whether the tile is on spawnable ground i.e. if the player can walk up to the birds.
        /// </summary>
        public SpawnableGroundSpawner()
        {
            condition = (location, tile, xCoord2, yCoord2) => isSpawnableGroundTile(location, tile, xCoord2, yCoord2);
        }
    }

    public abstract class TileSpawner : Spawner
    {
        public TileSpawnCondition condition;

        public override List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, int attempts = 100)
        {
            List<SingleBirdSpawnParameters> spawnList = new List<SingleBirdSpawnParameters>();

            int groupCount = Game1.random.Next(data.spawnData.minGroupCount, data.spawnData.maxGroupCount);
            ModEntry.modInstance.Monitor.Log($" Attempting to spawn {groupCount} group(s) with max {data.spawnData.maxGroupSize} {data.name}s.", LogLevel.Debug);

            for (int k = 0; k < groupCount; k++)
            {
                int groupSize = Game1.random.Next(data.spawnData.minGroupSize, data.spawnData.maxGroupSize);

                for (int j = 0; j < attempts; j++)
                {
                    int xCoord2 = Game1.random.Next(location.map.DisplayWidth / 64);
                    int yCoord2 = Game1.random.Next(location.map.DisplayHeight / 64);
                    Vector2 initialTile = new Vector2(xCoord2, yCoord2);

                    // if tile meets condition this will spawn one bird there and up to a MAXIMUM of groupSize-1 additional birds
                    if (condition(location, initialTile, xCoord2, yCoord2))
                    {
                        spawnList = spawnSingleBird(location, initialTile, (int)initialTile.X, (int)initialTile.Y, data.spawnData, data.id, spawnList);
                        foreach (Vector2 tile in Utils.getRandomPositionsStartingFromThisTile(initialTile, groupSize - 1))
                        {
                            spawnList = spawnSingleBird(location, tile, (int)tile.X, (int)tile.Y, data.spawnData, data.id, spawnList);
                        }
                        break; // this will break the for loop and stop spawning any more birds in this group
                    }
                }
            }

            return spawnList;
        }

        protected virtual List<SingleBirdSpawnParameters> spawnSingleBird(GameLocation location, Vector2 tile, int xCoord2, int yCoord2, SpawnData data, string id, List<SingleBirdSpawnParameters> spawnList)
        {
            if (condition(location, tile, xCoord2, yCoord2))
            {
                spawnList.Add(new SingleBirdSpawnParamsTile(condition, tile, id, data.birdType));
                ModEntry.modInstance.Monitor.Log($"Added {id} at {(int)tile.X} - {(int)tile.Y} to LocationBirdPosition at location {location.Name}.", LogLevel.Debug);
            }
            return spawnList;
        }
    }

    public abstract class Spawner
    {
        public abstract List<SingleBirdSpawnParameters> spawnBirds(GameLocation location, BirdData data, int attempts = 100);
    }
}

