using BirdsEverywhere.Domain;

namespace BirdsEverywhere.BirdTypes.Behaviors;

internal static class BirdBehaviors
{
    /// <summary>Flee, stop walking (only with a walk end chance), wake after sleeping, then the idle routine.</summary>
    public static IReadOnlyList<IBirdBehavior> Land(Routine routine, double wakeChance, double? walkStopChance)
    {
        List<IBirdBehavior> behaviors = new() { new FleeBehavior() };
        if (walkStopChance is { } stop)
        {
            behaviors.Add(new StopWalkingBehavior(stop));
        }
        behaviors.Add(new WakeBehavior(wakeChance));
        behaviors.Add(new IdleRoutineBehavior(routine));
        return behaviors;
    }

    /// <summary>Flee, stop walking (only with a walk end chance), then the idle routine.</summary>
    public static IReadOnlyList<IBirdBehavior> WaterLand(Routine routine, double? walkStopChance)
    {
        List<IBirdBehavior> behaviors = new() { new FleeBehavior() };
        if (walkStopChance is { } stop)
        {
            behaviors.Add(new StopWalkingBehavior(stop));
        }
        behaviors.Add(new IdleRoutineBehavior(routine));
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
