namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>While stopped on the ground, occasionally picks a uniformly random next action.</summary>
internal sealed class IdleRoutineBehavior : IBirdBehavior
{
    private readonly double _chance;
    private readonly IReadOnlyList<BirdAction> _options;

    public IdleRoutineBehavior(double chance, IReadOnlyList<BirdAction> options)
    {
        _chance = chance;
        _options = options;
    }

    public BirdAction Decide(in BirdContext context)
    {
        if (context.State != BehaviorStatus.Stopped)
        {
            return BirdAction.None;
        }
        // Roll before the other checks to keep the legacy random sequence.
        bool roll = context.Random.NextDouble() < _chance;
        return roll && context.AnimationIdle && !context.Airborne ? _options[context.Random.Next(_options.Count)] : BirdAction.None;
    }
}
