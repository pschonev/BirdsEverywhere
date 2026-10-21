using BirdsEverywhere.Domain;

namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>While stopped on the ground, occasionally picks the next action from the routine.</summary>
internal sealed class IdleRoutineBehavior : IBirdBehavior
{
    private readonly Routine _routine;

    public IdleRoutineBehavior(Routine routine) => _routine = routine;

    public BirdAction Decide(in BirdContext context)
    {
        if (context.State != BehaviorStatus.Stopped)
        {
            return BirdAction.None;
        }
        // Roll before the other checks to keep the legacy random sequence.
        bool roll = context.Random.NextDouble() < _routine.Chance;
        return roll && context.AnimationIdle && !context.Airborne ? BirdActionIds.Parse(Routine.Roll(_routine.Pick, context.Random)) : BirdAction.None;
    }
}
