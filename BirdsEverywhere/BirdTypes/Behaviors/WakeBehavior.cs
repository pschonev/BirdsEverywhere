namespace BirdsEverywhere.BirdTypes.Behaviors;

internal sealed class WakeBehavior : IBirdBehavior
{
    private readonly double _chance;

    public WakeBehavior(double chance) => _chance = chance;

    public BirdAction Decide(in BirdContext context) =>
        context.State == BehaviorStatus.Sleeping && context.Random.NextDouble() < _chance && context.AnimationIdle
            ? BirdAction.Stop
            : BirdAction.None;
}
