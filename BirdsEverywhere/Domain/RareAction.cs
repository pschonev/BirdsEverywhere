namespace BirdsEverywhere.Domain;

/// <summary>A routine action tried before the weighted pick: <c>Chance</c> per scheduled cue while <c>When</c> matches.</summary>
public sealed record RareAction(string Id, double Chance, SpawnConditions When)
{
    /// <summary>Unconditional entries always match; conditional ones need the current world (none in tests or before a save loads).</summary>
    public bool Matches(IdleWorld? world) =>
        (When.Seasons.IsEmpty && When.Weathers.IsEmpty && When.Days.IsEmpty && When.Times.IsEmpty)
        || (world is { } now && When.MatchesDay(now.Season, now.Weather, now.Day) && When.MatchesTime(now.Time));
}

/// <summary>Game state a routine's rare conditions read.</summary>
public sealed record IdleWorld(Season Season, Weather Weather, int Day, int Time);
