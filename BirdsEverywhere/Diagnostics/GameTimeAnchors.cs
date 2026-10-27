using BirdsEverywhere.Domain;
using StardewValley;

namespace BirdsEverywhere.Diagnostics;

/// <summary>Reads a location's light changes from the game. The game has no sunrise API, so sunrise is fixed at 0600.</summary>
public static class GameTimeAnchors
{
    private const int Sunrise = 600;

    public static TimeAnchors For(GameLocation? location) => location is null
        ? TimeAnchors.Default
        : new TimeAnchors(Sunrise, Game1.getStartingToGetDarkTime(location), Game1.getTrulyDarkTime(location));
}
