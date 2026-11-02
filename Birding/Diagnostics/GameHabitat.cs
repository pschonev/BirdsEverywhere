using Birding.Domain;
using StardewValley;

namespace Birding.Diagnostics;

internal static class GameHabitat
{
    public static bool Exists(Habitat habitat)
    {
        GameLocation? location = Game1.getLocationFromName(habitat.Location);
        return location is not null
            && (habitat.FarmType is null || string.Equals(habitat.FarmType, Game1.GetFarmTypeKey(), StringComparison.OrdinalIgnoreCase))
            && (habitat.FishArea is null || location.GetData()?.FishAreas?.ContainsKey(habitat.FishArea) == true);
    }
}
