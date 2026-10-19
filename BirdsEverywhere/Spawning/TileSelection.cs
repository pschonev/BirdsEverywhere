using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using static BirdsEverywhere.Spawners.Condition;

namespace BirdsEverywhere.Spawners;

public static class TileSelection
{
    private const int MapMargin = 3;
    private const int MinMapSize = 2 * MapMargin + 1;

    public static List<Vector2> Tiles(ILocationQuery location, TileSpawnCondition condition, IntRange groupCount, IntRange groupSize,
        Random random, int attemptsStartTile = 100, int attemptsPerBird = 20)
    {
        List<Vector2> tiles = new();
        Point mapSize = location.MapSize;
        if (mapSize.X < MinMapSize || mapSize.Y < MinMapSize)
        {
            return tiles;
        }

        int groups = Utils.NextInclusive(random, groupCount.Min, groupCount.Max);
        for (int group = 0; group < groups; group++)
        {
            int target = tiles.Count + Utils.NextInclusive(random, groupSize.Min, groupSize.Max);
            for (int attempt = 0; attempt < attemptsStartTile; attempt++)
            {
                Vector2 start = new(random.Next(MapMargin, mapSize.X - MapMargin - 1), random.Next(MapMargin, mapSize.Y - MapMargin - 1));
                if (!condition(location, start))
                {
                    continue;
                }

                int walkLength = (target - tiles.Count) * attemptsPerBird;
                foreach (Vector2 tile in Utils.getRandomPositionsStartingFromThisTile(random, start, mapSize, walkLength, branchChance: 0.5))
                {
                    if (tiles.Count >= target)
                    {
                        break;
                    }

                    if (condition(location, tile) && !tiles.Contains(tile))
                    {
                        tiles.Add(tile);
                    }
                }
                break;
            }
        }

        return tiles;
    }

    public static List<(T Candidate, Vector2 Position)> Features<T>(IEnumerable<T> candidates, IntRange count, Random random, Func<T, Random, Vector2?> place)
    {
        List<(T, Vector2)> placed = new();
        int target = Utils.NextInclusive(random, count.Min, count.Max);
        foreach (T candidate in Utils.Shuffle(random, candidates))
        {
            if (placed.Count >= target)
            {
                break;
            }

            if (place(candidate, random) is Vector2 position)
            {
                placed.Add((candidate, position));
            }
        }

        return placed;
    }
}
