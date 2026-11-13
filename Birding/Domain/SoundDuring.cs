namespace Birding.Domain;

/// <summary>While an action runs, roll <c>Chance</c> every <c>EveryMs</c> to play the <c>Role</c> sound (flight calls, contact calls).</summary>
public sealed record SoundDuring(string Role, double Chance, int EveryMs);
