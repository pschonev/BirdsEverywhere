using Microsoft.Xna.Framework;
using static Birding.Spawners.Condition;

namespace Birding.Spawners;

/// <summary>A planned bird that left a location by flying or swimming off; <c>Minute</c> is the in-game minute of the day.</summary>
public sealed record Departure(string Location, int Seed, int Minute);

/// <summary>Planned birds that left a location today; a bird may return only after <see cref="DelayMinutes"/>.</summary>
public sealed class DepartureTable
{
    public const int DelayMinutes = 40;

    private readonly Dictionary<(string Location, int Seed), int> _departedAt = new();

    public static int ToMinutes(int clock) => clock / 100 * 60 + clock % 100;

    public void Clear() => _departedAt.Clear();

    public void Record(Departure departure) => _departedAt[(departure.Location, departure.Seed)] = departure.Minute;

    public IReadOnlyList<Departure> Snapshot() => _departedAt.Select(entry => new Departure(entry.Key.Location, entry.Key.Seed, entry.Value)).ToList();

    public void Replace(IEnumerable<Departure> departures)
    {
        _departedAt.Clear();
        foreach (Departure departure in departures)
        {
            Record(departure);
        }
    }

    /// <summary>A departure is valid when the sender is in that location and the bird is in today's plan.</summary>
    public static bool IsValid(DailySpawnPlan plan, Departure departure, string? senderLocation) =>
        senderLocation == departure.Location
        && plan.Locations.TryGetValue(departure.Location, out IReadOnlyList<BirdPlacement>? placements)
        && placements.Any(placement => placement.Seed == departure.Seed);

    public void MarkReturned(string location, int seed) => _departedAt.Remove((location, seed));

    public bool IsDeparted(string location, int seed) => _departedAt.ContainsKey((location, seed));

    /// <summary>True when the bird left and enough time has passed that it may come back.</summary>
    public bool CanReturn(string location, int seed, int clock) =>
        _departedAt.TryGetValue((location, seed), out int minute) && ToMinutes(clock) >= minute + DelayMinutes;
}

/// <summary>Picks the tile a departed bird returns to: close to a spot, never an excluded tile, far from every farmer.</summary>
public static class ReturnTiles
{
    public const int SearchRadius = 6;
    public const int GroupRadius = 3;
    public const int FarmerDistance = 10;
    private const int Attempts = 60;

    /// <summary>The tile near <paramref name="origin"/> that satisfies <paramref name="condition"/>, or null when none does.</summary>
    public static Vector2? Find(ILocationQuery location, TileSpawnCondition condition, Vector2 origin, IReadOnlyCollection<Vector2> excluded, Random random, int radius = SearchRadius)
    {
        for (int attempt = 0; attempt < Attempts; attempt++)
        {
            Vector2 tile = origin + new Vector2(random.Next(-radius, radius + 1), random.Next(-radius, radius + 1));
            if (Eligible(location, condition, tile, excluded))
            {
                return tile;
            }
        }

        return null;
    }

    private const float ArrivalStepPx = 64f;
    private const int ArrivalMaxSteps = 100;

    /// <summary>The pixel position an arriving bird starts from: along the line from the nearest farmer through <paramref name="tile"/> and beyond, until it is off screen or at the map edge.</summary>
    public static Vector2 ArrivalStart(Point mapSize, Vector2 tile, IReadOnlyList<Vector2> farmers, Func<Vector2, bool> isVisible)
    {
        Vector2 target = (tile * 64f) + new Vector2(32f, 32f);
        Vector2 away = new(1f, 0f);
        if (farmers.Count > 0)
        {
            Vector2 nearest = farmers.OrderBy(farmer => Vector2.DistanceSquared(farmer, tile)).First();
            if (tile != nearest)
            {
                away = Vector2.Normalize(tile - nearest);
            }
        }
        Vector2 limit = new((mapSize.X * 64f) - 1f, (mapSize.Y * 64f) - 1f);
        Vector2 position = target;
        for (int step = 0; step < ArrivalMaxSteps; step++)
        {
            Vector2 next = position + (away * ArrivalStepPx);
            if (next.X < 0f || next.Y < 0f || next.X > limit.X || next.Y > limit.Y)
            {
                break;
            }
            position = next;
            if (!isVisible(position))
            {
                break;
            }
        }
        return position;
    }

    public static bool Eligible(ILocationQuery location, TileSpawnCondition condition, Vector2 tile, IReadOnlyCollection<Vector2> excluded) =>
        condition(location, tile) && !excluded.Contains(tile) && !TileSelection.IsNear(tile, location.FarmerTiles.ToList(), FarmerDistance) && !ThreatSight.Near(tile, location.NpcTiles, ThreatSight.NpcSpawnClearanceTiles);
}
