using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.BirdTypes
{
    // this should really be an interface but covariant return for interfaces is not implemented in C# 9
    abstract class CustomBirdTypeTerrainFeature : CustomBirdType
    {
        public CustomBirdTypeTerrainFeature() { }

        protected CustomBirdTypeTerrainFeature(int baseFrame, int tileX, int tileY, string birdName, string texture, int spriteWidth = 32, int spriteHeight = 32) :
            base(baseFrame, tileX, tileY, birdName, texture, spriteWidth, spriteHeight)
        { }

        public abstract CustomBirdType setTerrainFeature(int index, string locationName);
    }
}
