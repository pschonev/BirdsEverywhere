using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.Birds;

namespace BirdsEverywhere
{
    class BirdFactory
    {
        private delegate Critter critterDelegate(int tileX, int tileY, string birdName);

        private static readonly Dictionary<string, critterDelegate> birdTypes = new Dictionary<string, critterDelegate>() {
            {"LandBird", (tileX, tileY, birdName) => new LandBird(tileX, tileY, birdName)}
        };

        public static Critter createBird(int tileX, int tileY, string birdName, string birdType)
        {
            return birdTypes[birdType](tileX, tileY, birdName);
        }
    }
}
