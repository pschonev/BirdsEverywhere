using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal sealed class LandBird : BirdCritter
{
    private IReadOnlyList<IBirdBehavior>? _behaviors;

    public override string BirdTypeName { get; } = "LandBird";
    public float FlightOffset { get; set; }
    public int WalkTimer { get; set; }

    public LandBird() { }

    internal override BirdState CaptureExtras(BirdState state) => state with { FlightOffset = FlightOffset, WalkTimer = WalkTimer };

    internal override void RestoreExtras(BirdState state)
    {
        FlightOffset = state.FlightOffset;
        WalkTimer = state.WalkTimer;
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
            return UpdateReplica(time, State == BehaviorStatus.FlyingAway ? new Vector2(flip ? flee.Speed!.Value : -flee.Speed!.Value, 0f) : default,
                State == BehaviorStatus.FlyingAway ? -flee.Climb!.Value - FlightOffset : 0f);
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
                StartFlee(threat!, environment);
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
