using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.Spawners;

public static class Condition
{
    public delegate bool TileSpawnCondition(ILocationQuery location, Vector2 tile);

    public static bool IsValidGroundTile(ILocationQuery location, Vector2 tile) => location.IsGroundTile(tile);

    public static bool IsValidWaterTile(ILocationQuery location, Vector2 tile) => location.IsWaterTile(tile);

    public static bool IsValidWaterOrGroundTile(ILocationQuery location, Vector2 tile) =>
        location.IsGroundTile(tile) || location.IsWaterTile(tile);

    public static bool IsValidSpawnableGroundTile(ILocationQuery location, Vector2 tile) => location.IsSpawnableGroundTile(tile);

    public delegate bool TerrainSpawnCondition(GameLocation location, int index, Vector2 position);

    public static bool IsEligibleTree(GameLocation location, int index)
    {
        try
        {
            return location.terrainFeatures.Pairs.ElementAt(index).Value is Tree tree
                && tree.treeType.Value != Tree.leafyTree
                && tree.growthStage.Value >= 5;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool HasStraightPathToBush(GameLocation location, int index, Vector2 position)
    {
        LargeTerrainFeature bush = location.largeTerrainFeatures[index];
        if (!(bush is Bush))
        {
            return false;
        }

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
