using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class CurrentAnimatedSprite
    {
        public string texture { get; set; }
        public List<(int frame, int ms)> currentAnimation { get; set; }
        public int currentFrame { get; set; }
        public float timer { get; set; }
        public bool loop { get; set; }
        public int currentAnimationIndex { get; set; }
        public int spriteWidth { get; set; }
        public int spriteHeight { get; set; }

        public CurrentAnimatedSprite(AnimatedSprite sprite)
        {
            if (currentAnimation != null)
            {
                List<(int frame, int ms)> currentAnimation = new List<(int frame, int ms)>();
                foreach (FarmerSprite.AnimationFrame animFrame in sprite.CurrentAnimation)
                {
                    currentAnimation.Add((animFrame.frame, animFrame.milliseconds));
                }
                this.currentAnimation = currentAnimation;
            }
            else
                this.currentAnimation = null;
            this.currentFrame = sprite.currentFrame;
            this.texture = sprite.textureName.Value;
            this.timer = sprite.timer;
            this.loop = sprite.loop;
            this.currentAnimationIndex = sprite.currentAnimationIndex;
            this.spriteWidth = sprite.spriteWidth.Value;
            this.spriteHeight = sprite.spriteHeight.Value;
        }

        public static AnimatedSprite CurrentToAnimatedSprite(CurrentAnimatedSprite sprite)
        {
            if (sprite != null)
            {
                AnimatedSprite currentSprite = new AnimatedSprite(sprite.texture, sprite.currentFrame, sprite.spriteWidth, sprite.spriteHeight);
                if (sprite.currentAnimation != null)
                {
                    List<FarmerSprite.AnimationFrame> currentAnim = new List<FarmerSprite.AnimationFrame>();
                    foreach ((int frame, int ms) frameWithTime in sprite.currentAnimation)
                    {
                        currentAnim.Add(new FarmerSprite.AnimationFrame((short)(frameWithTime.frame), frameWithTime.ms));
                    }
                    currentSprite.setCurrentAnimation(currentAnim);
                }
                currentSprite.timer = sprite.timer;
                currentSprite.loop = sprite.loop;
                currentSprite.currentAnimationIndex = sprite.currentAnimationIndex;
                return currentSprite;
            }
            else
                return null;
        }
    }
}
