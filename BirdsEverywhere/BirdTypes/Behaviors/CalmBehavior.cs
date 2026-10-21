namespace BirdsEverywhere.BirdTypes.Behaviors;

internal sealed class CalmBehavior : IBirdBehavior
{
    public BirdAction Decide(in BirdContext context) =>
        context.State == BehaviorStatus.Alert && context.Calm ? BirdAction.Stop : BirdAction.None;
}
