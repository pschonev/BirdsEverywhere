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
            return UpdateReplica();
        }

        if (yJumpOffset < 0f && State != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }

        Character? threat = PollThreat(time, environment, ThreatRadiusTiles);
        BirdContext context = new(State, threat != null, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        Apply(BirdBehaviors.Decide(BirdBehaviors.Land, context), threat, environment);

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

    private void StartFlee(Character threat, GameLocation environment)
    {
        if (Rng.NextDouble() < 0.85)
        {
            Game1.playSound("SpringBirds");
        }
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
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
        State = BehaviorStatus.Walking;
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame((short)baseFrame, 100),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 1), 100)
        });
        sprite.loop = true;
        flip = position.X < startingPosition.X;
        WalkTimer = Rng.Next(5, 15) * 100;
    }

    private void StartPeckAnimation()
    {
        List<FarmerSprite.AnimationFrame> peckAnim = new()
        {
            new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 480),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 170, secondaryArm: false, flip),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 170, secondaryArm: false, flip),
        };
        int pecks = Rng.Next(1, 5);
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
        WalkTimer -= time.ElapsedGameTime.Milliseconds;
        if (WalkTimer < 0)
        {
            State = BehaviorStatus.Stopped;
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
        State = Rng.NextDouble() < 0.5 ? BehaviorStatus.Pecking : BehaviorStatus.Stopped;
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
