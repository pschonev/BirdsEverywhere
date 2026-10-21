using Microsoft.Xna.Framework;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
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
    private float _replicaYOffsetTarget;
    private Random _scheduleRandom = new();
    private long _nextScheduleTick;
    private long _playedIdleRevision = -1;

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
    [JsonProperty("idleTick")]
    public long IdleTick { get; set; }
    [JsonProperty("idleRevision")]
    public long IdleRevision { get; set; }
    [JsonProperty("idleCue")]
    public IdleCue? IdleCue { get; set; }

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
    protected Random VisualRng { get; private set; } = new();

    internal Random OwnerRandom => Rng;

    internal Random VisualRandom => VisualRng;

    internal void SeedVisualRandom(int seed) => VisualRng = new Random(seed);

    /// <summary>The spawn rule's resolved routine and sounds; null for birds from older snapshots, which use the bird type's defaults.</summary>
    [JsonProperty("composition")]
    public BirdComposition? Composition { get; set; }

    private BirdTypeProfile Profile => BirdTypeProfiles.ByBirdType[BirdTypeName];

    private BirdServices RequireServices() => Services ?? throw new InvalidOperationException($"{BirdTypeName} has no services.");

    [JsonIgnore]
    internal Routine IdleRoutine => Composition?.Routine
        ?? RequireServices().Routines[Profile.DefaultRoutine ?? throw new InvalidOperationException($"{BirdTypeName} has no idle routine.")];

    protected void PlaySound(string role)
    {
        IReadOnlyDictionary<string, string> sounds = Composition?.Sounds ?? Profile.Sounds;
        if (sounds.TryGetValue(role, out string? cue) && cue.Length > 0)
        {
            Game1.playSound(cue);
        }
    }

    [JsonIgnore]
    private SpriteLayout Layout => _layout ??= RequireServices().Layouts[Profile.LayoutId];

    private SpriteLayout? _layout;

    internal void PlayClip(string name, Random random, AnimatedSprite.endOfAnimationBehavior? onEnd = null)
    {
        SpriteClip clip = Layout.Clips[name];
        sprite.setCurrentAnimation(ClipPlayer.Frames(clip, baseFrame, flip, random, ClipEvent, onEnd));
        sprite.loop = clip.Loop;
    }

    protected void ShowPose(string name) => sprite.currentFrame = baseFrame + ClipPlayer.FirstCell(Layout.Clips[name]);

    protected virtual AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) =>
        throw new InvalidOperationException($"{BirdTypeName} has no clip event '{name}'.");

    [JsonIgnore]
    public BirdServices? Services { get; set; }

    [JsonIgnore]
    public bool IsReplica { get; set; }

    public void ApplyReplicaState(BirdCritter source)
    {
        bool continuingMotion = IsReplica && State == source.State && State is BehaviorStatus.FlyingAway or BehaviorStatus.Running;
        if (!IsReplica)
        {
            position = source.position;
        }
        _replicaTarget = source.position;
        startingPosition = source.startingPosition;
        bool pendingVisual = IsReplica && _playedIdleRevision == IdleRevision && source.IdleRevision == IdleRevision;
        if (!pendingVisual)
        {
            State = source.State;
            flip = source.flip;
        }
        if (continuingMotion)
        {
            _replicaYOffsetTarget = source.yOffset;
        }
        else
        {
            yOffset = source.yOffset;
            _replicaYOffsetTarget = source.yOffset;
        }
        yJumpOffset = source.yJumpOffset;
        gravityAffectedDY = source.gravityAffectedDY;
        if (!pendingVisual && (!continuingMotion || sprite?.CurrentAnimation == null || source.sprite.CurrentAnimation == null))
        {
            sprite = source.sprite;
        }
        if (IsReplica)
        {
            IdleTick = Math.Max(IdleTick, source.IdleTick);
            if (source.IdleRevision > IdleRevision)
            {
                IdleRevision = source.IdleRevision;
                IdleCue = source.IdleCue;
            }
            else if (source.IdleRevision == IdleRevision && source.IdleCue == null)
            {
                IdleCue = null;
            }
        }
    }

    protected BirdAction AdvanceIdle(Routine routine, bool eligible)
    {
        IdleTick++;
        if (!eligible)
        {
            if (IdleCue != null)
            {
                IdleCue = null;
                IdleRevision++;
            }
            _nextScheduleTick = IdleTick;
            return BirdAction.None;
        }
        if (IdleCue == null && _nextScheduleTick < IdleTick + 12)
        {
            long firstTick = Math.Max(IdleTick, _nextScheduleTick + 1);
            long lastTick = IdleTick + 120;
            for (long tick = firstTick; tick <= lastTick; tick++)
            {
                if (_scheduleRandom.NextDouble() >= routine.Chance)
                {
                    continue;
                }
                IdleCue = new IdleCue(tick, BirdActionIds.Parse(Routine.Roll(routine.Pick, _scheduleRandom)), _scheduleRandom.Next());
                IdleRevision++;
                _nextScheduleTick = tick;
                break;
            }
            if (IdleCue == null)
            {
                _nextScheduleTick = lastTick;
            }
        }
        if (IdleCue == null || IdleTick < IdleCue.Tick)
        {
            return BirdAction.None;
        }
        BirdAction action = IdleCue.Action;
        VisualRng = new Random(IdleCue.Seed);
        IdleCue = null;
        IdleRevision++;
        return action;
    }

    protected BirdAction AdvanceReplicaIdle()
    {
        IdleTick++;
        if (State != BehaviorStatus.Stopped || IdleCue == null || IdleTick < IdleCue.Tick || _playedIdleRevision == IdleRevision)
        {
            return BirdAction.None;
        }
        _playedIdleRevision = IdleRevision;
        VisualRng = new Random(IdleCue.Seed);
        return IdleCue.Action;
    }

    protected bool UpdateReplica(GameTime time, Vector2 velocity = default, float verticalVelocity = 0f)
    {
        if (State is BehaviorStatus.FlyingAway or BehaviorStatus.Running)
        {
            position += velocity;
            yOffset += verticalVelocity;
            _replicaTarget += velocity;
            _replicaYOffsetTarget += verticalVelocity;
        }
        position = Vector2.Lerp(position, _replicaTarget, 0.4f);
        yOffset = MathHelper.Lerp(yOffset, _replicaYOffsetTarget, 0.4f);
        if (sprite?.CurrentAnimation is { Count: > 0 } frames)
        {
            sprite.timer -= (float)time.ElapsedGameTime.TotalMilliseconds;
            while (sprite.timer <= 0 && sprite.CurrentAnimation is { Count: > 0 })
            {
                int next = sprite.currentAnimationIndex + 1;
                if (next >= frames.Count)
                {
                    if (!sprite.loop)
                    {
                        sprite.CurrentAnimation = null;
                        break;
                    }
                    next = 0;
                }
                sprite.currentAnimationIndex = next;
                sprite.currentFrame = frames[next].frame;
                sprite.timer += frames[next].milliseconds;
            }
        }
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
        _scheduleRandom = new Random(unchecked((int)BirdID ^ 0x5197A0B));
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
