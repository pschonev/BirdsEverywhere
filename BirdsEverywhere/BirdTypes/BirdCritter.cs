using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace BirdsEverywhere.BirdTypes;

internal abstract class BirdCritter : Critter
{
    private const int ThreatCheckIntervalMs = 200;

    private long _birdID;
    private Vector2 _replicaTarget;

    [JsonProperty("birdTypeName")]
    public abstract string BirdTypeName { get; }
    [JsonProperty("state")]
    public BehaviorStatus State { get; set; }
    [JsonProperty("birdName")]
    public string BirdName { get; set; } = "";
    [JsonProperty("texture")]
    public string Texture { get; set; } = "";
    [JsonProperty("characterCheckTimer")]
    public int CharacterCheckTimer { get; set; } = ThreatCheckIntervalMs;

    [JsonProperty("birdID")]
    public long BirdID
    {
        get => _birdID;
        set
        {
            _birdID = value;
            SeedRandom();
        }
    }

    [JsonIgnore]
    protected Random Rng { get; private set; } = new();

    [JsonIgnore]
    public BirdServices? Services { get; set; }

    [JsonIgnore]
    public bool IsReplica { get; set; }

    public void ApplyReplicaState(BirdCritter source)
    {
        if (!IsReplica)
        {
            position = source.position;
        }
        _replicaTarget = source.position;
        startingPosition = source.startingPosition;
        State = source.State;
        flip = source.flip;
        yOffset = source.yOffset;
        yJumpOffset = source.yJumpOffset;
        gravityAffectedDY = source.gravityAffectedDY;
        sprite = source.sprite;
    }

    protected bool UpdateReplica()
    {
        position = Vector2.Lerp(position, _replicaTarget, 0.4f);
        return false;
    }

    public BirdCritter() { }

    protected BirdCritter(int baseFrame, int tileX, int tileY, string birdName, string texture, BirdServices services, int spriteWidth = 32, int spriteHeight = 32)
        : base(baseFrame, new Vector2(tileX * 64, tileY * 64))
    {
        BirdName = birdName;
        Texture = texture;
        Services = services;
        sprite = new AnimatedSprite(GetTextureName(services.Content), baseFrame, spriteWidth, spriteHeight);
        BirdID = services.NewId();
    }

    public BirdCritter SetAnimatedSprite(AnimatedSprite sprite)
    {
        this.sprite = sprite;
        return this;
    }

    public void SeedRandom()
    {
        Rng = new Random((int)(BirdID % int.MaxValue));
    }

    private string GetTextureName(IModContentHelper content)
    {
        string assetPath = $"assets/{BirdName}/{Texture}.png";
        content.Load<Texture2D>(assetPath);
        return content.GetInternalAssetName(assetPath).Name;
    }

    public override void drawAboveFrontLayer(SpriteBatch b)
    {
        if (State == BehaviorStatus.FlyingAway)
        {
            base.draw(b);
        }
    }

    public void AddBirdObservation(string birdName, string location)
    {
        Services?.Observe(birdName, location);
    }

    /// <summary>Returns a farmer or character within <paramref name="radiusTiles"/>, checked at most every 200 ms.</summary>
    protected Character? PollThreat(GameTime time, GameLocation location, int radiusTiles) =>
        CheckIntervalElapsed(time) ? FindThreat(location, radiusTiles) : null;

    protected Character? FindThreat(GameLocation location, int radiusTiles)
    {
        float radiusSquared = radiusTiles * radiusTiles * 64f * 64f;
        foreach (Farmer farmer in location.farmers)
        {
            if (Vector2.DistanceSquared(farmer.Position, position) <= radiusSquared)
            {
                return farmer;
            }
        }

        return Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, radiusTiles, location);
    }

    protected bool CheckIntervalElapsed(GameTime time)
    {
        CharacterCheckTimer -= time.ElapsedGameTime.Milliseconds;
        if (CharacterCheckTimer >= 0)
        {
            return false;
        }
        CharacterCheckTimer = ThreatCheckIntervalMs;
        return true;
    }
}
