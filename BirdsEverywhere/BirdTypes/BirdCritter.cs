using Microsoft.Xna.Framework;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
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

    public abstract string BirdTypeName { get; }
    public BehaviorStatus State { get; set; }
    public string BirdName { get; set; } = "";
    public string Texture { get; set; } = "";
    public int CharacterCheckTimer { get; set; } = ThreatCheckIntervalMs;
    public long IdleTick { get; set; }
    public long IdleRevision { get; set; }
    public IdleCue? IdleCue { get; set; }

    public long BirdID
    {
        get => _birdID;
        set
        {
            _birdID = value;
            SeedRandom();
        }
    }

    protected Random Rng { get; private set; } = new();

    protected Random VisualRng { get; private set; } = new();

    internal Random OwnerRandom => Rng;

    /// <summary>Adds the type-specific fields to a snapshot.</summary>
    internal virtual BirdState CaptureExtras(BirdState state) => state;

    /// <summary>Restores the type-specific fields from a snapshot.</summary>
    internal virtual void RestoreExtras(BirdState state) { }

    /// <summary>Copies type-specific fields a replica needs from the owner's latest snapshot.</summary>
    protected virtual void ApplyReplicaExtras(BirdCritter source) { }

    internal Random VisualRandom => VisualRng;

    internal void SeedVisualRandom(int seed) => VisualRng = new Random(seed);

    /// <summary>The spawn rule's resolved routine and sounds; null for birds from older snapshots, which use the bird type's defaults.</summary>
    public BirdComposition? Composition { get; set; }

    private BirdTypeProfile Profile => BirdTypeProfiles.ByBirdType[BirdTypeName];

    private BirdServices RequireServices() => Services ?? throw new InvalidOperationException($"{BirdTypeName} has no services.");

    internal Routine IdleRoutine => Composition?.Routine
        ?? RequireServices().Routines[Profile.DefaultRoutine ?? throw new InvalidOperationException($"{BirdTypeName} has no idle routine.")];

    protected void PlaySound(string role)
    {
        IReadOnlyDictionary<string, string> sounds = Composition?.Sounds ?? Profile.Sounds;
        if (!sounds.TryGetValue(role, out string? cue) || cue.Length == 0 || Services is not { SoundVolume: > 0 } services || CurrentLocation is not { } location)
        {
            return;
        }
        double now = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        if (Composition?.SoundCooldownMs is { } cooldowns && cooldowns.TryGetValue(role, out int cooldown))
        {
            if (_lastSoundMs.TryGetValue(role, out double last) && now - last < cooldown)
            {
                return;
            }
            _lastSoundMs[role] = now;
        }

        string id = cue.StartsWith(AudioCues.Prefix, StringComparison.Ordinal) ? services.ModCuePrefix + cue[AudioCues.Prefix.Length..] : cue;
        Game1.sounds.PlayLocal(id, location, position / 64f, null, SoundContext.Default, out ICue played);
        if (services.SoundVolume < 1f)
        {
            played.Volume *= services.SoundVolume;
        }
    }

    private readonly Dictionary<string, double> _lastSoundMs = new(StringComparer.Ordinal);

    /// <summary>The location of the last update; sounds play there, so birds outside it stay silent.</summary>
    protected GameLocation? CurrentLocation { get; set; }

    /// <summary>The rule's action, or the bird type's default for birds from older snapshots.</summary>
    internal ActionDefinition Action(string id) =>
        Composition?.Actions is { } actions ? actions[id] : RequireServices().Actions[Profile.DefaultActions][id];

    /// <summary>Rolls the sound chance when the action has a sound role: owners on the owner RNG (legacy alarm rolls), replicas on their visual RNG.</summary>
    protected void PlayActionSound(ActionDefinition action)
    {
        if (action.Sound is { } role && (IsReplica ? VisualRng : Rng).NextDouble() < action.SoundChance)
        {
            PlaySound(role);
        }
    }

    private SpriteLayout Layout => _layout ??= RequireServices().Layouts[Profile.LayoutId];

    private SpriteLayout? _layout;

    internal void PlayClip(string name, Random random, AnimatedSprite.endOfAnimationBehavior? onEnd = null)
    {
        SpriteClip clip = Layout.Clips[name];
        sprite.setCurrentAnimation(ClipPlayer.Frames(clip, baseFrame, flip, random, ClipEvent, IsReplica ? null : onEnd));
        sprite.loop = clip.Loop;
        _ownClipEvents = IsReplica;
    }

    private bool _ownClipEvents;

    protected void ShowPose(string name) => sprite.currentFrame = baseFrame + ClipPlayer.FirstCell(Layout.Clips[name]);

    protected virtual AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) => name == "splash"
        ? _ => Splash(position + new Vector2(flip ? 32f : -32f, 0f))
        : throw new InvalidOperationException($"{BirdTypeName} has no clip event '{name}'.");

    /// <summary>The vanilla water splash at a world position, local only.</summary>
    protected void Splash(Vector2 at) =>
        CurrentLocation?.temporarySprites.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, at - new Vector2(32f, 32f), flicker: false, flipped: false));

    public BirdServices? Services { get; set; }

    public bool IsReplica { get; set; }

    public void ApplyReplicaState(BirdCritter source)
    {
        bool continuingMotion = IsReplica && State == source.State && State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating;
        if (!IsReplica)
        {
            position = source.position;
        }
        _replicaTarget = source.position;
        startingPosition = source.startingPosition;
        bool pendingVisual = IsReplica && _playedIdleRevision == IdleRevision && source.IdleRevision == IdleRevision;
        bool startsFleeing = IsReplica && State != source.State && source.State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating;
        bool startsAlert = State != source.State;
        if (!pendingVisual)
        {
            State = source.State;
            flip = source.flip;
        }
        if (startsFleeing && Services != null)
        {
            PlayActionSound(Action("flee"));
        }
        if (IsReplica && source.State == BehaviorStatus.Alert && startsAlert && Threat.Alert?.Sound is { } alertSound)
        {
            PlaySound(alertSound);
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
            _ownClipEvents = false;
        }
        if (IsReplica)
        {
            ApplyReplicaExtras(source);
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

    protected BirdAction AdvanceIdle(Routine routine, bool eligible, Func<string, bool>? allowed = null)
    {
        allowed ??= _ => true;
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
                if (routine.Choose(_scheduleRandom, allowed, Services?.World?.Invoke()) is not { } picked)
                {
                    break;
                }
                IdleCue = new IdleCue(tick, picked, _scheduleRandom.Next());
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
        string action = IdleCue.Action;
        VisualRng = new Random(IdleCue.Seed);
        IdleCue = null;
        IdleRevision++;
        return ResolveIdle(action);
    }

    /// <summary>Built-in ids map to the class's actions; custom ids (data-only actions) start here and leave the class's decision list alone.</summary>
    private BirdAction ResolveIdle(string id)
    {
        if (BirdActionIds.TryParse(id, out BirdAction action))
        {
            return action;
        }

        ActionDefinition custom = Action(id);
        PlayActionSound(custom);
        StartCustomAction(custom);
        return BirdAction.None;
    }

    /// <summary>Runs a custom action by primitive; the base handles <c>stay</c> (play the clip, stay stopped).</summary>
    protected virtual void StartCustomAction(ActionDefinition action) => PlayClip(action.Clip!, VisualRng);

    protected BirdAction AdvanceReplicaIdle()
    {
        IdleTick++;
        if (State is not (BehaviorStatus.Stopped or BehaviorStatus.Swimming) || IdleCue == null || IdleTick < IdleCue.Tick || _playedIdleRevision == IdleRevision)
        {
            return BirdAction.None;
        }
        _playedIdleRevision = IdleRevision;
        VisualRng = new Random(IdleCue.Seed);
        return ResolveIdle(IdleCue.Action);
    }

    protected bool UpdateReplica(GameTime time, Vector2 velocity = default, float verticalVelocity = 0f)
    {
        if (State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating)
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
                if (_ownClipEvents)
                {
                    frames[next].frameStartBehavior?.Invoke(null!);
                }
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

    /// <summary>The rule's reactions, or the bird type's default for birds from older snapshots.</summary>
    internal ThreatReaction Threat => (Composition?.Reactions ?? Profile.Reactions
        ?? throw new InvalidOperationException($"{BirdTypeName} has no reactions.")).Threat;

    private int _alertCalmMs;

    /// <summary>At most every 200 ms: the threat to flee from, or whether one is inside the alert ring.</summary>
    protected (Character? Flee, bool Alerted) PollThreats(GameTime time, GameLocation location) =>
        CheckIntervalElapsed(time) ? ScanThreats(location, Threat) : (null, false);

    /// <summary>Farmers within the flee radius (running ones count closer) or any other character in it; matches <see cref="FindThreat"/> for default reactions.</summary>
    protected (Character? Flee, bool Alerted) ScanThreats(GameLocation location, ThreatReaction threat)
    {
        int alertRadius = threat.Alert?.Radius ?? 0;
        bool alerted = false;
        foreach (Farmer farmer in location.farmers)
        {
            float tiles = Vector2.Distance(farmer.Position, position) / 64f;
            if (threat.RunningMultiplier > 1 && farmer.running && farmer.isMoving())
            {
                tiles /= (float)threat.RunningMultiplier;
            }
            if (tiles <= threat.FleeRadius)
            {
                return (farmer, false);
            }
            alerted |= tiles <= alertRadius;
        }

        if (Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, threat.FleeRadius, location) is { } other)
        {
            return (other, false);
        }
        return (null, alerted || (alertRadius > 0 && Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, alertRadius, location) != null));
    }

    /// <summary>True once no threat has been in the alert ring for the alert's calm time.</summary>
    protected bool TickCalm(GameTime time, bool alerted)
    {
        _alertCalmMs = alerted ? Threat.Alert?.CalmMs ?? 0 : _alertCalmMs - time.ElapsedGameTime.Milliseconds;
        return _alertCalmMs <= 0;
    }

    protected void StartAlert(GameLocation environment)
    {
        AlertReaction alert = Threat.Alert!;
        State = BehaviorStatus.Alert;
        sprite.loop = false;
        sprite.CurrentAnimation = null;
        PlayClip(alert.Clip, VisualRng);
        if (alert.Sound is { } role)
        {
            PlaySound(role);
        }
        AddBirdObservation(BirdName, environment.Name);
    }

    protected void ShowAlertPose()
    {
        if (sprite.CurrentAnimation == null)
        {
            ShowPose(Threat.Alert?.Clip ?? "stand");
        }
    }

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
