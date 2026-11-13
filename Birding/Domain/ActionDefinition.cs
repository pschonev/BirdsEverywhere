namespace Birding.Domain;

/// <summary>Binds a code primitive (<c>Do</c>) to a clip and parameters. Unused fields are null. <c>HiddenMs</c>/<c>Radius</c>: how long a diver stays under and how far (tiles) it may resurface. <c>Exclusive</c>: lock name only one bird per location holds while the action runs, then blocked for <c>ExclusiveCooldownMs</c>. <c>SoundRepeat</c>/<c>SoundIntervalMs</c>: a <c>stay</c> action replays its sound that many times, one per interval, holding the clip's first cell. <c>SoundDuring</c>: a role rolled repeatedly while the action runs.</summary>
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
    float? SpeedFactor = null,
    string? Exclusive = null,
    int ExclusiveCooldownMs = 0,
    StepRange? SoundRepeat = null,
    int SoundIntervalMs = 0,
    SoundDuring? SoundDuring = null);
