using Microsoft.Xna.Framework;
using Birding.Domain;
using Birding.Spawners;

namespace Birding.BirdTypes;

/// <summary>Pure escape decisions: where a threatened bird relocates to, or null to flee off screen.</summary>
internal static class EscapePlanner
{
    private const float MinDistanceTiles = 3;

    /// <summary>A ground or water tile from the reaction's escape options, or null to flee off screen.</summary>
    public static Vector2? Surface(ILocationQuery query, Vector2 from, Vector2 threat, ThreatReaction reaction, int relocations,
        IReadOnlyCollection<Vector2> occupied, Random random) =>
        Choose(reaction, relocations, option => option.Target switch
        {
            "ground" => Tiles(query, from, option.MaxDistance, query.IsGroundTile),
            "water" => Tiles(query, from, option.MaxDistance, query.IsWaterTile),
            _ => Array.Empty<Vector2>(),
        }, from, threat, occupied, random);

    /// <summary>One of <paramref name="trees"/> for an <c>otherTree</c> escape option, or null to flee off screen.</summary>
    public static Vector2? Tree(IEnumerable<Vector2> trees, Vector2 from, Vector2 threat, ThreatReaction reaction, int relocations,
        IReadOnlyCollection<Vector2> occupied, Random random) =>
        Choose(reaction, relocations, option => option.Target == "otherTree" ? trees : Array.Empty<Vector2>(), from, threat, occupied, random);

    /// <summary>Picks an option by weight among those with an eligible spot (off screen always qualifies), then a spot; null means flee. The weight roll is skipped when only one option qualifies.</summary>
    public static Vector2? Choose(ThreatReaction reaction, int relocations, Func<EscapeOption, IEnumerable<Vector2>> candidates,
        Vector2 from, Vector2 threat, IReadOnlyCollection<Vector2> occupied, Random random)
    {
        if (relocations >= reaction.MaxRelocations || reaction.Escape.IsEmpty)
        {
            return null;
        }
        List<(EscapeOption Option, List<Vector2> Spots)> open = new();
        foreach (EscapeOption option in reaction.Escape)
        {
            List<Vector2> spots = option.Target == EscapeOption.Offscreen
                ? new List<Vector2>()
                : Eligible(candidates(option), from, threat, reaction.FleeRadius, option.MaxDistance, occupied);
            if (option.Target == EscapeOption.Offscreen || spots.Count > 0)
            {
                open.Add((option, spots));
            }
        }
        if (open.Count == 0)
        {
            return null;
        }
        (EscapeOption chosen, List<Vector2> chosenSpots) = open.Count == 1 ? open[0] : Weighted(open, random);
        return chosen.Target == EscapeOption.Offscreen ? null : chosenSpots[random.Next(chosenSpots.Count)];
    }

    /// <summary>A random free candidate tile within reach of <paramref name="from"/> and outside the threat's flee radius; candidates are sorted first so the pick is seed-stable.</summary>
    public static Vector2? Pick(IEnumerable<Vector2> candidates, Vector2 from, Vector2 threat, int fleeRadius, int maxDistance,
        IReadOnlyCollection<Vector2> occupied, Random random)
    {
        List<Vector2> eligible = Eligible(candidates, from, threat, fleeRadius, maxDistance, occupied);
        return eligible.Count == 0 ? null : eligible[random.Next(eligible.Count)];
    }

    /// <summary>Height of the flight arc above the straight line, peaking halfway.</summary>
    public static float ArcOffset(float progress, float height) => -MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI) * height;

    private static List<Vector2> Eligible(IEnumerable<Vector2> candidates, Vector2 from, Vector2 threat, int fleeRadius, int maxDistance,
        IReadOnlyCollection<Vector2> occupied) =>
        candidates
            .Where(tile => tile != from && !occupied.Contains(tile))
            .Where(tile => Vector2.Distance(tile, from) is var reach && reach >= MinDistanceTiles && reach <= maxDistance)
            .Where(tile => Vector2.Distance(tile, threat) > fleeRadius + 1)
            .OrderBy(tile => tile.X).ThenBy(tile => tile.Y)
            .ToList();

    private static List<Vector2> Tiles(ILocationQuery query, Vector2 from, int reach, Func<Vector2, bool> matches)
    {
        List<Vector2> tiles = new();
        for (int x = Math.Max(0, (int)from.X - reach); x <= Math.Min(query.MapSize.X - 1, (int)from.X + reach); x++)
        {
            for (int y = Math.Max(0, (int)from.Y - reach); y <= Math.Min(query.MapSize.Y - 1, (int)from.Y + reach); y++)
            {
                Vector2 tile = new(x, y);
                if (matches(tile))
                {
                    tiles.Add(tile);
                }
            }
        }
        return tiles;
    }

    private static (EscapeOption, List<Vector2>) Weighted(List<(EscapeOption Option, List<Vector2> Spots)> open, Random random)
    {
        double roll = random.NextDouble() * open.Sum(entry => entry.Option.Weight);
        foreach ((EscapeOption option, List<Vector2> spots) in open)
        {
            roll -= option.Weight;
            if (roll < 0)
            {
                return (option, spots);
            }
        }
        return open[^1];
    }
}
