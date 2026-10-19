using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Registries;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere.Spawners;

internal sealed class LocationSpawner
{
    private readonly HashSet<BirdPlacement> _spawned = new();
    private readonly SpawnPatternRegistry _patterns;
    private readonly BirdTypeRegistry _birdTypes;
    private readonly BirdServices _services;

    public LocationSpawner(SpawnPatternRegistry patterns, BirdTypeRegistry birdTypes, BirdServices services)
    {
        _patterns = patterns;
        _birdTypes = birdTypes;
        _services = services;
    }

    public void BeginVisit() => _spawned.Clear();

    public void Populate(GameLocation location, DailySpawnPlan plan)
    {
        if (!plan.Locations.TryGetValue(location.Name ?? "", out IReadOnlyList<BirdPlacement>? placements))
        {
            return;
        }

        location.instantiateCrittersList();
        foreach (BirdPlacement placement in placements)
        {
            if (_spawned.Contains(placement) || !placement.IsActiveAt(Game1.timeOfDay))
            {
                continue;
            }

            if (!_patterns.TryCreate(new SpawnPatternId(placement.SpawnPattern), out Spawner? spawner))
            {
                _services.Monitor.LogOnce($"Bird '{placement.BirdId}' has unknown spawn pattern '{placement.SpawnPattern}'; skipping.", LogLevel.Warn);
                continue;
            }

            if (!spawner.IsStillValid(location, placement))
            {
                continue;
            }

            if (!_birdTypes.TryCreate(new BirdTypeId(placement.BirdType), placement, location, _services, out BirdCritter? bird))
            {
                _services.Monitor.LogOnce($"Bird '{placement.BirdId}' has unknown bird type '{placement.BirdType}' for its placement; skipping.", LogLevel.Warn);
                continue;
            }

            bird.birdID = placement.Seed;
            location.critters.Add(bird);
            _spawned.Add(placement);
            _services.Monitor.Log($"Added {placement.BirdId} at {(int)placement.Tile.X} - {(int)placement.Tile.Y}.", LogLevel.Debug);
        }
    }
}
