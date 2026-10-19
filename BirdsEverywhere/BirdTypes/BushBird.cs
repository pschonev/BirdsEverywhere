using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.BirdTypes;

class BushBird : CustomBirdTypeTerrainFeature
{
    public override string birdTypeName { get; } = "BushBird";
    private Bush bush;
    public int index { get; set; }
    public string locationName { get; set; }
    public int flightDistance { get; set; }

    public BushBird() { }
    public BushBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services)
    {
        position.Y += 48f;
        sprite.loop = true;
    }

    public override BushBird setTerrainFeature(int index, string locationName)
    {
        this.index = index;
        this.locationName = locationName;
        GameLocation location = Game1.getLocationFromName(locationName);

        bush = location.largeTerrainFeatures[index] as Bush;
        flip = bush.Tile.X < (position.X / 64);
        startingPosition = position;
        flightDistance = (int)Math.Abs(bush.Tile.X - (position.X / 64f)) - 1;
        return this;
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        characterCheckTimer -= time.ElapsedGameTime.Milliseconds;

        // check if it's time to run away from the farmer
        if (characterCheckTimer <= 0)
        {
            Character f = Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, flightDistance, environment);

            if (f != null && state != BehaviorStatus.Running)
            {
                if (Utility.isOnScreen(position, -32))
                {
                    state = BehaviorStatus.Running;
                    addBirdObservation(this.birdName, environment.Name);

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
                characterCheckTimer = 200;
            }
        }
        // runs away a certain distance
        if (state == BehaviorStatus.Running)
        {
            position.X += (flip ? (-6) : 6);
        }
        // check if it disappears into the bush
        if (state == BehaviorStatus.Running && characterCheckTimer <= 0)
        {
            characterCheckTimer = 200;

            Rectangle tileRect = new Rectangle((int)position.X + 32, (int)position.Y - 32, 4, 192);
            if (bush.getBoundingBox().Intersects(tileRect))
            {
                bush.performUseAction(bush.Tile);
                return true;
            }
        }
        return base.update(time, environment);
    }
}
