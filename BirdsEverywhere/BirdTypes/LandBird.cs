using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal sealed class LandBird : BirdCritter
{
    private const int ThreatRadiusTiles = 4;

    public override string birdTypeName { get; } = "LandBird";
    public float flightOffset { get; set; }
    public int walkTimer { get; set; }

    public LandBird() { }

    public LandBird(int tileX, int tileY, string birdName, string texture, BirdServices services, BehaviorStatus state = BehaviorStatus.Pecking)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        flip = random.NextDouble() < 0.5;
        position.X += 32f;
        position.Y += 32f;
        startingPosition = position;
        flightOffset = (float)random.NextDouble() - 0.5f;

        this.state = state;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        if (yJumpOffset < 0f && state != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }

        Character? threat = PollThreat(time, environment, ThreatRadiusTiles);
        BirdContext context = new(state, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, random);
        Apply(BirdBehaviors.Decide(BirdBehaviors.Land, context), threat, environment);

        switch (state)
        {
            case BehaviorStatus.Pecking:
                if (sprite.CurrentAnimation == null)
                {
                    StartPeckAnimation();
                }
                break;
            case BehaviorStatus.FlyingAway:
                position.X += flip ? 6f : -6f;
                yOffset -= 2f + flightOffset;
                break;
            case BehaviorStatus.Sleeping:
                if (sprite.CurrentAnimation == null)
                {
                    sprite.currentFrame = baseFrame + 5;
                }
                break;
            case BehaviorStatus.Walking:
                Walk(time, environment);
                break;
            case BehaviorStatus.Stopped:
                if (sprite.CurrentAnimation == null)
                {
                    sprite.currentFrame = baseFrame;
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
                state = action switch
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

    private void StartFlee(Character threat, GameLocation environment)
    {
        if (random.NextDouble() < 0.85)
        {
            Game1.playSound("SpringBirds");
        }
        state = BehaviorStatus.FlyingAway;
        addBirdObservation(birdName, environment.Name);
        flip = threat.Position.X <= position.X;
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame((short)(baseFrame + 6), 70),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 7), 60, secondaryArm: false, flip, PlayFlap),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 8), 70),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 7), 60)
        });
        sprite.loop = true;
    }

    private void StartWalk()
    {
        state = BehaviorStatus.Walking;
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame((short)baseFrame, 100),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 1), 100)
        });
        sprite.loop = true;
        flip = position.X < startingPosition.X;
        walkTimer = random.Next(5, 15) * 100;
    }

    private void StartPeckAnimation()
    {
        List<FarmerSprite.AnimationFrame> peckAnim = new()
        {
            new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 480),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 170, secondaryArm: false, flip),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 170, secondaryArm: false, flip),
        };
        int pecks = random.Next(1, 5);
        for (int i = 0; i < pecks; i++)
        {
            peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 70));
            peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 100, secondaryArm: false, flip, PlayPeck));
        }
        peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 100));
        peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 70, secondaryArm: false, flip));
        peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 1), 70, secondaryArm: false, flip));
        peckAnim.Add(new FarmerSprite.AnimationFrame((short)baseFrame, 500, secondaryArm: false, flip, DonePecking));
        sprite.loop = false;
        sprite.setCurrentAnimation(peckAnim);
    }

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
        walkTimer -= time.ElapsedGameTime.Milliseconds;
        if (walkTimer < 0)
        {
            state = BehaviorStatus.Stopped;
            sprite.loop = false;
            sprite.CurrentAnimation = null;
            sprite.currentFrame = baseFrame;
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
        state = random.NextDouble() < 0.5 ? BehaviorStatus.Pecking : BehaviorStatus.Stopped;
    }

    private void PlayFlap(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            Game1.playSound("batFlap");
        }
    }

    private void PlayPeck(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            Game1.playSound("shiny4");
        }
    }
}
