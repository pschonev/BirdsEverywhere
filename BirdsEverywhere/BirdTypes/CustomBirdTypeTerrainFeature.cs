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
        protected CustomBirdTypeTerrainFeature(int baseFrame, int tileX, int tileY, string birdName, int spriteWidth = 32, int spriteHeight = 32) :
            base(baseFrame, tileX, tileY, birdName, spriteWidth, spriteHeight)
        {}

        protected CustomBirdTypeTerrainFeature(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
                BehaviorStatus state, CurrentAnimatedSprite sprite, float gravityAffectedDY, float yOffset, float yJumpOffset,
                int characterCheckTimer = 200, int baseFrame = 0, int spriteWidth = 32, int spriteHeight = 32)
            : base(position, startingPosition, birdName, birdID, flip,
                state, sprite, gravityAffectedDY, yOffset, yJumpOffset,
                characterCheckTimer, baseFrame, spriteWidth, spriteHeight)
        { }
        public abstract CustomBirdType setTerrainFeature(int index, GameLocation location);
    }
}
