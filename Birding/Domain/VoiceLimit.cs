namespace Birding.Domain;

/// <summary>How many copies of one cue may play at once in a location (<c>Max</c>) and the least time between two starts (<c>MinGapMs</c>).</summary>
public sealed record VoiceLimit(int Max, int MinGapMs)
{
    public static readonly VoiceLimit Default = new(3, 120);
}
