namespace BirdsEverywhere.BirdTypes;

// this should really be an interface but covariant return for interfaces is not implemented in C# 9
abstract class CustomBirdTypeTerrainFeature : BirdCritter
{
    public CustomBirdTypeTerrainFeature() { }

    protected CustomBirdTypeTerrainFeature(int baseFrame, int tileX, int tileY, string birdName, string texture, BirdServices services, int spriteWidth = 32, int spriteHeight = 32) :
        base(baseFrame, tileX, tileY, birdName, texture, services, spriteWidth, spriteHeight)
    { }

    public abstract BirdCritter setTerrainFeature(int index, string locationName);
}
