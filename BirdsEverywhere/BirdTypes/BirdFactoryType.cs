using BirdsEverywhere.Spawners;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

class BirdFactory
{
    private delegate CustomBirdType critterDelegate(int tileX, int tileY, string birdName, string texture, BirdServices services);

    private static readonly Dictionary<string, critterDelegate> birdTypes = new Dictionary<string, critterDelegate>() {
        {"LandBird", (tileX, tileY, birdName, texture, services) => new LandBird(tileX, tileY, birdName, texture, services)},
        {"WaterLandBird", (tileX, tileY, birdName, texture, services) => new WaterLandBird(tileX, tileY, birdName, texture, services)}
    };

    private delegate CustomBirdTypeTerrainFeature critterDelegateTerrainFeature(int tileX, int tileY, string birdName, string texture, BirdServices services);

    private static readonly Dictionary<string, critterDelegateTerrainFeature> birdTypesTerrainFeature = new Dictionary<string, critterDelegateTerrainFeature>() {
        {"TreeTrunkBird", (tileX, tileY, birdName, texture, services) => new TreeTrunkBird(tileX, tileY, birdName, texture, services)},
        {"BushBird", (tileX, tileY, birdName, texture, services) => new BushBird(tileX, tileY, birdName, texture, services)}
    };

    public static IEnumerable<string> KnownBirdTypes => birdTypes.Keys.Concat(birdTypesTerrainFeature.Keys);

    public static CustomBirdType createBird(SingleBirdSpawnParameters sParams, GameLocation location, BirdServices services)
    {
        int x = (int)sParams.position.X;
        int y = (int)sParams.position.Y;
        return sParams switch
        {
            SingleBirdSpawnParamsTerrainFeature feature => birdTypesTerrainFeature[sParams.BirdType](x, y, sParams.ID, sParams.texture, services)
                .setTerrainFeature(feature.terrainFeatureIndex, location.Name),
            _ => birdTypes[sParams.BirdType](x, y, sParams.ID, sParams.texture, services),
        };
    }
}
