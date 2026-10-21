namespace BirdsEverywhere.Domain;

/// <summary>One sheet cell shown for a duration, optionally firing an event; <c>Flip</c> frames take the bird's facing.</summary>
public sealed record ClipFrame(int Cell, StepRange Ms, string? Event, bool Flip);
