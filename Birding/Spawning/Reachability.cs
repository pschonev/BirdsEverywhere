using Birding.Domain;
using Microsoft.Xna.Framework;

namespace Birding.Spawners;

/// <summary>Tiles a farmer can reach on foot, plus open water within <see cref="Reachability.WaterReach"/> of them.</summary>
public sealed class ReachableTiles
{
    private readonly bool[,] _tiles;

    public ReachableTiles(bool[,] tiles, int count)
    {
        _tiles = tiles;
        Count = count;
    }

    public int Count { get; }

    public bool Contains(Vector2 tile)
    {
        int x = (int)tile.X;
        int y = (int)tile.Y;
        return x >= 0 && y >= 0 && x < _tiles.GetLength(0) && y < _tiles.GetLength(1) && _tiles[x, y];
    }
}

public static class Reachability
{
    public const int WaterReach = SpawnParams.MaxShoreDistance;

    private static readonly Point[] _steps = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    /// <summary>Flood fills walkable land from the seeds, then water tiles up to <see cref="WaterReach"/> from that land.</summary>
    public static ReachableTiles Compute(Point size, Func<Vector2, bool> isLand, Func<Vector2, bool> isWater, IEnumerable<Vector2> seeds)
    {
        bool[,] reach = new bool[size.X, size.Y];
        List<Point> land = new();
        Queue<Point> queue = new();
        foreach (Vector2 seed in seeds)
        {
            Point point = new((int)seed.X, (int)seed.Y);
            if (OnMap(size, point) && !reach[point.X, point.Y] && isLand(seed))
            {
                reach[point.X, point.Y] = true;
                queue.Enqueue(point);
            }
        }

        while (queue.Count > 0)
        {
            Point point = queue.Dequeue();
            land.Add(point);
            foreach (Point step in _steps)
            {
                Point next = new(point.X + step.X, point.Y + step.Y);
                if (OnMap(size, next) && !reach[next.X, next.Y] && isLand(new Vector2(next.X, next.Y)))
                {
                    reach[next.X, next.Y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        int count = land.Count;
        Queue<(Point Tile, int Depth)> shore = new(land.Select(point => (point, 0)));
        while (shore.Count > 0)
        {
            (Point point, int depth) = shore.Dequeue();
            if (depth == WaterReach)
            {
                continue;
            }
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Point next = new(point.X + dx, point.Y + dy);
                    if (OnMap(size, next) && !reach[next.X, next.Y] && isWater(new Vector2(next.X, next.Y)))
                    {
                        reach[next.X, next.Y] = true;
                        count++;
                        shore.Enqueue((next, depth + 1));
                    }
                }
            }
        }

        return new ReachableTiles(reach, count);
    }

    private static bool OnMap(Point size, Point point) => point.X >= 0 && point.Y >= 0 && point.X < size.X && point.Y < size.Y;
}
