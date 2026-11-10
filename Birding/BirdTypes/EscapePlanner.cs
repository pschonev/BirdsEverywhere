using Microsoft.Xna.Framework;
using Birding.Domain;
using Birding.Spawners;

namespace Birding.BirdTypes;

/// <summary>The escape option picked for a threatened bird and its chosen tile.</summary>
internal readonly record struct EscapeChoice(EscapeOption Option, Vector2 Tile);

/// <summary>Pure escape decisions: where a threatened bird relocates to, or null to flee off screen.</summary>
internal static class EscapePlanner
{
    private const float MinDistanceTiles = 3;
    private const int LandingRadiusTiles = 6;
    private const float ConeCosine = 0.5f;

    /// <summary>A ground, water or canopy tile from the reaction's escape options, or null to flee off screen.</summary>
    public static EscapeChoice? Surface(ILocationQuery query, Vector2 from, Vector2 threat, ThreatReaction reaction, int relocations,
        IReadOnlyCollection<Vector2> occupied, Random random) =>
        Choose(reaction, relocations, option => option.Target switch
        {
            "ground" => Tiles(query, from, option.MaxDistance, tile => TileRules.CanLand(query, tile)),
            "water" => Tiles(query, from, option.MaxDistance, tile => TileRules.IsOpenWater(query, tile)),
            EscapeOption.Tree => query.MatureTrees(),
            _ => Array.Empty<Vector2>(),
        }, from, Threats(query, threat), occupied, random);

    /// <summary>A free landable tile near <paramref name="tree"/> and clear of every farmer, or null to flee off screen.</summary>
    public static Vector2? Landing(ILocationQuery query, Vector2 tree, int fleeRadius, Random random, IReadOnlyCollection<Vector2> occupied) =>
        Pick(Tiles(query, tree, LandingRadiusTiles, tile => TileRules.CanLand(query, tile)), tree, query.FarmerTiles, fleeRadius, LandingRadiusTiles, occupied, random);

    /// <summary>A mature tree inside the ±60° cone pointing away from the threat for an <c>otherTree</c> escape option, or null to flee off screen.</summary>
    public static EscapeChoice? Tree(ILocationQuery query, Vector2 from, Vector2 threat, ThreatReaction reaction, int relocations,
        IReadOnlyCollection<Vector2> occupied, Random random) =>
        Choose(reaction, relocations, option => option.Target == "otherTree" ? query.MatureTrees().Where(tree => InCone(from, threat, tree)) : Array.Empty<Vector2>(),
            from, Threats(query, threat), occupied, random);

    /// <summary>Picks an option by weight among those with an eligible spot (off screen always qualifies), then a spot; null means flee. The weight roll is skipped when only one option qualifies.</summary>
    public static EscapeChoice? Choose(ThreatReaction reaction, int relocations, Func<EscapeOption, IEnumerable<Vector2>> candidates,
        Vector2 from, IReadOnlyList<Vector2> threats, IReadOnlyCollection<Vector2> occupied, Random random)
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
                : Eligible(candidates(option), from, threats, reaction.FleeRadius, option.MaxDistance, occupied);
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
        return chosen.Target == EscapeOption.Offscreen ? null : new EscapeChoice(chosen, chosenSpots[random.Next(chosenSpots.Count)]);
    }

    /// <summary>A random free candidate tile within reach of <paramref name="from"/> and outside every threat's flee radius; candidates are sorted first so the pick is seed-stable.</summary>
    public static Vector2? Pick(IEnumerable<Vector2> candidates, Vector2 from, IReadOnlyList<Vector2> threats, int fleeRadius, int maxDistance,
        IReadOnlyCollection<Vector2> occupied, Random random)
    {
        List<Vector2> eligible = Eligible(candidates, from, threats, fleeRadius, maxDistance, occupied);
        return eligible.Count == 0 ? null : eligible[random.Next(eligible.Count)];
    }

    /// <summary>True when <paramref name="tile"/> lies within ±60° of the direction from <paramref name="threat"/> through <paramref name="from"/>; any tile qualifies when the two coincide.</summary>
    public static bool InCone(Vector2 from, Vector2 threat, Vector2 tile)
    {
        Vector2 away = from - threat;
        Vector2 offset = tile - from;
        if (away == Vector2.Zero || offset == Vector2.Zero)
        {
            return true;
        }
        return Vector2.Dot(Vector2.Normalize(away), Vector2.Normalize(offset)) >= ConeCosine;
    }

    /// <summary>Height of the flight arc above the straight line, peaking halfway.</summary>
    public static float ArcOffset(float progress, float height) => -MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI) * height;

    private static List<Vector2> Eligible(IEnumerable<Vector2> candidates, Vector2 from, IReadOnlyList<Vector2> threats, int fleeRadius, int maxDistance,
        IReadOnlyCollection<Vector2> occupied) =>
        candidates
            .Where(tile => tile != from && !occupied.Contains(tile))
            .Where(tile => Vector2.Distance(tile, from) is var reach && reach >= MinDistanceTiles && reach <= maxDistance)
            .Where(tile => threats.All(threat => Vector2.Distance(tile, threat) > fleeRadius + 1))
            .OrderBy(tile => tile.X).ThenBy(tile => tile.Y)
            .ToList();

    private static List<Vector2> Threats(ILocationQuery query, Vector2 threat)
    {
        List<Vector2> threats = new(query.FarmerTiles);
        if (!threats.Contains(threat))
        {
            threats.Add(threat);
        }
        return threats;
    }

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
