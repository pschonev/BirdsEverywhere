namespace BirdsEverywhere.BirdTypes.Behaviors;

internal sealed class FleeBehavior : IBirdBehavior
{
    public BirdAction Decide(in BirdContext context) =>
        context.ThreatNearby && context.State is not (BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Relocating) ? BirdAction.Flee : BirdAction.None;
}
