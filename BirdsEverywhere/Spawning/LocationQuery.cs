using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.Spawners;

public interface ILocationQuery
{
    string Name { get; }
    Point MapSize { get; }
    bool IsGroundTile(Vector2 tile);
    bool IsWaterTile(Vector2 tile);
    bool IsSpawnableGroundTile(Vector2 tile);
}

public sealed class GameLocationQuery : ILocationQuery
{
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

    private bool HasWater(Vector2 tile) =>
        _location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null;
}
