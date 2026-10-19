namespace BirdsEverywhere.BirdTypes.Behaviors;

internal sealed class StopWalkingBehavior : IBirdBehavior
{
    private readonly double _chance;

    public StopWalkingBehavior(double chance) => _chance = chance;

    public BirdAction Decide(in BirdContext context) =>
        context.State == BehaviorStatus.Walking && context.Random.NextDouble() < _chance ? BirdAction.Stop : BirdAction.None;
}
