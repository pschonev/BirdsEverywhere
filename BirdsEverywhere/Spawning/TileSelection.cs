using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners;

public static class TileSelection
{
    private const int MapMargin = 3;
    private const int MinMapSize = 2 * MapMargin + 1;
    public const int MinSpeciesDistance = 8;

    public static bool IsNear(Vector2 tile, IReadOnlyCollection<Vector2>? avoid, int distance = MinSpeciesDistance) =>
        avoid is not null && avoid.Any(other => Vector2.DistanceSquared(other, tile) < distance * distance);

    public static List<Vector2> Tiles(ILocationQuery location, TileSpawnCondition condition, IntRange groupCount, IntRange groupSize,
        Random random, int attemptsStartTile = 100, int attemptsPerBird = 20, IReadOnlyCollection<Vector2>? avoid = null,
        GroupLayout? layout = null, int speciesDistance = MinSpeciesDistance)
    {
        List<Vector2> tiles = new();
        Point mapSize = location.MapSize;
        if (mapSize.X < MinMapSize || mapSize.Y < MinMapSize)
        {
            return tiles;
        }
        if (layout is not null)
        {
            return Grouped(location, condition, groupCount, groupSize, random, attemptsStartTile, attemptsPerBird, avoid, layout, speciesDistance);
        }

        int groups = Utils.NextInclusive(random, groupCount.Min, groupCount.Max);
        for (int group = 0; group < groups; group++)
        {
            int target = tiles.Count + Utils.NextInclusive(random, groupSize.Min, groupSize.Max);
            for (int attempt = 0; attempt < attemptsStartTile; attempt++)
            {
                Vector2 start = new(random.Next(MapMargin, mapSize.X - MapMargin - 1), random.Next(MapMargin, mapSize.Y - MapMargin - 1));
                if (!condition(location, start) || IsNear(start, avoid, speciesDistance))
                {
                    continue;
                }

                int walkLength = (target - tiles.Count) * attemptsPerBird;
                foreach (Vector2 tile in Utils.GetRandomPositionsStartingFromThisTile(random, start, mapSize, walkLength, branchChance: 0.5))
                {
                    if (tiles.Count >= target)
                    {
                        break;
                    }

                    if (condition(location, tile) && !IsNear(tile, avoid, speciesDistance) && !tiles.Contains(tile))
                    {
                        tiles.Add(tile);
                    }
                }
                break;
            }
        }

        return tiles;
    }

    private static readonly Vector2[] _lineDirections = { new(1, 0), new(0, 1), new(1, 1), new(1, -1) };

    /// <summary>Rejection sampling per formation; groups may place fewer members than drawn when tiles run out.</summary>
    private static List<Vector2> Grouped(ILocationQuery location, TileSpawnCondition condition, IntRange groupCount, IntRange groupSize,
        Random random, int attemptsStartTile, int attemptsPerBird, IReadOnlyCollection<Vector2>? avoid, GroupLayout layout, int speciesDistance)
    {
        Point mapSize = location.MapSize;
        List<Vector2> tiles = new();
        List<Vector2> starts = new();
        bool Free(Vector2 tile, List<Vector2> members) =>
            tile.X >= 0 && tile.Y >= 0 && tile.X < mapSize.X && tile.Y < mapSize.Y
            && condition(location, tile) && !IsNear(tile, avoid, speciesDistance) && !tiles.Contains(tile)
            && members.All(member => Vector2.Distance(member, tile) >= layout.Spacing);

        int groups = Utils.NextInclusive(random, groupCount.Min, groupCount.Max);
        for (int group = 0; group < groups; group++)
        {
            int size = Utils.NextInclusive(random, groupSize.Min, groupSize.Max);
            List<Vector2> members = new();
            for (int attempt = 0; attempt < attemptsStartTile && members.Count == 0; attempt++)
            {
                Vector2 start = new(random.Next(MapMargin, mapSize.X - MapMargin - 1), random.Next(MapMargin, mapSize.Y - MapMargin - 1));
                if (Free(start, members) && starts.All(other => Vector2.Distance(other, start) >= layout.GroupSpacing))
                {
                    members.Add(start);
                }
            }
            if (members.Count == 0)
            {
                continue;
            }

            Vector2 origin = members[0];
            starts.Add(origin);
            if (layout.Formation == Formation.Line)
            {
                Vector2 direction = _lineDirections[random.Next(_lineDirections.Length)];
                for (int step = 1; members.Count < size && (direction * step * layout.Spacing).Length() <= layout.Radius; step++)
                {
                    foreach (int side in new[] { 1, -1 })
                    {
                        Vector2 tile = origin + (direction * step * layout.Spacing * side);
                        if (members.Count < size && Free(tile, members))
                        {
                            members.Add(tile);
                        }
                    }
                }
            }
            else
            {
                for (int attempt = 0; attempt < size * attemptsPerBird && members.Count < size; attempt++)
                {
                    double angle = random.NextDouble() * 2 * Math.PI;
                    double roll = random.NextDouble();
                    double distance = layout.Radius * (layout.Formation == Formation.Cluster ? roll : Math.Sqrt(roll));
                    Vector2 tile = new((float)Math.Round(origin.X + (Math.Cos(angle) * distance)), (float)Math.Round(origin.Y + (Math.Sin(angle) * distance)));
                    if (Free(tile, members))
                    {
                        members.Add(tile);
                    }
                }
            }
            tiles.AddRange(members);
        }
        return tiles;
    }

    public static List<(T Candidate, Vector2 Position)> Features<T>(IEnumerable<T> candidates, IntRange count, Random random, Func<T, Random, Vector2?> place,
        IReadOnlyCollection<Vector2>? avoid = null, int speciesDistance = MinSpeciesDistance)
    {
        List<(T, Vector2)> placed = new();
        int target = Utils.NextInclusive(random, count.Min, count.Max);
        foreach (T candidate in Utils.Shuffle(random, candidates))
        {
            if (placed.Count >= target)
            {
                break;
            }

            if (place(candidate, random) is Vector2 position && !IsNear(position, avoid, speciesDistance))
            {
                placed.Add((candidate, position));
            }
        }

        return placed;
    }
}
