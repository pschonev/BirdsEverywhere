using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

/// <summary>Points a bird at a tree or bush; resolves to null when the location or feature is gone.</summary>
internal readonly record struct TerrainAnchor(string? LocationName, int Index)
{
    public Tree? ResolveTree()
    {
        GameLocation? location = ResolveLocation();
        if (location is null || Index < 0 || Index >= location.terrainFeatures.Count())
        {
            return null;
        }
        return location.terrainFeatures.Pairs.ElementAt(Index).Value as Tree;
    }

    public Bush? ResolveBush()
    {
        GameLocation? location = ResolveLocation();
        if (location is null || Index < 0 || Index >= location.largeTerrainFeatures.Count)
        {
            return null;
        }
        return location.largeTerrainFeatures[Index] as Bush;
    }

    private GameLocation? ResolveLocation() =>
        string.IsNullOrEmpty(LocationName) ? null : Game1.getLocationFromName(LocationName);
}
