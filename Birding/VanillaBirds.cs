using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Birding;

public static class VanillaBirds
{
    public static void Remove(GameLocation location) =>
        location.critters?.RemoveAll(IsVanillaBird);

    private static bool IsVanillaBird(Critter critter) =>
        critter is Birdie or Seagull or Owl or Woodpecker;
}
