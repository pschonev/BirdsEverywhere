using Newtonsoft.Json;
using StardewValley;

namespace BirdsEverywhere;

public class CurrentAnimatedSprite
{
    [JsonProperty("texture")]
    public string Texture { get; set; } = "";

    [JsonProperty("currentAnimation")]
    public List<(int frame, int ms)>? CurrentAnimation { get; set; }

    [JsonProperty("currentFrame")]
    public int CurrentFrame { get; set; }

    [JsonProperty("timer")]
    public float Timer { get; set; }

    [JsonProperty("loop")]
    public bool Loop { get; set; }

    [JsonProperty("currentAnimationIndex")]
    public int CurrentAnimationIndex { get; set; }

    [JsonProperty("spriteWidth")]
    public int SpriteWidth { get; set; }

    [JsonProperty("spriteHeight")]
    public int SpriteHeight { get; set; }

    public CurrentAnimatedSprite() { }

    public CurrentAnimatedSprite(AnimatedSprite sprite)
    {
        CurrentAnimation = sprite.CurrentAnimation?.Select(f => (f.frame, f.milliseconds)).ToList();
        CurrentFrame = sprite.currentFrame;
        Texture = sprite.textureName.Value ?? "";
        Timer = sprite.timer;
        Loop = sprite.loop;
        CurrentAnimationIndex = sprite.currentAnimationIndex;
        SpriteWidth = sprite.SpriteWidth;
        SpriteHeight = sprite.SpriteHeight;
    }

    public static AnimatedSprite CurrentToAnimatedSprite(CurrentAnimatedSprite sprite)
    {
        AnimatedSprite currentSprite = new(sprite.Texture, sprite.CurrentFrame, sprite.SpriteWidth, sprite.SpriteHeight);
        if (sprite.CurrentAnimation != null)
        {
            currentSprite.setCurrentAnimation(sprite.CurrentAnimation
                .Select(f => new FarmerSprite.AnimationFrame((short)f.frame, f.ms))
                .ToList());
        }
        currentSprite.timer = sprite.Timer;
        currentSprite.loop = sprite.Loop;
        currentSprite.currentAnimationIndex = sprite.CurrentAnimationIndex;
        return currentSprite;
    }
}
