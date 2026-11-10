using System.Collections.Immutable;
using Birding.Domain;
using Newtonsoft.Json.Linq;

namespace Birding.Content;

/// <summary>Parses and writes a <c>when</c> object (rule or rare action): one clause, or <c>any</c> with several. Reports every problem, naming the field.</summary>
public static class ConditionParser
{
    private static readonly string[] _clauseKeys = { "seasons", "weather", "days", "times" };

    /// <summary>Parses <paramref name="token"/>; <paramref name="error"/> receives (path, message) per problem and the result is then unreliable.</summary>
    public static SpawnConditions Parse(JToken? token, string path, Action<string, string> error)
    {
        if (token is null or { Type: JTokenType.Null })
        {
            return SpawnConditions.Always;
        }
        if (token is not JObject body)
        {
            error(path, "must be an object.");
            return SpawnConditions.Always;
        }

        if (body.Property("any") is { } any)
        {
            foreach (JProperty other in body.Properties().Where(property => property.Name != "any" && property.Value.Type != JTokenType.Null))
            {
                error($"{path}.{other.Name}", $"cannot be combined with 'any'. If it is inherited from defaults or a template, set it to null there and move it into the clauses.");
            }
            if (any.Value is not JArray clauses)
            {
                error($"{path}.any", "must be a list of condition clauses.");
                return SpawnConditions.Always;
            }

            ImmutableArray<ConditionClause>.Builder parsed = ImmutableArray.CreateBuilder<ConditionClause>();
            for (int index = 0; index < clauses.Count; index++)
            {
                parsed.Add(Clause(clauses[index], $"{path}.any[{index}]", error));
            }
            return Build(parsed.ToImmutable());
        }

        return Build(ImmutableArray.Create(Clause(body, path, error)));
    }

    /// <summary>The flat form for zero or one clause, otherwise <c>any</c>; times keep their anchor text.</summary>
    public static JObject ToJson(SpawnConditions conditions) => conditions.Any.Length switch
    {
        0 => new JObject(),
        1 => ClauseJson(conditions.Any[0]),
        _ => new JObject { ["any"] = new JArray(conditions.Any.Select(ClauseJson)) },
    };

    /// <summary>Readable text such as <c>seasons spring | times sunrise–sunrise+120</c>, clauses joined by <c>OR</c>.</summary>
    public static string Describe(SpawnConditions conditions) => conditions.IsUnconditional ? "always"
        : string.Join("  OR  ", conditions.Any.Select(clause => string.Join(" | ", new[]
        {
            clause.Seasons.IsEmpty ? null : "seasons " + string.Join(", ", clause.Seasons),
            clause.Weathers.IsEmpty ? null : "weather " + string.Join(", ", clause.Weathers),
            clause.Days.IsEmpty ? null : "days " + string.Join(", ", clause.Days.Select(range => $"{range.Min}–{range.Max}")),
            clause.Times.IsEmpty ? null : "times " + string.Join(", ", clause.Times),
        }.Where(part => part is not null))));

    private static SpawnConditions Build(ImmutableArray<ConditionClause> clauses) =>
        clauses.Any(clause => clause.IsUnconditional) ? SpawnConditions.Always : new SpawnConditions(clauses);

    private static ConditionClause Clause(JToken? token, string path, Action<string, string> error)
    {
        if (token is not JObject body)
        {
            error(path, "must be an object.");
            return ConditionClause.Unconditional;
        }
        foreach (JProperty property in body.Properties().Where(property => !_clauseKeys.Contains(property.Name)))
        {
            error($"{path}.{property.Name}", $"is not a known condition. Known: {string.Join(", ", _clauseKeys)}.");
        }
        return new ConditionClause(
            Names<Season>(body["seasons"], path + ".seasons", error),
            Weathers(body["weather"], path + ".weather", error),
            Ranges(body["days"], 1, 28, path + ".days", error),
            Times(body["times"], path + ".times", error));
    }

    private static ImmutableArray<Weather> Weathers(JToken? token, string path, Action<string, string> error)
    {
        if (token is null or { Type: JTokenType.Null })
        {
            return ImmutableArray<Weather>.Empty;
        }
        if (token is not JArray names)
        {
            error(path, $"must be a list of {string.Join(", ", WeatherNames.Known)}.");
            return ImmutableArray<Weather>.Empty;
        }
        HashSet<Weather> result = new();
        foreach (JToken name in names)
        {
            if (name.Type == JTokenType.String && WeatherNames.TryExpand(name.Value<string>()!, out IReadOnlyList<Weather> expanded))
            {
                result.UnionWith(expanded);
            }
            else
            {
                error(path, $"unknown weather '{name}'. Use {string.Join(", ", WeatherNames.Known)}.");
            }
        }
        return result.Count == Enum.GetValues<Weather>().Length ? ImmutableArray<Weather>.Empty : result.OrderBy(weather => weather).ToImmutableArray();
    }

    private static ImmutableArray<T> Names<T>(JToken? token, string path, Action<string, string> error) where T : struct, Enum
    {
        if (token is null or { Type: JTokenType.Null })
        {
            return ImmutableArray<T>.Empty;
        }
        if (token is not JArray names)
        {
            error(path, $"must be a list of {string.Join(", ", Enum.GetNames<T>())}.");
            return ImmutableArray<T>.Empty;
        }
        ImmutableArray<T>.Builder result = ImmutableArray.CreateBuilder<T>();
        foreach (JToken name in names)
        {
            if (name.Type == JTokenType.String && Enum.TryParse(name.Value<string>(), true, out T parsed) && Enum.IsDefined(parsed))
            {
                result.Add(parsed);
            }
            else
            {
                error(path, $"unknown {typeof(T).Name.ToLowerInvariant()} '{name}'. Use {string.Join(", ", Enum.GetNames<T>())}.");
            }
        }
        return result.ToImmutable();
    }

    private static ImmutableArray<IntRange> Ranges(JToken? token, int min, int max, string path, Action<string, string> error)
    {
        if (token is null or { Type: JTokenType.Null })
        {
            return ImmutableArray<IntRange>.Empty;
        }
        if (token is not JArray ranges)
        {
            error(path, $"must be a list of [min, max] within {min}–{max}.");
            return ImmutableArray<IntRange>.Empty;
        }
        ImmutableArray<IntRange>.Builder result = ImmutableArray.CreateBuilder<IntRange>();
        for (int index = 0; index < ranges.Count; index++)
        {
            if (ranges[index] is JArray { Count: 2 } pair && pair.All(value => value.Type == JTokenType.Integer)
                && pair[0].Value<int>() >= min && pair[0].Value<int>() <= pair[1].Value<int>() && pair[1].Value<int>() <= max)
            {
                result.Add(new IntRange(pair[0].Value<int>(), pair[1].Value<int>()));
            }
            else
            {
                error($"{path}[{index}]", $"expected [min, max] with {min} <= min <= max <= {max}.");
            }
        }
        return result.ToImmutable();
    }

    private static ImmutableArray<TimeRange> Times(JToken? token, string path, Action<string, string> error)
    {
        if (token is null or { Type: JTokenType.Null })
        {
            return ImmutableArray<TimeRange>.Empty;
        }
        if (token is not JArray ranges)
        {
            error(path, $"must be a list of [from, to] times: HHMM within {TimeBound.MinTime}–{TimeBound.MaxTime}, or sunrise/dusk/dark with an optional +/-minutes.");
            return ImmutableArray<TimeRange>.Empty;
        }
        ImmutableArray<TimeRange>.Builder result = ImmutableArray.CreateBuilder<TimeRange>();
        for (int index = 0; index < ranges.Count; index++)
        {
            string rangePath = $"{path}[{index}]";
            if (ranges[index] is not JArray { Count: 2 } pair)
            {
                error(rangePath, "expected [from, to].");
                continue;
            }
            if (Bound(pair[0], rangePath + "[0]", error) is not { } from || Bound(pair[1], rangePath + "[1]", error) is not { } to)
            {
                continue;
            }
            bool ordered = from.Kind != to.Kind || from.Value <= to.Value;
            if (!ordered)
            {
                error(rangePath, "from must not be after to.");
                continue;
            }
            result.Add(new TimeRange(from, to));
        }
        return result.ToImmutable();
    }

    private static TimeBound? Bound(JToken token, string path, Action<string, string> error)
    {
        if (token.Type == JTokenType.Integer && token.Value<int>() is >= TimeBound.MinTime and <= TimeBound.MaxTime)
        {
            return TimeBound.At(token.Value<int>());
        }
        if (token.Type == JTokenType.String && TimeBound.TryParse(token.Value<string>()!, out TimeBound bound))
        {
            return bound;
        }
        error(path, $"'{token}' is not a time: use HHMM within {TimeBound.MinTime}–{TimeBound.MaxTime}, or sunrise/dusk/dark with an optional +/-minutes (for example \"sunrise+120\").");
        return null;
    }

    private static JObject ClauseJson(ConditionClause clause)
    {
        JObject json = new();
        if (!clause.Seasons.IsEmpty)
        {
            json["seasons"] = new JArray(clause.Seasons.Select(season => season.ToString()));
        }
        if (!clause.Weathers.IsEmpty)
        {
            json["weather"] = new JArray(clause.Weathers.Select(weather => weather.ToString()));
        }
        if (!clause.Days.IsEmpty)
        {
            json["days"] = new JArray(clause.Days.Select(range => new JArray(range.Min, range.Max)));
        }
        if (!clause.Times.IsEmpty)
        {
            json["times"] = new JArray(clause.Times.Select(range => new JArray(BoundJson(range.From), BoundJson(range.To))));
        }
        return json;
    }

    private static JValue BoundJson(TimeBound bound) => bound.Kind == AnchorKind.Fixed ? new JValue(bound.Value) : new JValue(bound.ToString());
}
