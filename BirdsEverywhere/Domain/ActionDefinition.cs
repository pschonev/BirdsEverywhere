namespace BirdsEverywhere.Domain;

/// <summary>Binds a code primitive (<c>Do</c>) to a clip and parameters. Unused fields are null. <c>HiddenMs</c>/<c>Radius</c>: how long a diver stays under and how far (tiles) it may resurface.</summary>
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
    StepRange? PauseMs,
    StepRange? HiddenMs = null,
    int? Radius = null,
    string? Target = null,
    float? SpeedFactor = null);
