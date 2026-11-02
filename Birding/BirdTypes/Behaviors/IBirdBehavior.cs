namespace Birding.BirdTypes.Behaviors;

internal interface IBirdBehavior
{
    BirdAction Decide(in BirdContext context);
}
