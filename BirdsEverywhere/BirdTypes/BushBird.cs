using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

internal sealed class BushBird : BirdCritter
{
    private const float RunSpeed = 6f;

    private Bush? _bush;

    public override string BirdTypeName { get; } = "BushBird";
    [JsonProperty("index")]
    public int Index { get; set; }
    [JsonProperty("locationName")]
    public string? LocationName { get; set; }
    [JsonProperty("flightDistance")]
    public int FlightDistance { get; set; }

    public BushBird() { }

    public BushBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        position.Y += 48f;
        sprite.loop = true;
    }

    public BushBird SetAnchor(int index, string? locationName)
    {
        Index = index;
        LocationName = locationName;
        _bush = new TerrainAnchor(locationName, index).ResolveBush();
        if (_bush == null)
        {
            return this;
        }
        flip = _bush.Tile.X > position.X / 64f;
        startingPosition = position;
        FlightDistance = (int)Math.Abs(_bush.Tile.X - (position.X / 64f)) - 1;
        return this;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        if (IsReplica)
        {
            return UpdateReplica();
        }

        if (_bush == null)
        {
            return true;
        }
        if (State == BehaviorStatus.Running)
        {
            position.X += flip ? RunSpeed : -RunSpeed;
            if (CheckIntervalElapsed(time) && ReachedBush())
            {
                _bush.performUseAction(_bush.Tile);
                return true;
            }
            return base.update(time, environment);
        }

        Character? threat = PollThreat(time, environment, FlightDistance);
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        if (BirdBehaviors.Decide(BirdBehaviors.FleeOnly, context) == BirdAction.Flee && Utility.isOnScreen(position, -32))
        {
            StartRun(environment);
        }
        return base.update(time, environment);
    }

    private bool ReachedBush() =>
        _bush!.getBoundingBox().Intersects(new Rectangle((int)position.X + 32, (int)position.Y - 32, 4, 192));

    private void StartRun(GameLocation environment)
    {
        State = BehaviorStatus.Running;
        AddBirdObservation(BirdName, environment.Name);
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame(baseFrame, 40),
            new FarmerSprite.AnimationFrame(baseFrame + 1, 40),
            new FarmerSprite.AnimationFrame(baseFrame + 2, 40),
            new FarmerSprite.AnimationFrame(baseFrame + 3, 100),
            new FarmerSprite.AnimationFrame(baseFrame + 5, 70),
            new FarmerSprite.AnimationFrame(baseFrame + 5, 40)
        });
        sprite.loop = true;
    }
}
