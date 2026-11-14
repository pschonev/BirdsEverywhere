using Birding.BirdTypes;
using Birding.Domain;
using Birding.Registries;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Birding.Spawners;

internal sealed class LocationSpawner
{
    private readonly HashSet<BirdPlacement> _spawned = new();
    private readonly SpawnPatternRegistry _patterns;
    private readonly BirdTypeRegistry _birdTypes;
    private readonly BirdServices _services;
    private readonly DepartureTable _departures;
    private bool _visitStart;

    public LocationSpawner(SpawnPatternRegistry patterns, BirdTypeRegistry birdTypes, BirdServices services, DepartureTable departures)
    {
        _patterns = patterns;
        _birdTypes = birdTypes;
        _services = services;
        _departures = departures;
    }

    public void BeginVisit()
    {
        _spawned.Clear();
        _visitStart = true;
    }

    public void ResumeVisit(GameLocation location, DailySpawnPlan plan)
    {
        BeginVisit();
        _visitStart = false;
        if (!plan.Locations.TryGetValue(location.Name, out IReadOnlyList<BirdPlacement>? placements))
        {
            return;
        }

        HashSet<long> birdIds = location.critters.OfType<BirdCritter>().Select(bird => bird.BirdID).ToHashSet();
        foreach (BirdPlacement placement in placements)
        {
            if (birdIds.Contains(placement.Seed) || placement.IsActiveAt(Game1.timeOfDay))
            {
                _spawned.Add(placement);
            }
        }
    }

    public void Clear(GameLocation location)
    {
        location.critters?.RemoveAll(critter => critter is BirdCritter);
        BeginVisit();
    }

    public void Populate(GameLocation location, DailySpawnPlan plan)
    {
        if (!plan.Locations.TryGetValue(location.Name ?? "", out IReadOnlyList<BirdPlacement>? placements))
        {
            return;
        }

        location.instantiateCrittersList();
        GameLocationQuery query = new(location);
        Dictionary<int, Vector2> groupAnchors = LiveGroupAnchors(location);
        bool visitStart = _visitStart;
        _visitStart = false;
        HashSet<long> liveBirds = location.critters.OfType<BirdCritter>().Select(bird => bird.BirdID).ToHashSet();
        foreach (BirdPlacement planned in placements)
        {
            bool departed = _departures.IsDeparted(location.Name ?? "", planned.Seed);
            PopulateAction action = PopulateDecision.For(planned.IsActiveAt(Game1.timeOfDay), _spawned.Contains(planned), departed,
                departed && _departures.CanReturn(location.Name ?? "", planned.Seed, Game1.timeOfDay), visitStart, liveBirds.Contains(planned.Seed));
            if (action == PopulateAction.Skip)
            {
                continue;
            }
            bool arriving = action == PopulateAction.Arrive;

            BirdPlacement placement = planned;

            if (!_patterns.TryCreate(new SpawnPatternId(placement.SpawnPattern), out Spawner? spawner))
            {
                _services.Monitor.LogOnce($"Bird '{placement.BirdId}' has unknown spawn pattern '{placement.SpawnPattern}'; skipping.", LogLevel.Warn);
                continue;
            }

            if (arriving && spawner is not TileSpawner)
            {
                continue;
            }

            if (departed && spawner is TileSpawner tileSpawner)
            {
                if (ReturnPlacement(query, tileSpawner, location, planned, groupAnchors) is not { } returned)
                {
                    continue;
                }
                placement = returned;
            }
            else if (!placement.Forced && !spawner.IsStillValid(location, placement))
            {
                continue;
            }

            if (!placement.Forced && ThreatSight.Near(placement.Tile, query.NpcTiles, ThreatSight.NpcSpawnClearanceTiles))
            {
                continue;
            }

            if (!_birdTypes.TryCreate(new BirdTypeId(placement.BirdType), placement, location, _services, out BirdCritter? bird))
            {
                _services.Monitor.LogOnce($"Bird '{placement.BirdId}' has unknown bird type '{placement.BirdType}' for its placement; skipping.", LogLevel.Warn);
                continue;
            }

            bird.BirdID = placement.Seed;
            bird.Composition = placement.Composition;
            bird.ApplyLayout();
            bird.GroupId = placement.GroupId;
            bird.Role = placement.Role;
            bird.Stage = placement.Stage;
            if (arriving && !bird.BeginArrival(ReturnTiles.ArrivalStart(query.MapSize, placement.Tile, query.FarmerTiles, point => Utility.isOnScreen(point, 128)), placement.Tile, location))
            {
                continue;
            }
            location.critters.Add(bird);
            _spawned.Add(planned);
            if (departed)
            {
                _departures.MarkReturned(location.Name ?? "", planned.Seed);
            }
            _services.Monitor.Log($"Added {placement.BirdId} at {(int)placement.Tile.X} - {(int)placement.Tile.Y}.", LogLevel.Debug);
        }
    }

    private static Dictionary<int, Vector2> LiveGroupAnchors(GameLocation location)
    {
        Dictionary<int, Vector2> anchors = new();
        foreach (BirdCritter bird in location.critters.OfType<BirdCritter>())
        {
            if (bird.GroupId is { } group)
            {
                anchors.TryAdd(group, new Vector2((int)(bird.position.X / 64f), (int)(bird.position.Y / 64f)));
            }
        }
        return anchors;
    }

    private static BirdPlacement? ReturnPlacement(ILocationQuery query, TileSpawner spawner, GameLocation location, BirdPlacement planned,
        Dictionary<int, Vector2> groupAnchors)
    {
        Random random = new(HashCode.Combine(planned.Seed, Game1.timeOfDay));
        List<Vector2> excluded = location.critters.OfType<BirdCritter>()
            .Select(bird => new Vector2((int)(bird.position.X / 64f), (int)(bird.position.Y / 64f)))
            .Append(planned.Tile)
            .ToList();
        bool grouped = planned.GroupId is { } id && groupAnchors.ContainsKey(id);
        Vector2 origin = grouped ? groupAnchors[planned.GroupId!.Value] : planned.Tile;
        if (ReturnTiles.Find(query, spawner.TileCondition, origin, excluded, random, grouped ? ReturnTiles.GroupRadius : ReturnTiles.SearchRadius) is not { } tile)
        {
            return null;
        }
        if (planned.GroupId is { } group)
        {
            groupAnchors.TryAdd(group, tile);
        }
        return planned with { Tile = tile };
    }
}
