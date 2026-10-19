using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

internal sealed class BushBird : BirdCritter
{
    private const float RunSpeed = 6f;

    private Bush? _bush;

    public override string birdTypeName { get; } = "BushBird";
    public int index { get; set; }
    public string? locationName { get; set; }
    public int flightDistance { get; set; }

    public BushBird() { }

    public BushBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        position.Y += 48f;
        sprite.loop = true;
    }

    public BushBird SetAnchor(int index, string? locationName)
    {
        this.index = index;
        this.locationName = locationName;
        _bush = new TerrainAnchor(locationName, index).ResolveBush();
        if (_bush == null)
        {
            return this;
        }
        flip = _bush.Tile.X > position.X / 64f;
        startingPosition = position;
        flightDistance = (int)Math.Abs(_bush.Tile.X - (position.X / 64f)) - 1;
        return this;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        if (_bush == null)
        {
            return true;
        }
        if (state == BehaviorStatus.Running)
        {
            position.X += flip ? RunSpeed : -RunSpeed;
            if (CheckIntervalElapsed(time) && ReachedBush())
            {
                _bush.performUseAction(_bush.Tile);
                return true;
            }
            return base.update(time, environment);
        }

        Character? threat = PollThreat(time, environment, flightDistance);
        BirdContext context = new(state, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, random);
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
        state = BehaviorStatus.Running;
        addBirdObservation(birdName, environment.Name);
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
