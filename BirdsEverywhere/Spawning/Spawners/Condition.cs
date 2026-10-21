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

    /// <summary>Checks a terrain feature anchor (by its tile) and the bird's spawn position.</summary>
    public delegate bool TerrainSpawnCondition(GameLocation location, Vector2 feature, Vector2 position);

    public static bool IsEligibleTree(TerrainFeature feature) =>
        feature is Tree tree && tree.treeType.Value != Tree.leafyTree && tree.growthStage.Value >= 5;

    public static bool IsEligibleTreeAt(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out TerrainFeature? feature) && IsEligibleTree(feature);

    public static Bush? BushAt(GameLocation location, Vector2 tile) =>
        location.largeTerrainFeatures.OfType<Bush>().FirstOrDefault(bush => bush.Tile == tile);

    public static bool HasStraightPathToBush(GameLocation location, Vector2 bushTile, Vector2 position)
    {
        if (BushAt(location, bushTile) is not { } bush)
        {
            return false;
        }

        // if bush is to the right, flip is active and bird looks to the right
        bool flip = bush.Tile.X < position.X;
        int distance = (int)Math.Abs(bush.Tile.X - position.X);

        for (int j = 0; j < distance; j++)
        {
            position.X += (flip ? (-1) : 1);
            if ((!bush.getBoundingBox().Intersects(new Rectangle((int)position.X * 64, (int)position.Y * 64, 64, 64)) &&
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
