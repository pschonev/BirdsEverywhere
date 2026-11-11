using Microsoft.Xna.Framework;

namespace Birding.Spawners;

/// <summary>Picks the spot beside a bush: on the side facing the nearest arrival tile, never within the alert radius of one.</summary>
public static class BushPlacement
{
    public const int MinDistance = 2;
    public const int MaxDistance = 5;

    /// <summary>The tile <paramref name="distance"/> tiles beside <paramref name="bush"/>, or null when an arrival tile is within <paramref name="alertRadius"/>.</summary>
    public static Vector2? Place(Vector2 bush, int distance, IReadOnlyCollection<Vector2> arrivals, int alertRadius, Random random)
    {
        int side = SideFacing(bush, arrivals, random);
        Vector2 position = new(bush.X + (side * distance), bush.Y);
        foreach (Vector2 arrival in arrivals)
        {
            if (Vector2.Distance(arrival, position) <= alertRadius)
            {
                return null;
            }
        }
        return position;
    }

    private static int SideFacing(Vector2 bush, IReadOnlyCollection<Vector2> arrivals, Random random)
    {
        if (arrivals.Count == 0)
        {
            return random.NextDouble() < 0.5 ? 1 : -1;
        }
        Vector2 nearest = arrivals.MinBy(arrival => Vector2.DistanceSquared(arrival, bush));
        int side = Math.Sign(nearest.X - bush.X);
        return side != 0 ? side : random.NextDouble() < 0.5 ? 1 : -1;
    }
}
