using Microsoft.Xna.Framework;

namespace Birding.Spawners;

/// <summary>Whether a bird on a trunk can be seen: a crown of another tree standing in front (south) of the trunk covers it.</summary>
public static class TrunkVisibility
{
    /// <summary>Tiles either side of a trunk that another tree's crown reaches.</summary>
    public const int CrownHalfWidthTiles = 1;

    /// <summary>Tiles south of the trunk within which a tree's base puts its crown over it: crowns reach about five tiles above their base.</summary>
    public const int CrownReachTiles = 5;

    public static bool IsCovered(Vector2 trunk, IEnumerable<Vector2> otherCrownBases) =>
        otherCrownBases.Any(other => other != trunk
            && MathF.Abs(other.X - trunk.X) <= CrownHalfWidthTiles
            && other.Y > trunk.Y && other.Y <= trunk.Y + CrownReachTiles);
}
