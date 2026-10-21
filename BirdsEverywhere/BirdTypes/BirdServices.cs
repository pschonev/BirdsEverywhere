using BirdsEverywhere.Domain;
using StardewModdingAPI;

namespace BirdsEverywhere.BirdTypes;

/// <summary><c>ModCuePrefix</c> turns an <c>audio:&lt;name&gt;</c> sound into the cue id registered for <c>assets/audio/&lt;name&gt;</c>; <c>SoundVolume</c> scales every bird sound (0 mutes); <c>World</c> feeds routine <c>rare</c> conditions.</summary>
public sealed record BirdServices(IMonitor Monitor, IModContentHelper Content, Func<long> NewId, Action<string, string> Observe,
    IReadOnlyDictionary<string, SpriteLayout> Layouts, IReadOnlyDictionary<string, Routine> Routines,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, ActionDefinition>> Actions, string ModCuePrefix = "", float SoundVolume = 1f,
    Func<IdleWorld?>? World = null);
