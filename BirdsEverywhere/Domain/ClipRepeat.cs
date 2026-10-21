namespace BirdsEverywhere.Domain;

/// <summary>Frames appended a rolled number of times after a clip's main frames.</summary>
public sealed record ClipRepeat(IReadOnlyList<ClipFrame> Frames, StepRange Count);
