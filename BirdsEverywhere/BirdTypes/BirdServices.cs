using BirdsEverywhere.Domain;
using StardewModdingAPI;

namespace BirdsEverywhere.BirdTypes;

public sealed record BirdServices(IMonitor Monitor, IModContentHelper Content, Func<long> NewId, Action<string, string> Observe,
    IReadOnlyDictionary<string, SpriteLayout> Layouts, IReadOnlyDictionary<string, Routine> Routines);
