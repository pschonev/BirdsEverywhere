using Birding.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Birding.BirdTypes;

internal sealed class BushBird : BirdCritter
{
    private Bush? _bush;

    public override string BirdTypeName { get; } = "BushBird";
    public Vector2 AnchorTile { get; set; }
    public string? LocationName { get; set; }

    public BushBird() { }

    internal override BirdState CaptureExtras(BirdState state) =>
        state with { AnchorTile = AnchorTile, LocationName = LocationName };

    internal override void RestoreExtras(BirdState state)
    {
        SetAnchor(state.AnchorTile ?? Vector2.Zero, state.LocationName);
    }

    public BushBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        position.Y += 48f;
        sprite.loop = true;
        State = BehaviorStatus.Stopped;
    }

    public BushBird SetAnchor(Vector2 tile, string? locationName)
    {
        AnchorTile = tile;
        LocationName = locationName;
        _bush = new TerrainAnchor(locationName, tile).ResolveBush();
        if (_bush == null)
        {
            return this;
        }
        flip = _bush.Tile.X > position.X / 64f;
        startingPosition = position;
        return this;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        CurrentLocation = environment;
        float runSpeed = Action("flee").Speed!.Value;
        if (IsReplica)
        {
            AdvanceReplicaIdle();
            if (State == BehaviorStatus.Alert)
            {
                ShowAlertPose();
            }
            return UpdateReplica(time, State == BehaviorStatus.Running ? new Vector2(flip ? runSpeed : -runSpeed, 0f) : default);
        }

        if (_bush == null)
        {
            return true;
        }
        if (State == BehaviorStatus.Running)
        {
            position.X += flip ? runSpeed : -runSpeed;
            if (CheckIntervalElapsed(time) && ReachedBush())
            {
                _bush.performUseAction(_bush.Tile);
                Services?.Departed?.Invoke(environment.Name, BirdID);
                return true;
            }
            return UpdateAndReportDeparture(time, environment);
        }

        (Character? threat, bool alerted) = PollThreats(time, environment);
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng, alerted, TickCalm(time, alerted));
        AdvanceIdle(IdleRoutine, State == BehaviorStatus.Stopped && threat == null && !alerted && context.AnimationIdle);
        BirdAction action = BirdBehaviors.Decide(BirdBehaviors.Bush, context);
        if (action == BirdAction.Flee && Utility.isOnScreen(position, -32))
        {
            StartRun(environment);
        }
        else if (action == BirdAction.Alert)
        {
            StartAlert(environment);
        }
        else if (action == BirdAction.Stop)
        {
            State = BehaviorStatus.Stopped;
        }
        else if (State == BehaviorStatus.Alert)
        {
            ShowAlertPose();
        }
        else if (State == BehaviorStatus.Stopped && sprite.CurrentAnimation == null)
        {
            ShowPose("stand");
        }
        return UpdateAndReportDeparture(time, environment);
    }

    private bool ReachedBush() =>
        _bush!.getBoundingBox().Intersects(new Rectangle((int)position.X + 32, (int)position.Y - 32, 4, 192));

    private void StartRun(GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        State = BehaviorStatus.Running;
        AddBirdObservation(BirdName, environment.Name);
        StartRunAnimation();
    }

    internal void StartRunAnimation() => PlayClip(Action("flee").Clip!, Rng);
}
