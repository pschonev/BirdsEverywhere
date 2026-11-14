using Microsoft.Xna.Framework;

namespace Birding.Spawners;

/// <summary>Terrain rules a bird can stand or swim on, shared by escape planning.</summary>
public static class TileRules
{
    public const int OpenWaterShoreDistance = 2;

    /// <summary>Ground a farmer could also walk on and see a bird on: not behind a bush or under a tree crown. Piers, docks and bridges are ground.</summary>
    public static bool CanLand(ILocationQuery location, Vector2 tile) =>
        location.IsGroundTile(tile) && location.IsPassable(tile) && location.IsReachable(tile) && !location.IsHiddenByScenery(tile);

    /// <summary>A feature tile (tree, bush) a farmer can stand next to: the tile itself or one of its eight neighbours is reachable on foot.</summary>
    public static bool IsReachableNear(ILocationQuery location, Vector2 tile)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (location.IsReachable(tile + new Vector2(dx, dy)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Water at least <see cref="OpenWaterShoreDistance"/> tiles from any ground.</summary>
    public static bool IsOpenWater(ILocationQuery location, Vector2 tile) =>
        location.IsWaterTile(tile) && location.IsReachable(tile) && ShoreDistance.From(location, tile, OpenWaterShoreDistance - 1) >= OpenWaterShoreDistance;
}
