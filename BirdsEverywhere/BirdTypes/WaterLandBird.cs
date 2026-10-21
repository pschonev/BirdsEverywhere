using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal sealed class WaterLandBird : BirdCritter
{
    private const int ThreatRadiusTiles = 4;

    public override string BirdTypeName { get; } = "WaterLandBird";

    public WaterLandBird() { }

    public WaterLandBird(int tileX, int tileY, string birdName, string texture, BirdServices services, BehaviorStatus state = BehaviorStatus.Stopped)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        flip = Rng.NextDouble() < 0.5;
        startingPosition = position;
        State = state;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        if (IsReplica)
        {
            ApplyReplicaIdle(AdvanceReplicaIdle());
            return UpdateReplica(time, State == BehaviorStatus.FlyingAway ? new Vector2(flip ? 4f : -4f, 0f) : default,
                State == BehaviorStatus.FlyingAway ? -2f : 0f);
        }

        Character? threat = PollThreat(time, environment, ThreatRadiusTiles);
        if (State == BehaviorStatus.Stopped && threat == null && IsOnWater(environment))
        {
            State = BehaviorStatus.Swimming;
        }
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        BirdAction idleAction = AdvanceIdle(IdleRoutine, State == BehaviorStatus.Stopped && threat == null && context.AnimationIdle && !context.Airborne);
        Apply(State == BehaviorStatus.Stopped && threat == null ? idleAction : BirdBehaviors.Decide(BirdBehaviors.WaterLand, context), threat, environment);

        switch (State)
        {
            case BehaviorStatus.Walking:
                Walk(environment);
                break;
            case BehaviorStatus.Swimming:
                Swim(time, environment);
                break;
            case BehaviorStatus.FlyingAway:
                position.X += flip ? 4f : -4f;
                yOffset -= 2f;
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

    private void StartFlee(Character threat, GameLocation environment)
    {
        if (Rng.NextDouble() < 0.25)
        {
            PlaySound("alarm");
        }
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = threat.Position.X <= position.X;
        StartFlyAnimation();
    }

    internal void StartFlyAnimation() => PlayClip("fly", VisualRng);

    internal void StartRest() => PlayClip("rest", VisualRng);

    internal void StartPeck() => PlayClip("peck", VisualRng);

    internal void StartHop() => PlayClip("hop", VisualRng);

    internal void StartWalk()
    {
        State = BehaviorStatus.Walking;
        PlayClip("walk", VisualRng);
        flip = VisualRng.NextDouble() < 0.5;
        if (VisualRng.NextDouble() < 0.33)
        {
            flip = position.X <= startingPosition.X;
        }
    }

    private void Walk(GameLocation environment)
    {
        if (!flip && !IsBlocked(environment, -2))
        {
            position.X -= 2f;
        }
        else if (flip && !IsBlocked(environment, 2))
        {
            position.X += 2f;
        }
    }

    private void Swim(GameTime time, GameLocation environment)
    {
        ShowPose("swim");
        float previousYOffset = yOffset;
        yOffset = (time.TotalGameTime.TotalMilliseconds + ((int)position.X * 4)) % 2000.0 < 1000.0 ? 2f : 0f;
        if (yOffset > previousYOffset)
        {
            environment.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 150f, 8, 0, new Vector2(position.X - 32f, position.Y - 32f), flicker: false, Rng.NextDouble() < 0.5, 0.001f, 0.01f, Color.White, 1f, 0.003f, 0f, 0f));
        }
    }

    private bool IsOnWater(GameLocation environment) =>
        environment.doesTileHaveProperty((int)(position.X / 64), (int)(position.Y / 64), "Water", "Back") != null;

    private bool IsBlocked(GameLocation environment, int dx) =>
        environment.isCollidingPosition(getBoundingBox(dx, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true);

    private void Hop(Farmer who)
    {
        gravityAffectedDY = -4f;
    }

    protected override AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) =>
        name == "hop" ? Hop : base.ClipEvent(name);
}
