using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

/// <summary>Points a bird at a tree or bush by the feature's tile; resolves to null when the location or feature is gone or changed kind.</summary>
internal readonly record struct TerrainAnchor(string? LocationName, Vector2 Tile)
{
    public Tree? ResolveTree() =>
        ResolveLocation() is { } location && location.terrainFeatures.TryGetValue(Tile, out TerrainFeature? feature) ? feature as Tree : null;

    public Bush? ResolveBush() => ResolveLocation() is { } location ? Condition.BushAt(location, Tile) : null;

    private GameLocation? ResolveLocation() =>
        string.IsNullOrEmpty(LocationName) ? null : Game1.getLocationFromName(LocationName);
}
