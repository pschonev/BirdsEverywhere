using Birding.Domain;

namespace Birding.BirdTypes;

/// <summary>Counts a running action's time and decides when to play its <c>soundDuring</c>: one roll per interval, restarting whenever the action changes or stops.</summary>
public sealed class SoundDuringTimer
{
    private SoundDuring? _current;
    private int _elapsedMs;

    /// <summary>Advances by <paramref name="elapsedMs"/> for the running action's sound, or null when none runs; true when the sound should play now.</summary>
    public bool Advance(SoundDuring? during, int elapsedMs, Random random)
    {
        if (during is null)
        {
            _current = null;
            return false;
        }
        if (!ReferenceEquals(during, _current))
        {
            _current = during;
            _elapsedMs = 0;
        }
        _elapsedMs += elapsedMs;
        if (_elapsedMs < during.EveryMs)
        {
            return false;
        }
        _elapsedMs -= during.EveryMs;
        return random.NextDouble() < during.Chance;
    }
}
