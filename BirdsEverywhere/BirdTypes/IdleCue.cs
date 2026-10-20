using BirdsEverywhere.BirdTypes.Behaviors;

namespace BirdsEverywhere.BirdTypes;

internal sealed record IdleCue(long Tick, BirdAction Action, int Seed);