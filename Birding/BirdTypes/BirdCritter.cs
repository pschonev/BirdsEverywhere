using Birding.BirdTypes.Behaviors;
using Birding.Content;
using Birding.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using Birding.Spawners;

namespace Birding.BirdTypes;

internal abstract class BirdCritter : Critter
{
    private const int ThreatCheckIntervalMs = 200;
    private const int DiscoveryRadiusTiles = 8;
    private const float RelocateArcHeight = 48f;
    private const int RetargetIntervalMs = 300;
    private const int MaxRetargets = 2;
    private const double RelocationCalmMs = 30000;

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
    public int? GroupId { get; set; }
    public string? Role { get; set; }
    public string? Stage { get; set; }
    public int Relocations { get; set; }
    public Vector2? RelocateTarget { get; set; }

    /// <summary>Where the current relocation flight started; restored birds restart the arc from their position.</summary>
    internal Vector2 RelocateFrom { get; set; }

    private int _retargetTimerMs = RetargetIntervalMs;

    private int _retargets;

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

    protected virtual ActionDefinition ReplicaFleeAction => Action("flee");

    /// <summary>Rolls the sound chance when the action has a sound role: owners on the owner RNG (legacy alarm rolls), replicas on their visual RNG.</summary>
    protected void PlayActionSound(ActionDefinition action)
    {
        if (action.Sound is { } role && (IsReplica ? VisualRng : Rng).NextDouble() < action.SoundChance)
        {
            PlaySound(role);
        }
    }

    private SpriteLayout Layout => _layout ??= RequireServices().Layouts[Composition?.LayoutId ?? Profile.LayoutId];

    private SpriteLayout? _layout;

    internal void ApplyLayout()
    {
        if (Composition?.LayoutId is not { } layoutId)
        {
            return;
        }
        SpriteLayout layout = RequireServices().Layouts[layoutId];
        sprite.SpriteWidth = layout.FrameWidth;
        sprite.SpriteHeight = layout.FrameHeight;
    }

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
        GroupId = source.GroupId;
        Role = source.Role;
        Stage = source.Stage;
        bool continuingMotion = IsReplica && State == source.State && State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating or BehaviorStatus.Following or BehaviorStatus.SwimmingAway;
        if (!IsReplica)
        {
            position = source.position;
        }
        _replicaTarget = source.position;
        startingPosition = source.startingPosition;
        bool pendingVisual = IsReplica && _playedIdleRevision == IdleRevision && source.IdleRevision == IdleRevision;
        bool startsFleeing = IsReplica && State != source.State && source.State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating or BehaviorStatus.SwimmingAway;
        bool startsAlert = State != source.State;
        if (!pendingVisual)
        {
            State = source.State;
            flip = source.flip;
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
            Relocations = source.Relocations;
            RelocateTarget = source.RelocateTarget;
            ApplyReplicaExtras(source);
            if (startsFleeing && Services != null)
            {
                PlayActionSound(ReplicaFleeAction);
            }
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
        Func<string, bool> permitted = allowed ?? (_ => true);
        allowed = id => permitted(id) && !LockBlocked(id);
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
                if (routine.Choose(_scheduleRandom, allowed, Services?.World?.Invoke(), Role) is not { } picked)
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
        if (custom.Exclusive is { } lockName && !IsReplica && !TryTakeLock(lockName, custom.ExclusiveCooldownMs))
        {
            return BirdAction.None;
        }
        PlayActionSound(custom);
        StartCustomAction(custom);
        return BirdAction.None;
    }

    private string? _heldLock;
    private int _heldCooldownMs;

    private static double NowMs => Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;

    private string? LockKey(string lockName) => CurrentLocation is { } location && Services?.Locks != null ? location.NameOrUniqueName + "/" + lockName : null;

    private bool LockBlocked(string id) =>
        !BirdActionIds.TryParse(id, out _) && Action(id).Exclusive is { } lockName && LockKey(lockName) is { } key && Services!.Locks!.IsBlocked(key, NowMs);

    private bool TryTakeLock(string lockName, int cooldownMs)
    {
        if (LockKey(lockName) is not { } key)
        {
            return true;
        }
        if (!Services!.Locks!.TryAcquire(key, BirdID, NowMs))
        {
            return false;
        }
        _heldLock = lockName;
        _heldCooldownMs = cooldownMs;
        return true;
    }

    private void ReleaseLock()
    {
        if (_heldLock is { } lockName && LockKey(lockName) is { } key)
        {
            Services!.Locks!.Release(key, BirdID, NowMs, _heldCooldownMs);
        }
        _heldLock = null;
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
        if (State is BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating or BehaviorStatus.SwimmingAway)
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

    /// <summary>The flock rules of the rule's reactions, or null when the bird has none.</summary>
    internal Cohesion? Cohesion => (Composition?.Reactions ?? Profile.Reactions)?.Cohesion;

    /// <summary>Where this bird last chose to escape to (an escape target or <see cref="EscapeOption.Offscreen"/>); informed mates try it first.</summary>
    internal string? EscapeOutcome { get; private set; }

    private int _alarmDelayMs;
    private Character? _alarmThreat;
    private BirdCritter? _alarmSource;
    private int _alertCalmMs;
    private double _lastThreatMs;
    private double _rejoinWaitMs = -1;
    private bool _rejoinDone;

    private static bool IsEscaping(BehaviorStatus state) =>
        state is BehaviorStatus.FlyingAway or BehaviorStatus.Relocating or BehaviorStatus.Running or BehaviorStatus.SwimmingAway or BehaviorStatus.Diving or BehaviorStatus.Hiding;

    /// <summary>Records the escape choice so informed mates can follow the same outcome.</summary>
    protected EscapeChoice? Record(EscapeChoice? choice)
    {
        EscapeOutcome = choice?.Option.Target ?? EscapeOption.Offscreen;
        return choice;
    }

    /// <summary>The trigger's threat once this bird's alarm delay has run out, else null.</summary>
    protected Character? TakeAlarm(GameTime time)
    {
        if (_alarmThreat is null)
        {
            return null;
        }
        _alarmDelayMs -= time.ElapsedGameTime.Milliseconds;
        if (_alarmDelayMs > 0)
        {
            return null;
        }
        Character threat = _alarmThreat;
        _alarmThreat = null;
        return threat;
    }

    private List<BirdCritter> FlockMembers(GameLocation location)
    {
        List<BirdCritter> members = new();
        if (GroupId is { } groupId && location.critters is { } critters)
        {
            foreach (Critter critter in critters)
            {
                if (critter is BirdCritter bird && !bird.IsReplica && bird.GroupId == groupId && bird.BirdName == BirdName)
                {
                    members.Add(bird);
                }
            }
            members.Sort((a, b) => a.BirdID.CompareTo(b.BirdID));
        }
        return members;
    }

    private bool NearFarmer(GameLocation location, int tiles)
    {
        foreach (Farmer farmer in location.farmers)
        {
            if (Vector2.Distance(farmer.Position, position) / 64f <= tiles)
            {
                return true;
            }
        }
        return false;
    }

    private void RaiseAlarm(GameLocation location, Character threat, Cohesion cohesion, double now)
    {
        List<BirdCritter> members = FlockMembers(location);
        int self = members.IndexOf(this);
        if (self < 0)
        {
            return;
        }
        List<AlarmMember> view = new(members.Count);
        foreach (BirdCritter bird in members)
        {
            bool available = bird == this || (bird.Cohesion != null && bird._alarmThreat == null && !IsEscaping(bird.State)
                && bird.NearFarmer(location, bird.Threat.FleeRadius + cohesion.LinkRadius));
            view.Add(new AlarmMember(bird.BirdID, bird.OwnTile, available));
        }
        foreach ((int index, int delayMs) in GroupAlarm.Plan(view, self, cohesion, (long)now))
        {
            members[index]._alarmThreat = threat;
            members[index]._alarmSource = this;
            members[index]._alarmDelayMs = delayMs;
        }
    }

    /// <summary>The flock's view for an escape plan, or null without flock rules or <c>spread</c>; consumes the pending alarm source.</summary>
    protected FlockHints? Flock(GameLocation location, Vector2 threatTile)
    {
        BirdCritter? source = _alarmSource;
        _alarmSource = null;
        if (Cohesion is not { Spread: > 0 } cohesion)
        {
            return null;
        }
        Vector2 sum = Vector2.Zero;
        int count = 0;
        List<Vector2> anchors = new();
        foreach (BirdCritter bird in FlockMembers(location))
        {
            if (bird != this && Vector2.Distance(bird.OwnTile, OwnTile) > cohesion.LinkRadius * 2)
            {
                continue;
            }
            sum += bird.OwnTile;
            count++;
            if (bird != this && bird.State == BehaviorStatus.Relocating && bird.RelocateTarget is { } target)
            {
                anchors.Add(target);
            }
        }
        return new FlockHints(cohesion, threatTile, count == 0 ? OwnTile : sum / count, source?.EscapeOutcome, anchors);
    }

    /// <summary>Once per calm period, after the flock's <c>rejoinDelayMs</c> of calm, a tile near the nearest mate when this bird has drifted beyond twice the link radius; null otherwise or when <paramref name="calm"/> is false.</summary>
    protected Vector2? Rejoin(GameTime time, GameLocation location, bool calm)
    {
        if (!calm || _rejoinDone || Cohesion is not { Regroup: true, Rally: > 0 } cohesion)
        {
            _rejoinWaitMs = -1;
            return null;
        }
        if (_rejoinWaitMs < 0)
        {
            _rejoinWaitMs = Rng.Next(cohesion.RejoinDelayMs.Min, cohesion.RejoinDelayMs.Max + 1);
        }
        _rejoinWaitMs -= time.ElapsedGameTime.Milliseconds;
        if (_rejoinWaitMs > 0)
        {
            return null;
        }
        _rejoinWaitMs = -1;
        _rejoinDone = true;
        BirdCritter? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (BirdCritter bird in FlockMembers(location))
        {
            float distance = Vector2.Distance(bird.OwnTile, OwnTile);
            if (bird != this && !IsEscaping(bird.State) && distance < nearestDistance)
            {
                nearest = bird;
                nearestDistance = distance;
            }
        }
        if (nearest is null || nearestDistance <= cohesion.LinkRadius * 2)
        {
            return null;
        }
        return EscapePlanner.Rejoin(new GameLocationQuery(location), OwnTile, nearest.OwnTile, Threat.FleeRadius, cohesion.Rally, OccupiedTiles(location), Rng);
    }

    /// <summary>At most every 200 ms: the threat to flee from, or whether one is inside the alert ring.</summary>
    protected (Character? Flee, bool Alerted) PollThreats(GameTime time, GameLocation location) =>
        TakeAlarm(time) is { } alarm ? (alarm, false) : CheckIntervalElapsed(time) ? ScanThreats(location, Threat) : (null, false);

    /// <summary>Farmers within the flee radius (running ones count closer) or any other character in it; matches <see cref="FindThreat"/> for default reactions.</summary>
    protected (Character? Flee, bool Alerted) ScanThreats(GameLocation location, ThreatReaction threat)
    {
        (Character? Flee, bool Alerted) result = FindThreats(location, threat);
        double now = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        result = FleeAfterAlert(location, threat, result, now);
        if (result.Flee != null && !IsReplica && !IsEscaping(State) && Cohesion is { Spread: > 0 } cohesion)
        {
            RaiseAlarm(location, result.Flee, cohesion, now);
        }
        if (result.Flee != null || result.Alerted)
        {
            _rejoinDone = false;
            _rejoinWaitMs = -1;
        }
        if (result.Flee != null || result.Alerted || Relocations == 0)
        {
            _lastThreatMs = now;
        }
        else if (now - _lastThreatMs >= RelocationCalmMs)
        {
            Relocations = 0;
        }
        return result;
    }

    private double _alertedSinceMs = -1;

    private (Character? Flee, bool Alerted) FleeAfterAlert(GameLocation location, ThreatReaction threat, (Character? Flee, bool Alerted) result, double now)
    {
        if (!result.Alerted || threat.Alert is not { FleeAfterMs: { } fleeAfter } alert)
        {
            _alertedSinceMs = -1;
            return result;
        }
        if (_alertedSinceMs < 0)
        {
            _alertedSinceMs = now;
        }
        else if (now - _alertedSinceMs >= fleeAfter
            && Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, alert.Radius, location) is { } watcher)
        {
            _alertedSinceMs = -1;
            return (watcher, false);
        }
        return result;
    }

    private (Character? Flee, bool Alerted) FindThreats(GameLocation location, ThreatReaction threat)
    {
        int alertRadius = threat.Alert?.Radius ?? 0;
        bool alerted = false;
        foreach (Farmer farmer in location.farmers)
        {
            float tiles = Vector2.Distance(farmer.Position, position) / 64f;
            if (tiles <= DiscoveryRadiusTiles && Utility.isOnScreen(position, 64))
            {
                AddBirdObservation(BirdName, location.Name);
            }
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

    /// <summary>Runs the vanilla update and reports a bird that flew or swam off the map as departed.</summary>
    protected bool UpdateAndReportDeparture(GameTime time, GameLocation environment)
    {
        bool remove = base.update(time, environment);
        if (_heldLock != null && (remove || State is not (BehaviorStatus.Stopped or BehaviorStatus.Swimming) || sprite.CurrentAnimation == null))
        {
            ReleaseLock();
        }
        if (remove && !IsReplica && State is BehaviorStatus.FlyingAway or BehaviorStatus.SwimmingAway or BehaviorStatus.Running)
        {
            Services?.Departed?.Invoke(environment.Name, BirdID);
        }
        return remove;
    }

    /// <summary>Tiles of the other birds in the location; relocations never land on them.</summary>
    protected HashSet<Vector2> OccupiedTiles(GameLocation location)
    {
        HashSet<Vector2> occupied = new();
        if (location.critters == null)
        {
            return occupied;
        }
        foreach (Critter critter in location.critters)
        {
            if (critter is BirdCritter bird && bird != this)
            {
                occupied.Add(new Vector2((int)(bird.position.X / 64f), (int)(bird.position.Y / 64f)));
            }
        }
        return occupied;
    }

    protected Vector2 OwnTile => new((int)(position.X / 64f), (int)(position.Y / 64f));

    /// <summary>Starts the relocation flight to <paramref name="target"/> (world position) after the caller has played sounds and set state.</summary>
    protected void BeginRelocation(Vector2 tile, Vector2 target, GameLocation environment, bool counted = true)
    {
        AddBirdObservation(BirdName, environment.Name);
        if (counted)
        {
            Relocations++;
        }
        RelocateTarget = tile;
        RelocateFrom = position;
        flip = target.X > position.X;
    }

    /// <summary>Tiles a retargeted flight must avoid; trunk birds override with their own occupancy.</summary>
    protected virtual HashSet<Vector2> RetargetOccupied(GameLocation location) => OccupiedTiles(location);

    /// <summary>Owner side, every 300 ms and at most twice per flight: the farmer to flee from when the flight target became unsafe, else null.</summary>
    protected Farmer? RetargetThreat(GameTime time, GameLocation location)
    {
        _retargetTimerMs -= time.ElapsedGameTime.Milliseconds;
        if (_retargetTimerMs > 0 || IsReplica || _retargets >= MaxRetargets || RelocateTarget is not { } target)
        {
            return null;
        }
        _retargetTimerMs = RetargetIntervalMs;
        if (!EscapePlanner.NeedsRetarget(new GameLocationQuery(location), OwnTile, target, Threat.FleeRadius, RetargetOccupied(location)))
        {
            return null;
        }
        Farmer? nearest = null;
        foreach (Farmer farmer in location.farmers)
        {
            if (nearest == null || Vector2.DistanceSquared(farmer.Position, position) < Vector2.DistanceSquared(nearest.Position, position))
            {
                nearest = farmer;
            }
        }
        if (nearest != null)
        {
            _retargets++;
        }
        return nearest;
    }

    /// <summary>The velocity towards <paramref name="target"/>, capped at <paramref name="speed"/>.</summary>
    protected Vector2 Step(Vector2 target, float speed)
    {
        Vector2 delta = target - position;
        return delta.Length() <= speed ? delta : Vector2.Normalize(delta) * speed;
    }

    /// <summary>Moves one step along the relocation arc; true on arrival, with the arc and target cleared.</summary>
    protected bool AdvanceRelocation(Vector2 target, float speed)
    {
        position += Step(target, speed);
        float total = Vector2.Distance(RelocateFrom, target);
        yOffset = total <= 0f ? 0f : EscapePlanner.ArcOffset(1f - Vector2.Distance(position, target) / total, RelocateArcHeight);
        if (position != target)
        {
            return false;
        }
        yOffset = 0f;
        RelocateTarget = null;
        _retargets = 0;
        return true;
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
