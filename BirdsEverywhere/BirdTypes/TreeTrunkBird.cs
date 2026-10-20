using BirdsEverywhere.BirdTypes.Behaviors;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

internal sealed class TreeTrunkBird : BirdCritter
{
    private const int ThreatRadiusTiles = 6;
    private const float FlyingSpeed = 6f;

    private Tree? _tree;

    public override string BirdTypeName { get; } = "TreeTrunkBird";
    [JsonProperty("index")]
    public int Index { get; set; }
    [JsonProperty("locationName")]
    public string? LocationName { get; set; }
    [JsonProperty("peckTimer")]
    public int PeckTimer { get; set; }

    public TreeTrunkBird() { }

    public TreeTrunkBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services, 16, 16)
    {
        position.X += 32f;
        startingPosition = position;
    }

    public TreeTrunkBird SetAnchor(int index, string? locationName)
    {
        Index = index;
        LocationName = locationName;
        _tree = new TerrainAnchor(locationName, index).ResolveTree();
        return this;
    }

    public override void drawAboveFrontLayer(SpriteBatch b)
    {
        sprite.draw(b, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-80f, -64f + yJumpOffset + yOffset)), 1f, 0, 0, Color.White, flip, 4f);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(0f, -4f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + Math.Max(-3f, (yJumpOffset + yOffset) / 16f), SpriteEffects.None, (position.Y - 1f) / 10000f);
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        if (IsReplica)
        {
            return UpdateReplica(time, State == BehaviorStatus.FlyingAway ? new Vector2(flip ? FlyingSpeed : -FlyingSpeed, 0f) : default,
                State == BehaviorStatus.FlyingAway ? -1f : 0f);
        }

        if (environment == null || sprite == null || _tree == null)
        {
            return true;
        }
        if (yJumpOffset < 0f && State != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }
        PeckTimer -= time.ElapsedGameTime.Milliseconds;
        if (State != BehaviorStatus.FlyingAway && PeckTimer <= 0 && sprite.CurrentAnimation == null)
        {
            StartPeck();
        }

        bool pollNow = CheckIntervalElapsed(time);
        Character? threat = pollNow ? FindThreat(environment, ThreatRadiusTiles) : null;
        bool treeFelled = pollNow && _tree.stump.Value;
        BirdContext context = new(State, threat != null || treeFelled, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        if (BirdBehaviors.Decide(BirdBehaviors.FleeOnly, context) == BirdAction.Flee)
        {
            StartFlee(threat, environment);
        }

        if (State == BehaviorStatus.FlyingAway)
        {
            position.X += flip ? FlyingSpeed : -FlyingSpeed;
            yOffset -= 1f;
        }
        return base.update(time, environment);
    }

    private void StartPeck()
    {
        int nibbles = Rng.Next(2, 8);
        List<FarmerSprite.AnimationFrame> anim = new();
        for (int i = 0; i < nibbles; i++)
        {
            anim.Add(new FarmerSprite.AnimationFrame(baseFrame, 100));
            anim.Add(new FarmerSprite.AnimationFrame(baseFrame + 1, 100, secondaryArm: false, flip: false, PlayPeck));
        }
        anim.Add(new FarmerSprite.AnimationFrame(baseFrame, 200, secondaryArm: false, flip: false, DonePecking));
        sprite.setCurrentAnimation(anim);
        sprite.loop = false;
    }

    private void StartFlee(Character? threat, GameLocation environment)
    {
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = threat == null || threat.Position.X <= position.X;
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 70),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 60, secondaryArm: false, flip, PlayFlap),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 70),
            new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 60)
        });
        sprite.loop = true;
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

    private bool IsBlocked(GameLocation environment, int dx) =>
        environment.isCollidingPosition(getBoundingBox(dx, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true);

    private void DonePecking(Farmer who)
    {
        PeckTimer = Rng.Next(1000, 3000);
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
            Game1.playSound("Cowboy_gunshot");
        }
    }
}
