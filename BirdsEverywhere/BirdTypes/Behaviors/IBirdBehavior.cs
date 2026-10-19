namespace BirdsEverywhere.BirdTypes.Behaviors;

internal interface IBirdBehavior
{
    BirdAction Decide(in BirdContext context);
}
