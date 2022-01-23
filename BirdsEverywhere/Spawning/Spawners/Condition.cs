using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;

namespace BirdsEverywhere.Spawners
{
    public class Condition
    {
        public delegate bool SpawnCondition(GameLocation location, Vector2 tile, int xCoord2, int yCoord2);

        public static bool isValidGroundTile(GameLocation location, Vector2 tile, int xCoord2, int yCoord2)
        {
            return (location.isTileOnMap(tile) &&
                location.isTileLocationTotallyClearAndPlaceable(tile) &&
                location.doesTileHaveProperty(xCoord2, yCoord2, "Water", "Back") == null);
        }

        public static bool isValidWaterTile(GameLocation location, Vector2 tile, int xCoord2, int yCoord2)
        {
            return (location.isTileOnMap(tile) &&
                    location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null);
        }

        public static bool isValidWaterOrGroundTile(GameLocation location, Vector2 tile, int xCoord2, int yCoord2)
        {
            return (location.isTileOnMap(tile) &&
                    (location.isTileLocationTotallyClearAndPlaceable(tile) || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null));
        }

        public static bool isSpawnableGroundTile(GameLocation location, Vector2 tile, int xCoord2, int yCoord2)
        {
            return (location.isTileOnMap(tile) &&
                            location.doesTileHaveProperty(xCoord2, yCoord2, "Water", "Back") == null &&
                            location.doesTileHaveProperty(xCoord2, yCoord2, "Spawnable", "Back") != null &&
                            !location.doesEitherTileOrTileIndexPropertyEqual(xCoord2, yCoord2, "Spawnable", "Back", "F") &&
                            location.isTileLocationTotallyClearAndPlaceable(xCoord2, yCoord2) &&
                            location.getTileIndexAt(xCoord2, yCoord2, "AlwaysFront") == -1 &&
                            location.getTileIndexAt(xCoord2, yCoord2, "Front") == -1 &&
                            !location.isBehindBush(tile) &&
                            (Game1.random.NextDouble() < 0.1 || !location.isBehindTree(tile)));
        }

        public static bool isEligibleTree(GameLocation location, int index)
        {
            try
            {
                TerrainFeature treeCandidate = location.terrainFeatures.Pairs.ElementAt(index).Value;

                return treeCandidate is Tree &&
                (int)(treeCandidate as Tree).treeType != 2 &&
                (int)(treeCandidate as Tree).growthStage >= 5;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return false;
            }
        }
    }
}
