using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace Birding.Spawners;

/// <summary>Areas that open up through progression. <c>Beach:East</c> is the land and water the Beach bridge connects.</summary>
public static class GatedAreas
{
    public const string BeachEast = "East";

    private const string Beach = "Beach";
    private static readonly Rectangle _beachBridge = new(58, 13, 4, 2);

    /// <summary>True when <paramref name="area"/> is a derived area of <paramref name="location"/>, not a fish area.</summary>
    public static bool Defines(string location, string area) => location == Beach && area == BeachEast;

    /// <summary>True when the tile is one the Beach bridge repair adds.</summary>
    public static bool IsBridge(string location, Vector2 tile) =>
        location == Beach && _beachBridge.Contains((int)tile.X, (int)tile.Y);

    public static bool BridgeFixed(GameLocation? location) => location is Beach beach && beach.bridgeFixed.Value;

    public static bool BridgeFixed() => BridgeFixed(Game1.getLocationFromName(Beach));
}
