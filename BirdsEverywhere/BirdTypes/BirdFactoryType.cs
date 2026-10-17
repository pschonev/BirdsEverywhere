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
        private delegate CustomBirdType critterDelegate(int tileX, int tileY, string birdName, string texture);

        private static readonly Dictionary<string, critterDelegate> birdTypes = new Dictionary<string, critterDelegate>() {
            {"LandBird", (tileX, tileY, birdName, texture) => new LandBird(tileX, tileY, birdName, texture)},
            {"WaterLandBird", (tileX, tileY, birdName, texture) => new WaterLandBird(tileX, tileY, birdName, texture)}
        };

        private delegate CustomBirdTypeTerrainFeature critterDelegateTerrainFeature(int tileX, int tileY, string birdName, string texture);

        private static readonly Dictionary<string, critterDelegateTerrainFeature> birdTypesTerrainFeature = new Dictionary<string, critterDelegateTerrainFeature>() {
            {"TreeTrunkBird", (tileX, tileY, birdName, texture) => new TreeTrunkBird(tileX, tileY, birdName, texture)},
            {"BushBird", (tileX, tileY, birdName, texture) => new BushBird(tileX, tileY, birdName, texture)}
        };

        public static CustomBirdType createBird(SingleBirdSpawnParameters sParams, GameLocation location)
        {
            if (sParams is SingleBirdSpawnParamsTile)
                return birdTypes[sParams.BirdType]((int)sParams.position.X, (int)sParams.position.Y, sParams.ID, sParams.texture);

            else if(sParams is SingleBirdSpawnParamsTerrainFeature)
            {
                return birdTypesTerrainFeature[sParams.BirdType]((int)sParams.position.X, (int)sParams.position.Y, sParams.ID, sParams.texture)
                    .setTerrainFeature((sParams as SingleBirdSpawnParamsTerrainFeature).terrainFeatureIndex, location.Name);
            }

            ModEntry.modInstance.Monitor.Log($"Can't create bird {sParams.ID} in bird factory.", LogLevel.Error);
            return null;
        }
    }
}
