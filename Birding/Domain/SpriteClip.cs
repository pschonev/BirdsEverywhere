namespace Birding.Domain;

/// <summary>Named animation: frames, then repeat rolled count times, then end.</summary>
public sealed record SpriteClip(IReadOnlyList<ClipFrame> Frames, ClipRepeat? Repeat, IReadOnlyList<ClipFrame> End, bool Loop);
