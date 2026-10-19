using System.Collections.Immutable;

namespace BirdsEverywhere.Domain;

public sealed record SpawnConditions(
    ImmutableArray<Season> Seasons,
    ImmutableArray<Weather> Weathers,
    ImmutableArray<IntRange> Days,
    ImmutableArray<IntRange> Times)
{
    public bool MatchesDay(Season season, Weather weather, int day) =>
        (Seasons.IsEmpty || Seasons.Contains(season))
        && (Weathers.IsEmpty || Weathers.Contains(weather))
        && (Days.IsEmpty || Days.Any(range => range.Contains(day)));

    public bool MatchesTime(int time) => Times.IsEmpty || Times.Any(range => range.Contains(time));
}