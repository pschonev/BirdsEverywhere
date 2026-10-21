using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal sealed class LandBird : BirdCritter
{
    private const int ThreatRadiusTiles = 4;

    public override string BirdTypeName { get; } = "LandBird";
    [JsonProperty("flightOffset")]
    public float FlightOffset { get; set; }
    [JsonProperty("walkTimer")]
    public int WalkTimer { get; set; }

    public LandBird() { }

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
        if (IsReplica)
        {
            ApplyReplicaIdle(AdvanceReplicaIdle());
            return UpdateReplica(time, State == BehaviorStatus.FlyingAway ? new Vector2(flip ? 6f : -6f, 0f) : default,
                State == BehaviorStatus.FlyingAway ? -2f - FlightOffset : 0f);
        }

        if (yJumpOffset < 0f && State != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }

        Character? threat = PollThreat(time, environment, ThreatRadiusTiles);
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        BirdAction idleAction = AdvanceIdle(IdleRoutine, State == BehaviorStatus.Stopped && threat == null && context.AnimationIdle && !context.Airborne);
        Apply(State == BehaviorStatus.Stopped && threat == null ? idleAction : BirdBehaviors.Decide(BirdBehaviors.Land, context), threat, environment);

        switch (State)
        {
            case BehaviorStatus.Pecking:
                if (sprite.CurrentAnimation == null)
                {
                    StartPeckAnimation();
                }
                break;
            case BehaviorStatus.FlyingAway:
                position.X += flip ? 6f : -6f;
                yOffset -= 2f + FlightOffset;
                break;
            case BehaviorStatus.Sleeping:
                if (sprite.CurrentAnimation == null)
                {
                    ShowPose("sleep");
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
                Hop();
                break;
            case BirdAction.TurnAndHop:
                flip = !flip;
                Hop();
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
                ShowPose("sleep");
                break;
            case BirdAction.Peck:
                State = BehaviorStatus.Pecking;
                StartPeckAnimation();
                break;
            case BirdAction.Hop:
            case BirdAction.TurnAndHop:
                if (action == BirdAction.TurnAndHop)
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
        if (Rng.NextDouble() < 0.85)
        {
            PlaySound("alarm");
        }
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = threat.Position.X <= position.X;
        StartFlyAnimation();
    }

    internal void StartFlyAnimation() => PlayClip("fly", VisualRng);

    internal void StartWalk()
    {
        State = BehaviorStatus.Walking;
        PlayClip("walk", VisualRng);
        flip = position.X < startingPosition.X;
        WalkTimer = VisualRng.Next(5, 15) * 100;
    }

    internal void StartPeckAnimation() => PlayClip("peck", VisualRng, DonePecking);

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
        if (!flip && !IsBlocked(environment, -1))
        {
            position.X -= 1f;
        }
        else if (flip && !IsBlocked(environment, 1))
        {
            position.X += 1f;
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

    private void Hop()
    {
        gravityAffectedDY = -2f;
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
