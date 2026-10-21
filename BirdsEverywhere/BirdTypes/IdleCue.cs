namespace BirdsEverywhere.BirdTypes;

/// <summary>Owner-picked idle action id (built-in or custom), the tick it fires, and the visual RNG seed peers replay it with.</summary>
internal sealed record IdleCue(long Tick, string Action, int Seed);