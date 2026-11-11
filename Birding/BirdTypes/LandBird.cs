using Birding.BirdTypes.Behaviors;
using Birding.Domain;
using Birding.Spawners;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Birding.BirdTypes;

internal sealed class LandBird : BirdCritter
{
    private IReadOnlyList<IBirdBehavior>? _behaviors;

    public override string BirdTypeName { get; } = "LandBird";
    public float FlightOffset { get; set; }
    public int WalkTimer { get; set; }
    public int HideTimer { get; set; }
    public Vector2? HideTree { get; set; }

    public LandBird() { }

    internal override BirdState CaptureExtras(BirdState state) => state with { FlightOffset = FlightOffset, WalkTimer = WalkTimer, HideTimer = HideTimer, AnchorTile = HideTree };

    internal override void RestoreExtras(BirdState state)
    {
        FlightOffset = state.FlightOffset;
        WalkTimer = state.WalkTimer;
        HideTimer = state.HideTimer;
        HideTree = state.AnchorTile;
    }

    public override void draw(SpriteBatch b)
    {
        if (State != BehaviorStatus.Hiding)
        {
            base.draw(b);
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
            return UpdateAndReportDeparture(time, environment);
        }

        if (State == BehaviorStatus.Hiding)
        {
            Hide(time, environment);
            return UpdateAndReportDeparture(time, environment);
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
        return UpdateAndReportDeparture(time, environment);
    }

    private void Apply(BirdAction action, Character? threat, GameLocation environment)
    {
        switch (action)
        {
            case BirdAction.Flee:
                if (RelocationTarget(environment, threat!) is { } choice)
                {
                    HideTree = choice.Option.Target == EscapeOption.Tree ? choice.Tile : null;
                    HideTimer = choice.Option.HideForMs;
                    StartRelocate(choice.Tile, environment, counted: true);
                }
                else
                {
                    StartFlee(threat!.Position.X <= position.X, environment);
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

    private void StartFlee(bool flyRight, GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = flyRight;
        StartFlyAnimation();
    }

    private static Vector2 TilePosition(Vector2 tile) => tile * 64f + new Vector2(32f, 32f);

    private EscapeChoice? RelocationTarget(GameLocation environment, Character threat) =>
        EscapePlanner.Surface(new GameLocationQuery(environment), OwnTile, threat.Tile, Threat, Relocations, OccupiedTiles(environment), Rng);

    private void StartRelocate(Vector2 tile, GameLocation environment, bool counted)
    {
        PlayActionSound(Action("flee"));
        BeginRelocation(tile, TilePosition(tile), environment, counted);
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
        if (!AdvanceRelocation(TilePosition(tile), speed))
        {
            return;
        }
        startingPosition = position;
        sprite.loop = false;
        sprite.CurrentAnimation = null;
        if (HideTree != null)
        {
            State = BehaviorStatus.Hiding;
            CurrentLocation?.localSound("leafrustle", OwnTile);
            return;
        }
        State = BehaviorStatus.Stopped;
        ShowPose("stand");
    }

    private void Hide(GameTime time, GameLocation environment)
    {
        Vector2 tree = HideTree!.Value;
        if (!TreeIntact(environment, tree))
        {
            HideTree = null;
            StartFlee(Rng.NextDouble() < 0.5, environment);
            return;
        }
        HideTimer -= time.ElapsedGameTime.Milliseconds;
        float watchRadius = Threat.FleeRadius;
        if (HideTimer > 0 || environment.farmers.Any(farmer => Vector2.Distance(farmer.Tile, tree) <= watchRadius))
        {
            return;
        }
        HideTree = null;
        if (EscapePlanner.Landing(new GameLocationQuery(environment), tree, Threat.FleeRadius, Rng, OccupiedTiles(environment)) is { } ground)
        {
            StartRelocate(ground, environment, counted: false);
        }
        else
        {
            StartFlee(Rng.NextDouble() < 0.5, environment);
        }
    }

    private static bool TreeIntact(GameLocation environment, Vector2 tile) =>
        environment.terrainFeatures.TryGetValue(tile, out TerrainFeature? feature)
        && feature is Tree { stump.Value: false, falling.Value: false, maxShake: 0f };

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
