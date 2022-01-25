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

        public static CustomBirdType createBird(SingleBirdSpawnParameters sParams, GameLocation location)
        {
            if (sParams is SingleBirdSpawnParamsTile)
                return birdTypes[sParams.BirdType]((int)sParams.Position.X, (int)sParams.Position.Y, sParams.ID);

            else if(sParams is SingleBirdSpawnParamsTerrainFeature)
            {
                CustomBirdTypeTerrainFeature birdType = birdTypes[sParams.BirdType]((int)sParams.Position.X, (int)sParams.Position.Y, sParams.ID) as CustomBirdTypeTerrainFeature;
                return birdType.setTerrainFeature((sParams as SingleBirdSpawnParamsTerrainFeature).terrainFeatureIndex, location);
            }
            return null;
        }
    }
}
