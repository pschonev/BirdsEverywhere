using Birding.Domain;

namespace Birding.BirdTypes;

/// <summary>Per-peer cap on identical cues playing at once. A key is a location plus a cue; a cue that reports itself stopped, or is older than <see cref="MaxVoiceMs"/>, stops counting.</summary>
public sealed class VoicePool
{
    public const double MaxVoiceMs = 10000;

    private readonly Dictionary<string, List<(double StartMs, Func<bool> IsPlaying)>> _voices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _lastStartMs = new(StringComparer.Ordinal);

    /// <summary>True if another copy of the cue may start now.</summary>
    public bool CanStart(string key, VoiceLimit limit, double nowMs)
    {
        if (_lastStartMs.TryGetValue(key, out double last) && nowMs - last < limit.MinGapMs)
        {
            return false;
        }
        if (!_voices.TryGetValue(key, out var playing))
        {
            return true;
        }
        playing.RemoveAll(voice => nowMs - voice.StartMs >= MaxVoiceMs || !voice.IsPlaying());
        return playing.Count < limit.Max;
    }

    /// <summary>Records a cue that has started.</summary>
    public void Started(string key, double nowMs, Func<bool> isPlaying)
    {
        _lastStartMs[key] = nowMs;
        if (!_voices.TryGetValue(key, out var playing))
        {
            _voices[key] = playing = new();
        }
        playing.Add((nowMs, isPlaying));
    }
}
