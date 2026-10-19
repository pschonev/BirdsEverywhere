namespace BirdsEverywhere.BirdTypes.Behaviors;

internal sealed class FleeBehavior : IBirdBehavior
{
    public BirdAction Decide(in BirdContext context) =>
        context.ThreatNearby && context.State != BehaviorStatus.FlyingAway ? BirdAction.Flee : BirdAction.None;
}
