namespace Birding.Domain;

/// <summary>Resolved per-rule composition carried by placements and live birds. Sound cue <c>""</c> means silent; <c>SoundCooldownMs</c> is the minimum gap per role on one bird; <c>SoundVoices</c> caps a role across the location.</summary>
public sealed record BirdComposition(Routine? Routine, IReadOnlyDictionary<string, string> Sounds, IReadOnlyDictionary<string, ActionDefinition>? Actions = null,
    IReadOnlyDictionary<string, int>? SoundCooldownMs = null, Reactions? Reactions = null, string? LayoutId = null,
    IReadOnlyDictionary<string, VoiceLimit>? SoundVoices = null);
