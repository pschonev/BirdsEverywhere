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
        public delegate bool TileSpawnCondition(ILocationQuery location, Vector2 tile);

        public static bool isValidGroundTile(ILocationQuery location, Vector2 tile) => location.IsGroundTile(tile);

        public static bool isValidWaterTile(ILocationQuery location, Vector2 tile) => location.IsWaterTile(tile);

        public static bool isValidWaterOrGroundTile(ILocationQuery location, Vector2 tile) =>
            location.IsGroundTile(tile) || location.IsWaterTile(tile);

        public static bool isSpawnableGroundTile(ILocationQuery location, Vector2 tile) => location.IsSpawnableGroundTile(tile);

        public delegate bool TerrainSpawnCondition(GameLocation location, int index, Vector2 position);

        public static bool isEligibleTree(GameLocation location, int index, Vector2 position)
        {
            try
            {
                TerrainFeature treeCandidate = location.terrainFeatures.Pairs.ElementAt(index).Value;

                return treeCandidate is Tree &&
                (treeCandidate as Tree).treeType.Value != Tree.leafyTree &&
                (int)(treeCandidate as Tree).growthStage.Value >= 5;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public static bool straightPathToBush(GameLocation location, int index, Vector2 position)
        {
            LargeTerrainFeature bush = location.largeTerrainFeatures[index];
            if (!(bush is Bush))
                return false;

            // if bush is to the right, flip is active and bird looks to the right
            bool flip = bush.Tile.X < position.X;
            int distance = (int)Math.Abs(bush.Tile.X - position.X);

            for (int j = 0; j < distance; j++)
            {
                position.X += (flip ? (-1) : 1);
                if ((!location.largeTerrainFeatures[index].getBoundingBox().Intersects(new Rectangle((int)position.X * 64, (int)position.Y * 64, 64, 64)) &&
                    !location.CanItemBePlacedHere(position)) ||
                    location.doesEitherTileOrTileIndexPropertyEqual((int)position.X, (int)position.Y, "Spawnable", "Back", "F")
                    )
                {
                    return false;
                }
            }
            return true;
        }
    }
}
