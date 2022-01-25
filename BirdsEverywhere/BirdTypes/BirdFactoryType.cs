using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.BirdTypes;
using StardewValley.TerrainFeatures;
using BirdsEverywhere.Spawners;
using StardewValley;
using StardewModdingAPI;

namespace BirdsEverywhere.BirdTypes
{
    class BirdFactory
    {
        private delegate CustomBirdType critterDelegate(int tileX, int tileY, string birdName);

        private static readonly Dictionary<string, critterDelegate> birdTypes = new Dictionary<string, critterDelegate>() {
            {"LandBird", (tileX, tileY, birdName) => new LandBird(tileX, tileY, birdName)},
            {"WaterLandBird", (tileX, tileY, birdName) => new WaterLandBird(tileX, tileY, birdName)},
            {"TreeTrunkBird", (tileX, tileY, birdName) => new TreeTrunkBird(tileX, tileY, birdName)},
            {"BushBird", (tileX, tileY, birdName) => new BushBird(tileX, tileY, birdName)}
        };

        private delegate CustomBirdTypeTerrainFeature critterDelegateTerrainFeature(int tileX, int tileY, string birdName);

        private static readonly Dictionary<string, critterDelegateTerrainFeature> birdTypesTerrainFeature = new Dictionary<string, critterDelegateTerrainFeature>() {
            {"TreeTrunkBird", (tileX, tileY, birdName) => new TreeTrunkBird(tileX, tileY, birdName)},
            {"BushBird", (tileX, tileY, birdName) => new BushBird(tileX, tileY, birdName)}
        };

        public static CustomBirdType createBird(SingleBirdSpawnParameters sParams, GameLocation location)
        {
            if (sParams is SingleBirdSpawnParamsTile)
                return birdTypes[sParams.BirdType]((int)sParams.position.X, (int)sParams.position.Y, sParams.ID);

            else if(sParams is SingleBirdSpawnParamsTerrainFeature)
            {
                return birdTypesTerrainFeature[sParams.BirdType]((int)sParams.position.X, (int)sParams.position.Y, sParams.ID)
                    .setTerrainFeature((sParams as SingleBirdSpawnParamsTerrainFeature).terrainFeatureIndex, location);
            }

            ModEntry.modInstance.Monitor.Log($"Can't create bird {sParams.ID} in bird factory.", LogLevel.Error);
            return null;
        }
    }
}
