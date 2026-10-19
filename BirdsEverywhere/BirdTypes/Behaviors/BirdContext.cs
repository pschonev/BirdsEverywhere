namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>Game-free snapshot of a bird for one tick.</summary>
internal readonly record struct BirdContext(BehaviorStatus State, bool ThreatNearby, bool AnimationIdle, bool Airborne, Random Random);
