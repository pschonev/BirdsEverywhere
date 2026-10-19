namespace BirdsEverywhere.BirdTypes.Behaviors;

internal static class BirdBehaviors
{
    public static readonly IReadOnlyList<IBirdBehavior> Land = new IBirdBehavior[]
    {
        new FleeBehavior(),
        new WakeBehavior(chance: 0.003),
        new IdleRoutineBehavior(chance: 0.008, new[] { BirdAction.Sleep, BirdAction.Peck, BirdAction.Hop, BirdAction.TurnAndHop, BirdAction.Walk, BirdAction.Walk }),
    };

    public static readonly IReadOnlyList<IBirdBehavior> WaterLand = new IBirdBehavior[]
    {
        new FleeBehavior(),
        new StopWalkingBehavior(chance: 0.005),
        new IdleRoutineBehavior(chance: 0.003, new[] { BirdAction.Peck, BirdAction.Rest, BirdAction.Hop, BirdAction.Walk }),
    };

    public static readonly IReadOnlyList<IBirdBehavior> FleeOnly = new IBirdBehavior[]
    {
        new FleeBehavior(),
    };

    /// <summary>The first behavior with an opinion wins; list order is priority.</summary>
    public static BirdAction Decide(IReadOnlyList<IBirdBehavior> behaviors, in BirdContext context)
    {
        for (int i = 0; i < behaviors.Count; i++)
        {
            BirdAction action = behaviors[i].Decide(context);
            if (action != BirdAction.None)
            {
                return action;
            }
        }
        return BirdAction.None;
    }
}
