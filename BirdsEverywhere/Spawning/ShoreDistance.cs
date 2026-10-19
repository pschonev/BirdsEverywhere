using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;

namespace BirdsEverywhere.Spawners;

public static class ShoreDistance
{
    /// <summary>Tiles to the nearest ground (from a water tile) or nearest water (from any other tile); one past the maximum when none is in range.</summary>
    public static int From(ILocationQuery location, Vector2 tile)
    {
        Func<Vector2, bool> target = location.IsWaterTile(tile) ? location.IsGroundTile : location.IsWaterTile;
        for (int ring = 1; ring <= SpawnParams.MaxShoreDistance; ring++)
        {
            for (int dx = -ring; dx <= ring; dx++)
            {
                int step = Math.Abs(dx) == ring ? 1 : 2 * ring;
                for (int dy = -ring; dy <= ring; dy += step)
                {
                    if (target(new Vector2(tile.X + dx, tile.Y + dy)))
                    {
                        return ring;
                    }
                }
            }
        }

        return SpawnParams.MaxShoreDistance + 1;
    }
}
