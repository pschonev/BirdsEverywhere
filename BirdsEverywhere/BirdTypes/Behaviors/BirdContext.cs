namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>Game-free snapshot of a bird for one tick. <c>ThreatInAlertRange</c>: a threat between the flee and alert radius at this poll; <c>Calm</c>: no such threat for the alert's calm time.</summary>
internal readonly record struct BirdContext(BehaviorStatus State, bool ThreatNearby, bool AnimationIdle, bool Airborne, Random Random,
    bool ThreatInAlertRange = false, bool Calm = false);
