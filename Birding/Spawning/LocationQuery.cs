using Microsoft.Xna.Framework;
using StardewValley;

namespace Birding.Spawners;

public interface ILocationQuery
{
    string Name { get; }
    Point MapSize { get; }
    bool IsGroundTile(Vector2 tile);
    bool IsWaterTile(Vector2 tile);
    bool IsSpawnableGroundTile(Vector2 tile);
    string? FishArea(Vector2 tile);
    bool IsPassable(Vector2 tile);
    IReadOnlyList<Vector2> FarmerTiles { get; }

    /// <summary>Tiles of villagers and pets that count as threats: visible characters within sight of a farmer (<see cref="ThreatSight"/>).</summary>
    IReadOnlyList<Vector2> NpcTiles => Array.Empty<Vector2>();

    /// <summary>Everything a bird keeps away from when it picks where to go: every farmer plus <see cref="NpcTiles"/>.</summary>
    IReadOnlyList<Vector2> ThreatTiles => FarmerTiles.Concat(NpcTiles).ToList();
    IEnumerable<Vector2> MatureTrees();

    /// <summary>True for a tile a player cannot see a bird on: it lies behind a bush or under a tree crown, so a landing there looks like hiding.</summary>
    bool IsHiddenByScenery(Vector2 tile) => false;

    /// <summary>Trunks a trunk bird can be seen on: <see cref="MatureTrees"/> that no other tree, bush or front-layer tile covers.</summary>
    IEnumerable<Vector2> TrunkTrees() => MatureTrees();

    /// <summary>Trees a bird can hide in: <see cref="MatureTrees"/> that currently have leaves.</summary>
    IEnumerable<Vector2> CanopyTrees() => MatureTrees();

    /// <summary>Tiles where a farmer can appear: next to each warp, plus the farmers' own tiles.</summary>
    IEnumerable<Vector2> ArrivalTiles() => FarmerTiles;

    /// <summary>True when a farmer can walk to the tile, or to land within shore reach of it.</summary>
    bool IsReachable(Vector2 tile) => true;

    /// <summary>True when the tile is in the fish area or derived area (see <see cref="GatedAreas"/>).</summary>
    bool InArea(string area, Vector2 tile) => FishArea(tile) == area;
}

public sealed class GameLocationQuery : ILocationQuery
{
    private static readonly Dictionary<(string Name, bool BridgeFixed, bool BridgeBlocked), ReachableTiles> _reachable = new();

    private readonly GameLocation _location;

    public GameLocationQuery(GameLocation location)
    {
        _location = location;
    }

    public string Name => _location.Name;

    public Point MapSize => new Point(_location.map.DisplayWidth / 64, _location.map.DisplayHeight / 64);

    public bool IsGroundTile(Vector2 tile) =>
        _location.isTileOnMap(tile) && _location.CanItemBePlacedHere(tile) && !HasWater(tile);

    public bool IsWaterTile(Vector2 tile) =>
        _location.isTileOnMap(tile) && HasWater(tile);

    public bool IsSpawnableGroundTile(Vector2 tile)
    {
        int x = (int)tile.X;
        int y = (int)tile.Y;
        return _location.isTileOnMap(tile)
            && !HasWater(tile)
            && _location.doesTileHaveProperty(x, y, "Spawnable", "Back") != null
            && !_location.doesEitherTileOrTileIndexPropertyEqual(x, y, "Spawnable", "Back", "F")
            && _location.CanItemBePlacedHere(tile)
            && _location.getTileIndexAt(x, y, "AlwaysFront") == -1
            && _location.getTileIndexAt(x, y, "Front") == -1
            && !_location.isBehindBush(tile)
            && !_location.isBehindTree(tile);
    }

    public string? FishArea(Vector2 tile) =>
        _location.TryGetFishAreaForTile(tile, out string id, out _) ? id : null;

    public bool IsPassable(Vector2 tile) =>
        _location.isTileOnMap(tile) && _location.isTilePassable(tile);

    public IReadOnlyList<Vector2> FarmerTiles => _location.farmers.Select(farmer => farmer.Tile).ToList();

    public IReadOnlyList<Vector2> NpcTiles
    {
        get
        {
            List<Vector2> farmers = FarmerTiles.ToList();
            return _location.characters.Where(character => !character.IsInvisible).Select(character => character.Tile)
                .Where(tile => ThreatSight.InSight(tile, farmers)).ToList();
        }
    }

    public IEnumerable<Vector2> MatureTrees() =>
        _location.terrainFeatures.Pairs.Where(pair => Condition.IsEligibleTree(pair.Value)).Select(pair => pair.Key);

    public bool IsHiddenByScenery(Vector2 tile) => _location.isBehindBush(tile) || _location.isBehindTree(tile);

    public IEnumerable<Vector2> TrunkTrees() => MatureTrees().Where(tile => Condition.IsTrunkVisible(_location, tile));

    public IEnumerable<Vector2> CanopyTrees() =>
        _location.terrainFeatures.Pairs.Where(pair => Condition.IsCanopyTree(pair.Value)).Select(pair => pair.Key);

    public bool IsReachable(Vector2 tile)
    {
        ReachableTiles reachable = Reach(bridgeBlocked: false);
        return reachable.Count == 0 || reachable.Contains(tile);
    }

    public bool InArea(string area, Vector2 tile)
    {
        if (!GatedAreas.Defines(Name, area))
        {
            return FishArea(tile) == area;
        }

        ReachableTiles reachable = Reach(bridgeBlocked: false);
        ReachableTiles withoutBridge = Reach(bridgeBlocked: true);
        return reachable.Contains(tile) && !withoutBridge.Contains(tile);
    }

    /// <summary>Drops cached reachability; call when a day starts or a save loads.</summary>
    public static void ClearReachability() => _reachable.Clear();

    private ReachableTiles Reach(bool bridgeBlocked)
    {
        (string, bool, bool) key = (Name, GatedAreas.BridgeFixed(_location), bridgeBlocked);
        if (_reachable.TryGetValue(key, out ReachableTiles? cached))
        {
            return cached;
        }

        ReachableTiles computed = Reachability.Compute(MapSize,
            tile => IsPassable(tile) && !HasWater(tile) && !(bridgeBlocked && GatedAreas.IsBridge(Name, tile)),
            IsWaterTile,
            ArrivalTiles());
        _reachable[key] = computed;
        return computed;
    }

    public IEnumerable<Vector2> ArrivalTiles()
    {
        foreach (Warp warp in _location.warps)
        {
            yield return new Vector2(warp.X, warp.Y);
            yield return new Vector2(warp.X + 1, warp.Y);
            yield return new Vector2(warp.X - 1, warp.Y);
            yield return new Vector2(warp.X, warp.Y + 1);
            yield return new Vector2(warp.X, warp.Y - 1);
        }

        foreach (Vector2 tile in FarmerTiles)
        {
            yield return tile;
        }
    }

    private bool HasWater(Vector2 tile) =>
        _location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null;
}
