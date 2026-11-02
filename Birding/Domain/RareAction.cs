namespace Birding.Domain;

/// <summary>A routine action tried before the weighted pick: <c>Chance</c> per scheduled cue while <c>When</c> matches.</summary>
public sealed record RareAction(string Id, double Chance, SpawnConditions When)
{
    /// <summary>Unconditional entries always match; conditional ones need the current world (none in tests or before a save loads).</summary>
    public bool Matches(IdleWorld? world) =>
        When.IsUnconditional
        || (world is { } now && When.Matches(now.Season, now.Weather, now.Day, now.Time, now.Anchors ?? TimeAnchors.Default));
}

/// <summary>Game state a routine's rare conditions read; <c>Anchors</c> are the current location's light changes.</summary>
public sealed record IdleWorld(Season Season, Weather Weather, int Day, int Time, TimeAnchors? Anchors = null);
