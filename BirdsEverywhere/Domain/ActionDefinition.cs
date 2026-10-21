namespace BirdsEverywhere.Domain;

/// <summary>Binds a code primitive (<c>Do</c>) to a clip and parameters. Unused fields are null.</summary>
public sealed record ActionDefinition(
    string Do,
    string? Clip,
    string? Sound,
    double SoundChance,
    StepRange? DurationMs,
    double? EndChance,
    float? Speed,
    float? Climb,
    float? Impulse,
    bool Turn,
    StepRange? PauseMs);
