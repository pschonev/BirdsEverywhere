namespace BirdsEverywhere.BirdTypes.Behaviors;

internal static class BirdBehaviors
{
    /// <summary>Flee, alert and calm down, stop walking (only with a walk end chance), then wake after sleeping. Idle actions come from the scheduled cue, not this list.</summary>
    public static IReadOnlyList<IBirdBehavior> Land(double wakeChance, double? walkStopChance)
    {
        List<IBirdBehavior> behaviors = new() { new FleeBehavior(), new AlertBehavior(), new CalmBehavior() };
        if (walkStopChance is { } stop)
        {
            behaviors.Add(new StopWalkingBehavior(stop));
        }
        behaviors.Add(new WakeBehavior(wakeChance));
        return behaviors;
    }

    /// <summary>Flee, alert and calm down, then stop walking (only with a walk end chance).</summary>
    public static IReadOnlyList<IBirdBehavior> WaterLand(double? walkStopChance)
    {
        List<IBirdBehavior> behaviors = new() { new FleeBehavior(), new AlertBehavior(), new CalmBehavior() };
        if (walkStopChance is { } stop)
        {
            behaviors.Add(new StopWalkingBehavior(stop));
        }
        return behaviors;
    }

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
