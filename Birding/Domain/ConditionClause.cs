using System.Collections.Immutable;

namespace Birding.Domain;

/// <summary>Conditions that must all hold; an empty list means any.</summary>
public sealed record ConditionClause(
    ImmutableArray<Season> Seasons,
    ImmutableArray<Weather> Weathers,
    ImmutableArray<IntRange> Days,
    ImmutableArray<TimeRange> Times)
{
    public static readonly ConditionClause Unconditional = new(
        ImmutableArray<Season>.Empty, ImmutableArray<Weather>.Empty, ImmutableArray<IntRange>.Empty, ImmutableArray<TimeRange>.Empty);

    public bool IsUnconditional => Seasons.IsEmpty && Weathers.IsEmpty && Days.IsEmpty && Times.IsEmpty;

    public bool MatchesDay(Season season, Weather weather, int day) =>
        (Seasons.IsEmpty || Seasons.Contains(season))
        && (Weathers.IsEmpty || Weathers.Contains(weather))
        && (Days.IsEmpty || Days.Any(range => range.Contains(day)));

    public bool MatchesTime(int time, TimeAnchors anchors) =>
        Times.IsEmpty || Times.Any(range => range.Resolve(anchors).Contains(time));
}
