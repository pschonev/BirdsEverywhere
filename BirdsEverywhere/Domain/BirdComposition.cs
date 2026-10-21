namespace BirdsEverywhere.Domain;

/// <summary>Resolved per-rule composition carried by placements and live birds. Sound cue <c>""</c> means silent; <c>SoundCooldownMs</c> is the minimum gap per role.</summary>
public sealed record BirdComposition(Routine? Routine, IReadOnlyDictionary<string, string> Sounds, IReadOnlyDictionary<string, ActionDefinition>? Actions = null,
    IReadOnlyDictionary<string, int>? SoundCooldownMs = null, Reactions? Reactions = null);
