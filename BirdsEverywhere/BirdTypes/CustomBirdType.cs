using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace BirdsEverywhere.BirdTypes;

abstract class CustomBirdType : Critter
{
    public enum BehaviorStatus
    {
        Pecking,
        FlyingAway,
        Sleeping,
        Stopped,
        Walking,
        Running,
        Swimming
    }

    private long _birdID;

    public abstract string birdTypeName { get; }
    public BehaviorStatus state { get; set; }
    public string birdName { get; set; }
    public string texture { get; set; }
    public int characterCheckTimer { get; set; } = 200;

    public long birdID
    {
        get => _birdID;
        set
        {
            _birdID = value;
            seedRandom();
        }
    }

    [JsonIgnore]
    protected Random random;

    [JsonIgnore]
    public BirdServices? services { get; set; }

    public CustomBirdType() { }

    protected CustomBirdType(int baseFrame, int tileX, int tileY, string birdName, string texture, BirdServices services, int spriteWidth = 32, int spriteHeight = 32)
        : base(baseFrame, new Vector2(tileX * 64, tileY * 64))
    {
        this.birdName = birdName;
        this.texture = texture;
        this.services = services;
        this.sprite = new AnimatedSprite(getTextureName(services.Content), baseFrame, spriteWidth, spriteHeight);
        this.birdID = services.NewId();
    }

    public CustomBirdType setAnimatedSprite(AnimatedSprite sprite)
    {
        this.sprite = sprite;
        return this;
    }

    public void seedRandom()
    {
        this.random = new Random((int)(this.birdID % Int32.MaxValue));
    }

    private string getTextureName(IModContentHelper content)
    {
        string assetPath = $"assets/{this.birdName}/{this.texture}.png";
        content.Load<Texture2D>(assetPath);
        return content.GetInternalAssetName(assetPath).Name;
    }

    public override void drawAboveFrontLayer(SpriteBatch b)
    {
        if (state == BehaviorStatus.FlyingAway)
        {
            base.draw(b);
        }
    }

    public void addBirdObservation(string birdName, string location)
    {
        services?.Observe(birdName, location);
    }
}
