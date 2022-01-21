﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.BellsAndWhistles;
using BirdsEverywhere.BirdTypes;

namespace BirdsEverywhere.BirdTypes
{
    class BirdFactory
    {
        private delegate CustomBirdType critterDelegate(int tileX, int tileY, string birdName);

        private static readonly Dictionary<string, critterDelegate> birdTypes = new Dictionary<string, critterDelegate>() {
            {"LandBird", (tileX, tileY, birdName) => new LandBird(tileX, tileY, birdName)},
            {"WaterLandBird", (tileX, tileY, birdName) => new WaterLandBird(tileX, tileY, birdName)}
        };

        public static CustomBirdType createBird(int tileX, int tileY, string birdName, string birdType)
        {
            return birdTypes[birdType](tileX, tileY, birdName);
        }
    }
}
