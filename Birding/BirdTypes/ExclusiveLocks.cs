namespace Birding.BirdTypes;

/// <summary>Runtime-only named locks, one holder at a time; a lock stays blocked until its holder releases it plus the cooldown, or <see cref="MaxHoldMs"/> if the holder vanishes.</summary>
public sealed class ExclusiveLocks
{
    public const double MaxHoldMs = 30000;

    private readonly Dictionary<string, (long Holder, double BlockedUntil)> _locks = new(StringComparer.Ordinal);

    /// <summary>True while the lock is held or cooling down.</summary>
    public bool IsBlocked(string key, double nowMs) => _locks.TryGetValue(key, out var entry) && nowMs < entry.BlockedUntil;

    /// <summary>Takes the lock unless it is blocked.</summary>
    public bool TryAcquire(string key, long holder, double nowMs)
    {
        if (IsBlocked(key, nowMs))
        {
            return false;
        }
        _locks[key] = (holder, nowMs + MaxHoldMs);
        return true;
    }

    /// <summary>Frees the lock held by <paramref name="holder"/> and blocks it for the cooldown; other holders are ignored.</summary>
    public void Release(string key, long holder, double nowMs, int cooldownMs)
    {
        if (_locks.TryGetValue(key, out var entry) && entry.Holder == holder)
        {
            _locks[key] = (0, nowMs + cooldownMs);
        }
    }
}
