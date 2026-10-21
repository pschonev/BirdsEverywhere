using Microsoft.Xna.Framework;

namespace BirdsEverywhere.BirdTypes;

internal static class Relocation
{
    private const float MinDistanceTiles = 3;

    /// <summary>A random free candidate tile within reach of <paramref name="from"/> and outside the threat's flee radius; candidates are sorted first so the pick is seed-stable.</summary>
    public static Vector2? Pick(IEnumerable<Vector2> candidates, Vector2 from, Vector2 threat, int fleeRadius, int maxDistance,
        IReadOnlyCollection<Vector2> occupied, Random random)
    {
        List<Vector2> eligible = candidates
            .Where(tile => tile != from && !occupied.Contains(tile))
            .Where(tile => Vector2.Distance(tile, from) is var reach && reach >= MinDistanceTiles && reach <= maxDistance)
            .Where(tile => Vector2.Distance(tile, threat) > fleeRadius + 1)
            .OrderBy(tile => tile.X).ThenBy(tile => tile.Y)
            .ToList();
        return eligible.Count == 0 ? null : eligible[random.Next(eligible.Count)];
    }

    /// <summary>Height of the flight arc above the straight line, peaking halfway.</summary>
    public static float ArcOffset(float progress, float height) => -MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI) * height;
}
