using Microsoft.Xna.Framework;
using Birding.Domain;
using Birding.Spawners;

namespace Birding.BirdTypes;

internal static class Relocation
{
    private const float MinDistanceTiles = 3;

    public static Vector2? PickSurface(ILocationQuery query, Vector2 from, Vector2 threat, ThreatReaction reaction,
        IReadOnlyCollection<Vector2> occupied, Random random)
    {
        if (reaction.Relocate is not { } relocate || random.NextDouble() >= relocate.Chance)
        {
            return null;
        }
        List<Vector2> candidates = new();
        int reach = relocate.MaxDistance;
        for (int x = Math.Max(0, (int)from.X - reach); x <= Math.Min(query.MapSize.X - 1, (int)from.X + reach); x++)
        {
            for (int y = Math.Max(0, (int)from.Y - reach); y <= Math.Min(query.MapSize.Y - 1, (int)from.Y + reach); y++)
            {
                Vector2 tile = new(x, y);
                if (relocate.Target is "water" or "any" && query.IsWaterTile(tile)
                    || relocate.Target is "ground" or "any" && query.IsGroundTile(tile))
                {
                    candidates.Add(tile);
                }
            }
        }
        return Pick(candidates, from, threat, reaction.FleeRadius, reach, occupied, random);
    }

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
