namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>Freezes a grounded bird when a threat is inside the alert ring; swimming, diving and airborne birds ignore it.</summary>
internal sealed class AlertBehavior : IBirdBehavior
{
    public BirdAction Decide(in BirdContext context) =>
        context.ThreatInAlertRange && !context.Airborne
            && context.State is not (BehaviorStatus.FlyingAway or BehaviorStatus.Running or BehaviorStatus.Alert or BehaviorStatus.Swimming or BehaviorStatus.Diving)
            ? BirdAction.Alert
            : BirdAction.None;
}
