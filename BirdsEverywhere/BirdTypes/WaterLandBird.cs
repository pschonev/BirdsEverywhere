using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal sealed class WaterLandBird : BirdCritter
{
    private const int DefaultDiveRadius = 3;

    private IReadOnlyList<IBirdBehavior>? _behaviors;
    private int? _walkTimer;
    private string? _followRole;
    private float _followSpeedFactor = 1f;
    private Vector2 _swimDirection;
    private ActionDefinition? _activeFlee;
    private ThreatAlternative? _selectedThreat;

    public override string BirdTypeName { get; } = "WaterLandBird";
    public bool Hidden { get; set; }
    public int DiveTimer { get; set; }
    public int DiveRadius { get; set; }
    internal string? FollowRole => _followRole;
    internal Vector2 SwimDirection => _swimDirection;
    protected override ActionDefinition ReplicaFleeAction => _activeFlee ?? Action("flee");

    public WaterLandBird() { }

    internal override BirdState CaptureExtras(BirdState state) => state with
    {
        Hidden = Hidden, DiveTimer = DiveTimer, DiveRadius = DiveRadius, FollowRole = _followRole, FollowSpeedFactor = _followSpeedFactor,
        SwimDirection = _swimDirection, ActiveFlee = _activeFlee,
    };

    internal override void RestoreExtras(BirdState state)
    {
        Hidden = state.Hidden;
        DiveTimer = state.DiveTimer;
        DiveRadius = state.DiveRadius;
        _followRole = state.FollowRole;
        _followSpeedFactor = state.FollowSpeedFactor;
        _swimDirection = state.SwimDirection;
        _activeFlee = state.ActiveFlee;
    }

    protected override void ApplyReplicaExtras(BirdCritter source)
    {
        if (source is not WaterLandBird owner)
        {
            return;
        }
        if (Hidden && !owner.Hidden)
        {
            position = owner.position;
            Splash(position);
        }
        Hidden = owner.Hidden;
        DiveTimer = owner.DiveTimer;
        DiveRadius = owner.DiveRadius;
        _followRole = owner._followRole;
        _followSpeedFactor = owner._followSpeedFactor;
        _swimDirection = owner._swimDirection;
        _activeFlee = owner._activeFlee;
    }

    public override void draw(SpriteBatch b)
    {
        if (!Hidden)
        {
            base.draw(b);
        }
    }

    public WaterLandBird(int tileX, int tileY, string birdName, string texture, BirdServices services, BehaviorStatus state = BehaviorStatus.Stopped)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        flip = Rng.NextDouble() < 0.5;
        startingPosition = position;
        State = state;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        CurrentLocation = environment;
        ActionDefinition flee = _activeFlee ?? Action("flee");
        if (IsReplica)
        {
            ApplyReplicaIdle(AdvanceReplicaIdle());
            return UpdateReplica(time, State == BehaviorStatus.SwimmingAway ? _swimDirection * flee.Speed!.Value
                : State == BehaviorStatus.FlyingAway ? new Vector2(flip ? flee.Speed!.Value : -flee.Speed!.Value, 0f) : default,
                State == BehaviorStatus.FlyingAway ? -flee.Climb!.Value : 0f);
        }

        (Character? threat, bool alerted) = PollFamilyThreats(time, environment);
        BirdCritter? fleeingMember = threat is null ? FleeingGroupMember(environment) : null;
        Vector2? groupThreat = fleeingMember?.position;
        if (State == BehaviorStatus.Stopped && threat == null && groupThreat is null && IsOnWater(environment))
        {
            State = BehaviorStatus.Swimming;
        }
        BirdContext context = new(State, threat != null || groupThreat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng, alerted, TickCalm(time, alerted));
        bool swimming = State == BehaviorStatus.Swimming;
        bool idle = (State == BehaviorStatus.Stopped || (swimming && IdleRoutine.HasOptions(IsDive))) && threat == null && groupThreat is null && !alerted;
        BirdAction idleAction = AdvanceIdle(IdleRoutine, idle && context.AnimationIdle && !context.Airborne, swimming ? IsDive : id => !IsDive(id));
        _behaviors ??= BirdBehaviors.WaterLand(Action("walk").EndChance);
        Apply(idle ? idleAction : BirdBehaviors.Decide(_behaviors, context), threat?.Position ?? groupThreat, environment);
        MatchGroupFlee(fleeingMember);

        switch (State)
        {
            case BehaviorStatus.Walking:
                Walk(time, environment);
                break;
            case BehaviorStatus.Swimming:
                Swim(time, environment);
                break;
            case BehaviorStatus.Following:
                Follow(time, environment);
                break;
            case BehaviorStatus.SwimmingAway:
                Vector2 next = position + _swimDirection * flee.Speed!.Value;
                if (environment.doesTileHaveProperty((int)(next.X / 64), (int)(next.Y / 64), "Water", "Back") != null)
                {
                    position = next;
                    Swim(time, environment);
                }
                else
                {
                    State = BehaviorStatus.Swimming;
                }
                break;
            case BehaviorStatus.FlyingAway:
                position.X += flip ? flee.Speed!.Value : -flee.Speed!.Value;
                yOffset -= flee.Climb!.Value;
                break;
            case BehaviorStatus.Stopped:
                if (sprite.CurrentAnimation == null)
                {
                    ShowPose("stand");
                }
                break;
            case BehaviorStatus.Alert:
                ShowAlertPose();
                break;
            case BehaviorStatus.Diving when Hidden:
                DiveTimer -= time.ElapsedGameTime.Milliseconds;
                if (DiveTimer <= 0)
                {
                    Resurface(environment);
                }
                break;
        }
        return base.update(time, environment);
    }

    /// <summary>Water-only custom actions are picked while swimming, all others while standing.</summary>
    private bool IsDive(string id) => !BirdActionIds.TryParse(id, out _) && Action(id).Do is "dive" or "follow";

    private (Character? Flee, bool Alerted) PollFamilyThreats(GameTime time, GameLocation environment)
    {
        if (Composition?.Reactions?.Alternatives is null)
        {
            _selectedThreat = null;
            return PollThreats(time, environment);
        }
        if (!CheckIntervalElapsed(time))
        {
            return (null, false);
        }
        List<string> roles = new();
        if (GroupId is { } groupId && environment.critters is { } critters)
        {
            foreach (var critter in critters)
            {
                if (critter is BirdCritter member && member.GroupId == groupId && member.BirdName == BirdName && member.Role is { } role)
                {
                    roles.Add(role);
                }
            }
        }
        IdleWorld? world = Services?.World?.Invoke();
        ReactionContext context = new(roles, IsOnWater(environment), world?.Season ?? Domain.Season.Spring,
            world?.Weather ?? Weather.Sun, world?.Time ?? 600, Stage);
        _selectedThreat = Composition.Reactions.Select(context);
        return ScanThreats(environment, _selectedThreat.Threat);
    }

    protected override void StartCustomAction(ActionDefinition action)
    {
        if (action.Do == "follow")
        {
            _followRole = action.Target!["groupRole:".Length..];
            _followSpeedFactor = action.SpeedFactor!.Value;
            State = BehaviorStatus.Following;
            PlayClip(Action("swim").Clip!, VisualRng);
            return;
        }
        if (action.Do != "dive")
        {
            base.StartCustomAction(action);
            return;
        }
        State = BehaviorStatus.Diving;
        Hidden = false;
        DiveRadius = action.Radius ?? DefaultDiveRadius;
        DiveTimer = IsReplica ? 0 : action.HiddenMs!.Value.Roll(Rng);
        PlayClip(action.Clip!, VisualRng, Submerge);
    }

    private void Submerge(Farmer who)
    {
        Hidden = true;
        sprite.CurrentAnimation = null;
    }

    /// <summary>Surfaces on a random water tile within the dive radius (or in place), owner RNG.</summary>
    private void Resurface(GameLocation environment)
    {
        Point here = new((int)(position.X / 64), (int)(position.Y / 64));
        List<Point> water = new();
        for (int dy = -DiveRadius; dy <= DiveRadius; dy++)
        {
            for (int dx = -DiveRadius; dx <= DiveRadius; dx++)
            {
                Point tile = new(here.X + dx, here.Y + dy);
                if (tile != here && environment.doesTileHaveProperty(tile.X, tile.Y, "Water", "Back") != null)
                {
                    water.Add(tile);
                }
            }
        }
        if (water.Count > 0)
        {
            Point target = water[Rng.Next(water.Count)];
            position = new Vector2(target.X * 64f, target.Y * 64f);
        }
        Hidden = false;
        DiveTimer = 0;
        State = BehaviorStatus.Swimming;
        Splash(position);
    }

    private void Apply(BirdAction action, Vector2? threatPosition, GameLocation environment)
    {
        switch (action)
        {
            case BirdAction.Flee:
                StartFlee(threatPosition!.Value, environment);
                break;
            case BirdAction.Alert:
                StartAlert(environment);
                break;
            case BirdAction.Stop:
                State = BehaviorStatus.Stopped;
                sprite.loop = false;
                sprite.CurrentAnimation = null;
                ShowPose("stand");
                break;
            case BirdAction.Peck:
                StartPeck();
                break;
            case BirdAction.Rest:
                StartRest();
                break;
            case BirdAction.Hop:
                StartHop();
                break;
            case BirdAction.Walk:
                StartWalk();
                break;
        }
    }

    private void ApplyReplicaIdle(BirdAction action)
    {
        switch (action)
        {
            case BirdAction.Peck:
                StartPeck();
                break;
            case BirdAction.Rest:
                StartRest();
                break;
            case BirdAction.Hop:
                StartHop();
                break;
            case BirdAction.Walk:
                StartWalk();
                break;
        }
    }

    private BirdCritter? FleeingGroupMember(GameLocation environment)
    {
        if (GroupId is not { } groupId || environment.critters is not { } critters)
        {
            return null;
        }
        foreach (var critter in critters)
        {
            if (critter is BirdCritter member && member != this && member.GroupId == groupId && member.BirdName == BirdName
                && member.State is BehaviorStatus.FlyingAway or BehaviorStatus.SwimmingAway)
            {
                return member;
            }
        }
        return null;
    }

    internal void MatchGroupFlee(BirdCritter? member)
    {
        if (member is WaterLandBird { State: BehaviorStatus.SwimmingAway } leader && State == BehaviorStatus.SwimmingAway)
        {
            _swimDirection = leader.SwimDirection;
            flip = _swimDirection.X > 0;
        }
    }

    private void StartFlee(Vector2 threatPosition, GameLocation environment)
    {
        ActionDefinition flee = Action("flee");
        if (_selectedThreat?.Flee is { } selected)
        {
            if (selected.Do == "swimAway" && IsOnWater(environment))
            {
                flee = flee with { Do = "swimAway", Clip = Action("swim").Clip, Sound = selected.Sound ?? flee.Sound };
            }
            else
            {
                ActionDefinition flight = flee.Do == "flyOff" ? flee : Services!.Actions["waterland_actions"]["flee"];
                flee = flight with { Sound = selected.Sound ?? flee.Sound };
            }
        }
        else if (flee.Do == "swimAway" && !IsOnWater(environment))
        {
            flee = Services!.Actions["waterland_actions"]["flee"];
        }
        _activeFlee = flee;
        PlayActionSound(flee);
        AddBirdObservation(BirdName, environment.Name);
        if (flee.Do == "swimAway")
        {
            Vector2 difference = position - threatPosition;
            _swimDirection = difference.LengthSquared() > 0 ? Vector2.Normalize(difference) : new Vector2(flip ? 1f : -1f, 0f);
            State = BehaviorStatus.SwimmingAway;
            flip = _swimDirection.X > 0;
            PlayClip(flee.Clip!, VisualRng);
            return;
        }
        State = BehaviorStatus.FlyingAway;
        flip = threatPosition.X <= position.X;
        StartFlyAnimation();
    }

    internal void StartFlyAnimation() => PlayClip((_activeFlee ?? Action("flee")).Clip!, VisualRng);

    internal void StartRest() => PlayClip(Action("rest").Clip!, VisualRng);

    internal void StartPeck() => PlayClip(Action("peck").Clip!, VisualRng);

    internal void StartHop() => PlayClip(Action("hop").Clip!, VisualRng);

    internal void StartWalk()
    {
        ActionDefinition walk = Action("walk");
        State = BehaviorStatus.Walking;
        PlayClip(walk.Clip!, VisualRng);
        flip = VisualRng.NextDouble() < 0.5;
        if (VisualRng.NextDouble() < 0.33)
        {
            flip = position.X <= startingPosition.X;
        }
        _walkTimer = walk.DurationMs?.Roll(VisualRng);
    }

    private void Walk(GameTime time, GameLocation environment)
    {
        float speed = Action("walk").Speed!.Value;
        if (!flip && !IsBlocked(environment, -(int)speed))
        {
            position.X -= speed;
        }
        else if (flip && !IsBlocked(environment, (int)speed))
        {
            position.X += speed;
        }
        if (_walkTimer is { } remaining)
        {
            _walkTimer = remaining - time.ElapsedGameTime.Milliseconds;
            if (_walkTimer < 0)
            {
                Apply(BirdAction.Stop, null, environment);
            }
        }
    }

    private void Swim(GameTime time, GameLocation environment)
    {
        ShowPose(Action("swim").Clip!);
        float previousYOffset = yOffset;
        yOffset = (time.TotalGameTime.TotalMilliseconds + ((int)position.X * 4)) % 2000.0 < 1000.0 ? 2f : 0f;
        if (yOffset > previousYOffset)
        {
            environment.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 150f, 8, 0, new Vector2(position.X - 32f, position.Y - 32f), flicker: false, Rng.NextDouble() < 0.5, 0.001f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
        }
    }

    private void Follow(GameTime time, GameLocation environment)
    {
        BirdCritter? leader = null;
        if (GroupId is { } groupId && _followRole is { } role && environment.critters is { } critters)
        {
            foreach (var critter in critters)
            {
                if (critter is BirdCritter candidate && candidate != this && candidate.GroupId == groupId && candidate.BirdName == BirdName && candidate.Role == role)
                {
                    leader = candidate;
                    break;
                }
            }
        }
        if (leader is null)
        {
            State = BehaviorStatus.Swimming;
            return;
        }
        Vector2 difference = leader.position - position;
        float distance = difference.Length();
        if (distance > 64f)
        {
            float speed = Action("walk").Speed!.Value * _followSpeedFactor;
            Vector2 next = position + Vector2.Normalize(difference) * Math.Min(speed, distance - 64f);
            if (environment.doesTileHaveProperty((int)(next.X / 64), (int)(next.Y / 64), "Water", "Back") != null)
            {
                flip = difference.X > 0;
                position = next;
            }
        }
        Swim(time, environment);
    }

    private bool IsOnWater(GameLocation environment) =>
        environment.doesTileHaveProperty((int)(position.X / 64), (int)(position.Y / 64), "Water", "Back") != null;

    private bool IsBlocked(GameLocation environment, int dx) =>
        environment.isCollidingPosition(getBoundingBox(dx, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true);

    private void Hop(Farmer who)
    {
        gravityAffectedDY = -Action("hop").Impulse!.Value;
    }

    protected override AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) =>
        name == "hop" ? Hop : base.ClipEvent(name);
}
