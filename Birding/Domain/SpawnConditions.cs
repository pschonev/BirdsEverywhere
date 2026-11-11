using System.Collections.Immutable;

namespace Birding.Domain;

/// <summary>Clauses of which any one may hold. No clauses, or an unconditional clause, means unconditional; overlapping clauses count once.</summary>
public sealed record SpawnConditions(ImmutableArray<ConditionClause> Any)
{
    public static readonly SpawnConditions Always = new(ImmutableArray<ConditionClause>.Empty);

    public bool IsUnconditional => Any.IsEmpty || Any.Any(clause => clause.IsUnconditional);

    public bool MatchesDay(Season season, Weather weather, int day, bool bridgeFixed = false) =>
        Any.IsEmpty || Any.Any(clause => clause.MatchesDay(season, weather, day, bridgeFixed));

    public bool Matches(Season season, Weather weather, int day, int time, TimeAnchors anchors, bool bridgeFixed = false) =>
        Any.IsEmpty || Any.Any(clause => clause.MatchesDay(season, weather, day, bridgeFixed) && clause.MatchesTime(time, anchors));

    /// <summary>
    /// Windows in which the rule is active on this day: empty means all day (a matching clause has no times), otherwise the matching clauses'
    /// windows resolved at the anchors. A window the anchors leave empty is kept (<c>Min &gt; Max</c>) and never contains a time.
    /// </summary>
    public IReadOnlyList<IntRange> TimesOn(Season season, Weather weather, int day, TimeAnchors anchors, bool bridgeFixed = false)
    {
        List<IntRange> windows = new();
        foreach (ConditionClause clause in Any.Where(clause => clause.MatchesDay(season, weather, day, bridgeFixed)))
        {
            if (clause.Times.IsEmpty)
            {
                return Array.Empty<IntRange>();
            }
            windows.AddRange(clause.Times.Select(range => range.Resolve(anchors)));
        }
        return windows;
    }
}
