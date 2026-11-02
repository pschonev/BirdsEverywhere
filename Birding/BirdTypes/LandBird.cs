using Birding.BirdTypes.Behaviors;
using Birding.Domain;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Birding.BirdTypes;

internal sealed class LandBird : BirdCritter
{
    private const float ArcHeight = 48f;
    private IReadOnlyList<IBirdBehavior>? _behaviors;
    private Vector2 _relocateFrom;

    public override string BirdTypeName { get; } = "LandBird";
    public float FlightOffset { get; set; }
    public int WalkTimer { get; set; }
    public int Relocations { get; set; }
    public Vector2? RelocateTarget { get; set; }

    public LandBird() { }

    internal override BirdState CaptureExtras(BirdState state) => state with { FlightOffset = FlightOffset, WalkTimer = WalkTimer, Relocations = Relocations, RelocateTarget = RelocateTarget };

    internal override void RestoreExtras(BirdState state)
    {
        FlightOffset = state.FlightOffset;
        WalkTimer = state.WalkTimer;
        Relocations = state.Relocations;
        RelocateTarget = state.RelocateTarget;
        _relocateFrom = position;
    }

    protected override void ApplyReplicaExtras(BirdCritter source)
    {
        if (source is LandBird owner)
        {
            Relocations = owner.Relocations;
            RelocateTarget = owner.RelocateTarget;
        }
    }

    public LandBird(int tileX, int tileY, string birdName, string texture, BirdServices services, BehaviorStatus state = BehaviorStatus.Pecking)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        flip = Rng.NextDouble() < 0.5;
        position.X += 32f;
        position.Y += 32f;
        startingPosition = position;
        FlightOffset = (float)Rng.NextDouble() - 0.5f;

        State = state;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        CurrentLocation = environment;
        ActionDefinition flee = Action("flee");
        if (IsReplica)
        {
            ApplyReplicaIdle(AdvanceReplicaIdle());
            Vector2 velocity = State switch
            {
                BehaviorStatus.FlyingAway => new Vector2(flip ? flee.Speed!.Value : -flee.Speed!.Value, 0f),
                BehaviorStatus.Relocating when RelocateTarget is { } tile => Step(TilePosition(tile), flee.Speed!.Value),
                _ => default,
            };
            return UpdateReplica(time, velocity,
                State == BehaviorStatus.FlyingAway ? -flee.Climb!.Value - FlightOffset : 0f);
        }

        if (State == BehaviorStatus.Relocating)
        {
            Fly(flee.Speed!.Value);
            return base.update(time, environment);
        }

        if (yJumpOffset < 0f && State != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }

        (Character? threat, bool alerted) = PollThreats(time, environment);
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng, alerted, TickCalm(time, alerted));
        bool idle = State == BehaviorStatus.Stopped && threat == null && !alerted;
        BirdAction idleAction = AdvanceIdle(IdleRoutine, idle && context.AnimationIdle && !context.Airborne);
        _behaviors ??= BirdBehaviors.Land(Action("sleep").EndChance ?? 0, Action("walk").EndChance);
        Apply(idle ? idleAction : BirdBehaviors.Decide(_behaviors, context), threat, environment);

        switch (State)
        {
            case BehaviorStatus.Pecking:
                if (sprite.CurrentAnimation == null)
                {
                    StartPeckAnimation();
                }
                break;
            case BehaviorStatus.FlyingAway:
                position.X += flip ? flee.Speed!.Value : -flee.Speed!.Value;
                yOffset -= flee.Climb!.Value + FlightOffset;
                break;
            case BehaviorStatus.Sleeping:
                if (sprite.CurrentAnimation == null)
                {
                    ShowPose(Action("sleep").Clip!);
                }
                break;
            case BehaviorStatus.Walking:
                Walk(time, environment);
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
        }
        return base.update(time, environment);
    }

    private void Apply(BirdAction action, Character? threat, GameLocation environment)
    {
        switch (action)
        {
            case BirdAction.Flee:
                if (RelocationTarget(environment, threat!) is { } tile)
                {
                    StartRelocate(tile, environment);
                }
                else
                {
                    StartFlee(threat!, environment);
                }
                break;
            case BirdAction.Alert:
                StartAlert(environment);
                break;
            case BirdAction.Stop:
            case BirdAction.Sleep:
            case BirdAction.Peck:
                State = action switch
                {
                    BirdAction.Sleep => BehaviorStatus.Sleeping,
                    BirdAction.Peck => BehaviorStatus.Pecking,
                    _ => BehaviorStatus.Stopped,
                };
                break;
            case BirdAction.Hop:
                Hop(Action("hop"));
                break;
            case BirdAction.TurnAndHop:
                Hop(Action("turnAndHop"));
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
            case BirdAction.Sleep:
                State = BehaviorStatus.Sleeping;
                sprite.CurrentAnimation = null;
                ShowPose(Action("sleep").Clip!);
                break;
            case BirdAction.Peck:
                State = BehaviorStatus.Pecking;
                StartPeckAnimation();
                break;
            case BirdAction.Hop:
            case BirdAction.TurnAndHop:
                if (Action(action == BirdAction.Hop ? "hop" : "turnAndHop").Turn)
                {
                    flip = !flip;
                }
                break;
            case BirdAction.Walk:
                StartWalk();
                break;
        }
    }

    private void StartFlee(Character threat, GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = threat.Position.X <= position.X;
        StartFlyAnimation();
    }

    private static Vector2 TilePosition(Vector2 tile) => tile * 64f + new Vector2(32f, 32f);

    private Vector2 Step(Vector2 target, float speed)
    {
        Vector2 delta = target - position;
        return delta.Length() <= speed ? delta : Vector2.Normalize(delta) * speed;
    }

    private Vector2? RelocationTarget(GameLocation environment, Character threat)
    {
        if (Threat.Relocate is not { } relocate || Relocations >= relocate.MaxRelocations)
        {
            return null;
        }
        HashSet<Vector2> occupied = environment.critters?.OfType<BirdCritter>().Where(bird => bird != this)
            .Select(bird => new Vector2((int)(bird.position.X / 64f), (int)(bird.position.Y / 64f))).ToHashSet() ?? new();
        return Relocation.PickSurface(new Birding.Spawners.GameLocationQuery(environment), new Vector2((int)(position.X / 64f), (int)(position.Y / 64f)), threat.Tile, Threat, occupied, Rng);
    }

    private void StartRelocate(Vector2 tile, GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        AddBirdObservation(BirdName, environment.Name);
        Relocations++;
        RelocateTarget = tile;
        _relocateFrom = position;
        flip = TilePosition(tile).X > position.X;
        State = BehaviorStatus.Relocating;
        StartFlyAnimation();
    }

    private void Fly(float speed)
    {
        if (RelocateTarget is not { } tile)
        {
            State = BehaviorStatus.FlyingAway;
            return;
        }
        Vector2 target = TilePosition(tile);
        position += Step(target, speed);
        float total = Vector2.Distance(_relocateFrom, target);
        yOffset = total <= 0f ? 0f : Relocation.ArcOffset(1f - Vector2.Distance(position, target) / total, ArcHeight);
        if (position != target)
        {
            return;
        }
        yOffset = 0f;
        RelocateTarget = null;
        startingPosition = position;
        State = BehaviorStatus.Stopped;
        sprite.loop = false;
        sprite.CurrentAnimation = null;
        ShowPose("stand");
    }

    internal void StartFlyAnimation() => PlayClip(Action("flee").Clip!, VisualRng);

    internal void StartWalk()
    {
        ActionDefinition walk = Action("walk");
        State = BehaviorStatus.Walking;
        PlayClip(walk.Clip!, VisualRng);
        flip = position.X < startingPosition.X;
        WalkTimer = walk.DurationMs is { } duration ? duration.Roll(VisualRng) : int.MaxValue;
    }

    internal void StartPeckAnimation() => PlayClip(Action("peck").Clip!, VisualRng, DonePecking);

    private void HopDrift(GameLocation environment)
    {
        if (!flip && !IsBlocked(environment, -2))
        {
            position.X -= 2f;
        }
        else if (!IsBlocked(environment, 2))
        {
            position.X += 2f;
        }
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
        WalkTimer -= time.ElapsedGameTime.Milliseconds;
        if (WalkTimer < 0)
        {
            State = BehaviorStatus.Stopped;
            sprite.loop = false;
            sprite.CurrentAnimation = null;
            ShowPose("stand");
        }
    }

    private bool IsBlocked(GameLocation environment, int dx) =>
        environment.isCollidingPosition(getBoundingBox(dx, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true);

    private void Hop(ActionDefinition hop)
    {
        if (hop.Turn)
        {
            flip = !flip;
        }
        gravityAffectedDY = -hop.Impulse!.Value;
    }

    private void DonePecking(Farmer who)
    {
        State = IdleRoutine.After("peck", Rng) switch
        {
            "peck" => BehaviorStatus.Pecking,
            "sleep" => BehaviorStatus.Sleeping,
            _ => BehaviorStatus.Stopped,
        };
    }

    protected override AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) => name switch
    {
        "peck" => PlayPeck,
        "flap" => PlayFlap,
        _ => base.ClipEvent(name),
    };

    private void PlayFlap(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            PlaySound("flap");
        }
    }

    private void PlayPeck(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            PlaySound("peck");
        }
    }
}
