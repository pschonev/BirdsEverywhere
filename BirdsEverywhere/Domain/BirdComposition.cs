namespace BirdsEverywhere.Domain;

/// <summary>Resolved per-rule composition carried by placements and live birds. Sound cue <c>""</c> means silent.</summary>
public sealed record BirdComposition(Routine? Routine, IReadOnlyDictionary<string, string> Sounds, IReadOnlyDictionary<string, ActionDefinition>? Actions = null);
